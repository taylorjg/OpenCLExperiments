using System;
using System.Runtime.InteropServices;
using OpenCL.Net;

namespace ClUtils
{
    public class PinnedArrayOfStruct<T> : IPinnedArrayOfStruct, IDisposable where T : struct
    {
        private PinnedObject _pinnedObject;

        public IMem Buffer { get; }
        public int Size { get; }
        public IntPtr Handle => _pinnedObject;

        public PinnedArrayOfStruct(Context context, T[] arr, MemMode memMode = MemMode.ReadOnly)
        {
            _pinnedObject = arr.Pin();

            var elementSize = Marshal.SizeOf(typeof (T));
            Size = arr.Length*elementSize;

            var memFlags = MemFlags.UseHostPtr;
            memFlags |= memMode == MemMode.ReadOnly ? MemFlags.ReadOnly : MemFlags.None;
            memFlags |= memMode == MemMode.WriteOnly ? MemFlags.WriteOnly : MemFlags.None;
            memFlags |= memMode == MemMode.ReadWrite ? MemFlags.ReadWrite : MemFlags.None;

            ErrorCode errorCode;
            Buffer = Cl.CreateBuffer(context, memFlags, (IntPtr) Size, Handle, out errorCode);
            errorCode.Check("CreateBuffer");
        }

        public void Dispose()
        {
            Buffer.Dispose();
            _pinnedObject.Unpin();
        }
    }
}
