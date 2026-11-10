using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace U_Shoot.GUI.Controls
{
    public partial class Ctrl_OptionButton : Ctrl_Option
    {
        [Browsable(true)]
        [Category("Action")]
        [Description("Invoked when user clicks button")]
        public event EventHandler ButtonClick;

        public Ctrl_OptionButton() : this("Button Text", "")
        {

        }
        public Ctrl_OptionButton(string OptionName, string OptionValue)
        {
            InitializeComponent();
            SetOptionName("");

            this.Height += 10;
            Btn_Option.Height += 10;

            Btn_Option.Text = OptionName;
            Btn_Option.Width = TextRenderer.MeasureText(OptionName, Btn_Option.Font).Width + 40;    
            Btn_Option.Tag = OptionValue;

            if (OptionName.Contains("TEST"))
            {
                Btn_Option.Width += 10;
                Btn_Option.TextAlign = System.Drawing.ContentAlignment.MiddleRight;

                Btn_Option.Image = Properties.Resources.chip_25px;
                Btn_Option.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            }
            else if (OptionName.Contains("Unity Plugin"))
            {
                Btn_Option.Width += 10;
                Btn_Option.TextAlign = System.Drawing.ContentAlignment.MiddleRight;

                Btn_Option.Image = Properties.Resources.Unity_25px;
                Btn_Option.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            }
        }

        public override string GetOptionValue()
        {
            return Btn_Option.Tag.ToString();
        }

        public void EnableButton(bool Enabled)
        {
            Btn_Option.Enabled = Enabled;
        }

        private void Btn_Option_Click(object sender, EventArgs e)
        {
            //bubble the event up to the parent
            if (ButtonClick != null)
                ButtonClick(this, e);
        }

        protected override void DrawLayout()
        {
            Btn_Option.Location = new System.Drawing.Point((this.Width - Btn_Option.Width) / 2, Btn_Option.Location.Y);
        }   
    }
}
