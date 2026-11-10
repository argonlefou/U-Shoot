using UsCore.Config;

namespace U_Shoot.Tools.UnityPluginConfigs
{
    public partial class NerfArcade : BaseUnityConfig
    {
        private string _CabinetType = "EUROPE";

        public NerfArcade(string GameFolder, GameConfig Config) : base(GameFolder, Config)
        {
            InitializeComponent();
        }

        protected override void LoadData()
        {
            base.LoadData();

            int i = 6;
            if (int.TryParse(_Plugin_IniFile.IniReadValue("System", "CAB_TEMPLATE"), out i))
            {
                if (i > 0 && i < Cbox_CabinetType.Items.Count)
                    _CabinetType = Cbox_CabinetType.Items[i].ToString();
            }
            Cbox_CabinetType.Text = _CabinetType;
        }

        protected override void SaveData()
        {
            base.SaveData();

            _Plugin_IniFile.IniWriteValue("System", "CAB_TEMPLATE", Cbox_CabinetType.SelectedIndex.ToString());
        }
    }
}
