using System;
using System.ComponentModel;
using System.IO;
using UsCore.Config;


namespace U_Shoot.Tools.TargetBravo
{
    public partial class NoCrosshairMod : BaseCopyWindow
    {
        protected override string LogFileName { get { return "_OperationGhostNoCrosshairLog.txt"; } }

        public NoCrosshairMod(string GameFolder, GameConfig Config) : base(GameFolder, Config)
        {
            _SourceZip_Path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Properties.Resources.DATA_PAYLOADS_FOLDER, "tbravo", "NoCrosshair_Patch.zip");
            _DestinationPath = Path.Combine(_GameFolderPath, @"root\img\UI");
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
