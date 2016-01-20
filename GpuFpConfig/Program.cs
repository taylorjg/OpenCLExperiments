using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using OpenCL.Net;
using Environment = OpenCL.Net.Environment;

namespace GpuFpConfig
{
    internal static class Program
    {
        private static void Main(/* string[] args */)
        {
            ErrorCode errorCode;
            var platformIds = Cl.GetPlatformIDs(out errorCode);
            Check(errorCode, "GetPlatformIDs");

            var platformNames = platformIds.Select(platformId =>
            {
                ErrorCode innerErrorCode;
                var platformName = Cl.GetPlatformInfo(platformId, PlatformInfo.Name, out innerErrorCode);
                Check(innerErrorCode, "GetPlatformIDs");
                return platformName.ToString();
            });

            foreach (var platformName in platformNames)
            {
                var environment = new Environment(platformName);
                EnumerateDevices(environment.Context, environment.Devices);
            }
        }

        private static void EnumerateDevices(Context context, IReadOnlyCollection<Device> devices)
        {
            foreach (var device in devices)
                DumpDeviceDetails(device);

            Console.WriteLine();

            foreach (var device in devices)
                DumpWorkGroupInfo(context, device);

            Console.WriteLine();
        }

        // /* cl_device_fp_config - bitfield */
        // #define CL_FP_DENORM                                (1 << 0)
        // #define CL_FP_INF_NAN                               (1 << 1)
        // #define CL_FP_ROUND_TO_NEAREST                      (1 << 2)
        // #define CL_FP_ROUND_TO_ZERO                         (1 << 3)
        // #define CL_FP_ROUND_TO_INF                          (1 << 4)
        // #define CL_FP_FMA                                   (1 << 5)
        // #define CL_FP_SOFT_FLOAT                            (1 << 6)
        // #define CL_FP_CORRECTLY_ROUNDED_DIVIDE_SQRT         (1 << 7)

        // https://www.khronos.org/registry/cl/api/2.1/cl.h
        private const int ClFpDenorm = (1 << 0);
        private const int ClFpInfNan = (1 << 1);
        private const int ClFpRoundToNearest = (1 << 2);
        private const int ClFpRoundToZero = (1 << 3);
        private const int ClFpRoundToInf = (1 << 4);
        private const int ClFpFma = (1 << 5);
        private const int ClFpSoftFloat = (1 << 6);
        private const int ClFpCorrectlyRoundedDivideSqrt = (1 << 7);

        private static void DumpDeviceDetails(Device device)
        {
            ErrorCode errorCode;

            var deviceName = Cl.GetDeviceInfo(device, DeviceInfo.Name, out errorCode).ToString();
            Check(errorCode, "GetDeviceInfo(DeviceInfo.Name)");
            Console.WriteLine($"DeviceInfo.Name: {deviceName}");

            var fpConfig = Cl.GetDeviceInfo(device, DeviceInfo.SingleFpConfig, out errorCode).CastTo<int>();
            Check(errorCode, "GetDeviceInfo(DeviceInfo.SingleFpConfig)");
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
            Console.WriteLine($"GlobalMemSize: {globalMemSize}");

            var localMemSize = Cl.GetDeviceInfo(device, DeviceInfo.LocalMemSize, out errorCode).CastTo<long>();
            Console.WriteLine($"LocalMemSize: {localMemSize}");

            var maxComputeUnits = Cl.GetDeviceInfo(device, DeviceInfo.MaxComputeUnits, out errorCode).CastTo<int>();
            Console.WriteLine($"MaxComputeUnits: {maxComputeUnits}");

            var maxWorkGroupSize = Cl.GetDeviceInfo(device, DeviceInfo.MaxWorkGroupSize, out errorCode).CastTo<int>();
            Console.WriteLine($"MaxWorkGroupSize: {maxWorkGroupSize}");

            var maxWorkItemSizes = Cl.GetDeviceInfo(device, DeviceInfo.MaxWorkItemSizes, out errorCode).CastTo<int>();
            Console.WriteLine($"MaxWorkItemSizes: {maxWorkItemSizes}");

            var maxWorkItemDimensions = Cl.GetDeviceInfo(device, DeviceInfo.MaxWorkItemDimensions, out errorCode).CastTo<int>();
            Console.WriteLine($"MaxWorkItemDimensions: {maxWorkItemDimensions}");
        }

        private static void Check(ErrorCode errorCode, string message)
        {
            if (errorCode != ErrorCode.Success)
                throw new Exception($"{message}: {errorCode}");
        }

