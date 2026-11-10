using UsCore.Config;

namespace U_Shoot.Tools.UnityPluginConfigs
{
    public partial class CommonVertical : BaseUnityConfig
    {
        private bool _HorizontalScreen = true;

        public CommonVertical(string GameFolder, GameConfig Config) : base(GameFolder, Config)
        {
            InitializeComponent();
        }

        protected override void LoadData()
        {
            base.LoadData();

            _Plugin_IniFile.IniReadBoolValue("Video", "HORIZONTAL_SCREEN", ref _HorizontalScreen);
            Chk_HorizontalScreen.Checked = _HorizontalScreen;
        }

        protected override void SaveData()
        {
            base.SaveData();

            _Plugin_IniFile.IniWriteValue("Video", "HORIZONTAL_SCREEN", Chk_HorizontalScreen.Checked ? "1" : "0");
        }
    }
}
