using System;
using System.Collections.Generic;
using System.Windows.Forms;
using U_Shoot.GUI.Controls;
using UsCore.Config;

namespace U_Shoot.GUI
{
    public partial class Wnd_Inputs : Form
    {
        private const int FLP_WIDTH_MARGIN = 15;

        private List<BaseConfig_Input> _SystemInputs;
        private List<BaseConfig_Player> _PlayerInputs;

        public List<BaseConfig_Input> SystemInputs
        { get { return _SystemInputs; } }

        public List<BaseConfig_Player> PlayerInputs
        { get { return _PlayerInputs; } }

        private FlowLayoutPanel[] _FlpPlayers = new FlowLayoutPanel[4];

        public Wnd_Inputs(string Title, BaseConfig SourceConfig)
        {
            InitializeComponent();

            SetFlowLayoutPanelDesign(Flp_System);
            SetFlowLayoutPanelDesign(Flp_P1);
            SetFlowLayoutPanelDesign(Flp_P2);
            SetFlowLayoutPanelDesign(Flp_P3);
            SetFlowLayoutPanelDesign(Flp_P4);
            _FlpPlayers[0] = Flp_P1;
            _FlpPlayers[1] = Flp_P2;
            _FlpPlayers[2] = Flp_P3;
            _FlpPlayers[3] = Flp_P4;

            this.Text = Title;

            //Program config will use all tabs
            if (SourceConfig.GetType() == typeof(ProgramConfig))
            {
                //Nothing to do
            }
            //Game config will not use some players tab according to the config itself
            else if (SourceConfig.GetType() == typeof(GameConfig))
            {
                int PlayersNum = SourceConfig.Players_Inputs.Count;
                for (int i = 4; i > PlayersNum; i--)
                {
                    TabControl_Inputs.TabPages.RemoveAt(i);
                }
            }

            //Populating Tabs
            foreach (BaseConfig_Input Input in SourceConfig.System_Inputs)
            {
                if (Input is InputButton)
                {
                    InputButton kButton = (InputButton)Input;
                    Ctrl_InputButton b = new Ctrl_InputButton(kButton.Keys);
                    b.ButtonName = Input.Name;
                    b.Width = Flp_System.Width - FLP_WIDTH_MARGIN;

                    if (this.Text != Properties.Resources.DEFAULT_INPUTS_WND_TITLE)
                    {
                        foreach (BaseConfig_Input DefaultInput in ProgramConfig.GetInstance().System_Inputs)
                        {
                            if (kButton.Name.Equals(DefaultInput.Name) && DefaultInput.GetNumberOfEntries() > 0)
                            {
                                b.ShowDefaultIcon(true);
                                break;
                            }
                        }
                    }

                    Flp_System.Controls.Add(b);
                }
            }
            foreach (BaseConfig_Player Player in SourceConfig.Players_Inputs)
            {
                foreach (BaseConfig_Input Input in Player.Inputs)
                {
                    if (Input is InputButton)
                    {
                        InputButton kButton = (InputButton)Input;
                        Ctrl_InputButton b = new Ctrl_InputButton(kButton.Keys);
                        b.ButtonName = Input.Name;
                        b.Width = _FlpPlayers[Player.Id - 1].Width - FLP_WIDTH_MARGIN;

                        if (this.Text != Properties.Resources.DEFAULT_INPUTS_WND_TITLE)
                        {
                            for (int i = 0; i < ProgramConfig.DEFAULT_PLAYER_COUNT; i++)
                            {
                                if (ProgramConfig.GetInstance().Players_Inputs[i].Id == Player.Id)
                                {
                                    foreach (BaseConfig_Input DefaultInput in ProgramConfig.GetInstance().Players_Inputs[i].Inputs)
                                    {
                                        if (kButton.Name.Equals(DefaultInput.Name) && DefaultInput.GetNumberOfEntries() > 0)
                                        {
                                            b.ShowDefaultIcon(true);
                                            break;
                                        }
                                    }
                                }
                            }
                        }

                        _FlpPlayers[Player.Id - 1].Controls.Add(b);
                    }
                    else if (Input is InputAxes)
                    {
                        InputAxes kAxis = (InputAxes)Input;
                        Ctrl_InputAxis a = new Ctrl_InputAxis(kAxis.Keys);
                        a.ButtonName = Input.Name;
                        a.Width = _FlpPlayers[Player.Id - 1].Width - FLP_WIDTH_MARGIN;

                        if (this.Text != Properties.Resources.DEFAULT_INPUTS_WND_TITLE)
                        {
                            for (int i = 0; i < ProgramConfig.DEFAULT_PLAYER_COUNT; i++)
                            {
                                if (ProgramConfig.GetInstance().Players_Inputs[i].Id == Player.Id)
                                {
                                    foreach (BaseConfig_Input DefaultInput in ProgramConfig.GetInstance().Players_Inputs[i].Inputs)
                                    {
                                        if (kAxis.Name.Equals(DefaultInput.Name) && DefaultInput.GetNumberOfEntries() > 0)
                                        {
                                            a.ShowDefaultIcon(true);
                                            break;
                                        }
                                    }
                                }
                            }
                        }

                        _FlpPlayers[Player.Id - 1].Controls.Add(a);
                    }

                }
            }

            //Now that everything is populated, enabling scroolling again
            //Enabling that at creation makes items to be misaligned when added during the size change (width change because of screollbar)
            Flp_System.AutoScroll = true;
            Flp_P1.AutoScroll = true;
            Flp_P2.AutoScroll = true;
            Flp_P3.AutoScroll = true;
            Flp_P4.AutoScroll = true;
        }

