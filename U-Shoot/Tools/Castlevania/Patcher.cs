using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Windows.Forms;
using UsCore.Config;

namespace U_Shoot.Tools.Castlevania
{
    public partial class Patcher : BaseCopyWindow
    {
        protected override string LogFileName { get { return "_CastlevaniaPatcherLog.txt"; } }

        private readonly byte[] _PatchData = new byte[] { 0x78, 0x78 };   //Replace unknown char by "xx"
        private readonly string _ReplacementString = "xx";

        public Patcher(string GameFolder, GameConfig Config)  : base (GameFolder, Config)
        {
            Pbar_Patch.Maximum = 8;

            this.Text = "Patching files...";
        }

        /// <summary>
        /// There are 8 files with an unreadable character on european(?) Windows:
        /// data\chr\k_st2_picture_1_��.dds
        /// data\chr\m_006_g��d_d.dds
        /// data\chr\m_006_g��d_n.dds
        /// data\chr\m_006_g��d_��.dds
        /// data\chr\m_017_dth2_��.dds
        /// data\chr\m_020_hap_��.dds
        /// data\chr\st4_armrtana_��.dds
        /// data\item\m_017_dth2_��.dds
        /// 
        /// For each one of them, we will need to replace it with a readable name and patch every other files pointing to them accordingly
        /// </summary>
        protected override void BgWorker_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            if (Directory.Exists(_GameFolderPath))
            {
                PatchFilename_1();
                PatchFilename_2();
                PatchFilename_3();
                PatchFilename_4();
                PatchFilename_5();
                PatchFilename_6();
                PatchFilename_7();
                PatchFilename_8();
            }
            else
            {
                MessageBox.Show("Directory not found: " + _GameFolderPath, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _BgWorker.ReportProgress(0);
            }
        }
    
        #region Filename Patch

        //data\chr\k_st2_picture_1_��.dds
        private void PatchFilename_1()
        {
            byte[] originalName = new byte[] { 0x6B, 0x5F, 0x73, 0x74, 0x32, 0x5F, 0x70, 0x69, 0x63, 0x74, 0x75, 0x72, 0x65, 0x5F, 0x31, 0x5F, 0x82, 0x93 };
            PatchFile(_GameFolderPath + @"\data\chr\k_st2_picture_0.CLAY", originalName, _PatchData, originalName.Length - 2);
            PatchFile(_GameFolderPath + @"\data\chr\k_st2_picture_1.CLAY", originalName, _PatchData, originalName.Length - 2);
            PatchFile(_GameFolderPath + @"\data\chr\k_st2_picture_2A.CLAY", originalName, _PatchData, originalName.Length - 2);
            PatchFile(_GameFolderPath + @"\data\chr\k_st2_picture_2B.CLAY", originalName, _PatchData, originalName.Length - 2);
            PatchFile(_GameFolderPath + @"\data\chr\k_st2_picture_2C.CLAY", originalName, _PatchData, originalName.Length - 2);
            CopyFileToReadableName(_GameFolderPath + @"\data\chr", "26AA5416DF651D370558219B897602D7", "k_st2_picture_1_" + _ReplacementString + ".dds");
            WriteLog("");
            _BgWorker.ReportProgress(1);
        }

        //data\chr\m_006_g��d_d.dds
        private void PatchFilename_2()
        {
            byte[] originalName = new byte[] { 0x6D, 0x5F, 0x30, 0x30, 0x36, 0x5F, 0x67, 0x82, 0x8C, 0x64, 0x5F, 0x64 };
            PatchFile(_GameFolderPath + @"\data\chr\m_006_gld.CLAY", originalName, _PatchData, 7);
            CopyFileToReadableName(_GameFolderPath + @"\data\chr", "EE31B662BE128467DC158775671676C9", "m_006_g" + _ReplacementString + "d_d.dds");
            WriteLog("");
            _BgWorker.ReportProgress(1);
        }

