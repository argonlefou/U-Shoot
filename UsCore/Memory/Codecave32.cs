using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using UsCore.Logging;

namespace UsCore.Memory
{
    public class Codecave32 : Codecave
    {
        public UInt32 CaveAddress
        {
            get { return (UInt32)_Cave_Address; }
        }
        public int CaveOffset
        {
            get { return _CaveOffset; }
        }

        public Codecave32(Process p, UInt32 BaseAddress, UInt32 CodecaveSize = 0x800) : base(p, new IntPtr(BaseAddress), CodecaveSize)
        { }

        /// <summary>
        /// Write bytes in memory, read from an array of bytes
        /// </summary>
        /// <param name="Buffer">Array of bytes to write</param>
        /// <returns>True if success, otherwise False</returns>
        public override bool Write_Bytes(Byte[] Buffer)
        {
            UIntPtr BytesWritten = UIntPtr.Zero;
            if (Win32.WriteProcessMemory(_ProcessHandle, new IntPtr(CaveAddress + _CaveOffset), Buffer, (UIntPtr)Buffer.Length, out BytesWritten))
            {
                _CaveOffset += (int)BytesWritten;
                return true;
            }
            else
            {
                return false;
            }
        }

        //call Address
        public bool Write_call(UInt32 Address)
        {
            UInt32 JmpAddress = Address - (CaveAddress + (UInt32)_CaveOffset) - 5;
            List<Byte> Buffer = new List<byte>
            {
                0xE8
            };
            Buffer.AddRange(BitConverter.GetBytes(JmpAddress));
            return Write_Bytes(Buffer.ToArray());
        }

        ////cmp eax,[Value]
        //public bool Write_cmp(int Value)
        //{
        //    List<Byte> Buffer = new List<byte>();
        //    Buffer.Add(0x81);
        //    Buffer.Add(0xF9);
        //    Buffer.AddRange(BitConverter.GetBytes(Value));
        //    return Write_Bytes(Buffer.ToArray());
        //}

        ////je [Address]
        //public bool Write_je(UInt32 Address)
        //{
        //    UInt32 JmpAddress = Address - (_Cave_Address + _CaveOffset) - 6;
        //    List<Byte> Buffer = new List<byte>();
        //    Buffer.Add(0x0F);
        //    Buffer.Add(0x84);
        //    Buffer.AddRange(BitConverter.GetBytes(JmpAddress));
        //    return Write_Bytes(Buffer.ToArray());
        //}

        ////jb [Address]
        //public bool Write_jb(UInt32 Address)
        //{
        //    UInt32 JmpAddress = Address - (_Cave_Address + _CaveOffset) - 6;
        //    List<Byte> Buffer = new List<byte>();
        //    Buffer.Add(0x0F);
        //    Buffer.Add(0x82);
        //    Buffer.AddRange(BitConverter.GetBytes(JmpAddress));
        //    return Write_Bytes(Buffer.ToArray());
        //}

        //jng [Address]
        public bool Write_jng(UInt32 Address)
        {
            UInt32 iCaveAddress = CaveAddress;
            UInt32 JmpAddress = Address - (iCaveAddress + (UInt32)_CaveOffset) - 6;
            List<Byte> Buffer = new List<byte>
            {
                0x0F,
                0x8E
            };
            Buffer.AddRange(BitConverter.GetBytes(JmpAddress));
            return Write_Bytes(Buffer.ToArray());
        }

        //jnl [address]
        public bool Write_jnl(UInt32 Address)
        {
            UInt32 iCaveAddress = CaveAddress;
            UInt32 JmpAddress = Address - (iCaveAddress + (UInt32)_CaveOffset) - 6;
            List<byte> Buffer = new List<byte>{
                0x0F,
                0x8D
            };
            Buffer.AddRange(BitConverter.GetBytes(JmpAddress));
            return Write_Bytes(Buffer.ToArray());
        }

        //jng [Address]
        public bool Write_jg(UInt32 Address)
        {
            UInt32 iCaveAddress = CaveAddress;
            UInt32 JmpAddress = Address - (iCaveAddress + (UInt32)_CaveOffset) - 6;
            List<Byte> Buffer = new List<byte>
            {
                0x0F,
                0x8F
            };
            Buffer.AddRange(BitConverter.GetBytes(JmpAddress));
            return Write_Bytes(Buffer.ToArray());
        }

        ////jng [Address]
        //public bool Write_ja(UInt32 Address)
        //{
        //    UInt32 JmpAddress = Address - (_Cave_Address + _CaveOffset) - 6;
        //    List<Byte> Buffer = new List<byte>();
        //    Buffer.Add(0x0F);
        //    Buffer.Add(0x87);
        //    Buffer.AddRange(BitConverter.GetBytes(JmpAddress));
        //    return Write_Bytes(Buffer.ToArray());
        //}

