using System;
using System.Diagnostics;
using System.IO;
using UsCore.Config;
using UsCore.IPC;
using UsCore.Launcher;
using UsCore.Logging;
using UsCore.VirtualControllers;

namespace U_Shoot.Games
{
    public class BaseGameUnity64 : BaseGame64
    {
        protected override bool PATCH_PEB_DEBUG_FLAG { get { return false; } }

        protected const string ASSEMBLYCSHARPDll_NAME = "Assembly-CSharp.dll";
        //protected UInt32 _AssemblyCSharpDll_BaseAddress;

        protected class Base_InputData : DsTcpData
        {
            public byte HideCrosshairs = 0;
            public byte EnableInputsHack = 0;

            public Base_InputData(int PlayerNumber) : base(PlayerNumber)
            {
            }
        }

        protected class Base_OutputData : DsTcpData
        {
            public Base_OutputData(int PlayerNumber) : base(PlayerNumber)
            {
            }
        }

        protected string _MainWindowTitle = string.Empty;

        protected DsTcp_Client _Tcpclient;
        protected Base_OutputData _OutputData;
        protected Base_InputData _InputData;

        public BaseGameUnity64(GameConfig Config) : base(Config)
        {
            //Start TcpClient to dial with Unity Game
            _Tcpclient = new DsTcp_Client("127.0.0.1", DsTcp_Client.DS_TCP_CLIENT_PORT);
            _Tcpclient.PacketReceived += DsTcp_Client_PacketReceived;
            _Tcpclient.TcpConnected += DsTcp_client_TcpConnected;
            _Tcpclient.Connect();

            //Using this to discard console window if visibility flag is set in BepInEx.cfg
            _WindowTitleFilterMode = WindowTitleFilter.StartWith;
        }

        /// <summary>
        /// Generate Doorstop arguments to replace doorstop_config.ini
        /// Based on the game folder to set BepInEx path to load
        /// </summary>
        protected string CreateDoorstopArguments(string PluginFolderName, bool IsIL2CPP = false)
        {
            string DoorStopEnabled_Arg = "--doorstop-enabled true";
            string DoorStopTargetAssembly_Arg = string.Empty;

            //Regular/IL2CPP BepInEx have different preloader targets
            if (!IsIL2CPP)
                DoorStopTargetAssembly_Arg = "--doorstop-target-assembly \"" + Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Properties.Resources.DATA_UNITY_FOLDER, PluginFolderName, Properties.Resources.BEPINEX_PRELOADER) + "\"";
            else
                DoorStopTargetAssembly_Arg = "--doorstop-target-assembly \"" + Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Properties.Resources.DATA_UNITY_FOLDER, PluginFolderName, Properties.Resources.BEPINEX_PRELOADER_IL2CPP) + "\"";

