using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Windows.Forms;
using UsCore.Config;

namespace U_Shoot.Tools.WalkingDead
{
    /// <summary>
    /// Credits to handsome_van for creating the 'CrimsonBlood.patch' binary to mod the original game blood texture 
    /// </summary>
    public class CrimsonBlood : BaseCopyWindow
    {
        protected override string LogFileName { get { return "_WalkingDeadPatcherLog.txt"; } }

        private const string PATCH_ZIP_FILENAME = "CrimsonBlood.zip";
        private const string PATCH_FILENAME = "CrimsonBlood.patch";
        private const string FX_FILEPATH = @"g6\walkingdead\data\g6";
        private const string FX_FILENAME = "walkdead_fx.g6";
        private const string FX_ORIGINAL_MD5 = "21e6c9daa6accde06ba65f8a4de28c8f";

        public CrimsonBlood(string GameFolder, GameConfig Config) : base(GameFolder, Config)
        {
            Pbar_Patch.Maximum = 2;

            this.Text = "Patching files...";
        }

        protected override void BgWorker_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            string sRomDirectory = Path.GetDirectoryName(_Config.GetStringOption("Game ELF [game]"));
            if (Directory.Exists(sRomDirectory))
            {
                string sTargetFile = Path.Combine(sRomDirectory, FX_FILEPATH, FX_FILENAME);
                string FxMd5 = Compute_MD5(sTargetFile);

                if (string.Equals(FxMd5, FX_ORIGINAL_MD5, StringComparison.OrdinalIgnoreCase))
                {
                    WriteLog("Found a clean file to patch : " + sTargetFile);
                    try
                    {
                        //Making backup of the original file
                        File.Copy(sTargetFile, sTargetFile + ".backup", true);

                        //Reading data directly from the zip archive
                        using (FileStream Fs = new FileStream(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Properties.Resources.DATA_PAYLOADS_FOLDER, _Config.RomName, PATCH_ZIP_FILENAME), FileMode.Open))
                        {
                            using (ZipArchive zPatchArchive = new ZipArchive(Fs, ZipArchiveMode.Read))
                            {
                                ZipArchiveEntry PatchFile = zPatchArchive.GetEntry(PATCH_FILENAME);

                                byte[] bPatchData;
                                Stream St = PatchFile.Open();
                                using (MemoryStream Ms = new MemoryStream())
                                {
                                    St.CopyTo(Ms);
                                    bPatchData = Ms.ToArray();
                                }

                                //Decrypting the patch headers
                                int pCount = BitConverter.ToInt32(bPatchData, 4);
                                int[] pStartAddress = new int[pCount];
                                int[] pLength = new int[pCount];
                                List<byte[]> pPayload = new List<byte[]>(pCount);

                                int FilePointer = 0x08;
                                for (int i = 0; i < pCount; i++)
                                {
                                    pStartAddress[i] = BitConverter.ToInt32(bPatchData, FilePointer);
                                    FilePointer += 0x08;

                                    int PatchLength = BitConverter.ToInt32(bPatchData, FilePointer);
                                    pLength[i] = PatchLength;
                                    FilePointer += 0x04;

                                    byte[] b = new byte[PatchLength];
                                    Array.Copy(bPatchData, FilePointer, b, 0, PatchLength);
                                    pPayload.Add(b);
                                    FilePointer += PatchLength;
                                }
                                WriteLog("Successfully unpacked " + Fs.Name);
                                WriteLog("Number of patch to apply: " + pCount);

                                //Applying the patch
                                byte[] bOriginalFileData = File.ReadAllBytes(sTargetFile);
                                byte[] bFinalFileData = File.ReadAllBytes(sTargetFile);
                                for (int i = 0; i < pCount; i++)
                                {
                                    WriteLog("Applying Patch " + i + " ...");
                                    for (int wPointer = 0; wPointer < pLength[i]; wPointer++)
                                    {
                                        byte bOrigin = bOriginalFileData[pStartAddress[i] + wPointer];
                                        byte bPatchByte = pPayload[i][wPointer];
                                        byte bResult = (byte)(bOrigin ^ bPatchByte);
                                        bFinalFileData[pStartAddress[i] + wPointer] = bResult;
                                    }
                                    _BgWorker.ReportProgress(1);
                                }

                                //Writing Pathed file
                                File.WriteAllBytes(sTargetFile, bFinalFileData);
                                WriteLog("\nExtraction complete !");
                            }
                        }
                    }
                    catch (Exception Ex)
                    {
                        MessageBox.Show("Error patching file: " + Ex.Message.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        _BgWorker.ReportProgress(0);
                    }
                }
                else
                {
                    MessageBox.Show("MD5 check fail, skip patching " + sTargetFile, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    _BgWorker.ReportProgress(0);
                }
            }
            else
            {
                MessageBox.Show("Directory not found: " + sRomDirectory, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _BgWorker.ReportProgress(0);
            }
        }
    }
}
