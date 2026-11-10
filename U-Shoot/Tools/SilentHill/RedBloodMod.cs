using System;
using System.ComponentModel;
using System.IO;
using UsCore.Config;

namespace U_Shoot.Tools.SilentHill
{
    public partial class RedBloodMod : BaseCopyWindow
    {
        protected override string LogFileName { get { return "_SilentHillRedBloodLog.txt"; } }

        public RedBloodMod(string GameFolder, GameConfig Config) : base(GameFolder, Config)
        {
            _SourceZip_Path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Properties.Resources.DATA_PAYLOADS_FOLDER, "sha", "SHA-BloodMod.zip");
            _DestinationPath = Path.Combine(_GameFolderPath, @"Data\effect\texture");
            SetProgressBarMaxValue();
        }

        #region Background Worker

        protected override void BgWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            ExtractArchive();
        }

        #endregion

    }
}
