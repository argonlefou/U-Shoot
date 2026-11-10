using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using UsCore.Config;
using static U_Shoot.Games.Game_ArcadepcHsfr;

namespace U_Shoot.Tools.UnityPluginConfigs
{
    public partial class MechaTornado : BaseUnityConfig
    {
        private string _Players = "4";
        private bool _UseWaterGun = false;

        public MechaTornado(string GameFolder, GameConfig Config) : base(GameFolder, Config)
        {
            InitializeComponent();
        }

        protected override void LoadData()
        {
            base.LoadData();

            _Players = _Plugin_IniFile.IniReadValue("System", "PLAYERS");
            Cbox_Players.Text = _Players;

            _Plugin_IniFile.IniReadBoolValue("System", "USE_WATERGUN", ref _UseWaterGun);
            Chk_WaterGun.Checked = _UseWaterGun;
        }

        protected override void SaveData()
        {
            base.SaveData();

            _Plugin_IniFile.IniWriteValue("System", "PLAYERS", Cbox_Players.Text);
            _Plugin_IniFile.IniWriteValue("System", "USE_WATERGUN", Chk_WaterGun.Checked ? "1" : "0");
        }
    }
}