        //jnl [Address]
        public bool Write_jl(UInt32 Address)
        {
            UInt32 iCaveAddress = CaveAddress;
            UInt32 JmpAddress = Address - (iCaveAddress + (UInt32)_CaveOffset) - 6;
            List<Byte> Buffer = new List<byte>
            {
                0x0F,
                0x8C
            };
            Buffer.AddRange(BitConverter.GetBytes(JmpAddress));
            return Write_Bytes(Buffer.ToArray());
        }

        //jmp [Address]
        public bool Write_jmp(UInt32 Address)
        {
            UInt32 iCaveAddress = CaveAddress;
            UInt32 JmpAddress = Address - (iCaveAddress + (UInt32)_CaveOffset) - 5;
            List<Byte> Buffer = new List<byte>
            {
                0xE9
            };
            Buffer.AddRange(BitConverter.GetBytes(JmpAddress));
            return Write_Bytes(Buffer.ToArray());
        }


        /// <summary>
        /// Create both Jump-To and Jump-Back instruction and inject the codecave in memory 
        /// </summary>
        /// <param name="iStruct">InjectionStruct with needed offset to inject the codecave to the good paert of the game code</param>
        /// <param name="sCodeCaveName">Name that will be used to log the codecave creation</param>
        public void InjectToOffset(InjectionStruct32 iStruct, string sCodeCaveName, bool bCreateButNotInject = false)
        {
            InjectToOffset((UInt32)_ModuleBaseAddress, iStruct, sCodeCaveName, bCreateButNotInject);
        }

        /// <summary>
        /// Create both Jump-To and Jump-Back instruction and inject the codecave in memory 
        /// </summary>
        /// <param name="iStruct">InjectionStruct with needed offset to inject the codecave to the good paert of the game code</param>
        /// <param name="sCodeCaveName">Name that will be used to log the codecave creation</param>
        public void InjectToOffset(UInt32 BaseAddress, InjectionStruct32 iStruct, string sCodeCaveName, bool bCreateButNotInject = false)
        {
            //Jump back
            Write_jmp(BaseAddress + iStruct.InjectionReturnOffset);

            Logger.WriteLog("Adding " + sCodeCaveName + " CodeCave at : 0x" + _Cave_Address.ToString("X8"));

            //Code injection
            UIntPtr bytesWritten = UIntPtr.Zero;
            UInt32 jumpTo = 0;
            jumpTo = (UInt32)_Cave_Address - (BaseAddress + iStruct.InjectionOffset) - 5;
            List<byte> Buffer = new List<byte>
            {
                0xE9
            };
            Buffer.AddRange(BitConverter.GetBytes(jumpTo));
            for (int i = 0; i < iStruct.NeededNops; i++)
            {
                Buffer.Add(0x90);
            }

            //For "crash" debug purpose, sometimes I need to create the codecave to examine it, without making the game jump to it.
            if (!bCreateButNotInject)
                Win32.WriteProcessMemory(_ProcessHandle, new IntPtr(BaseAddress + iStruct.InjectionOffset), Buffer.ToArray(), (UIntPtr)Buffer.Count, out bytesWritten);
        }

        /// <summary>
        /// Create both Jump-To and Jump-Back instruction and inject the codecave in memory 
        /// </summary>
        /// <param name="iStruct">InjectionStruct with needed offset to inject the codecave to the good paert of the game code</param>
        /// <param name="sCodeCaveName">Name that will be used to log the codecave creation</param>
        public void InjectToAddress(InjectionStruct32 iStruct, string sCodeCaveName, bool bCreateButNotInject = false)
        {
            //Jump back
            Write_jmp(iStruct.InjectionReturnOffset);

            Logger.WriteLog("Adding " + sCodeCaveName + " CodeCave at : 0x" + _Cave_Address.ToString("X8"));

            //Code injection
            UIntPtr bytesWritten = UIntPtr.Zero;
            UInt32 jumpTo = 0;
            jumpTo = CaveAddress - iStruct.InjectionOffset - 5;
            List<byte> Buffer = new List<byte>
            {
                0xE9
            };
            Buffer.AddRange(BitConverter.GetBytes(jumpTo));
            for (int i = 0; i < iStruct.NeededNops; i++)
            {
                Buffer.Add(0x90);
            }

            //For "crash" debug purpose, sometimes I need to create the codecave to examine it, without making the game jump to it.
            if (!bCreateButNotInject)
                Win32.WriteProcessMemory(_ProcessHandle, new IntPtr(iStruct.InjectionOffset), Buffer.ToArray(), (UIntPtr)Buffer.Count, out bytesWritten);
        }
    }
}