        private static void DumpWorkGroupInfo(Context context, Device device)
        {
            var resourceName = "GpuFpConfig.sum.cl";
            var source = GetProgramSourceFromResource(resourceName);

            var strings = new[] {source};
            var lengths = new[] {(IntPtr) source.Length};

            ErrorCode errorCode;
            var program = Cl.CreateProgramWithSource(context, (uint)strings.Length, strings, lengths, out errorCode);
            errorCode.Check();

            errorCode = Cl.BuildProgram(program, 1, new [] {device}, string.Empty, null, IntPtr.Zero);
            errorCode.Check();

            var binSizes = Cl.GetProgramInfo(program, ProgramInfo.BinarySizes, out errorCode).CastToArray<int>(1);
            errorCode.Check();
            var binSize = binSizes[0];
            Console.WriteLine($"binSize: {binSize}");

            var buffer = new InfoBuffer((IntPtr)binSize);
            var buffers = new InfoBufferArray(buffer);
            IntPtr ret;
            errorCode = Cl.GetProgramInfo(program, ProgramInfo.Binaries, (IntPtr)4, buffers, out ret);
            errorCode.Check();

            var disassemblyBytes = buffer.CastToArray<byte>(binSize);
            File.WriteAllBytes($"{resourceName}_disassembly.txt", disassemblyBytes);

            var kernels = Cl.CreateKernelsInProgram(program, out errorCode);
            var kernel = kernels[0];

            var workGroupSize = Cl.GetKernelWorkGroupInfo(kernel, device, KernelWorkGroupInfo.WorkGroupSize, out errorCode).CastTo<int>();
            errorCode.Check();
            Console.WriteLine($"WorkGroupSize: {workGroupSize}");

            var localMemSize = Cl.GetKernelWorkGroupInfo(kernel, device, KernelWorkGroupInfo.LocalMemSize, out errorCode).CastTo<int>();
            errorCode.Check();
            Console.WriteLine($"LocalMemSize: {localMemSize}");

            var compileWorkGroupSize = Cl.GetKernelWorkGroupInfo(kernel, device, KernelWorkGroupInfo.CompileWorkGroupSize, out errorCode).CastTo<int>();
            errorCode.Check();
            Console.WriteLine($"CompileWorkGroupSize: {compileWorkGroupSize}");

            var preferredWorkGroupSizeMultiple = Cl.GetKernelWorkGroupInfo(kernel, device, (KernelWorkGroupInfo)0x11B3, out errorCode).CastTo<int>();
            errorCode.Check();
            Console.WriteLine($"PreferredWorkGroupSizeMultiple: {preferredWorkGroupSizeMultiple}");

            var privateMemSize = Cl.GetKernelWorkGroupInfo(kernel, device, (KernelWorkGroupInfo)0x11B4, out errorCode).CastTo<int>();
            errorCode.Check();
            Console.WriteLine($"PrivateMemSize: {privateMemSize}");

            const int size = 1024;
            var floatsA = Enumerable.Range(1, size).Select(n => (float)n).ToArray();
            var floatsB = Enumerable.Range(1, size).Select(n => (float)n).ToArray();
            var floatsC = new float[size];

            const int cb = sizeof (float)*size;
            var ha = Marshal.AllocHGlobal(cb);
            var hb = Marshal.AllocHGlobal(cb);
            var hc = Marshal.AllocHGlobal(cb);

            Marshal.Copy(floatsA, 0, ha, size);
            Marshal.Copy(floatsB, 0, hb, size);

            var ba = Cl.CreateBuffer(context, MemFlags.ReadOnly | MemFlags.UseHostPtr, (IntPtr)cb, ha, out errorCode);
            errorCode.Check();

            var bb = Cl.CreateBuffer(context, MemFlags.ReadOnly | MemFlags.UseHostPtr, (IntPtr)cb, hb, out errorCode);
            errorCode.Check();

            var bc = Cl.CreateBuffer(context, MemFlags.WriteOnly | MemFlags.UseHostPtr, (IntPtr)cb, hc, out errorCode);
            errorCode.Check();

            var commandQueue = Cl.CreateCommandQueue(context, device, CommandQueueProperties.ProfilingEnable, out errorCode);
            errorCode.Check();

            errorCode = Cl.SetKernelArg(kernel, 0, ba);
            errorCode.Check();

            errorCode = Cl.SetKernelArg(kernel, 1, bb);
            errorCode.Check();

            errorCode = Cl.SetKernelArg(kernel, 2, bc);
            errorCode.Check();

            var globalWorkSize = new[] { (IntPtr)size};
            Event e1;
            errorCode = Cl.EnqueueNDRangeKernel(
                commandQueue,
                kernel,
                (uint) globalWorkSize.Length, // workDim
                null, // globalWorkOffset
                globalWorkSize,
                null, // localWorkSize
                0, // numEventsInWaitList
                null, // eventWaitList
                out e1);
            errorCode.Check();

            Event e2;
            errorCode = Cl.EnqueueReadBuffer(commandQueue, bc, Bool.False, IntPtr.Zero, (IntPtr)cb, hc, 0, null, out e2);
            errorCode.Check();

            var evs = new[] {e2};
            Cl.WaitForEvents((uint)evs.Length, evs);

            Marshal.Copy(hc, floatsC, 0, size);
        }

        private static string GetProgramSourceFromResource(string resourceName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            using (var stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null) throw new ApplicationException($"Failed to load resource {resourceName}");
                var streamReader = new StreamReader(stream);
                return streamReader.ReadToEnd();
            }
        }
    }
}
