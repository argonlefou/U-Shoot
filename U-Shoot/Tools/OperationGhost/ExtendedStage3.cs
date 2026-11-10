using System;
using System.ComponentModel;
using System.IO;
using UsCore.Config;

namespace U_Shoot.Tools.OperationGhost
{
    public partial class ExtendedStage3 :  BaseCopyWindow
    {
        protected override string LogFileName { get { return "_OperationGhostExtendedStage3.txt"; } }

        public ExtendedStage3(string GameFolder, GameConfig Config) : base(GameFolder, Config)
        {
            _SourceZip_Path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Properties.Resources.DATA_PAYLOADS_FOLDER, "opghost", "Extended_Stage3.zip");
            _DestinationPath = Path.Combine(_GameFolderPath, @"root\scene\m_ship");
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
