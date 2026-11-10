using System.Windows.Forms;

namespace U_Shoot.GUI.Controls
{
    public partial class Ctrl_OptionList : Ctrl_Option
    {
        public Ctrl_OptionList() : this ("Option Name", "On;On,Off")
        {            
        }
        public Ctrl_OptionList(string OptionName, string OptionValue)
        {
            InitializeComponent();
            SetOptionName(OptionName);

            string sValue = string.Empty;

            //Displayed value is the first in the string
            string[] sBuffer = OptionValue.Split(';');
            if (sBuffer.Length > 0)
                sValue = sBuffer[0];

            //Rest of options are in a comma separated list
            if (sBuffer.Length > 1)
            {
                foreach (string s in sBuffer[1].Split(','))
                    Cbox_Option.Items.Add(s);
            }
            Cbox_Option.Text = sValue;

            //Adjusting Combobox size based on inner items width
            int maxWidth = 0, temp = 0;
            foreach (var obj in Cbox_Option.Items)
            {
                temp = TextRenderer.MeasureText(obj.ToString(), Cbox_Option.Font).Width;
                if (temp > maxWidth)
                {
                    maxWidth = temp;
                }
            }
            Cbox_Option.Width = maxWidth + 20;
        }

        public override string GetOptionValue()
        {
            string sValue = Cbox_Option.Text + ";";
            for (int i = 0; i < Cbox_Option.Items.Count; i++)
            {
                sValue += Cbox_Option.Items[i].ToString();
                if (i != Cbox_Option.Items.Count -1)
                    sValue += ",";
            }
            return sValue;
        }

        //Make sure Combobox is at the right most position 
        protected override void DrawLayout()
        {
            Cbox_Option.Location = new System.Drawing.Point(this.Width - Cbox_Option.Width - 10, Cbox_Option.Location.Y);
        }
    }
}
