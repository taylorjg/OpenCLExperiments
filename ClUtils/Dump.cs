using System;
using OpenCL.Net;

namespace ClUtils
{
    public static class Dump
    {
        // https://www.khronos.org/registry/cl/api/2.1/cl.h
        private const int ClFpDenorm = (1 << 0);
        private const int ClFpInfNan = (1 << 1);
        private const int ClFpRoundToNearest = (1 << 2);
        private const int ClFpRoundToZero = (1 << 3);
        private const int ClFpRoundToInf = (1 << 4);
        private const int ClFpFma = (1 << 5);
        private const int ClFpSoftFloat = (1 << 6);
        private const int ClFpCorrectlyRoundedDivideSqrt = (1 << 7);

        public static void DeviceDetails(Device device)
        {
            ErrorCode errorCode;

            var deviceName = Cl.GetDeviceInfo(device, DeviceInfo.Name, out errorCode).ToString();
            errorCode.Check("GetDeviceInfo(DeviceInfo.Name)");
            Console.WriteLine($"DeviceInfo.Name: {deviceName}");

            var fpConfig = Cl.GetDeviceInfo(device, DeviceInfo.SingleFpConfig, out errorCode).CastTo<int>();
            errorCode.Check("GetDeviceInfo(DeviceInfo.SingleFpConfig)");
            if ((fpConfig & ClFpDenorm) != 0) Console.WriteLine("CL_FP_DENORM");
            if ((fpConfig & ClFpInfNan) != 0) Console.WriteLine("CL_FP_INF_NAN");
            if ((fpConfig & ClFpRoundToNearest) != 0) Console.WriteLine("CL_FP_ROUND_TO_NEAREST");
            if ((fpConfig & ClFpRoundToZero) != 0) Console.WriteLine("CL_FP_ROUND_TO_ZERO");
            if ((fpConfig & ClFpRoundToInf) != 0) Console.WriteLine("CL_FP_ROUND_TO_INF");
            if ((fpConfig & ClFpFma) != 0) Console.WriteLine("CL_FP_FMA");
            if ((fpConfig & ClFpSoftFloat) != 0) Console.WriteLine("CL_FP_SOFT_FLOAT");
            if ((fpConfig & ClFpCorrectlyRoundedDivideSqrt) != 0) Console.WriteLine("CL_FP_CORRECTLY_ROUNDED_DIVIDE_SQRT");

            var vendor = Cl.GetDeviceInfo(device, DeviceInfo.Vendor, out errorCode).ToString();
            Console.WriteLine($"Vendor: {vendor}");

            var globalMemSize = Cl.GetDeviceInfo(device, DeviceInfo.GlobalMemSize, out errorCode).CastTo<long>();
            errorCode.Check("GetDeviceInfo(DeviceInfo.GlobalMemSize)");
            Console.WriteLine($"GlobalMemSize: {globalMemSize}");

            var localMemSize = Cl.GetDeviceInfo(device, DeviceInfo.LocalMemSize, out errorCode).CastTo<long>();
            errorCode.Check("GetDeviceInfo(DeviceInfo.LocalMemSize)");
            Console.WriteLine($"LocalMemSize: {localMemSize}");

            var maxComputeUnits = Cl.GetDeviceInfo(device, DeviceInfo.MaxComputeUnits, out errorCode).CastTo<int>();
            errorCode.Check("GetDeviceInfo(DeviceInfo.MaxComputeUnits)");
            Console.WriteLine($"MaxComputeUnits: {maxComputeUnits}");

            var maxWorkGroupSize = Cl.GetDeviceInfo(device, DeviceInfo.MaxWorkGroupSize, out errorCode).CastTo<int>();
            errorCode.Check("GetDeviceInfo(DeviceInfo.MaxWorkGroupSize)");
            Console.WriteLine($"MaxWorkGroupSize: {maxWorkGroupSize}");

            var maxWorkItemSizes = Cl.GetDeviceInfo(device, DeviceInfo.MaxWorkItemSizes, out errorCode).CastTo<int>();
            errorCode.Check("GetDeviceInfo(DeviceInfo.MaxWorkItemSizes)");
            Console.WriteLine($"MaxWorkItemSizes: {maxWorkItemSizes}");

            var maxWorkItemDimensions = Cl.GetDeviceInfo(device, DeviceInfo.MaxWorkItemDimensions, out errorCode).CastTo<int>();
            errorCode.Check("GetDeviceInfo(DeviceInfo.MaxWorkItemDimensions)");
            Console.WriteLine($"MaxWorkItemDimensions: {maxWorkItemDimensions}");
        }
    }
}
