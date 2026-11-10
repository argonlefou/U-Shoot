using HarmonyLib;

namespace BepInEx_UShoot_Plugin.Patch
{
    internal class mDebugFlow
    {
        /// <summary>
        /// Debug flow is Updated during the stage, we can hook it up to get some data and outputs
        /// </summary>
        [HarmonyPatch(typeof(DebugFlow), "StageUpdate")]
        class StageUpdate
        {
            static bool Prefix(GameData GameData)
            {
                for (int PlayerIndex = 0; PlayerIndex < GameData.PlayerData.Length; PlayerIndex++)
                {
                    int PlayerID = GameData.PlayerData[PlayerIndex].PlayerID;
                    UShoot_Plugin.OutputData.Life[PlayerID] = (byte)(GameData.PlayerData[PlayerIndex].PlayerLive.PlayLive * 100.0f);

                    for (int GunIndex = 0; GunIndex < GameData.PlayerData[PlayerIndex].PlayerGun.GunStock().Length; GunIndex++)
                    {
                        if (GameData.PlayerData[PlayerIndex].PlayerGun.GunStock()[GunIndex].Select)
                        {
                            UShoot_Plugin.OutputData.SelectedWeapon[PlayerID] = (byte)(GunIndex + 1);
                            UShoot_Plugin.OutputData.Ammo[PlayerID] = GameData.PlayerData[PlayerID].PlayerGun.GunStock()[GunIndex].Remain;
                        }
                    }
                }
                return false;
            }
        }
    }
}