            string DoorStopMonoDllPath_Arg = "--doorstop-mono-dll-search-path-override \"" + Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Properties.Resources.DATA_UNITY_FOLDER, PluginFolderName, Properties.Resources.BEPINEX_CORE) + "\"";

            return DoorStopEnabled_Arg + " " + DoorStopTargetAssembly_Arg + " " + DoorStopMonoDllPath_Arg;
        }

        /// <summary>
        /// Unity games are loading Assembly-CSharp.dll with game code, but it's not loaded by windows and mapped so 
        /// it will not trigger LOAD_DLL_DEBUG_EVENT event...
        /// </summary>
        protected override void AttachDebuggerEvents()
        {
            _GameLauncher.OnExitProcess += ExitProcessEvent;
        }

        ~BaseGameUnity64()
        {
            if (_Tcpclient != null)
                _Tcpclient.Disconnect();
        }

        #region Debugger Events

        #region CREATE_PROCESS_DEBUG_EVENT

        protected override void CreateProcessEvent(object sender, x86_x64_Debugger.CreateProcessDebugEventArgs e)
        {
            x86_x64_Debugger Dbg = (x86_x64_Debugger)sender;

            Logger.WriteLog("[CREATE_PROCESS_DEBUG_EVENT]");

            _Process = Process.GetProcessById((int)(e.dwProcessId));
            _Process_Handle = Dbg.CreatedProcessHandle;
            _Process_MemoryBaseAddress = e.CreateProcessInfo.lpBaseOfImage;
            _Process_EntryPointAddress = e.CreateProcessInfo.lpStartAddress;

            Logger.WriteLog(" > Process ID: " + _Process.Id);
            Logger.WriteLog(" > Process Memory Base Address: 0x" + _Process_MemoryBaseAddress.ToString(Dbg.HexStringFormat));
            Logger.WriteLog(" > Process Entry Point Address: 0x" + _Process_EntryPointAddress.ToString(Dbg.HexStringFormat));
            //HANDLE should always be in 32Bits range (https://learn.microsoft.com/fr-fr/windows/win32/winprog64/interprocess-communication?redirectedfrom=MSDN)
            Logger.WriteLog(" > Process Handle: 0x" + _Process_Handle.ToString("X8"));
            Logger.WriteLog(" > Process hThread: 0x" + e.CreateProcessInfo.hThread.ToString("X8"));

            string AssemblyCSharp_Path = Path.Combine(Path.GetDirectoryName(_GameConfig.TargetPath), Path.GetFileName(_GameConfig.TargetPath).Replace(".exe", "_Data"), "Managed", ASSEMBLYCSHARPDll_NAME);
            if (File.Exists(AssemblyCSharp_Path))
            {
                CheckMd5(AssemblyCSharp_Path, _KnownMd5Prints);
            }
            else
            {
                Logger.WriteLog("Assembly-CSharp.dll file not found : " + AssemblyCSharp_Path);
            }

            RaiseGameHookedEvent();

            //Start the Timer that will either hook an emulator when ROM is loaded, or close the program if the target binary is not running
            _tProcess.Start();
        }

        #endregion

        #region EXCEPTION_DEBUG_EVENT

        protected override void ExceptionEvent(object sender, x86_x64_Debugger.ExceptionDebugEventArgs e)
        {
            //Nothing special to do here
            base.ExceptionEvent(sender, e);
        }

        #endregion

        #region EXIT_PROCESS_DEBUG_EVENT

        protected override void ExitProcessEvent(object sender, x86_x64_Debugger.ExitProcessDebugEventArgs e)
        {
            Logger.WriteLog("[EXIT_PROCESS_DEBUG_EVENT] dwExitCode = " + e.ExitProcessInfo.dwExitCode.ToString());
            if (_Process_Handle != IntPtr.Zero)
                Win32.CloseHandle(_Process_Handle);

            if (_Tcpclient != null)
                _Tcpclient.Disconnect();

            RaiseGameClosedEvent();
        }

        #endregion

        #endregion

        #region Screen

        /// <summary>
        /// Default GameScale functio, just sending coordinate in a similar way Unity Input.MousePosition() would work
        /// </summary>
        public override bool GameScale(VirtualPlayerController PlayerController)
        {
            if (_Process_Handle != IntPtr.Zero)
            {
                try
                {
                    double TotalResX = _ClientRect.Right - _ClientRect.Left;
                    double TotalResY = _ClientRect.Bottom - _ClientRect.Top;
                    Logger.WriteLog(" > Game Window Rect (Px) = [ " + TotalResX + "x" + TotalResY + " ]");

                    //Coordinates are Window size range, but inverted Y
                    int X_Value = PlayerController.Computed_X;
                    int Y_Value = (int)TotalResY - PlayerController.Computed_Y;

                    if (X_Value < 0)
                        X_Value = 0;
                    if (Y_Value < 0)
                        Y_Value = 0;
                    if (X_Value > (int)TotalResX)
                        X_Value = (int)TotalResX;
                    if (Y_Value > (int)TotalResY)
                        Y_Value = (int)TotalResY;

                    PlayerController.Computed_X = X_Value;
                    PlayerController.Computed_Y = Y_Value;

                    return true;
                }
                catch (Exception ex)
                {
                    Logger.WriteLog("Error scaling mouse coordonates to GameFormat : " + ex.Message.ToString());
                }
            }
            return false;
        }

        #endregion

        #region MemoryHack

        protected override void Apply_MemoryHacks()
        {
            //Nothing to do here as all the hacks are done in the Plugin DLL
            //Also, GameHooked event will be raised within the TCP connected event
        }

        #endregion

        /// <summary>
        /// Sending a TCP message on connection
        /// </summary>
        protected void DsTcp_client_TcpConnected(Object Sender, EventArgs e)
        {
            if (_GameConfig.NoCrosshair)
                _InputData.HideCrosshairs = 1;
            else
                _InputData.HideCrosshairs = 0;

            if (_GameConfig.NoInputs)
                _InputData.EnableInputsHack = 0;
            else
                _InputData.EnableInputsHack = 1;

            _Tcpclient.SendMessage(_InputData.ToByteArray());

            RaiseGameHookedEvent();
        }

        protected virtual void DsTcp_Client_PacketReceived(Object Sender, DsTcp_Client.PacketReceivedEventArgs e)
        { }
    }
}
