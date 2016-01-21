using System;
using OpenCL.Net;

namespace ClUtils
{
    public static class ErrorCodeExtensions
    {
        public static void Check(this ErrorCode errorCode, string message)
        {
            if (errorCode != ErrorCode.Success)
                throw new Exception($"{message}: {errorCode}");
        }
    }
}
