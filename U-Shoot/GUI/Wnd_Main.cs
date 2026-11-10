using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using U_Shoot.GUI;
using U_Shoot.GUI.Controls;
using U_Shoot.Properties;
using UsCore.Config;
using UsCore.Logging;
using UsCore.RawInput;

namespace U_Shoot
{
    public partial class Wnd_Main : Form
    {
        private const string CATEGORY_ALL = "- ALL -";

        private List<UShoot_Program> _RunningGames;

        public Wnd_Main()
        {
            InitializeComponent();
            this.Text = "U-SHoot GUI v" + System.Reflection.Assembly.GetEntryAssembly().GetName().Version.ToString();

            _RunningGames = new List<UShoot_Program>();

            Cbox_Platform.SelectedItem = Cbox_Platform.Items[0];
            Txt_FilterName.Text = string.Empty;

            //Remove Horizontal Scrollbar on LowLayoutPanel
            Flp_Games.AutoScroll = false;
            Flp_Games.HorizontalScroll.Enabled = false;
            Flp_Games.HorizontalScroll.Visible = false;
            Flp_Games.HorizontalScroll.Maximum = 0;
            Flp_Games.AutoScroll = true;

            //Set Flow Layout Panel DoubleBuffered property to "true"
            System.Reflection.PropertyInfo aProp = Flp_Games.GetType().GetProperty("DoubleBuffered", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            aProp.SetValue(Flp_Games, true, null);

            if (!RawInputManager.GetInstance().InitManager())
            {
                MessageBox.Show("Error Initializing RawInputManager !", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (!RawInputManager.GetInstance().RegisterDevices())
                MessageBox.Show("Error registering Raw Input Messsages !", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void Wnd_GuiMain_Load(object sender, EventArgs e)
        {
            Flp_Games.SuspendLayout();
            List<Ctrl_Game> ControlList = new List<Ctrl_Game>();
            foreach (string ConfigFile in Directory.GetFiles(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Properties.Resources.DATA_TEMPLATES_FOLDER)))
            {
                if (ConfigFile.ToLower().EndsWith(".xml"))
                {
                    try
                    {
                        Ctrl_Game g = new Ctrl_Game(ConfigFile)
                        {
                            Width = Flp_Games.Width - 2
                        };
                        ControlList.Add(g);

                        g.ButtonControlsClick += Btn_GameControls_Click;
                        g.ButtonOptionsClick += Btn_GameOptions_Click;
                        g.ButtonRunClick += Btn_GameRun_Click;
                        AddCategoryFilter(g.GameConfig.Category);
                    }
                    catch
                    {
                        MessageBox.Show("Error loading the following config file:\n" + ConfigFile + "\n\nPlease check the file again.", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }

            //Display game list based on the game name rather than FileName
            ControlList = ControlList.OrderBy(c => c.GameName).ToList();
            foreach (Ctrl_Game g in ControlList)
            {
                Flp_Games.Controls.Add(g);
            }

            Flp_Games.ResumeLayout();
        }

        private void AddCategoryFilter(String Category)
        {
            foreach (string sPlatform in Cbox_Platform.Items)
            {
                if (sPlatform == Category)
                {
                    return;
                }
            }
            Cbox_Platform.Items.Add(Category);
        }

        #region GUI

        /// <summary>
        /// Autoscroll is enabled on Flow Layout Panel, and Anchor a control will not work
        /// </summary>
        private void Flp_Games_SizeChanged(object sender, EventArgs e)
        {
            Flp_Games.SuspendLayout();
            foreach (Ctrl_Game g in Flp_Games.Controls)
            {
                if (Flp_Games.VerticalScroll.Visible)
                    g.Width = Flp_Games.Width - SystemInformation.VerticalScrollBarWidth - 2;
                else
                    g.Width = Flp_Games.Width - 2;
            }
            Flp_Games.ResumeLayout();
        }

        /// <summary>
        /// Show/Hide games in the list based on the name
        /// </summary>
        private void Txt_FilterName_TextChanged(object sender, EventArgs e)
        {
            Flp_Games.SuspendLayout();
            foreach (Ctrl_Game g in Flp_Games.Controls)
            {
                if (Txt_FilterName.Text.Length > 0)
                {
                    if (g.GameName.ToLower().Contains(Txt_FilterName.Text.ToLower()))
                        g.Visible = true;
                    else
                        g.Visible = false;
                }
                else
                {
                    g.Visible = true;
                }
            }
            Flp_Games.ResumeLayout();
        }

        /// <summary>
        /// Show/Hide games in the list based on the game category
        /// </summary>
        private void Cbox_Platform_SelectedIndexChanged(object sender, EventArgs e)
        {
            Flp_Games.SuspendLayout();
            foreach (Ctrl_Game g in Flp_Games.Controls)
            {
                if (Cbox_Platform.Text != CATEGORY_ALL)
                {
                    if (g.GameConfig.Category.Equals(Cbox_Platform.Text))
                        g.Visible = true;
                    else
                        g.Visible = false;
                }
                else
                {
                    g.Visible = true;
                }
            }
            Flp_Games.ResumeLayout();
        }

        private void Btn_DefaultControls_Click(object sender, EventArgs e)
        {
            Wnd_Inputs w = new Wnd_Inputs(Properties.Resources.DEFAULT_INPUTS_WND_TITLE, ProgramConfig.GetInstance());
            if (w.ShowDialog() == DialogResult.OK)
            {
                ProgramConfig.GetInstance().System_Inputs.Clear();
                ProgramConfig.GetInstance().System_Inputs.AddRange(w.SystemInputs);
                ProgramConfig.GetInstance().Players_Inputs.Clear();
                ProgramConfig.GetInstance().Players_Inputs.AddRange(w.PlayerInputs);
                ProgramConfig.GetInstance().SaveConfig();
                w.Close();
            }
        }

        private void Btn_DefaultPath_Click(object sender, EventArgs e)
        {
            Wnd_DefaultPath w = new Wnd_DefaultPath();
            w.ShowDialog();
        }

        private void Btn_Outputs_Click(object sender, EventArgs e)
        {
            Wnd_Outputs w = new Wnd_Outputs();
            w.ShowDialog();
        }
       
        /// <summary>
        /// Show the Input controls form when a game has it's button pressed
        /// </summary>
        private void Btn_GameControls_Click(object sender, EventArgs e)
        {
            Ctrl_Game g = (Ctrl_Game)sender;
            Wnd_Inputs w = new Wnd_Inputs(g.GameName + " | Controls", g.GameConfig);
            if (w.ShowDialog() == DialogResult.OK)
            {
                g.GameConfig.System_Inputs.Clear();
                g.GameConfig.System_Inputs.AddRange(w.SystemInputs);
                g.GameConfig.Players_Inputs.Clear();
                g.GameConfig.Players_Inputs.AddRange(w.PlayerInputs);
                g.GameConfig.SaveConfig(g.ConfigFilePath);
                w.Close();
            }
        }

        /// <summary>
        /// Show the Option form when a game has it's button pressed
        /// </summary>
        private void Btn_GameOptions_Click(object sender, EventArgs e)
        {
            Ctrl_Game g = (Ctrl_Game)sender;
            Wnd_GameOptions w = new Wnd_GameOptions(g.GameConfig, g.ConfigFilePath);
            w.ShowDialog();
        }

        /// <summary>
        /// Run a game when a game has has it's button pressed
        /// </summary>
        private void Btn_GameRun_Click(object sender, EventArgs e)
        {
            Ctrl_Game g = (Ctrl_Game)sender;

            Process process = new Process();
            process.StartInfo.FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Resources.APP_NAME + ".exe");
            process.StartInfo.Arguments = "-game=" + Path.GetFileName(g.ConfigFilePath);
            process.StartInfo.UseShellExecute = true;
            bool b = process.Start();

            if (!b)
                Logger.WriteLog("Errro");

            /*Ctrl_Game g = (Ctrl_Game)sender;
            UShoot_Program RunningProgram = new UShoot_Program(g.ConfigFilePath, true);
            _RunningGames.Add(RunningProgram);*/
        }

        #endregion

        private void Wnd_Main_FormClosed(object sender, FormClosedEventArgs e)
        {
            foreach (Control c in Flp_Games.Controls) 
            {
                if (c is Ctrl_Game)
                {
                    ((Ctrl_Game)c).RemoveMessageFilter();
                }
            }

            foreach (UShoot_Program game in _RunningGames)
            {
                game.EndGame();
            }
        }

        
    }

}
