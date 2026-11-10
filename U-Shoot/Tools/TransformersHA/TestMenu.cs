using System;
using UsCore.Config;

namespace U_Shoot.Tools.TransformersHA
{
    public partial class TestMenu : BaseTestMenu
    {
        private const string VERSION = "3.0";

        private const UInt32 JCONFIG_HEADER_OFFSET = 0x00;
        private const UInt32 JCONFIG_HEADER_LENGTH = 0x1C;
        private const UInt32 JCONFIG_HEADER2_OFFSET_1 = 0x80;
        private const UInt32 JCONFIG_HEADER2_OFFSET_2 = 0x280;
        private const UInt32 JCONFIG_HEADER2_LENGTH = 0x0C;
        private const UInt32 JCONFIG_CREDITS_OFFSET = 0x20;
        private const UInt32 JCONFIG_CREDITS_LENGTH = 0x1C;        
        private const UInt32 JCONFIG_OPTIONS_OFFSET = 0x1000;
        private const UInt32 JCONFIG_OPTIONS_LENGTH = 0x64;
        
        public TestMenu() : base ("", "Tha.zip", null)
        { }

        public TestMenu(string GameFolder, GameConfig Config) : base (GameFolder, "Tha.zip", Config)
        {
            InitializeComponent();
            this.Text = "Transformers Human Alliance - TEST Menu | v" + VERSION;
        }

        protected override void OnLoad()
        {
            //No change
            base.OnLoad();
        }

        protected override void DisplayJconfigEepromValues()
        {
            Cbox_Region.SelectedIndex = _EepromContent[JCONFIG_OPTIONS_OFFSET + 0x28];

            Cbox_FreePlay.SelectedIndex = _EepromContent[JCONFIG_CREDITS_OFFSET + 10];
            cbox_CreditStart.SelectedIndex = _EepromContent[JCONFIG_CREDITS_OFFSET + 15] - 1;
            Cbox_CreditContinue.SelectedIndex = _EepromContent[JCONFIG_CREDITS_OFFSET + 16] - 1;
            Cbox_CreditJoin.SelectedIndex = _EepromContent[JCONFIG_CREDITS_OFFSET + 17] - 1;

            Cbox_Difficulty.SelectedIndex = _EepromContent[JCONFIG_OPTIONS_OFFSET + 0x08];
            Cbox_Advertise.SelectedIndex = _EepromContent[JCONFIG_OPTIONS_OFFSET + 0x10];
            Cbox_Revival.SelectedIndex = _EepromContent[JCONFIG_OPTIONS_OFFSET + 0x2C];
            Cbox_P1Recoil.SelectedIndex = _EepromContent[JCONFIG_OPTIONS_OFFSET + 0x34];
            Cbox_P2Recoil.SelectedIndex = _EepromContent[JCONFIG_OPTIONS_OFFSET + 0x38];

            Cbox_ContinueCountdown.SelectedIndex = _EepromContent[JCONFIG_OPTIONS_OFFSET + 0x31];
            Cbox_KidsMode.SelectedIndex = _EepromContent[JCONFIG_OPTIONS_OFFSET + 0x44];
            Cbox_LaserSight.SelectedIndex = _EepromContent[JCONFIG_OPTIONS_OFFSET + 0x14];
            Cbox_StageSelect.SelectedIndex = _EepromContent[JCONFIG_OPTIONS_OFFSET + 0x48];
            Cbox_Swipe.SelectedIndex = 0;    
        }

        private void UpdateHeaderData(UInt32 DataStartOffset)
        {
            _EepromContent[0x0C] = (byte)(1 << Cbox_Region.SelectedIndex);
            byte[] UncheckedData = new byte[JCONFIG_HEADER_LENGTH];
            for (int i = 0; i < JCONFIG_HEADER_LENGTH; i++)
            {
                UncheckedData[i] = _EepromContent[DataStartOffset + i + 4];
            }
            UInt32 Checksum = CalculateChecksum(UncheckedData);
            _EepromContent[DataStartOffset] = BitConverter.GetBytes(Checksum)[0];
            _EepromContent[DataStartOffset + 1] = BitConverter.GetBytes(Checksum)[1];
            _EepromContent[DataStartOffset + 2] = BitConverter.GetBytes(Checksum)[2];
            _EepromContent[DataStartOffset + 3] = BitConverter.GetBytes(Checksum)[3];
        }

