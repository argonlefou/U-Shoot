namespace U_Shoot.GUI.Controls
{
    public partial class Ctrl_OptionBool : Ctrl_Option
    {

        public Ctrl_OptionBool() : this ("Option Name", "False")
        {
        }
        public Ctrl_OptionBool(string OptionName, string OptionValue)
        {
            InitializeComponent();
            SetOptionName(OptionName);
            Cbox_Option.Checked = false;
            bool b = false;
            bool.TryParse(OptionValue, out b);
            Cbox_Option.Checked = b;
        }

        public override string GetOptionValue()
        {
            return Cbox_Option.Checked.ToString();
        }

        protected override void DrawLayout()
        {
            Cbox_Option.Location = new System.Drawing.Point(this.Width - Cbox_Option.Width - 10, Cbox_Option.Location.Y);
        }
    }
}
