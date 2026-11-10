using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using U_Shoot.GUI.Controls;
using UsCore.Config;

namespace U_Shoot.GUI
{
    public partial class Wnd_GameOptions : Form
    {
        private GameConfig _Config;
        private string _ConfigFilePath;

        private Ctrl_TargetPath _CtrlTarget;

        private Color _ColorEven = Color.FromArgb(222, 225, 231);
        private Color _ColorOdd = Color.FromArgb(241, 241, 241);

        public Wnd_GameOptions(GameConfig Config, string ConfigFilePath)
        {
            InitializeComponent();

            //Remove Horizontal Scrollbar on LowLayoutPanel
            Flp_Options.AutoScroll = false;
            Flp_Options.HorizontalScroll.Enabled = false;
            Flp_Options.HorizontalScroll.Visible = false;
            Flp_Options.HorizontalScroll.Maximum = 0;
            Flp_Options.AutoScroll = true;

            _Config = Config;
            _ConfigFilePath = ConfigFilePath;

            this.Text = _Config.Title + " | Options";

            _CtrlTarget = new Ctrl_TargetPath(_Config.TargetFileName,  _Config.TargetPath, _Config.Category);
            AddFlowLayoutItem(_CtrlTarget);


            foreach (BaseConfig_Option Option in _Config.Options)
            {
                if (Option.Type.Equals(BaseConfig_Option.ItemType.Bool.ToString()))
                {
                    Ctrl_OptionBool c = new Ctrl_OptionBool(Option.Name, Option.Value);
                    AddFlowLayoutItem(c);
                }
                else if (Option.Type.Equals(BaseConfig_Option.ItemType.Numeric.ToString()))
                {
                    Ctrl_OptionNumeric c = new Ctrl_OptionNumeric(Option.Name, Option.Value);
                    AddFlowLayoutItem(c);
                }
                else if (Option.Type.Equals(BaseConfig_Option.ItemType.Text.ToString()))
                {
                    Ctrl_OptionText c = new Ctrl_OptionText(Option.Name, Option.Value);
                    AddFlowLayoutItem(c);
                }
                else if (Option.Type.Equals(BaseConfig_Option.ItemType.List.ToString()))
                {
                    Ctrl_OptionList c = new Ctrl_OptionList(Option.Name, Option.Value);
                    AddFlowLayoutItem(c);
                }
                else if (Option.Type.Equals(BaseConfig_Option.ItemType.Button.ToString()))
                {
                    Ctrl_OptionButton c = new Ctrl_OptionButton(Option.Name, Option.Value);
                    c.ButtonClick += Btn_Option_Click;
                    AddFlowLayoutItem(c);
                }
                else if (Option.Type.Equals(BaseConfig_Option.ItemType.Browse.ToString()))
                {
                    Ctrl_OptionBrowse c = new Ctrl_OptionBrowse(Option.Name, Option.Value);
                    AddFlowLayoutItem(c);
                }
            }

            ScaleFlowLayoutItems();

            //Contratsing background with the last item displayed
            if (Flp_Options.Controls.Count % 2 == 0)
                Flp_Options.BackColor = _ColorOdd;
            else
                Flp_Options.BackColor = _ColorEven;
        }

        private void AddFlowLayoutItem(UserControl TargetControl)
        {
            TargetControl.Width = Flp_Options.Width - 2;
            Flp_Options.Controls.Add(TargetControl);
            if (Flp_Options.Controls.Count % 2 == 0)
                TargetControl.BackColor = _ColorEven;
            else
                TargetControl.BackColor = _ColorOdd;
        }

        /// <summary>
        /// Make sure Items are taking the container width, depending on scrollbar or size change...
        /// </summary>
        private void ScaleFlowLayoutItems()
        {
            Flp_Options.SuspendLayout();
            foreach (Control c in Flp_Options.Controls)
            {
                if (Flp_Options.VerticalScroll.Visible)
                    c.Width = Flp_Options.Width - SystemInformation.VerticalScrollBarWidth - 2;
                else
                    c.Width = Flp_Options.Width - 2;
            }
            Flp_Options.ResumeLayout();
        }

        private void Btn_Save_Click(object sender, System.EventArgs e)
        {
            try
            {
                _Config.TargetPath = _CtrlTarget.GetOptionValue();

                foreach (Control c in Flp_Options.Controls)
                {
                    if (c is Ctrl_Option)
                    {
                        foreach (BaseConfig_Option ConfigOption in _Config.Options)
                        {
                            if (ConfigOption.Name.Equals(((Ctrl_Option)c).GetOptionName()))
                            {
                                ConfigOption.Value = ((Ctrl_Option)c).GetOptionValue();
                                break;
                            }
                        }
                    }
                }

                if (!_Config.SaveConfig(_ConfigFilePath))
                    MessageBox.Show("Error saving game file !", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);

                Close();
            }
            catch(Exception Ex)
            {
                MessageBox.Show(Ex.Message.ToString(), this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            
        }

        private void Btn_Cancel_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void Flp_Options_SizeChanged(object sender, EventArgs e)
        {
            ScaleFlowLayoutItems();
        }

        private void Btn_Option_Click(object sender, EventArgs e)
        {
            Ctrl_OptionButton Control = (Ctrl_OptionButton)sender;
            if (_CtrlTarget.GetOptionValue() != string.Empty)
            {
                string GameFolderPath = Path.GetDirectoryName(_CtrlTarget.GetOptionValue());
                if (Directory.Exists(GameFolderPath))
                {
                    string sType = "U_Shoot.Tools." + Control.GetOptionValue();

                    Type ToolType = Type.GetType("U_Shoot.Tools." + Control.GetOptionValue());
                    var myObj = Activator.CreateInstance(ToolType, new object[] { GameFolderPath, _Config });
                    Form w = (Form)myObj;
                    w.ShowDialog();
                }
                else
                {
                    MessageBox.Show("Target directory not found : " + GameFolderPath, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }                
            }
            else
            {
                MessageBox.Show("Please fill in the target path before using that button", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }
    }
}
