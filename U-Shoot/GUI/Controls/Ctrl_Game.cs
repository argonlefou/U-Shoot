using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using UsCore.Config;

namespace U_Shoot.GUI.Controls
{
    public partial class Ctrl_Game : UserControl
    {
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        private readonly Color COLOR_UNSELECTED = Color.White;
        private readonly Color COLOR_SELECTED = Color.LightBlue;

        private const string TOOLTIP_CONTROLS_TEXT = "Edit game Inputs";
        private const string TOOLTIP_OPTIONS_TEXT = "Change game Options";
        private const string TOOLTIPE_RUN_TEXT = "Start Game";

        private MouseMoveMessageFilter MouseMessageFilter;

        private bool _HighlightState = true;

        private GameConfig _Config;
        private string _ConfigFilePath;

        [Description("ConfigFilePath"), Category("Data")]
        public string ConfigFilePath
        {
            get => _ConfigFilePath;
            set => _ConfigFilePath = value;
        }

        [Description("Game Name"), Category("Data")]
        public string GameName
        {
            get => Lbl_GameName.Text;
            set => Lbl_GameName.Text = value;
        }       

        public GameConfig GameConfig
        { 
            get { return _Config; }
            set
            {
                _Config = value;
                Lbl_GameName.Text = _Config.Title;

                Btn_Options.Visible = IsBtnOptionsUsed();
                Btn_Controls.Visible = IsBtnControlsUsed();
            }
        }

        #region Events

        [Browsable(true)]
        [Category("Action")]
        [Description("Invoked when user clicks CONTROLS button")]
        public event EventHandler ButtonControlsClick;

        [Browsable(true)]
        [Category("Action")]
        [Description("Invoked when user clicks OPTIONS button")]
        public event EventHandler ButtonOptionsClick;

        [Browsable(true)]
        [Category("Action")]
        [Description("Invoked when user clicks RUN button")]
        public event EventHandler ButtonRunClick;

        private void Btn_Controls_Click(object sender, EventArgs e)
        {
            if (ButtonControlsClick != null)
                ButtonControlsClick(this, e);
        }

        private void Btn_Options_Click(object sender, EventArgs e)
        {
            if (ButtonOptionsClick != null)
                ButtonOptionsClick(this, e);
        }

        private void Btn_Run_Click(object sender, EventArgs e)
        {
            if (!File.Exists(GameConfig.TargetPath))
            {
                string DefaultPath = string.Empty;
                switch (GameConfig.Category)
                {
                    case "Flycast": DefaultPath = ProgramConfig.GetInstance().GetStringOption(Properties.Resources.DEFAULT_FLYCAST_PATH_OPTION); break;
                    case "Lindbergh": DefaultPath = ProgramConfig.GetInstance().GetStringOption(Properties.Resources.DEFAULT_LINUXLOADER_PATH_OPTION); break;
                    case "System 2X6": DefaultPath = ProgramConfig.GetInstance().GetStringOption(Properties.Resources.DEFAULT_PCSX2X6EMULATOR_PATH_OPTION); break;
                    case "System 357": DefaultPath = ProgramConfig.GetInstance().GetStringOption(Properties.Resources.DEFAULT_RPCS3EMULATOR_PATH_OPTION); break;
                    default: break;
                }

                if (string.IsNullOrEmpty(DefaultPath))
                {
                    MessageBox.Show("Target file not found, please check the Game settings", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

            if (ButtonRunClick != null)
                ButtonRunClick(this, e);
        }

        #endregion

        public Ctrl_Game()
        {
            InitializeComponent();
        }
        
        public Ctrl_Game(string ConfigFilePath)
        {
            InitializeComponent();
            _ConfigFilePath = ConfigFilePath;

            _Config = GameConfig.LoadFromDisk(_ConfigFilePath);

            Lbl_GameName.Text = _Config.Title;
            SetHighlightState(false);

            Btn_Controls.Visible = false;
            Btn_Options.Visible = false;
        }

        private void Ctrl_Game_Load(object sender, EventArgs e)
        {
            this.MouseMessageFilter = new MouseMoveMessageFilter();
            this.MouseMessageFilter.TargetControl = this;
            Application.AddMessageFilter(this.MouseMessageFilter);
        }

        public void RemoveMessageFilter()
        {
            Application.RemoveMessageFilter(this.MouseMessageFilter);
        }

        //protected override void Dispose()
        //{
        //    Application.RemoveMessageFilter(this.MouseMessageFilter);
        //}

        private class MouseMoveMessageFilter : IMessageFilter
        {
            public Ctrl_Game TargetControl { get; set; }

            public bool PreFilterMessage(ref Message m)
            {
                //Disabling Highlight effect if the main Form is covered by another DialogBox
                //TargetControl = this
                //1st Parent = FlowLayoutPanel
                //2nd Parent = Main Form
                try
                {

                    IntPtr fg = GetForegroundWindow();
                    if (fg != TargetControl.Parent.Parent.Handle)
                        return false;

                    int numMsg = m.Msg;
                    if (numMsg == 0x0200 /*WM_MOUSEMOVE*/)
                    {
                        //TargetControl.SetText("Control.MousePosition: " + Control.MousePosition.ToString() + ", ClientRectangle=" + TargetControl.PointToClient(Control.MousePosition));
                        if (TargetControl.ClientRectangle.Contains(TargetControl.PointToClient(Control.MousePosition)))
                            TargetControl.SetHighlightState(true);
                        else
                            TargetControl.SetHighlightState(false);
                    }
                }
                catch 
                { 
                    //Exceptions can be raised when the form is closing and parents are removed before that Object, while mouse is moving on it's position
                }
                return false;
            }
        }

        /// <summary>
        /// Change the control display according to the mouse position (over it or not)
        /// </summary>
        /// <param name="Highlight"></param>
        public void SetHighlightState(bool Highlight)
        {
            if (_HighlightState != Highlight)
            {
                _HighlightState = Highlight;
                if (Highlight)
                {
                    this.BackColor = COLOR_SELECTED;
                    Btn_Controls.Visible = IsBtnControlsUsed();
                    Btn_Options.Visible = IsBtnOptionsUsed();
                    Btn_Run.Visible = true;
                }
                else
                {
                    this.BackColor = COLOR_UNSELECTED;
                    Btn_Controls.Visible = false;
                    Btn_Options.Visible = false;
                    Btn_Run.Visible = false;
                }
            }
        }

        private bool IsBtnControlsUsed()
        {
            if (_Config.System_Inputs.Count == 0 && _Config.Players_Inputs.Count == 0)
                return false;
            return true;
        }

        private bool IsBtnOptionsUsed()
        {
            /*if (_Config.Options.Count == 0) 
                return false;*/

            //ALWAYS used !! Because of TargetPath !!
            return true;
        }

        private void Btn_Controls_MouseHover(object sender, EventArgs e)
        {
            toolTip1.Show(TOOLTIP_CONTROLS_TEXT, Btn_Controls);
        }

        private void Btn_Options_MouseHover(object sender, EventArgs e)
        {
            toolTip1.Show(TOOLTIP_OPTIONS_TEXT, Btn_Options);
        }

        private void Btn_Run_MouseHover(object sender, EventArgs e)
        {
            toolTip1.Show(TOOLTIPE_RUN_TEXT + " [-game=" + Path.GetFileName(_ConfigFilePath) + "]", Btn_Run);
        }
    }
}
