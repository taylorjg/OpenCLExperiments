using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using OpenCL.Net;

namespace ClUtils
{
    public class MyPinnedArrayOfStruct : IDisposable
    {
        private readonly object _arr;
        private readonly MemMode _memMode;

        public IMem Buffer { get; }
        public int Size { get; }
        public IntPtr Handle { get; }

        private MyPinnedArrayOfStruct(Context context, float[] arr, MemMode memMode = MemMode.ReadOnly)
        {
            _arr = arr;
            _memMode = memMode;

            Size = arr.Length*sizeof (float);
            Handle = Marshal.AllocHGlobal(Size);

            if (memMode == MemMode.ReadOnly || memMode == MemMode.ReadWrite)
            {
                var elementType = typeof (float);
                var action1 = ElementTypeToCopyActions[elementType].Item1;
                action1(arr, Handle);
            }

            var memFlags = MemFlags.UseHostPtr;
            memFlags |= memMode == MemMode.ReadOnly ? MemFlags.ReadOnly : MemFlags.None;
            memFlags |= memMode == MemMode.WriteOnly? MemFlags.WriteOnly : MemFlags.None;
            memFlags |= memMode == MemMode.ReadWrite ? MemFlags.ReadWrite : MemFlags.None;

            ErrorCode errorCode;
            Buffer = Cl.CreateBuffer(context, memFlags, (IntPtr)Size, Handle, out errorCode);
            errorCode.Check("CreateBuffer");
        }

        public void Dispose()
        {
            if (_memMode == MemMode.ReadWrite || _memMode == MemMode.WriteOnly)
            {
                var elementType = typeof(float);
                var action2 = ElementTypeToCopyActions[elementType].Item2;
                action2(_arr, Handle);
            }

            Marshal.FreeHGlobal(Handle);

            Buffer.Dispose();
        }

        public static MyPinnedArrayOfStruct Create(Context context, float[] arr, MemMode memMode = MemMode.ReadOnly)
        {
            return new MyPinnedArrayOfStruct(context, arr, memMode);
        }

        //public static MyPinnedArrayOfStruct Create(Context context, int[] arr, MemMode memMode = MemMode.ReadOnly)
        //{
        //    return new MyPinnedArrayOfStruct(context, arr, memMode);
        //}

        private static readonly Dictionary<Type, Tuple<Action<object, IntPtr>, Action<object, IntPtr>>> ElementTypeToCopyActions = new Dictionary
            <Type, Tuple<Action<object, IntPtr>, Action<object, IntPtr>>>
        {
            {
                typeof (float),
                Tuple.Create<Action<object, IntPtr>, Action<object, IntPtr>>(
                    (obj, handle) =>
                    {
                        var arr = (float[]) obj;
                        Marshal.Copy(arr, 0, handle, arr.Length);
                    },
                    (obj, handle) =>
                    {
                        var arr = (float[]) obj;
                        Marshal.Copy(handle, arr, 0, arr.Length);
                    })
            }
        };
    }
}
