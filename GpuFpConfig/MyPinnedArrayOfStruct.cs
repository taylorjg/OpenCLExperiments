using System;
using System.Runtime.InteropServices;
using OpenCL.Net;

namespace GpuFpConfig
{
    public class MyPinnedArrayOfStruct : IDisposable
    {
        private readonly float[] _arr;
        private readonly MemMode _memMode;
        public IMem Buffer { get; }
        public int Size { get; }
        public IntPtr Handle { get; }

        public MyPinnedArrayOfStruct(Context context, float[] arr, MemMode memMode = MemMode.ReadOnly)
        {
            _arr = arr;
            _memMode = memMode;

            Size = arr.Length*sizeof (float);
            Handle = Marshal.AllocHGlobal(Size);

            if (memMode == MemMode.ReadOnly || memMode == MemMode.ReadWrite)
            {
                Marshal.Copy(arr, 0, Handle, arr.Length);
            }

            ErrorCode errorCode;
            var memFlags = MemFlags.UseHostPtr;
            memFlags |= memMode == MemMode.ReadOnly ? MemFlags.ReadOnly : MemFlags.None;
            memFlags |= memMode == MemMode.WriteOnly? MemFlags.WriteOnly : MemFlags.None;
            memFlags |= memMode == MemMode.ReadWrite ? MemFlags.ReadWrite : MemFlags.None;
            Buffer = Cl.CreateBuffer(context, memFlags, (IntPtr)Size, Handle, out errorCode);
            errorCode.Check();
        }

        public void Dispose()
        {
            if (_memMode == MemMode.ReadWrite || _memMode == MemMode.WriteOnly)
            {
                Marshal.Copy(Handle, _arr, 0, _arr.Length);
            }

            Marshal.FreeHGlobal(Handle);

            Buffer.Dispose();
        }
    }
}
