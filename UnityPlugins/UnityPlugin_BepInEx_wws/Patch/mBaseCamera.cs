using HarmonyLib;

namespace BepInEx_UShoot_Plugin.Patch
{
    /// <summary>
    /// This class is using calls from a DLL not present in the game release, causing multiple ERROR
    /// "Blanking" calls from this class allow the Guns ti run "normally"
    /// </summary>
    class mBaseCamera
    {
        [HarmonyPatch(typeof(MVSDK.BaseCamera), "CaptureThreadProc")]
        class CaptureThreadProc
        {
            static bool Prefix()
            {
                //DemulShooter_Plugin.MyLogger.LogMessage("mBaseCamera.CaptureThreadProc()");
                return false;
            }
        }

        [HarmonyPatch(typeof(MVSDK.BaseCamera), "Close")]
        class Close
        {
            static bool Prefix()
            {
                //DemulShooter_Plugin.MyLogger.LogMessage("mBaseCamera.Close()");
                return false;
            }
        }

        [HarmonyPatch(typeof(MVSDK.BaseCamera), "GetFrameData")]
        class GetFrameData
        {
            static bool Prefix(ref bool __result)
            {
                //DemulShooter_Plugin.MyLogger.LogMessage("mBaseCamera.GetFrameData()");
                __result = true;
                return false;
            }
        }

        [HarmonyPatch(typeof(MVSDK.BaseCamera), "GetImageResolution")]
        class GetImageResolution
        {
            static bool Prefix()
            {
                //DemulShooter_Plugin.MyLogger.LogMessage("mBaseCamera.GetImageResolution()");
                return false;
            }
        }

        [HarmonyPatch(typeof(MVSDK.BaseCamera), "InitCamera")]
        class InitCamera
        {
            static bool Prefix(ref bool __result)
            {
                //DemulShooter_Plugin.MyLogger.LogMessage("mBaseCamera.InitCamera()");
                __result = true;
                return false;
            }
        }

        [HarmonyPatch(typeof(MVSDK.BaseCamera), "LoadDefaultConfig")]
        class LoadDefaultConfig
        {
            static bool Prefix()
            {
                //DemulShooter_Plugin.MyLogger.LogMessage("mBaseCamera.LoadDefaultConfig()");
                return false;
            }
        }

        [HarmonyPatch(typeof(MVSDK.BaseCamera), "Pause")]
        class Pause
        {
            static bool Prefix()
            {
                //DemulShooter_Plugin.MyLogger.LogMessage("mBaseCamera.Pause()");
                return false;
            }

        }
        [HarmonyPatch(typeof(MVSDK.BaseCamera), "Play")]
        class Play
        {
            static bool Prefix(ref bool __result)
            {
                //DemulShooter_Plugin.MyLogger.LogMessage("mBaseCamera.Play()");
                __result = true;
                return false;
            }
        }

        [HarmonyPatch(typeof(MVSDK.BaseCamera), "SetExposureState")]
        class SetExposureState
        {
            static bool Prefix()
            {
                //DemulShooter_Plugin.MyLogger.LogMessage("mBaseCamera.SetExposureState()");
                return false;
            }
        }

        [HarmonyPatch(typeof(MVSDK.BaseCamera), "SetImageResolution", new[] { typeof(int), typeof(int), typeof(int), typeof(int) })]
        class SetImageResolution
        {
            static bool Prefix(ref MVSDK.CameraSdkStatus __result)
            {
                //DemulShooter_Plugin.MyLogger.LogMessage("mBaseCamera.SetImageResolution()");
                __result = MVSDK.CameraSdkStatus.CAMERA_STATUS_SUCCESS;
                return false;
            }
        }

        [HarmonyPatch(typeof(MVSDK.BaseCamera), "SetMonochrome")]
        class SetMonochrome
        {
            static bool Prefix()
            {
                //DemulShooter_Plugin.MyLogger.LogMessage("mBaseCamera.SetMonochrome()");
                return false;
            }
        }

        [HarmonyPatch(typeof(MVSDK.BaseCamera), "UpdateExposure")]
        class UpdateExposure
        {
            static bool Prefix()
            {
                //DemulShooter_Plugin.MyLogger.LogMessage("mBaseCamera.UpdateExposure()");
                return false;
            }
        }

        [HarmonyPatch(typeof(MVSDK.BaseCamera))]
        [HarmonyPatch("AnalogGain", MethodType.Setter)]
        class set_AnalogGain
        {
            static bool Prefix(float value, float ___m_fAnalogGain)
            {
                //DemulShooter_Plugin.MyLogger.LogMessage("mBaseCamera.set_AnalogGain()");
                ___m_fAnalogGain = value;
                return false;
            }
        }
    }
}
