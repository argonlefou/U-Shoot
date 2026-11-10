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
    public partial class SkullOfShadow : BaseUnityConfig
    {
        private string _Language = "EN";

        public SkullOfShadow(string GameFolder, GameConfig Config) : base(GameFolder, Config)
        {
            InitializeComponent();
        }

        protected override void LoadData()
        {
            base.LoadData();

            _Language = _Plugin_IniFile.IniReadValue("System", "LANGUAGE");
            Cbox_Language.Text = _Language;
        }

        protected override void SaveData()
        {
            base.SaveData();

            _Plugin_IniFile.IniWriteValue("System", "LANGUAGE", Cbox_Language.Text);
        }
    }
}
