using HarmonyLib;

namespace BepInEx_USoot_Plugin.Patch
{
    internal class mSYFireAndroidIO
    {
        [HarmonyPatch(typeof(SYFireAndroidIO), "OpenDoor")]
        class OpenDoor
        {
            static bool Prefix(int id)
            {
                //DemulShooter_Plugin.MyLogger.LogMessage("SYFireAndroidIO.OpenDoor(): id=" + id);
                UShoot_Plugin.OutputData.GiftDoor[id] = 1;
                return true;
            }
        }

        [HarmonyPatch(typeof(SYFireAndroidIO), "OpenTickets")]
        class OpenTickets
        {
            static bool Prefix(int id)
            {
                //DemulShooter_Plugin.MyLogger.LogMessage("SYFireAndroidIO.OpenTickets(): id=" + id);
                return true;
            }
        }

        /// <summary>
        /// Gettings gift light status [0 = OFF / 1 = ON  / 2 = FLASH]
        /// </summary>
        [HarmonyPatch(typeof(SYFireAndroidIO), "SetGiftLightState")]
        class SetGiftLightState
        {
            static bool Prefix(SYFireAndroidIO.GiftLight giftLight)
            {
                //DemulShooter_Plugin.MyLogger.LogMessage("SYFireAndroidIO.SetGiftLightState(): giftLight=" + giftLight.ToString());
                UShoot_Plugin.OutputData.GiftLight = (byte)giftLight;
                return true;
            }
        }

        /// <summary>
        /// Gettings key light status [0 = OFF / 1 = ON / 2 = FLASH / 3 = FLASH 20sec. / 4 = ON 20sec.]
        /// </summary>
        [HarmonyPatch(typeof(SYFireAndroidIO), "SetKeyLightState")]
        class SetKeyLightState
        {
            static bool Prefix(SYFireAndroidIO.KeyType keyType, SYFireAndroidIO.KeyLight keyLight)
            {
                //DemulShooter_Plugin.MyLogger.LogMessage("SYFireAndroidIO.SetKeyLightState(): keyType=" + keyType.ToString() + ", keyLight=" + keyLight.ToString());
                switch (keyType)
                {
                    case SYFireAndroidIO.KeyType.Left:
                        {
                            UShoot_Plugin.OutputData.LeftKeyLight = (byte)keyLight;
                        }break;
                    case SYFireAndroidIO.KeyType.Center:
                        {
                            UShoot_Plugin.OutputData.CenterKeyLight = (byte)keyLight;
                        }
                        break;
                    case SYFireAndroidIO.KeyType.Right:
                        {
                            UShoot_Plugin.OutputData.RightKeyLight = (byte)keyLight;
                        }
                        break;
                    case SYFireAndroidIO.KeyType.All:
                        {
                            UShoot_Plugin.OutputData.LeftKeyLight = (byte)keyLight;
                            UShoot_Plugin.OutputData.CenterKeyLight = (byte)keyLight;
                            UShoot_Plugin.OutputData.RightKeyLight = (byte)keyLight;
                        }
                        break;
                }
                return true;
            }
        }

        /// <summary>
        /// Gettings light status
        /// Changing higher values so that we can store everything on one half-byte
        /// 0= GameStart,
        /// 1=GameOver,
        /// 2=Default,
        /// 3=Bullseye,
        /// 4=Rankings,
        /// 5=Miss,
        /// 51=CountDown15, --> 6
        /// 52=CountDown12,
        /// 53=CountDown9,
        /// 54=CountDown6,
        /// 55=CountDown3,
        /// 56=CountDownOver --> 11
        /// </summary>
        [HarmonyPatch(typeof(SYFireAndroidIO), "SetLight")]
        class SetLight
        {
            static bool Prefix(SYFireAndroidIO.Light light, SYFireAndroidIO.LightMode mode)
            {
                //DemulShooter_Plugin.MyLogger.LogMessage("SYFireAndroidIO.SetLight(): light=" + light.ToString() + ", mode=" + mode.ToString());
                if (light == SYFireAndroidIO.Light.Screen_Side) //Other lights not used in code
                {
                    if ((int)mode > 50)
                    {
                        UShoot_Plugin.OutputData.ScreenSideLight = (byte)(mode - 45);
                    }
                    else
                    {
                        UShoot_Plugin.OutputData.ScreenSideLight = (byte)(mode);
                    }
                }
                return true;
            }
        }

    }
}
