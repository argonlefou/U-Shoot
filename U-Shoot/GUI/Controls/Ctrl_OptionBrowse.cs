using System;
using System.IO;
using System.Windows.Forms;

namespace U_Shoot.GUI.Controls
{
    public partial class Ctrl_OptionBrowse : Ctrl_Option
    {
        private string _TargetFileName = string.Empty;

        public Ctrl_OptionBrowse() : this("Option Name", string.Empty)
        {
        }
        public Ctrl_OptionBrowse(string OptionName, string OptionValue)
        {
            InitializeComponent();
            SetOptionName(OptionName);

            string[] sBuffer = OptionName.Split('[', ']');
            if (sBuffer.Length > 0)
                _TargetFileName = sBuffer[1];

            Txt_OptionValue.Text = OptionValue;
        }
        public override string GetOptionValue()
        {
            return Txt_OptionValue.Text;
        }

        //Make sure Label option is always visible, textbox using the leftover width
        protected override void DrawLayout()
        {
            Btn_Browse.Location = new System.Drawing.Point(this.Width - Btn_Browse.Width - 10, Btn_Browse.Location.Y);

            Txt_OptionValue.Width = this.Width - TextRenderer.MeasureText(Lbl_OptionName.Text, Lbl_OptionName.Font).Width - Btn_Browse.Width - (10 * 4);
            Txt_OptionValue.Location = new System.Drawing.Point(this.Width - Txt_OptionValue.Width - Btn_Browse.Width - (10 * 2), Txt_OptionValue.Location.Y);
        }

        private void Btn_Browse_Click(object sender, EventArgs e)
        {
            openFileDialog1.Title = "";
            openFileDialog1.FileName = "";

            if (_TargetFileName != string.Empty)
            {
                openFileDialog1.Title = "Select " + _TargetFileName;

                string TargetExtension = Path.GetExtension(_TargetFileName);
                if (TargetExtension != string.Empty)
                    openFileDialog1.Filter = _TargetFileName + "|" + _TargetFileName + "|" + TargetExtension + " files (*" + TargetExtension + ")|*" + TargetExtension + "|All files (*.*)|*.*";
                else
                    openFileDialog1.Filter = _TargetFileName + "|" + _TargetFileName + "|All files (*.*)|*.*";
            }

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                Txt_OptionValue.Text = openFileDialog1.FileName;
            }
        }
    }
}
