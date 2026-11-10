using UsCore.Config;

namespace U_Shoot.Tools.UnityPluginConfigs
{
    public partial class TombRaider : BaseUnityConfig
    {
        private string _Language = "EN";

        public TombRaider(string GameFolder, GameConfig Config) : base(GameFolder, Config)
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
