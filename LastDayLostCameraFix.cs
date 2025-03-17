using HarmonyLib;
using UnityEngine;
using Zorro.Core;
using Photon.Pun;
using System.Collections;
using System.Reflection;
using PimDeWitte.UnityMainThreadDispatcher;

namespace LastDayLostCameraFix
{
    [ContentWarningPlugin("LastDayLostCameraFix", "3.0", vanillaCompatible: false)]
    public class LastDayLostCameraFix
    {
        private static readonly Harmony harmony = new Harmony("com.artkopt.LastDayLostCameraFix");
        static LastDayLostCameraFix()
        {
            DebugCW.Log($"The 'LastDayLostCameraFix' mod has been launched");
            ApplyPatches();
        }

        public static void ApplyPatches()
        {
            try
            {
                DebugCW.Log("Attempting to apply patches");
                if (!Harmony.HasAnyPatches(harmony.Id))
                {
                    harmony.PatchAll();
                    DebugCW.Log("Patches applied successfully");
                }
            }
            catch (Exception ex)
            {
                DebugCW.LogException(ex);
                DebugCW.LogError("Failed to apply patches. Mod may not work correctly.");
            }
        }

        [HarmonyPatch(typeof(SurfaceNetworkHandler), nameof(SurfaceNetworkHandler.InitSurface))]
        public static class Patch_InitSurface
        {
            [HarmonyPrefix]
            static bool Prefix(SurfaceNetworkHandler __instance)
            {
                try
                {
                    DebugCW.Log("Patch 'InitSurface' called");

                    if (!UnityMainThreadDispatcher.Exists())
                    {
                        DebugCW.LogWarning("'UnityMainThreadDispatcher' not found...");
                        GameObject dispatcherObj = new GameObject("UnityMainThreadDispatcher");
                        dispatcherObj.AddComponent<UnityMainThreadDispatcher>();
                        DebugCW.Log("'UnityMainThreadDispatcher' created and added to the scene");
                    }

                    Debug.Log("Initializing Surface");
                    __instance.m_View = __instance.GetComponent<PhotonView>();
                    __instance.m_SteamLobby = MainMenuHandler.SteamLobbyHandler;

                    if (SurfaceNetworkHandler.RoomStats == null)
                    {
                        SurfaceNetworkHandler.RoomStats = new RoomStatsHolder(__instance, SingletonAsset<BigNumbers>.Instance.StartMoney, BigNumbers.GetQuota(0), 3);
                        if (PhotonNetwork.IsMasterClient)
                        {
                            PhotonNetwork.CurrentRoom.IsOpen = true;
                            PhotonNetwork.CurrentRoom.IsVisible = true;
                            PhotonGameLobbyHandler.Instance.SetCurrentObjective(new InviteFriendsObjective());
                            __instance.CheckSave();
                        }
                        else
                        {
                            __instance.OnRoomPropertiesUpdate(PhotonNetwork.CurrentRoom.CustomProperties);
                        }

                        if (SurfaceNetworkHandler.RoomStats.CurrentDay <= 1)
                        {
                            __instance.firstDay = true;
                            string localizedString = LocalizationKeys.GetLocalizedString(LocalizationKeys.Keys.HelmetWelcome);
                            HelmetText.Instance.SetHelmetText(localizedString, 3f);
                            Debug.Log("NEW RUN!");
                            if (PhotonNetwork.IsMasterClient)
                            {
                                __instance.SpawnSurfacePickups();
                            }
                        }
                        else if (PhotonNetwork.IsMasterClient)
                        {
                            __instance.m_VideoCameraSpawner.SpawnMe(force: true);
                        }

                        if (__instance.m_SteamLobby != null)
                        {
                            __instance.m_SteamLobby.OpenLobby();
                        }

                        SpawnHandler.Instance.SpawnLocalPlayer(Spawns.House);
                    }
                    else
                    {
                        Debug.Log("Should do next day here but waiting for upload?");
                        if (TimeOfDayHandler.TimeOfDay == TimeOfDay.Evening)
                        {
                            RichPresenceHandler.SetPresenceState(RichPresenceState.Status_AtHouse);
                            if (PhotonNetwork.IsMasterClient)
                            {
                                CheckCameraOnMasterClient(__instance);
                            }

                            if (!Player.justDied && !__instance.m_FailedQuota)
                            {
                                SpawnHandler.Instance.SpawnLocalPlayer(Spawns.DiveBell);
                            }
                        }
                    }

                    if (!__instance.m_FailedQuota)
                    {
                        __instance.ShopHandler.InitShopHandler();
                    }

                    return false;
                }
                catch (Exception ex)
                {
                    DebugCW.LogException(ex);

                    if (Harmony.HasAnyPatches(harmony.Id))
                    {
                        DebugCW.Log("Patch disabled");
                        harmony.UnpatchSelf();
                    }

                    HelmetText.Instance.SetHelmetText(
                        $"Patch '{Assembly.GetExecutingAssembly().GetName().Name!}' - Disabled!\n" +
                        "Please report the error to the mod developer.\n",
                        3f
                    );
                    return true;
                }
            }
        }

        public static void CheckCameraOnMasterClient(SurfaceNetworkHandler __instance)
        {
            DebugCW.Log("Waiting for the camera to appear...");
            UnityMainThreadDispatcher.Instance().Enqueue(CheckCameraOnMasterClientCoroutine(__instance));
        }

        private static IEnumerator CheckCameraOnMasterClientCoroutine(SurfaceNetworkHandler __instance)
        {
            float maxWaitTime = 1f;
            float elapsedTime = 0f;

            while (!__instance.CheckIfCameraIsPresent(includeBrokencamera: true) && elapsedTime < maxWaitTime)
            {
                DebugCW.Log($"Waiting... {elapsedTime}s");
                yield return new WaitForSeconds(0.2f);
                elapsedTime += 0.2f;

                if (elapsedTime >= maxWaitTime)
                {
                    DebugCW.LogWarning("Camera not found...");
                    break;
                }
            }

            UnityMainThreadDispatcher.Instance().Enqueue(CheckIfCameraIsPresentCoroutine(__instance));
        }

        public static IEnumerator CheckIfCameraIsPresentCoroutine(SurfaceNetworkHandler __instance)
        {
            SurfaceNetworkHandler.ReturnedFromLostWorldWithCamera = __instance.CheckIfCameraIsPresent(includeBrokencamera: true);
            if (!SurfaceNetworkHandler.ReturnedFromLostWorldWithCamera)
            {
                SurfaceNetworkHandler.RoomStats.ResetCameraUpgrades();
                if (PhotonNetwork.IsMasterClient)
                {
                    PhotonGameLobbyHandler.Instance.SetCurrentObjective(new GoToBedFailedObjective());
                    if (SurfaceNetworkHandler.RoomStats.IsQuotaDay && !SurfaceNetworkHandler.RoomStats.CalculateIfReachedQuota())
                    {
                        __instance.NextDay();
                    }
                }
            }
            else if (PhotonNetwork.IsMasterClient)
            {
                PhotonGameLobbyHandler.Instance.SetCurrentObjective(new ExtractVideoObjective());
            }

            if (!__instance.m_FailedQuota)
            {
                __instance.CheckForHospitalBill();
            }

            yield return null;
        }
    }
}