        //data\chr\m_006_g��d_n.dds
        private void PatchFilename_3()
        {
            byte[] originalName = new byte[] { 0x6D, 0x5F, 0x30, 0x30, 0x36, 0x5F, 0x67, 0x82, 0x8C, 0x64, 0x5F, 0x6E };
            PatchFile(_GameFolderPath + @"\data\chr\m_006_gld.CLAY", originalName, _PatchData, 7);
            CopyFileToReadableName(_GameFolderPath + @"\data\chr", "6CFA1215D362841E1B99617529F836CA", "m_006_g" + _ReplacementString + "d_n.dds");
            WriteLog("");
            _BgWorker.ReportProgress(1);
        }

        //data\chr\m_006_g��d_��.dds
        private void PatchFilename_4()
        {
            byte[] originalName = new byte[] { 0x6D, 0x5F, 0x30, 0x30, 0x36, 0x5F, 0x67, 0x82, 0x8C, 0x64, 0x5F, 0x82, 0x93 };
            PatchFile(_GameFolderPath + @"\data\chr\m_006_gld.CLAY", originalName, _PatchData, 7);
            //Double occurence of the character, need a tweak to patch it at 2 different places
            originalName = new byte[] { 0x6D, 0x5F, 0x30, 0x30, 0x36, 0x5F, 0x67, 0x78, 0x78, 0x64, 0x5F, 0x82, 0x93 };
            PatchFile(_GameFolderPath + @"\data\chr\m_006_gld.CLAY", originalName, _PatchData, originalName.Length - 2);
            CopyFileToReadableName(_GameFolderPath + @"\data\chr", "0AEA78372DBEC25498000BD4A49E72D5", "m_006_g" + _ReplacementString + "d_" + _ReplacementString + ".dds");
            WriteLog("");
            _BgWorker.ReportProgress(1);
        }

        //data\chr\m_017_dth2_��.dds
        private void PatchFilename_5()
        {

            byte[] originalName = new byte[] { 0x6D, 0x5F, 0x30, 0x31, 0x37, 0x5F, 0x64, 0x74, 0x68, 0x32, 0x5F, 0x82, 0x92 };
            PatchFile(_GameFolderPath + @"\data\chr\m_017_dth2.CLAY", originalName, _PatchData, originalName.Length - 2);
            CopyFileToReadableName(_GameFolderPath + @"\data\chr", "048E0DC8CA7E49A03F3AD73B70F17E14", "m_017_dth2_" + _ReplacementString + ".dds");
            WriteLog("");
            _BgWorker.ReportProgress(1);
        }

        //data\chr\m_020_hap_��.dds
        private void PatchFilename_6()
        {
            byte[] originalName = new byte[] { 0x6D, 0x5F, 0x30, 0x32, 0x30, 0x5F, 0x68, 0x61, 0x70, 0x5F, 0x82, 0x92 };
            PatchFile(_GameFolderPath + @"\data\chr\m_020_hap.CLAY", originalName, _PatchData, originalName.Length - 2);
            CopyFileToReadableName(_GameFolderPath + @"\data\chr", "048E0DC8CA7E49A03F3AD73B70F17E14", "m_020_hap_" + _ReplacementString + ".dds");
            WriteLog("");
            _BgWorker.ReportProgress(1);
        }

        //data\chr\st4_armrtana_��.dds
        private void PatchFilename_7()
        {
            byte[] originalName = new byte[] { 0x73, 0x74, 0x34, 0x5F, 0x61, 0x72, 0x6D, 0x72, 0x74, 0x61, 0x6E, 0x61, 0x5F, 0x82, 0x8E };
            PatchFile(_GameFolderPath + @"\data\chr\k_st4_armrtana_0.CLAY", originalName, _PatchData, originalName.Length - 2);
            PatchFile(_GameFolderPath + @"\data\chr\k_st4_armrtana_1.CLAY", originalName, _PatchData, originalName.Length - 2);
            CopyFileToReadableName(_GameFolderPath + @"\data\chr", "0CAFD5E2A8EB873BA33DE2677B8CE1D6", "st4_armrtana_" + _ReplacementString + ".dds");
            WriteLog("");
            _BgWorker.ReportProgress(1);
        }

