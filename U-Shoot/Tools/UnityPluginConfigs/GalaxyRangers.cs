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

namespace U_Shoot.Tools.UnityPluginConfigs
{
    public partial class GalaxyRangers : BaseUnityConfig
    {
        private string _Players = "4";
        private string _Language = "EN";

        public GalaxyRangers(string GameFolder, GameConfig Config) : base(GameFolder, Config)
        {
            InitializeComponent();
        }

        protected override void LoadData()
        {
            base.LoadData();

            _Players = _Plugin_IniFile.IniReadValue("System", "PLAYERS");
            Cbox_Players.Text = _Players;

            _Language = _Plugin_IniFile.IniReadValue("System", "LANGUAGE");
            Cbox_Language.Text = _Language;
        }

        protected override void SaveData()
        {
            base.SaveData();

            _Plugin_IniFile.IniWriteValue("System", "PLAYERS", Cbox_Players.Text);
            _Plugin_IniFile.IniWriteValue("System", "LANGUAGE", Cbox_Language.Text);
        }
    }
}
