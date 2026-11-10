using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Security.Principal;
using System.Threading;
using System.Windows.Forms;
using U_Shoot.Games;
using UsCore.Config;
using UsCore.Logging;
using UsCore.MameOutput;
using UsCore.RawInput;

namespace U_Shoot
{
    public class UShoot_Program : ApplicationContext
    {
        //Tray Icon
        private ContextMenu _TrayIconMenu;
        private NotifyIcon _TrayIcon;
        private Icon _UnhookedIcon = Properties.Resources.UnHooked_Icon;
        private Icon _HookedIcon = Properties.Resources.Hooked_Icon;

        //Output (Windows Message)
        private Wm_OutputHelper _Wm_OutputHelper;

        //Output (Network)
        private Net_OutputHelper _Net_OutputHelper;
        private Thread _OutputUpdateLoop;

        //Game options
        private BaseGame _Game;
        private GameConfig _GameConfig;

        //InterProcessCommunication (Memory Mapped Files)
        private const String DEMULSHOOTER_INPUTS_MMF_NAME = "DemulShooter_MMF_Inputs";
        private const String DEMULSHOOTER_OUTPUTS_MMF_NAME = "DemulShooter_MMF_Outputs";
        private const String DEMULSHOOTER_INPUTS_MUTEX_NAME = "DemulShooter_Inputs_Mutex";
        private const String DEMULSHOOTER_OUTPUTS_MUTEX_NAME = "DemulShooter_Outputs_Mutex";

        //If the Program is started from GUI, closing a game should not close the whole GUI
        //Same thing for the EXIT option in the tray icon
        private bool _RunFromGUI;

