using System;
using System.Runtime.InteropServices;
using UsCore.Logging;

namespace UsCore.Launcher
{

    /// <summary>
    /// Using an Interface to allow the debugger to retrieve x86/x64 context automatically in the good structure
    /// Debugger/Launcher will be running on x64 CPU, so x64+Wow64 will be used to hook x64 and x86 applications
    /// I386 will be discarded as it would not allow x86 Launcher to run x64 applications
    /// </summary>
    public interface IThreadContext
    {       
        void EnableSingleStep();
        bool Load(IntPtr hThread);
        bool Store(IntPtr hThread);
    }

    internal sealed class AlignedBuffer : IDisposable
    {
        private IntPtr _raw;
        public IntPtr Ptr { get; private set; }

        public AlignedBuffer(int size, int alignment)
        {
            _raw = Marshal.AllocHGlobal(size + alignment - 1);
            long a = alignment;
            Ptr = new IntPtr((_raw.ToInt64() + a - 1) & ~(a - 1));
        }

        public void Dispose()
        {
            if (_raw != IntPtr.Zero) 
            { 
                Marshal.FreeHGlobal(_raw); 
                _raw = IntPtr.Zero; 
            }
        }
    }


    #region ThreadContext for I386

    [StructLayout(LayoutKind.Sequential)]
    public struct CONTEXT32
    {
        public uint Flags;
        public uint Dr0, Dr1, Dr2, Dr3, Dr6, Dr7;
        public uint FloatSave;
        public uint SegGs, SegFs, SegEs, SegDs;
        public uint Edi, Esi, Ebx, Edx, Ecx, Eax;
        public uint Ebp, Eip, SegCs, EFlags, Esp, SegSs;
    }

    #endregion

    #region ThreadContext for x64

    [StructLayout(LayoutKind.Sequential)]
    public struct CONTEXT64
    {
        public ulong P1Home, P2Home, P3Home, P4Home, P5Home, P6Home;    // 0x00
        public uint ContextFlags;                                       // 0x30
        public uint MxCsr;                                              // 0x34
        public ushort SegCs, SegDs, SegEs, SegFs, SegGs, SegSs;         // 0x38
        public uint EFlags;                                             // 0x44
        public ulong Dr0, Dr1, Dr2, Dr3, Dr6, Dr7;                      // 0x48
        public ulong Rax, Rcx, Rdx, Rbx, Rsp, Rbp, Rsi, Rdi;            // 0x78
        public ulong R8, R9, R10, R11, R12, R13, R14, R15;
        public ulong Rip;                                               // 0xF8

        // XMM_SAVE_AREA32 (512 bytes, Xmm0-15 are inside it at +0xA0)
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 512)]
        public byte[] FltSave;                                          // 0x100

        // M128A VectorRegister[26]
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 416)]
        public byte[] VectorRegister;                                   // 0x300

        public ulong VectorControl;                                     // 0x4A0
        public ulong DebugControl;
        public ulong LastBranchToRip;
        public ulong LastBranchFromRip;
        public ulong LastExceptionToRip;
        public ulong LastExceptionFromRip;                              // total 0x4D0
    }

    public class ThreadContext64 : IThreadContext
    {
        public CONTEXT64 Context;
        private bool _bLoaded;

        public bool Load(IntPtr hThread)
        {
            _bLoaded = false;
            Context = new CONTEXT64
            {
                ContextFlags = Win32.CONTEXT_CONTROL | Win32.CONTEXT_INTEGER,
                FltSave = new byte[512],
                VectorRegister = new byte[416]
            };

            using (AlignedBuffer buf = new AlignedBuffer(Marshal.SizeOf(typeof(CONTEXT64)), 16))
            {
                Marshal.StructureToPtr(Context, buf.Ptr, false);
                if (!Win32.GetThreadContext(hThread, buf.Ptr))
                {
                    Logger.WriteLog("GetThreadContext failed, error " + Marshal.GetLastWin32Error());
                    return false;
                }
                Context = (CONTEXT64)Marshal.PtrToStructure(buf.Ptr, typeof(CONTEXT64));
            }
            _bLoaded = true;
            return true;
        }

        public bool Store(IntPtr hThread)
        {
            if (!_bLoaded) return false;

            using (AlignedBuffer buf = new AlignedBuffer(Marshal.SizeOf(typeof(CONTEXT64)), 16))
            {
                Marshal.StructureToPtr(Context, buf.Ptr, false);
                if (!Win32.SetThreadContext(hThread, buf.Ptr))
                {
                    Logger.WriteLog("SetThreadContext failed, error " + Marshal.GetLastWin32Error());
                    return false;
                }
            }
            return true;
        }

        public void EnableSingleStep() 
        { 
            Context.EFlags |= 0x100; 
        }
    }   

    #endregion

    #region ThreadContext for Wow64 (x86 running on x64)

    [StructLayout(LayoutKind.Sequential)]
    public struct WOW64_CONTEXT
    {
        public uint ContextFlags;

        public uint Dr0;
        public uint Dr1;
        public uint Dr2;
        public uint Dr3;
        public uint Dr6;
        public uint Dr7;

        //FLOATING_SAVE_AREA unused but simulated
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 112)]
        public byte[] FloatSave;

        public uint SegGs;
        public uint SegFs;
        public uint SegEs;
        public uint SegDs;

        public uint Edi;
        public uint Esi;
        public uint Ebx;
        public uint Edx;
        public uint Ecx;
        public uint Eax;

        public uint Ebp;
        public uint Eip;
        public uint SegCs;
        public uint EFlags;
        public uint Esp;
        public uint SegSs;

        //OBLIGATOIRE
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 512)]
        public byte[] ExtendedRegisters;
    }

    public class ThreadContextWow64 : IThreadContext
    {
        public WOW64_CONTEXT Context;
        private bool _bLoaded;

        public bool Load(IntPtr hThread)
        {
            _bLoaded = false;
            Context = new WOW64_CONTEXT
            {
                // 0x10007: CONTROL | INTEGER | SEGMENTS, enough for EIP/ESP/EFlags and general registers
                ContextFlags = (uint)Win32.CONTEXT_FLAGS.CONTEXT_FULL,
                FloatSave = new byte[112],
                ExtendedRegisters = new byte[512]
            };

            using (AlignedBuffer buf = new AlignedBuffer(Marshal.SizeOf(typeof(WOW64_CONTEXT)), 16))
            {
                Marshal.StructureToPtr(Context, buf.Ptr, false);
                if (!Win32.Wow64GetThreadContext(hThread, buf.Ptr))
                {
                    Logger.WriteLog("Wow64GetThreadContext failed, error " + Marshal.GetLastWin32Error());
                    return false;
                }
                Context = (WOW64_CONTEXT)Marshal.PtrToStructure(buf.Ptr, typeof(WOW64_CONTEXT));
            }
            _bLoaded = true;
            return true;
        }

        public bool Store(IntPtr hThread)
        {
            if (!_bLoaded) return false;

            using (AlignedBuffer buf = new AlignedBuffer(Marshal.SizeOf(typeof(WOW64_CONTEXT)), 16))
            {
                Marshal.StructureToPtr(Context, buf.Ptr, false);
                if (!Win32.Wow64SetThreadContext(hThread, buf.Ptr))
                {
                    Logger.WriteLog("Wow64SetThreadContext failed, error " + Marshal.GetLastWin32Error());
                    return false;
                }
            }
            return true;
        }

        public void EnableSingleStep()
        { 
            Context.EFlags |= 0x100; 
        }
    }

    #endregion


}
