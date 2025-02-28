using HarmonyLib;
using UnityEngine;
using System.Collections;
using Zorro.Core;
using Photon.Pun;

namespace LastDayLostCameraFix
{
    [ContentWarningPlugin("LastDayLostCameraFix", "2.0", vanillaCompatible: false)]
    public class LastDayLostCameraFix
    {
        static LastDayLostCameraFix()
        {
            LogFile.LogMessage("Мод LastDayLostCameraFix запущен");

            try
            {
                LogFile.LogMessage("Попытка применения патчей");

                var harmony = new Harmony("com.artkopt.LastDayLostCameraFix");
                harmony.PatchAll();

                LogFile.LogMessage("Патчи применены.\n");
            }
            catch (Exception ex)
            {
                LogFile.LogMessage($"Ошибка при применении патчей: {ex.Message}");
            }
        }

        [HarmonyPatch(typeof(SurfaceNetworkHandler))]
        public static class Patch_InitSurface
        {
            [HarmonyPatch(nameof(SurfaceNetworkHandler.InitSurface))]
            [HarmonyPrefix]
            static bool Prefix(SurfaceNetworkHandler __instance)
            {
                LogFile.LogMessage("Патч InitSurface вызван");

                __instance.StartCoroutine(DelayCoroutine(__instance));

                return false;
            }