        private void SetFlowLayoutPanelDesign(FlowLayoutPanel Panel)
        {
            Panel.AutoScroll = false;
            Panel.HorizontalScroll.Enabled = false;
            Panel.HorizontalScroll.Visible = false;
            Panel.HorizontalScroll.Maximum = 0;
            //Panel.AutoScroll = true;
            Panel.SizeChanged += Flp_SizeChanged;
        }

        /// <summary>
        /// Autoscroll is enabled on Flow Layout Panel, and Anchor a control will not work
        /// </summary>
        private void Flp_SizeChanged(object sender, EventArgs e)
        {
            FlowLayoutPanel Flp = (FlowLayoutPanel)sender;
            Flp.SuspendLayout();
            foreach (Control c in Flp.Controls)
            {
                if (Flp.VerticalScroll.Visible)
                    c.Width = Flp.Width - SystemInformation.VerticalScrollBarWidth - FLP_WIDTH_MARGIN;
                else
                    c.Width = Flp.Width - FLP_WIDTH_MARGIN;
            }
            Flp.ResumeLayout();
        }

        private void Btn_Save_Click(object sender, EventArgs e)
        {
            _SystemInputs = new List<BaseConfig_Input>();
            _PlayerInputs = new List<BaseConfig_Player>();

            foreach (Control c in Flp_System.Controls)
            {
                if (c is Ctrl_InputButton)
                {
                    InputButton NewButton = new InputButton(((Ctrl_InputButton)c).ButtonName);
                    foreach (Ctrl_InputButtonItem ButtonItem in ((Ctrl_InputButton)c).GetInputItems())
                    {
                        InputButtonEntry k = new InputButtonEntry();
                        k.DevicePath = ButtonItem.DevicePath;
                        k.DeviceName = ButtonItem.DeviceName;
                        k.ButtonId = ButtonItem.ButtonId;
                        k.ButtonName = ButtonItem.ButtonName;
                        k.DeviceType = ButtonItem.DeviceType.ToString();
                        NewButton.Keys.Add(k);
                    }
                    _SystemInputs.Add(NewButton);
                }
            }

            for (int i = 1; i < TabControl_Inputs.TabCount; i++)
            {
                BaseConfig_Player Player = new BaseConfig_Player(i);
                Player.Inputs = new List<BaseConfig_Input>();
                foreach (Control c in _FlpPlayers[i - 1].Controls)
                {
                    if (c is Ctrl_InputButton)
                    {
                        InputButton NewButton = new InputButton(((Ctrl_InputButton)c).ButtonName);
                        foreach (Ctrl_InputButtonItem ButtonItem in ((Ctrl_InputButton)c).GetInputItems())
                        {
                            InputButtonEntry k = new InputButtonEntry();
                            k.DevicePath = ButtonItem.DevicePath;
                            k.DeviceName = ButtonItem.DeviceName;
                            k.ButtonId = ButtonItem.ButtonId;
                            k.ButtonName = ButtonItem.ButtonName;
                            k.DeviceType = ButtonItem.DeviceType.ToString();
                            NewButton.Keys.Add(k);
                        }
                        Player.Inputs.Add(NewButton);
                    }
                    else if (c is Ctrl_InputAxis)
                    {
                        InputAxes NewAxis = new InputAxes(((Ctrl_InputAxis)c).ButtonName);
                        foreach (Ctrl_InputAxisItem AxisItem in ((Ctrl_InputAxis)c).GetInputItems())
                        {
                            NewAxis.Keys.Add(AxisItem.AxesEntry);
                        }
                        Player.Inputs.Add(NewAxis);
                    }
                }
                _PlayerInputs.Add(Player);
            }

            DialogResult = DialogResult.OK;
        }

        private void Btn_Cancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}