        //data\item\m_017_dth2_��.dds
        private void PatchFilename_8()
        {
            byte[] originalName = new byte[] { 0x6D, 0x5F, 0x30, 0x31, 0x37, 0x5F, 0x64, 0x74, 0x68, 0x32, 0x5F, 0x82, 0x92 };
            PatchFile(_GameFolderPath + @"\data\item\k_emy_sickle2.CLAY", originalName, _PatchData, originalName.Length - 2);
            CopyFileToReadableName(_GameFolderPath + @"\data\item", "048E0DC8CA7E49A03F3AD73B70F17E14", "m_017_dth2_" + _ReplacementString + ".dds");
            WriteLog("");
            _BgWorker.ReportProgress(1);
        }

        private void PatchFile(string FileName, byte[] ArrayToFind, byte[] PatchData, int PatchOffset)
        {
            if (File.Exists(FileName))
            {
                try
                {
                    byte[] bData = File.ReadAllBytes(FileName);

                    using (BinaryWriter bw = new BinaryWriter(File.Open(FileName, FileMode.Open, FileAccess.ReadWrite)))
                    {
                        WriteLog("Patching " + FileName + " ...");

                        int i = 1;
                        foreach (long position in LocateTargetsInBytes(bData, ArrayToFind))
                        {
                            WriteLog("[" + i.ToString("D2") + "] 0x" + position.ToString("X8"));
                            bw.BaseStream.Seek(position + PatchOffset, SeekOrigin.Begin);
                            bw.Write(PatchData);
                            i++;
                        }
                    }
                }
                catch (Exception Ex)
                {
                    MessageBox.Show("Error patching file: " + FileName + "\n\n" + Ex.Message.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    _BgWorker.ReportProgress(0);
                }
            }
            else
            {
                MessageBox.Show("File not found: " + FileName, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                WriteLog("Error ! File not found: " + FileName);
                _BgWorker.ReportProgress(0);
            }
        }

        #endregion

        #region Search in file

        private readonly long[] Empty = new long[0];

        public long[] LocateTargetsInBytes(byte[] self, byte[] candidate)
        {
            if (IsEmptyLocate(self, candidate))
                return Empty;

            var list = new List<long>();

            for (long i = 0; i < self.Length; i++)
            {
                if (!IsMatch(self, i, candidate))
                    continue;

                list.Add(i);
            }

            return list.Count == 0 ? Empty : list.ToArray();
        }

        static bool IsMatch(byte[] array, long position, byte[] candidate)
        {
            if (candidate.Length > (array.Length - position))
                return false;

            for (int i = 0; i < candidate.Length; i++)
                if (array[position + i] != candidate[i])
                    return false;

            return true;
        }

        static bool IsEmptyLocate(byte[] array, byte[] candidate)
        {
            return array == null
                || candidate == null
                || array.Length == 0
                || candidate.Length == 0
                || candidate.Length > array.Length;
        }

        #endregion

        private void CopyFileToReadableName(string FolderPath, string Md5Target, string TargetFileName)
        {
            if (Directory.Exists(FolderPath))
            {
                foreach (string s in Directory.GetFiles(FolderPath))
                {
                    //If the replacement file already exists, skip
                    if (Path.GetFileName(s) == TargetFileName)
                        continue;

                    if (Compute_MD5(s).Equals(Md5Target))
                    {
                        File.Copy(s, FolderPath + @"\" + TargetFileName, true);
                        break;
                    }
                }                
                WriteLog("Succesfully created " + FolderPath + @"\" + TargetFileName);
            }
            else
            {
                WriteLog("Directory not found: " + FolderPath);
            }
        }

        private bool Check_DatafileExists(string BaseFolder)
        {
            if (File.Exists(BaseFolder))
                return true;
            else
                return false;
        }
    }
}