            private static IEnumerator DelayCoroutine(SurfaceNetworkHandler __instance)
            {
                LogFile.LogMessage("Инициализация поверхности");
                Debug.Log("Initializing Surface");
                __instance.m_View = __instance.GetComponent<PhotonView>();
                LogFile.LogMessage($"PhotonView получен: {__instance.m_View != null}");

                __instance.m_SteamLobby = MainMenuHandler.SteamLobbyHandler;
                LogFile.LogMessage($"SteamLobbyHandler получен: {__instance.m_SteamLobby != null}");

                if (SurfaceNetworkHandler.RoomStats == null)
                {
                    LogFile.LogMessage("RoomStatus равен null, создавая новый экземпляр.");
                    SurfaceNetworkHandler.RoomStats = new RoomStatsHolder(__instance, SingletonAsset<BigNumbers>.Instance.StartMoney, BigNumbers.GetQuota(0), 3);
                    LogFile.LogMessage($"RoomStats создан. StartMoney: {SingletonAsset<BigNumbers>.Instance.StartMoney}, Quota: {BigNumbers.GetQuota(0)}");

                    if (PhotonNetwork.IsMasterClient)
                    {
                        LogFile.LogMessage("Являемся MasterClient.");
                        PhotonNetwork.CurrentRoom.IsOpen = true;
                        PhotonNetwork.CurrentRoom.IsVisible = true;
                        LogFile.LogMessage($"Комната открыта: {PhotonNetwork.CurrentRoom.IsOpen}, Комната видна: {PhotonNetwork.CurrentRoom.IsVisible}");

                        PhotonGameLobbyHandler.Instance.SetCurrentObjective(new InviteFriendsObjective());
                        LogFile.LogMessage("Установлена цель: InviteFriendsObjective.");
                        __instance.CheckSave();
                        LogFile.LogMessage("Вызван CheckSave.");
                    }
                    else
                    {
                        LogFile.LogMessage("Не являемся MasterClient, обновляем RoomProperties.");
                        __instance.OnRoomPropertiesUpdate(PhotonNetwork.CurrentRoom.CustomProperties);
                    }

                    if (SurfaceNetworkHandler.RoomStats.CurrentDay <= 1)
                    {
                        LogFile.LogMessage($"Первый день или меньше (CurrentDay: {SurfaceNetworkHandler.RoomStats.CurrentDay}).");
                        __instance.firstDay = true;
                        string localizedString = LocalizationKeys.GetLocalizedString(LocalizationKeys.Keys.HelmetWelcome);
                        HelmetText.Instance.SetHelmetText(localizedString, 3f);
                        Debug.Log("NEW RUN!");
                        if (PhotonNetwork.IsMasterClient)
                        {
                            LogFile.LogMessage("Являемся MasterClient, вызываем SpawnSurfacePickups.");
                            __instance.SpawnSurfacePickups();
                        }
                    }
                    else if (PhotonNetwork.IsMasterClient)
                    {
                        LogFile.LogMessage("Не первый день и являемся MasterClient, спавним VideoCamera.");
                        __instance.m_VideoCameraSpawner.SpawnMe(force: true);
                    }

                    if (__instance.m_SteamLobby != null)
                    {
                        LogFile.LogMessage("SteamLobby существует, открываем лобби.");
                        __instance.m_SteamLobby.OpenLobby();
                    }

                    SpawnHandler.Instance.SpawnLocalPlayer(Spawns.House);
                    LogFile.LogMessage("Спавним локального игрока в доме.");
                }
                else
                {
                    LogFile.LogMessage("RoomStats не null.");
                    Debug.Log("Should do next day here but waiting for upload?");
                    if (TimeOfDayHandler.TimeOfDay == TimeOfDay.Evening)
                    {
                        LogFile.LogMessage("Время суток - вечер.");
                        RichPresenceHandler.SetPresenceState(RichPresenceState.Status_AtHouse);
                        if (PhotonNetwork.IsMasterClient)
                        {
                            /*===========================================*/
                            LogFile.LogMessage("Задержка началась...");
                            yield return new WaitForSeconds(1f);
                            LogFile.LogMessage("Задержка завершена.");
                            /*===========================================*/

                            LogFile.LogMessage("Являемся MasterClient, проверяем наличие камеры.");
                            SurfaceNetworkHandler.ReturnedFromLostWorldWithCamera = __instance.CheckIfCameraIsPresent(includeBrokencamera: true);
                            LogFile.LogMessage($"Наличие камеры после возвращения: {SurfaceNetworkHandler.ReturnedFromLostWorldWithCamera}");

                            if (!SurfaceNetworkHandler.ReturnedFromLostWorldWithCamera)
                            {
                                LogFile.LogMessage("Камера отсутствует, сбрасываем апгрейды.");
                                SurfaceNetworkHandler.RoomStats.ResetCameraUpgrades();
                                if (PhotonNetwork.IsMasterClient)
                                {
                                    LogFile.LogMessage("Являемся MasterClient, устанавливаем цель GoToBedFailedObjective.");
                                    PhotonGameLobbyHandler.Instance.SetCurrentObjective(new GoToBedFailedObjective());
                                    if (SurfaceNetworkHandler.RoomStats.IsQuotaDay && !SurfaceNetworkHandler.RoomStats.CalculateIfReachedQuota())
                                    {
                                        LogFile.LogMessage("Сегодня день квоты и квота не выполнена, вызываем NextDay.");
                                        __instance.NextDay();
                                    }
                                }
                            }
                            else if (PhotonNetwork.IsMasterClient)
                            {
                                LogFile.LogMessage("Камера присутствует, устанавливаем цель ExtractVideoObjective.");
                                PhotonGameLobbyHandler.Instance.SetCurrentObjective(new ExtractVideoObjective());
                            }

                            if (!__instance.m_FailedQuota)
                            {
                                LogFile.LogMessage("Квота не провалена, проверяем наличие счёта из больницы.");
                                __instance.CheckForHospitalBill();
                            }
                        }

                        if (!Player.justDied && !__instance.m_FailedQuota)
                        {
                            LogFile.LogMessage("Игрок не умер и квота не провалена, спавним в DiveBell.");
                            SpawnHandler.Instance.SpawnLocalPlayer(Spawns.DiveBell);
                        }
                    }
                }

                if (!__instance.m_FailedQuota)
                {
                    LogFile.LogMessage("Квота не провалена, инициализируем ShopHandler.");
                    __instance.ShopHandler.InitShopHandler();
                }
            }
        }

        // Логирование для отладки
        public class LogFile
        {
            public static void LogMessage(string message)
            {
                string logFilePath = @"D:\\Mod_for_ContentWarning\\Mods\\Disable_Disappearance_Camera_In_Bell\\log.txt";
                System.IO.File.AppendAllText(logFilePath, $"[{System.DateTime.Now}]: {message}\n");
            }
        }
    }
}
