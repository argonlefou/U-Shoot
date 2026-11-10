using UsCore.Config;

namespace U_Shoot.Tools.UnityPluginConfigs
{
    public partial class FireHero2023 : BaseUnityConfig
    {
        private string _Language = "EN";
        private string _Players = "4";
        private string _CabinetType = "A";

        public FireHero2023(string GameFolder, GameConfig Config) : base(GameFolder, Config)
        {
            InitializeComponent();
        }

        protected override void LoadData()
        {
            base.LoadData();

            _Language = _Plugin_IniFile.IniReadValue("System", "LANGUAGE");
            Cbox_Language.Text = _Language;

            _Players = _Plugin_IniFile.IniReadValue("System", "PLAYERS");
            Cbox_Players.Text = _Players;

            _CabinetType = _Plugin_IniFile.IniReadValue("System", "CABINET_TYPE");
            Cbox_Type.Text = _CabinetType;
        }

        protected override void SaveData()
        {
            base.SaveData();

            _Plugin_IniFile.IniWriteValue("System", "LANGUAGE", Cbox_Language.Text);
            _Plugin_IniFile.IniWriteValue("System", "PLAYERS", Cbox_Players.Text);
            _Plugin_IniFile.IniWriteValue("System", "CABINET_TYPE", Cbox_Type.Text);
        }
    }
}
