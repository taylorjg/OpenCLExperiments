using System;
using OpenCL.Net;

namespace ClUtils
{
    public interface IPinnedArrayOfStruct
    {
        IMem Buffer { get; }
        int Size { get; }
        IntPtr Handle { get; }
    }
}
