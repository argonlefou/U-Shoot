using System.Windows.Forms;
using U_Shoot.Games;
using UsCore.Config;

namespace U_Shoot.Tools.UnityPluginConfigs
{
    public partial class Common : BaseUnityConfig
    {
        public Common(string GameFolder, GameConfig Config) : base(GameFolder, Config)
        {
            InitializeComponent();
        }
    }
}