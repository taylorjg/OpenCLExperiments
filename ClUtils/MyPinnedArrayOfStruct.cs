using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using OpenCL.Net;

namespace ClUtils
{
    public class MyPinnedArrayOfStruct : IDisposable
    {
        private readonly Type _elementType;
        private readonly object _arr;
        private readonly MemMode _memMode;

        public IMem Buffer { get; }
        public int Size { get; }
        public IntPtr Handle { get; }

        private MyPinnedArrayOfStruct(Context context, Type elementType, object arr, int length, MemMode memMode = MemMode.ReadOnly)
        {
            _elementType = elementType;
            _arr = arr;
            _memMode = memMode;

            var elementSize = Marshal.SizeOf(elementType);
            Size = length*elementSize;
            Handle = Marshal.AllocHGlobal(Size);

            if (IsReadable)
            {
                ElementTypeToCopyActions[elementType].Item1(arr, Handle);
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
            if (IsWriteable)
            {
                ElementTypeToCopyActions[_elementType].Item2(_arr, Handle);
            }

            Marshal.FreeHGlobal(Handle);

            Buffer.Dispose();
        }

        public static MyPinnedArrayOfStruct Create(Context context, float[] arr, MemMode memMode = MemMode.ReadOnly)
        {
            return new MyPinnedArrayOfStruct(context, typeof(float), arr, arr.Length, memMode);
        }

        public static MyPinnedArrayOfStruct Create(Context context, int[] arr, MemMode memMode = MemMode.ReadOnly)
        {
            return new MyPinnedArrayOfStruct(context, typeof(int), arr, arr.Length, memMode);
        }

        public static MyPinnedArrayOfStruct Create(Context context, long[] arr, MemMode memMode = MemMode.ReadOnly)
        {
            return new MyPinnedArrayOfStruct(context, typeof(long), arr, arr.Length, memMode);
        }

        private bool IsReadable => _memMode == MemMode.ReadOnly || _memMode == MemMode.ReadWrite;
        private bool IsWriteable => _memMode == MemMode.WriteOnly || _memMode == MemMode.ReadWrite;

        private static readonly Dictionary<Type, Tuple<Action<object, IntPtr>, Action<object, IntPtr>>>
            ElementTypeToCopyActions =
                new Dictionary<Type, Tuple<Action<object, IntPtr>, Action<object, IntPtr>>>
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
                    },
                    {
                        typeof (int),
                        Tuple.Create<Action<object, IntPtr>, Action<object, IntPtr>>(
                            (obj, handle) =>
                            {
                                var arr = (int[]) obj;
                                Marshal.Copy(arr, 0, handle, arr.Length);
                            },
                            (obj, handle) =>
                            {
                                var arr = (int[]) obj;
                                Marshal.Copy(handle, arr, 0, arr.Length);
                            })
                    },
                    {
                        typeof (long),
                        Tuple.Create<Action<object, IntPtr>, Action<object, IntPtr>>(
                            (obj, handle) =>
                            {
                                var arr = (long[]) obj;
                                Marshal.Copy(arr, 0, handle, arr.Length);
                            },
                            (obj, handle) =>
                            {
                                var arr = (long[]) obj;
                                Marshal.Copy(handle, arr, 0, arr.Length);
                            })
                    }
                };
    }
}
