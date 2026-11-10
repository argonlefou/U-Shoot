using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace UsCore.Memory
{
    public class Codecave
    {
        #region WIN32 Interrop

        internal class Win32
        {
            [DllImport("kernel32.dll")]
            public static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, UInt32 dwSize, MemoryAllocType flAllocationType, MemoryPageProtect flProtect);

            [DllImport("kernel32.dll", SetLastError = true, EntryPoint = "WriteProcessMemory")]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern Boolean WriteProcessMemory([In] IntPtr hProcess, [In] IntPtr lpBaseAddress, [Out] Byte[] lpBuffer, [In] UIntPtr nSize, [Out] out UIntPtr lpNumberOfBytesWritten);

            [Flags]
            public enum MemoryAllocType
            {
                MEM_COMMIT = 0x1000,
                MEM_RESERVE = 0x2000,
                MEM_RESET = 0x8000,
                MEM_RESET_UNDO = 0x1000000,
            }

            [Flags]
            public enum MemoryPageProtect
            {
                PAGE_EXECUTE = 0x10,
                PAGE_EXECUTE_READ = 0x20,
                PAGE_EXECUTE_READWRITE = 0x40,
                PAGE_EXECUTE_WRITECOPY = 0x80,
                PAGE_NOACCESS = 0x01,
                PAGE_READONLY = 0x02,
                PAGE_READWRITE = 0x04,
                PAGE_WRITECOPY = 0x08,
                PAGE_TARGETS_INVALID = 0x40000000,
                PAGE_TARGETS_NO_UPDATE = 0x40000000
            }
        }

        #endregion

        protected UInt32 _CodecaveAllocSize = 0;

        protected Process _Process;
        protected IntPtr _ProcessHandle;
        protected IntPtr _ModuleBaseAddress = IntPtr.Zero;
        protected IntPtr _Cave_Address = IntPtr.Zero;
        protected int _CaveOffset = 0;

        public Codecave(Process p, IntPtr BaseAddress, UInt32 CodecaveSize = 0x800)
        {
            _Process = p;
            _ModuleBaseAddress = BaseAddress;
            _CodecaveAllocSize = CodecaveSize;

            Open();
            Alloc(_CodecaveAllocSize);
        }

        /// <summary>
        /// Trying to access the process
        /// </summary>
        /// <returns>True if success, otherwise False</returns>
        private bool Open()
        {
            _ProcessHandle = _Process.Handle;
            if (_ProcessHandle != IntPtr.Zero)
                return true;
            else
                return false;
        }

        /// <summary>
        /// Reserves a region of memory within the virtual address space of a specified process. 
        /// The function initializes the memory it allocates to zero.
        /// </summary>
        /// <param name="Size">The size of the region of memory to allocate, in bytes.</param>
        /// <returns>True is success, otherwise False</returns>
        private bool Alloc(UInt32 Size)
        {
            //Allocation mémoire
            _Cave_Address = Win32.VirtualAllocEx(_ProcessHandle, IntPtr.Zero, Size, Win32.MemoryAllocType.MEM_COMMIT, Win32.MemoryPageProtect.PAGE_EXECUTE_READWRITE);
            if (_Cave_Address != IntPtr.Zero)
                return true;
            else
                return false;
        }

        /// <summary>
        /// Write bytes in memory, read from an array of bytes
        /// </summary>
        /// <param name="Buffer">Array of bytes to write</param>
        /// <returns>True if success, otherwise False</returns>
        public virtual bool Write_Bytes(Byte[] Buffer)
        {
            //Handled differently between 32/64 Bits process
            return true;
        }

        /// <summary>
        /// Write byte in memory
        /// </summary>
        /// <param name="Buffer">Array of bytes to write</param>
        /// <returns>True if success, otherwise False</returns>
        public bool Write_Byte(byte bData)
        {
            return Write_Bytes(new byte[] { bData });
        }        

        /// <summary>
        /// Write bytes in memory, read from a string like "00 00 00 00"
        /// </summary>
        /// <param name="StrBuffer">String formated series of bytes to write</param>
        /// <returns>True if success, otherwise False</returns>
        public bool Write_StrBytes(string StrBuffer)
        {
            String[] sBytes = StrBuffer.Split(' ');
            List<Byte> Buffer = new List<byte>();
            foreach (string hex in sBytes)
            {
                Buffer.Add((byte)Convert.ToInt32(hex, 16));
            }
            return Write_Bytes(Buffer.ToArray());
        }

        /// <summary>
        /// Write a sequence of 0x90 bytes in Memory (= NOP code)
        /// </summary>
        /// <param name="Amount">Number of bytes to writes with NOP</param>
        /// <returns>True if success, otherwise False</returns>
        public bool Write_nop(int Amount = 1)
        {
            List<Byte> Buffer = new List<byte>();
            for (int i = 0; i < Amount; i++)
            {
                Buffer.Add(0x90);
            }
            return Write_Bytes(Buffer.ToArray());
        }
    }
}
