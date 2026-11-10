using System;
using System.Text;
using UsCore.Config;
using UsCore.Launcher;

namespace U_Shoot.Games
{
    public class Game_Pcsx2x6 : BaseGame64
    {
        protected override bool PATCH_PEB_DEBUG_FLAG { get { return false; } }

        private const string ACGAME_PATH_OPTION = "ROM [*.acgame] file";
        private const string PCSX2X6_OPTIONS_OPTION = "PCSX2X6 options";

        protected UInt64 _EEmem_Ptr_Address = 0;

        #region LOADER MEMORY ADDRESSES 

        #endregion

        #region INPUTS MEMORY ADDRESSES    

        #endregion

        #region OUTPUTS MEMORY ADDRESSES

        #endregion

        public Game_Pcsx2x6(GameConfig Config) : base(Config)
        {
            _GameLauncher.Add_CmdLine_Arg(_GameConfig.GetStringOption(PCSX2X6_OPTIONS_OPTION));
            _GameLauncher.Add_CmdLine_Arg(_GameConfig.GetStringOption(ACGAME_PATH_OPTION), true);
        }

        #region Debugger Events

        #region CREATE_PROCESS_DEBUG_EVENT

        protected override void CreateProcessEvent(object sender, x86_x64_Debugger.CreateProcessDebugEventArgs e)
        {
            base.CreateProcessEvent(sender, e);

            //The game has a debugger detection system and will refuse to run if the Debugger is still attached passed that point
            DetachDebuggerAndReopenProcess();
        }

        #endregion        

        #endregion

        /// <summary>
        /// Address of an exported variable of the emulator, from the export table of its loaded image
        /// Credits to m33ts4k0z (https://github.com/m33ts4k0z) for finding that
        /// </summary>
        protected UInt64 FindExport(String Name)
        {
            byte[] Header = ReadBytes(GameProcess_ModuleBaseAddress, 0x400);
            int PeOffset = BitConverter.ToInt32(Header, 0x3C);
            UInt32 ExportRva = BitConverter.ToUInt32(Header, PeOffset + 0x88);    //PE32+ data directory 0
            if (ExportRva == 0)
                return 0;

            byte[] Directory = ReadBytes(GameProcess_ModuleBaseAddress + ExportRva, 40);
            int NamesCount = BitConverter.ToInt32(Directory, 24);
            UInt32 FunctionsRva = BitConverter.ToUInt32(Directory, 28);
            UInt32 NamesRva = BitConverter.ToUInt32(Directory, 32);
            UInt32 OrdinalsRva = BitConverter.ToUInt32(Directory, 36);
            byte[] Names = ReadBytes(GameProcess_ModuleBaseAddress + NamesRva, (Int32)NamesCount * 4);
            byte[] Ordinals = ReadBytes(GameProcess_ModuleBaseAddress + OrdinalsRva, (Int32)NamesCount * 2);
            for (int i = 0; i < NamesCount; i++)
            {
                byte[] Str = ReadBytes(GameProcess_ModuleBaseAddress + BitConverter.ToUInt32(Names, 4 * i), (Int32)Name.Length + 1);
                if (Encoding.ASCII.GetString(Str, 0, Name.Length) == Name && Str[Name.Length] == 0)
                {
                    UInt16 Ordinal = BitConverter.ToUInt16(Ordinals, 2 * i);
                    UInt32 VariableRva = ReadUInt32((IntPtr)(GameProcess_ModuleBaseAddress + FunctionsRva + 4 * (UInt32)Ordinal));
                    return GameProcess_ModuleBaseAddress + VariableRva;
                }
            }
            return 0;
        }

        protected string ReadString(UInt64 Address, int Length)
        {
            byte[] b = ReadBytes(Address, Length);
            return Encoding.ASCII.GetString(b);
        }
    }
}