        public UShoot_Program(string GameConfigFilePath, bool RunFromGUI = false)
        {
            string[] sArgs = Environment.GetCommandLineArgs();
            foreach (string sOption in sArgs)
            {
                switch (sOption.ToLower())
                {
                    case "-v": Logger.IsEnabled = true; break;
                    case "-t": Logger.IsTraceEnabled = true; break;
                    default: break;
                }
            }

            _RunFromGUI = RunFromGUI;

            Logger.InitLogFileName();
            Logger.WriteLog("");
            Logger.WriteLog("---------------- Program Start -- " + Properties.Resources.APP_NAME + " v" + System.Reflection.Assembly.GetEntryAssembly().GetName().Version.ToString() + " ----------------");

            Logger.WriteLog("Loading Game config file " + GameConfigFilePath);

            _GameConfig = GameConfig.LoadFromDisk(GameConfigFilePath);
            if (_GameConfig == null)
            {
                Logger.WriteLog("Impossible to load game config file, aborting...");
                Environment.Exit(0);
            }

            //Creating TrayIcon and TrayIcon "Exit" menu
            Application.ApplicationExit += new EventHandler(OnApplicationExit);
            
            //checking a few details before starting...
            if (_GameConfig.TargetPath.Length == 0)
            {
                string DefaultPath = string.Empty;
                switch (_GameConfig.Category)
                {
                    case "Flycast": DefaultPath = ProgramConfig.GetInstance().GetStringOption(Properties.Resources.DEFAULT_FLYCAST_PATH_OPTION); break;
                    case "Lindbergh": DefaultPath = ProgramConfig.GetInstance().GetStringOption(Properties.Resources.DEFAULT_LINUXLOADER_PATH_OPTION); break;
                    case "System 2X6": DefaultPath = ProgramConfig.GetInstance().GetStringOption(Properties.Resources.DEFAULT_PCSX2X6EMULATOR_PATH_OPTION); break;
                    case "System 357": DefaultPath = ProgramConfig.GetInstance().GetStringOption(Properties.Resources.DEFAULT_RPCS3EMULATOR_PATH_OPTION); break;
                    default: break;
                }

                if (string.IsNullOrEmpty(DefaultPath))
                {
                    Logger.WriteLog("Target file path is empty, aborting...");
                    Environment.Exit(0);
                }
                else
                {
                    _GameConfig.TargetPath = DefaultPath;
                }
            }
            if (!File.Exists(_GameConfig.TargetPath))
            {
                Logger.WriteLog("Target file not found: " + _GameConfig.TargetPath);
                Environment.Exit(0);
            }

            //Tray Icon
            //Looking for explorer.exe process, if not skip TrayIcon to make the program work
            Process[] pExplorer = Process.GetProcessesByName("explorer");
            if (pExplorer.Length > 0)
            {
                _TrayIcon = new NotifyIcon();
                _TrayIcon.Text = Properties.Resources.APP_NAME;
                _TrayIcon.Icon = _UnhookedIcon;
                _TrayIconMenu = new ContextMenu();
                _TrayIconMenu.MenuItems.Add("Exit", OnTrayExitSelected);
                _TrayIcon.ContextMenu = _TrayIconMenu;
                _TrayIcon.Visible = true;
            }
            if (_TrayIcon != null)
                _TrayIcon.Text += "[" + _GameConfig.RomName + "]";

            Logger.WriteLog("Running as Administrator : " + IsRunningAsAdmin().ToString());

            //Running RawInputManager
            Logger.WriteLog("Starting Raw Input Manager...");
            if (!RawInputManager.GetInstance().InitManager())
            {
                Logger.WriteLog("Error Initializing RawInputManager !");
                return;
            }
            if (!RawInputManager.GetInstance().RegisterDevices())
                Logger.WriteLog("Error registering Raw Input Messsages !");   

            Type GameType = Assembly.GetExecutingAssembly().GetType("U_Shoot.Games.Game_" + _GameConfig.Identifier);
            if (GameType != null) 
            {
                Logger.WriteLog("Creating instance of [" + GameType.FullName + "]...");
                _Game = Activator.CreateInstance(GameType, new object[] { _GameConfig }) as BaseGame;
                _Game.OnGameHooked += OnGameHooked;
                _Game.OnGameClosed += OnGameClosed;

                //Starting Windows Message or TCP network Output Daemons
                bool bStartOutputThread = false;
                if (ProgramConfig.GetInstance().GetBoolOption(ProgramConfig.WINDOWS_OUTPUTS))
                {
                    Logger.WriteLog("Creating Window Message Output Helper...");
                    bStartOutputThread = true;
                    _Wm_OutputHelper = new Wm_OutputHelper();
                    _Wm_OutputHelper.OnIdStringRequestReceived += Wm_OutputHelper_OnIdStringRequestReceived;
                    _Wm_OutputHelper.RomNameToSend = _GameConfig.RomName;
                    _Wm_OutputHelper.Start();
                }
                else if (ProgramConfig.GetInstance().GetBoolOption(ProgramConfig.NETWORK_OUTPUTS))
                {
                    Logger.WriteLog("Creating Network Output Helper...");
                    bStartOutputThread = true;
                    _Net_OutputHelper = new Net_OutputHelper(_GameConfig.RomName);
                    _Net_OutputHelper.Start();
                }
                if (bStartOutputThread)
                {
                    _OutputUpdateLoop = new Thread(new ThreadStart(ReadAndSendOutput_Thread));
                    _OutputUpdateLoop.Start();
                }

                _Game.Start();
            }
            else
            {
                if (_RunFromGUI)
                {
                    MessageBox.Show(GameType.FullName + "class not found, can't start the game launcher !", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    Logger.WriteLog(GameType.FullName + " not found, can't start the game launcher. Abording...");
                    Application.Exit();
                }
                
            }
        }

        /// <summary>
        /// End the program (usually beeing called by the main GUI windows when visible)
        /// This will trigger the onGameClosed event to clean and exit
        /// </summary>
        public void EndGame()
        {
            _Game.Close();
        }

        /// <summary>
        /// Check if user has Elevated rights (Admin)
        /// </summary>
        /// <returns></returns>
        private bool IsRunningAsAdmin()
        {
            bool isElevated = false;
            try
            {
                using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
                {
                    WindowsPrincipal principal = new WindowsPrincipal(identity);
                    isElevated = principal.IsInRole(WindowsBuiltInRole.Administrator);
                }
                return isElevated;
            }
            catch (Exception Ex)
            {
                Logger.WriteLog("Error checking Admin rights for current user : " + Ex.Message.ToString());
                return false;
            }
        }

        /// <summary>
        /// Output handling thread :
        /// This infinite loop will check the targeted game values and send them to registerd Output clients
        /// </summary>
        private void ReadAndSendOutput_Thread()
        {
            while (true)
            {
                if (_Game != null && _Game.ProcessHooked)
                {
                    _Game.UpdateOutputValues();

                    if (ProgramConfig.GetInstance().GetBoolOption(ProgramConfig.WINDOWS_OUTPUTS))
                    {
                        if (_Wm_OutputHelper.RomNameSent)
                            _Wm_OutputHelper.SendValues(_Game.Outputs);
                    }
                    else if (ProgramConfig.GetInstance().GetBoolOption(ProgramConfig.NETWORK_OUTPUTS))
                    {
                        if (_Net_OutputHelper != null)
                            _Net_OutputHelper.BroadcastValues(_Game.Outputs);
                    }
                }

                Win32.MM_BeginPeriod(1);
                Thread.Sleep(ProgramConfig.GetInstance().GetNumericOption(ProgramConfig.OUTPUTS_POLLING_DELAY, 1));
                Win32.MM_EndPeriod(1);
            }
        }

        private void Wm_OutputHelper_OnIdStringRequestReceived(object sender, Wm_OutputHelper.WmOutputsEventArgs e)
        {
            uint Id = (uint)e.lParam;
            if (Id == 0)
            {
                _Wm_OutputHelper.SendIdString(e.wParam, _Wm_OutputHelper.RomNameToSend, 0);
                _Wm_OutputHelper.RomNameSent = true;
            }
            else
            {
                if (_Game.Outputs.Count > 0)
                {
                    string s = _Game.GetOutputDescriptionFromId(Id);
                    _Wm_OutputHelper.SendIdString(e.wParam, s, Id);
                }
            }
        }


        /// <summary>
        /// Exit from TrayIcon menu entry
        /// This will trigger the onGameClosed event to clean and exit
        /// </summary>
        private void OnTrayExitSelected(object sender, EventArgs e)
        {
            _Game.Close();
        }

        private void OnGameHooked(object sender, EventArgs e)
        { 
            // Using hidden option to choose if the debugger will stay attached (getting child process EXIT event and closing things, but can't attach another one to debug)
            // or not (allowing another debugger to be attached later)
            // Default behaviour is keeping the game process attached, to be able to catch the EXIT_PROCESS event
            // Detching it is only to use other Debugger to work on game's memory and hacks
            if (ProgramConfig.GetInstance().GetBoolOption("DetachDebugger", true))
            {
                _Game.DetachDebuggerAndReopenProcess();
            }

            if (_TrayIcon != null)
            {
                _TrayIcon.Icon = _HookedIcon;
                _TrayIcon.Text += "[Hooked]";
            }

            if (ProgramConfig.GetInstance().GetBoolOption(ProgramConfig.WINDOWS_OUTPUTS))
            {
                _Game.SetMamePauseState(false);
                //Following can cause racong condition with the output Thread loop
                //Wm_OutputHelper.GetInstance().SendValues(_Game.Outputs);
            }

            //Sending info to the Net_OutputHelper if existing
            if (_Net_OutputHelper != null && ProgramConfig.GetInstance().GetBoolOption(ProgramConfig.NETWORK_OUTPUTS))
            {
                _Net_OutputHelper.SetGameHookedState(true);
                _Net_OutputHelper.BroadcatStartMessage();
            }
        }

        private void OnGameClosed(object sender, EventArgs e)
        {
            CleanAppBeforeExit();
            if (!_RunFromGUI) 
                Environment.Exit(0);
        }

        private void OnApplicationExit(object sender, EventArgs e)
        {            
            //CleanAppBeforeExit();

            //Need to investigate, Application.Exit() does not close the program....
            //Something may be still running in background
            //Doubling the Exit with a call to Environment.Exit(0) to kill process
            Environment.Exit(0);
        }

        /// <summary>
        /// Application Exit cleanup
        /// </summary>
        private void CleanAppBeforeExit()
        {
            Logger.WriteLog("Cleaning things before exiting application...");

            if (_OutputUpdateLoop != null)
                _OutputUpdateLoop.Abort();

            if (ProgramConfig.GetInstance().GetBoolOption(ProgramConfig.WINDOWS_OUTPUTS))
            {
                //Simply sending MameStop may cause MameHooker to not release dll properly
                //MAME goes from MameStop to MameStart with PAUSE enabled and "__empty" rom
                //Which is not possible here due to the WndProc quitting before getting MameHooker request ??
                // Solution would be to Send a new MameStart with no values before MAmeStopping again
                if (_Game != null)
                {
                    _Game.SetMamePauseState(true);
                    _Wm_OutputHelper.SendValues(_Game.Outputs);
                }
                _Wm_OutputHelper.Stop();

                _Wm_OutputHelper.RomNameToSend = Wm_OutputHelper.MAME_ROM_EMPTY;
                _Wm_OutputHelper.Start();
                _Wm_OutputHelper.Stop();
            }

            else if (_Net_OutputHelper != null && ProgramConfig.GetInstance().GetBoolOption(ProgramConfig.NETWORK_OUTPUTS))
            {
                _Net_OutputHelper.Stop();
            }          

            //Cleanup so that the icon will be removed when the application is closed
            if (_TrayIcon != null)
            {
                _TrayIcon.Visible = false;
                _TrayIcon.Dispose();
            }
        }
    }
}