        private void UpdateHeaderData2(UInt32 DataStartOffset)
        {
            _EepromContent[DataStartOffset + 0x0C] = (byte)(1 << Cbox_Region.SelectedIndex);
            byte[] UncheckedData = new byte[JCONFIG_HEADER2_LENGTH];
            for (int i = 0; i < JCONFIG_HEADER2_LENGTH; i++)
            {
                UncheckedData[i] = _EepromContent[DataStartOffset + i + 4];
            }
            UInt32 Checksum = CalculateChecksum(UncheckedData);
            _EepromContent[DataStartOffset] = BitConverter.GetBytes(Checksum)[0];
            _EepromContent[DataStartOffset + 1] = BitConverter.GetBytes(Checksum)[1];
            _EepromContent[DataStartOffset + 2] = BitConverter.GetBytes(Checksum)[2];
            _EepromContent[DataStartOffset + 3] = BitConverter.GetBytes(Checksum)[3];
        }

        private void UpdateCreditsData(UInt32 DataStartOffset)
        {
            _EepromContent[DataStartOffset + 10] = (byte)Cbox_FreePlay.SelectedIndex;
            _EepromContent[DataStartOffset + 15] = (byte)(cbox_CreditStart.SelectedIndex + 1);
            _EepromContent[DataStartOffset + 16] = (byte)(Cbox_CreditContinue.SelectedIndex + 1);
            _EepromContent[DataStartOffset + 17] = (byte)(Cbox_CreditJoin.SelectedIndex + 1);
            byte[] UncheckedData = new byte[JCONFIG_CREDITS_LENGTH];
            for (int i = 0; i < JCONFIG_CREDITS_LENGTH; i++)
            {
                UncheckedData[i] = _EepromContent[DataStartOffset + i + 4];
            }

            UInt32 Checksum = CalculateChecksum(UncheckedData);
            _EepromContent[DataStartOffset] = BitConverter.GetBytes(Checksum)[0];
            _EepromContent[DataStartOffset + 1] = BitConverter.GetBytes(Checksum)[1];
            _EepromContent[DataStartOffset + 2] = BitConverter.GetBytes(Checksum)[2];
            _EepromContent[DataStartOffset + 3] = BitConverter.GetBytes(Checksum)[3];
        }

        private void UpdateOptionsData(UInt32 DataStartOffset)
        {
            _EepromContent[DataStartOffset + 0x28] = (byte)(Cbox_Region.SelectedIndex);
            if (Cbox_Region.SelectedIndex == 3)
                _EepromContent[DataStartOffset + 0x41] = 1;
            else
                _EepromContent[DataStartOffset + 0x41] = 0;

            _EepromContent[DataStartOffset + 0x08] = (byte)Cbox_Difficulty.SelectedIndex;
            _EepromContent[DataStartOffset + 0x10] = (byte)Cbox_Advertise.SelectedIndex;
            _EepromContent[DataStartOffset + 0x48] = (byte)Cbox_Revival.SelectedIndex;
            _EepromContent[DataStartOffset + 0x34] = (byte)Cbox_P1Recoil.SelectedIndex;
            _EepromContent[DataStartOffset + 0x38] = (byte)Cbox_P2Recoil.SelectedIndex;
            _EepromContent[DataStartOffset + 0x31] = (byte)Cbox_ContinueCountdown.SelectedIndex;
            _EepromContent[DataStartOffset + 0x44] = (byte)Cbox_KidsMode.SelectedIndex;
            _EepromContent[DataStartOffset + 0x14] = (byte)Cbox_LaserSight.SelectedIndex;
            _EepromContent[DataStartOffset + 0x2C] = (byte)Cbox_StageSelect.SelectedIndex;

            byte[] UncheckedData = new byte[JCONFIG_OPTIONS_LENGTH];
            for (int i = 0; i < JCONFIG_OPTIONS_LENGTH; i++)
            {
                UncheckedData[i] = _EepromContent[DataStartOffset + i + 4];
            }
            UInt32 Checksum = CalculateChecksum(UncheckedData);
            _EepromContent[DataStartOffset] = BitConverter.GetBytes(Checksum)[0];
            _EepromContent[DataStartOffset + 1] = BitConverter.GetBytes(Checksum)[1];
            _EepromContent[DataStartOffset + 2] = BitConverter.GetBytes(Checksum)[2];
            _EepromContent[DataStartOffset + 3] = BitConverter.GetBytes(Checksum)[3];
        }

        private void Btn_Save_Click(object sender, EventArgs e)
        {
            UpdateHeaderData(JCONFIG_HEADER_OFFSET);
            UpdateHeaderData2(JCONFIG_HEADER2_OFFSET_1);
            UpdateHeaderData2(JCONFIG_HEADER2_OFFSET_2);

            UpdateCreditsData(JCONFIG_CREDITS_OFFSET);
            UpdateCreditsData(JCONFIG_CREDITS_OFFSET + 0x200);
            UpdateOptionsData(JCONFIG_OPTIONS_OFFSET);
            UpdateOptionsData(JCONFIG_OPTIONS_OFFSET + 0x100);

            SaveFile();

            Close();
        }

        private void Btn_Close_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
