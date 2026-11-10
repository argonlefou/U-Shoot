using System.Windows.Forms;

namespace U_Shoot.GUI.Controls
{
    public partial class Ctrl_OptionNumeric : Ctrl_Option
    {
        public Ctrl_OptionNumeric() : this("Option Name", "0")
        {
            
        }
        public Ctrl_OptionNumeric(string OptionName, string OptionValue)
        {
            InitializeComponent();
            SetOptionName(OptionName);
            int i = 0;
            int.TryParse(OptionValue, out i);
            Txt_Value.Value = i;
        }


        public override string GetOptionValue()
        {
            return Txt_Value.Value.ToString();
        }

        protected override void DrawLayout()
        {            
            Txt_Value.Location = new System.Drawing.Point(this.Width - Txt_Value.Width - 10, Txt_Value.Location.Y);
        }

        private void Txt_Value_Enter(object sender, System.EventArgs e)
        {
            Txt_Value.Select(0, Txt_Value.Text.Length);
        }

        private void Txt_Value_Click(object sender, System.EventArgs e)
        {
            Txt_Value.Select(0, Txt_Value.Text.Length);
        }
    }
}
