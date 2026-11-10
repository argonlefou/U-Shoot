using System;
using UsCore.Config;
using UsCore.Launcher;
using UsCore.Logging;

namespace U_Shoot.Games
{
    public class Game_Flycast : BaseGame64
    {
        protected override bool PATCH_PEB_DEBUG_FLAG { get { return false; } }

        private const string ROMPATH_OPTION = "Rom Path";
        private const string FLYCASTOTPTIONS_OPTION = "Flycast options";

        protected UInt64 _GameRAM_Address = 0;

        public Game_Flycast(GameConfig Config) : base(Config)
        {
            _GameLauncher.Add_CmdLine_Arg(_GameConfig.GetStringOption(FLYCASTOTPTIONS_OPTION));
            _GameLauncher.Add_CmdLine_Arg("\"" + _GameConfig.GetStringOption(ROMPATH_OPTION + " [" + _GameConfig.RomName + ".zip]") + "\"");
        }

        //That game will need to patch DLL a load time before main exe starts
        protected override void AttachDebuggerEvents()
        {
            _GameLauncher.OnOutputDebugString += OutputDebugStringEvent;
        }

        #region Debugger Events

        #region EXCEPTION_DEBUG_EVENT

        protected override void ExceptionEvent(object sender, x86_x64_Debugger.ExceptionDebugEventArgs e)
        {
            base.ExceptionEvent(sender, e);
        }

        #endregion

        #region OUTPUTDEBUGSTRING_DEBUG_EVENT

        /// <summary>
        /// Flycast is using OutputDebugString to display some debug data
        /// Especially when it's creating  Virtual Memory regions to load roms
        /// We can use this to intercept those message and get BASE, RAM and VRAM region addresses if needed
        /// </summary>
        protected override void OutputDebugStringEvent(object sender, x86_x64_Debugger.OutputDebugStringDebugEventArgs e)
        {
            if (_GameRAM_Address == 0)
            {
                try
                {
                    string Message = string.Empty;
                    byte[] Buffer = ReadBytes(e.OutputDebugStringInfo.lpDebugStringData, e.OutputDebugStringInfo.nDebugStringLength);

                    if (e.OutputDebugStringInfo.fUnicode == 0)
                        Message = System.Text.Encoding.Default.GetString(Buffer);
                    else
                        Message = System.Text.Encoding.Unicode.GetString(Buffer);

                    if (Message.Contains(" VRAM"))
                    {
                        int VramPos = Message.IndexOf(" VRAM", 0, Message.Length, StringComparison.InvariantCulture);
                        if (VramPos != -1)
                        {
                            string strAddress = Message.Substring(VramPos - 16, 16);
                            if (UInt64.TryParse(strAddress, System.Globalization.NumberStyles.HexNumber, null, out _GameRAM_Address))
                            {
                                Logger.WriteLog("RAM Address detected: 0x" + _GameRAM_Address.ToString("X16"));
                                RaiseGameHookedEvent();
                            }
                            else
                            {
                                Logger.WriteLog("Error trying to convert RAM address string: " + strAddress);
                            }
                        }
                    }
                }
                catch
                { }
            }
        }

        #endregion

        #endregion     

        #region MemoryHack

        //Removing the Hacks as there are none to apply
        //Removing the Raise_GameHookedEvent() as we don't want it yet
        protected override void Apply_MemoryHacks()
        {            
        }

        #endregion
    }
}
