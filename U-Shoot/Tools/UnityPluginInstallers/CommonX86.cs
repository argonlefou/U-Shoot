using System;
using System.IO;
using UsCore.Config;

namespace U_Shoot.Tools.UnityPluginInstallers
{
    public partial class CommonX86 : BaseCopyWindow
    {
        public CommonX86(string GameFolder, GameConfig Config) : base(GameFolder, Config)
        {
            string TmpFolderPath = Path.GetTempPath();
            _LogFilePath = Path.Combine(Path.GetTempPath(), DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss_") + "BepInEx_Install_" + _Config.RomName + ".txt");

            string[] _BepInExFilesToCopy = Directory.GetFiles(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Properties.Resources.DATA_UNITY_FOLDER, Properties.Resources.BEPINEX_X86_FOLDER), "*.*", SearchOption.AllDirectories);
            string[] _GameFilesToCopy = Directory.GetFiles(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Properties.Resources.DATA_UNITY_FOLDER, _Config.RomName), "*.*", SearchOption.AllDirectories);

            Pbar_Patch.Maximum = _BepInExFilesToCopy.Length + _GameFilesToCopy.Length;

            this.Text = "Copying files...";
        }

        protected override void BgWorker_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            CopyBepInExFiles(Properties.Resources.BEPINEX_X86_FOLDER);
        }
    }
}
