using System.Windows.Forms;

namespace U_Shoot.GUI.Controls
{
    public partial class Ctrl_OptionText : Ctrl_Option
    {
        public Ctrl_OptionText() : this("Option Name", string.Empty)
        {            
        }
        public Ctrl_OptionText(string OptionName, string OptionValue)
        {
            InitializeComponent();
            SetOptionName(OptionName);
            Txt_OptionValue.Text = OptionValue;
        }
        public override string GetOptionValue()
        {
            return Txt_OptionValue.Text;
        }

        //Make sure Label option is always visible, textbox using the leftover width
        protected override void DrawLayout() 
        {
            Txt_OptionValue.Width = this.Width - TextRenderer.MeasureText(Lbl_OptionName.Text, Lbl_OptionName.Font).Width - (10 * 3);
            Txt_OptionValue.Location = new System.Drawing.Point(this.Width - Txt_OptionValue.Width - 10, Txt_OptionValue.Location.Y);
        }
    }
}
