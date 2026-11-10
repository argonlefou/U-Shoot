using System;
using UsCore.Config;

namespace U_Shoot.Tools.GoldenGun
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
        private const UInt32 JCONFIG_OPTIONS_LENGTH = 0x60;
        
        public TestMenu() : base("", "sgg.zip", null)
        { }

        public TestMenu(string GameFolder, GameConfig Config) : base (GameFolder, "sgg.zip", Config)
        {
            InitializeComponent();
            this.Text = "SEGA Golden gun - TEST Menu | v" + VERSION;
        }

        protected override void OnLoad()
        {
            //No change
            base.OnLoad();
        }        

        protected override void DisplayJconfigEepromValues()
        {
            Cbox_FreePlay.SelectedIndex = _EepromContent[JCONFIG_CREDITS_OFFSET + 10];
            cbox_CreditStart.SelectedIndex = _EepromContent[JCONFIG_CREDITS_OFFSET + 15] - 1;
            Cbox_CreditContinue.SelectedIndex = _EepromContent[JCONFIG_CREDITS_OFFSET + 16] - 1;
            Cbox_CreditPremium.SelectedIndex = _EepromContent[JCONFIG_CREDITS_OFFSET + 17] - 1;
            Cbox_CreditExchange.SelectedIndex = 0;

            Cbox_Difficulty.SelectedIndex = _EepromContent[JCONFIG_OPTIONS_OFFSET + 0x08];
            Cbox_Advertise.SelectedIndex = _EepromContent[JCONFIG_OPTIONS_OFFSET + 0x10];
            Cbox_LifeInit.SelectedIndex = _EepromContent[JCONFIG_OPTIONS_OFFSET + 0x29] - 1;
            Cbox_LifeMax.SelectedIndex = _EepromContent[JCONFIG_OPTIONS_OFFSET + 0x2A] - 1;
            Cbox_Revival.SelectedIndex = _EepromContent[JCONFIG_OPTIONS_OFFSET + 0x2C];
            Cbox_P1Recoil.SelectedIndex = _EepromContent[JCONFIG_OPTIONS_OFFSET + 0x34];
            Cbox_P2Recoil.SelectedIndex = _EepromContent[JCONFIG_OPTIONS_OFFSET + 0x38];
            Cbox_Voice.SelectedIndex = _EepromContent[JCONFIG_OPTIONS_OFFSET + 0x28] / 2;
            Cbox_Cabinet.SelectedIndex = Cbox_Voice.SelectedIndex;

            Cbox_PremiumStart.SelectedIndex = _EepromContent[JCONFIG_OPTIONS_OFFSET + 0x14];
            Cbox_AtkPwrPlus.SelectedIndex = _EepromContent[JCONFIG_OPTIONS_OFFSET + 0x18] - 1;
            Cbox_LifePlus.SelectedIndex = _EepromContent[JCONFIG_OPTIONS_OFFSET + 0x19] - 1;
            Cbox_MagazinePlus.SelectedIndex = _EepromContent[JCONFIG_OPTIONS_OFFSET + 0x1A] - 1;
            Cbox_Exchange.SelectedIndex = 1;
            Cbox_ScorePlus.SelectedIndex = 0;
            Cbox_Continue.SelectedIndex = _EepromContent[JCONFIG_OPTIONS_OFFSET + 0x30];
        }


        private void UpdateHeaderData(UInt32 DataStartOffset)
        {
            _EepromContent[0x0C] = (byte)((Cbox_Voice.SelectedIndex) * 6 + 2);
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
            _EepromContent[DataStartOffset + 0x0C] = (byte)((Cbox_Voice.SelectedIndex) * 6 + 2);            
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
            _EepromContent[DataStartOffset + 17] = (byte)(Cbox_CreditPremium.SelectedIndex + 1);
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
            _EepromContent[DataStartOffset + 0x45] = 0x06;
            _EepromContent[DataStartOffset + 0x47] = 0x0A;
            _EepromContent[DataStartOffset + 0x49] = 0x80;
            _EepromContent[DataStartOffset + 0x4B] = 0x80;
            _EepromContent[DataStartOffset + 0x4D] = 0xFA;
            _EepromContent[DataStartOffset + 0x4F] = 0xF5;

            _EepromContent[DataStartOffset + 0x45 + 0x10] = 0x06;
            _EepromContent[DataStartOffset + 0x47 + 0x10] = 0x0A;
            _EepromContent[DataStartOffset + 0x49 + 0x10] = 0x80;
            _EepromContent[DataStartOffset + 0x4B + 0x10] = 0x80;
            _EepromContent[DataStartOffset + 0x4D + 0x10] = 0xFA;
            _EepromContent[DataStartOffset + 0x4F + 0x10] = 0xF5;

            _EepromContent[DataStartOffset + 0x08] = (byte)Cbox_Difficulty.SelectedIndex;
            _EepromContent[DataStartOffset + 0x10] = (byte)Cbox_Advertise.SelectedIndex;
            _EepromContent[DataStartOffset + 0x29] = (byte)(Cbox_LifeInit.SelectedIndex + 1);
            _EepromContent[DataStartOffset + 0x2A] = (byte)(Cbox_LifeMax.SelectedIndex + 1);
            _EepromContent[DataStartOffset + 0x2C] = (byte)Cbox_Revival.SelectedIndex;
            _EepromContent[DataStartOffset + 0x34] = (byte)Cbox_P1Recoil.SelectedIndex;
            _EepromContent[DataStartOffset + 0x38] = (byte)Cbox_P2Recoil.SelectedIndex;
            //Language needs 3 changes + Header change
            //_EepromContent[DataStartOffset + 0x14] = (byte)(Cbox_Voice.SelectedIndex);
            _EepromContent[DataStartOffset + 0x28] = (byte)(Cbox_Voice.SelectedIndex * 2 + 1);
            _EepromContent[DataStartOffset + 0x41] = (byte)(Cbox_Voice.SelectedIndex);
            _EepromContent[DataStartOffset + 0x42] = (byte)(1 - Cbox_Voice.SelectedIndex);
            _EepromContent[DataStartOffset + 0x14] = (byte)Cbox_PremiumStart.SelectedIndex;
            _EepromContent[DataStartOffset + 0x18] = (byte)(Cbox_AtkPwrPlus.SelectedIndex + 1);
            _EepromContent[DataStartOffset + 0x19] = (byte)(Cbox_LifePlus.SelectedIndex + 1);
            _EepromContent[DataStartOffset + 0x1A] = (byte)(Cbox_MagazinePlus.SelectedIndex + 1);
            _EepromContent[DataStartOffset + 0x30] = (byte)Cbox_Continue.SelectedIndex;
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
            UpdateOptionsData(JCONFIG_OPTIONS_OFFSET + 0x80);

            SaveFile();

            Close();
        }

        private void Btn_Close_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void Cbox_Voice_SelectedIndexChanged(object sender, EventArgs e)
        {
            Cbox_Cabinet.SelectedIndex = Cbox_Voice.SelectedIndex;
        }        
    }
}
