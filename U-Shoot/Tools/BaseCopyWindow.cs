using System;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Windows.Forms;
using UsCore.Config;

namespace U_Shoot.Tools
{
    public partial class BaseCopyWindow : Form
    {
        protected virtual string LogFileName { get { return ""; } }

        protected string _GameFolderPath = string.Empty;
        protected string _LogFilePath = string.Empty;

        protected string _SourceZip_Path = string.Empty;
        protected string _DestinationPath = string.Empty;

        protected GameConfig _Config;

        public BaseCopyWindow() : this("", null)
        { }

        public BaseCopyWindow(string GameFolderPath, GameConfig Config)
        {
            InitializeComponent();

            this.Text = "Copying Files...";

            Btn_Close.Enabled = false;
            Btn_ShowLog.Enabled = false;
            _GameFolderPath = GameFolderPath;
            _Config = Config;

            string TmpFolderPath = Path.GetTempPath();
            _LogFilePath = Path.Combine(Path.GetTempPath(), DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + LogFileName);
        }

        protected virtual void BaseCopyWindow_Load(object sender, EventArgs e)
        {
            _BgWorker.RunWorkerAsync();
        }

        protected void SetProgressBarMaxValue()
        {
            if (File.Exists(_SourceZip_Path))
            {
                try
                {
                    using (ZipArchive source = ZipFile.Open(_SourceZip_Path, ZipArchiveMode.Read, null))
                    {
                        Pbar_Patch.Maximum = source.Entries.Count;
                    }
                }
                catch (Exception Ex)
                {
                    MessageBox.Show("Impossible to open archive:\n\n" + Ex.Message.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Close();
                }
            }
        }

        #region Background Worker

        protected virtual void BgWorker_DoWork(object sender, DoWorkEventArgs e)
        { }

        protected virtual void BgWorker_ProgressChanged(object sender, System.ComponentModel.ProgressChangedEventArgs e)
        {
            if (e.ProgressPercentage > 0)
                Pbar_Patch.Value += e.ProgressPercentage;
            else
                Close();
        }

        protected virtual void BgWorker_RunWorkerCompleted(object sender, System.ComponentModel.RunWorkerCompletedEventArgs e)
        {
            if (File.Exists(_LogFilePath))
                Btn_ShowLog.Enabled = true;

            Pbar_Patch.Value = Pbar_Patch.Maximum;

            Btn_Close.Enabled = true;

            this.Text = "Success !";

            WriteLog("");
            WriteLog("Plugin installation complete !");
        }

        #endregion

        protected string Compute_MD5(string FilePath)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(FilePath))
                {
                    var hash = md5.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "");
                }
            }
        }

        /// <summary>
        /// Generic function for BackgroundWorker to extract zip archive 
        /// </summary>
        protected void ExtractArchive()
        {
            if (File.Exists(_SourceZip_Path))
            {
                if (Directory.Exists(_DestinationPath))
                {
                    try
                    {
                        using (ZipArchive source = ZipFile.Open(_SourceZip_Path, ZipArchiveMode.Read, null))
                        {
                            foreach (ZipArchiveEntry entry in source.Entries)
                            {
                                string fullPath = Path.GetFullPath(Path.Combine(_DestinationPath, entry.FullName));

                                if (Path.GetFileName(fullPath).Length != 0)
                                {
                                    WriteLog("Extracting " + fullPath + " ...");
                                    entry.ExtractToFile(fullPath, true);
                                    _BgWorker.ReportProgress(1);
                                }
                            }
                            WriteLog("\nExtraction complete !");
                        }
                    }
                    catch (Exception Ex)
                    {
                        MessageBox.Show("Impossible to extract archive:\n\n" + Ex.Message.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        _BgWorker.ReportProgress(0);
                    }
                }
                else
                {
                    MessageBox.Show("Directory not found: " + _DestinationPath, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    _BgWorker.ReportProgress(0);
                }
            }
            else
            {
                MessageBox.Show("File not found: " + _DestinationPath, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _BgWorker.ReportProgress(0);
            }
        }

        /// <summary>
        /// Genereic function to install BepInEx plugin files into the game folder
        /// </summary>
        protected void CopyBepInExFiles(string BaseUnityFolder)
        {
            if (Directory.Exists(_GameFolderPath))
            {
                //Instaling BepInEx common files
                CopyDirectory(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Properties.Resources.DATA_UNITY_FOLDER, BaseUnityFolder ), _GameFolderPath, true);

                //Installing game specific plugin files
                CopyDirectory(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Properties.Resources.DATA_UNITY_FOLDER, _Config.RomName ), _GameFolderPath, true);

                switch (_Config.RomName)
                {
                    case "hwspr2":
                    case "hwwar2": Rename_BepInEx_Doorstop_Dll("version.dll"); break;
                    default:break;
                }
            }
            else
            {
                MessageBox.Show("Directory not found: " + _GameFolderPath, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                WriteLog("Directory not found: " + _GameFolderPath);
                _BgWorker.ReportProgress(0);
            }
        }

        private void CopyDirectory(string SourceDir, string DestinationDir, bool recursive)
        {
            DirectoryInfo dInfo = new DirectoryInfo(SourceDir);

            if (!dInfo.Exists)
            {
                MessageBox.Show("Directory not found: " + SourceDir, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                WriteLog("Directory not found: " + SourceDir);
                _BgWorker.ReportProgress(0);
            }
            else
            {
                DirectoryInfo[] dInfos = dInfo.GetDirectories();

                string strFilePath = string.Empty;
                try
                {      
                    Directory.CreateDirectory(DestinationDir);

                    foreach (FileInfo File in dInfo.GetFiles())
                    {
                        strFilePath = Path.Combine(DestinationDir, File.Name);
                        WriteLog("Copying " + strFilePath + " ...");
                        File.CopyTo(strFilePath, true);
                        _BgWorker.ReportProgress(1);
                    }
                }
                catch (Exception Ex)
                {
                    WriteLog("Error copying " + strFilePath + " : \n" + Ex.Message.ToString());
                    MessageBox.Show("Error copying " + strFilePath + " : \n" + Ex.Message.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    _BgWorker.ReportProgress(0);
                }
                
                if (recursive)
                {
                    foreach (DirectoryInfo subDir in dInfos)
                    {
                        string newDestinationDir = Path.Combine(DestinationDir, subDir.Name);
                        CopyDirectory(subDir.FullName, newDestinationDir, true);
                    }
                }
            }
        }

        /// <summary>
        /// Some (older) games may need to change the winhttp.dll to version.dll (or any other dll loaded by the program) to allow BepInEx DoorStop to hook
        /// </summary>
        /// <param name="DllName"></param>
        private void Rename_BepInEx_Doorstop_Dll(string DllName)
        {
            WriteLog("Renaming winhttp.dll to " + DllName + " ...");
            string DoorStopPath = Path.Combine(_GameFolderPath, DllName);
            if (File.Exists(DoorStopPath))
                File.Delete(DoorStopPath);
            File.Move(Path.Combine(_GameFolderPath, "winhttp.dll"), DoorStopPath);
        }

        protected virtual void WriteLog(string Text)
        {
            try
            {
                File.AppendAllText(_LogFilePath, Text + "\n");
            }
            catch { }
        }

        private void Btn_ShowLog_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(_LogFilePath);
        }

        private void Btn_Close_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
