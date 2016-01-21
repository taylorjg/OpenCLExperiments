using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using ClUtils;
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
            errorCode.Check("GetPlatformIDs");

            var platformNames = platformIds.Select(platformId =>
            {
                ErrorCode innerErrorCode;
                var platformName = Cl.GetPlatformInfo(platformId, PlatformInfo.Name, out innerErrorCode);
                innerErrorCode.Check("GetPlatformIDs");
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

        private static void DumpWorkGroupInfo(Context context, Device device)
        {
            const string resourceName = "GpuFpConfig.sum.cl";

            var source = GetProgramSourceFromResource(resourceName);

            var strings = new[] {source};
            var lengths = new[] {(IntPtr) source.Length};

            ErrorCode errorCode;
            var program = Cl.CreateProgramWithSource(context, (uint)strings.Length, strings, lengths, out errorCode);
            errorCode.Check("CreateProgramWithSource");

            errorCode = Cl.BuildProgram(program, 1, new [] {device}, string.Empty, null, IntPtr.Zero);
            errorCode.Check("BuildProgram");

            var binSizes = Cl.GetProgramInfo(program, ProgramInfo.BinarySizes, out errorCode).CastToArray<int>(1);
            errorCode.Check("GetProgramInfo(ProgramInfo.BinarySizes)");
            var binSize = binSizes[0];
            Console.WriteLine($"binSize: {binSize}");

            var buffer = new InfoBuffer((IntPtr)binSize);
            var buffers = new InfoBufferArray(buffer);
            IntPtr ret;
            errorCode = Cl.GetProgramInfo(program, ProgramInfo.Binaries, (IntPtr)4, buffers, out ret);
            errorCode.Check("GetProgramInfo(ProgramInfo.Binaries)");

            var disassemblyBytes = buffer.CastToArray<byte>(binSize);
            File.WriteAllBytes($"{resourceName}_disassembly.txt", disassemblyBytes);

            var kernels = Cl.CreateKernelsInProgram(program, out errorCode);
            errorCode.Check("CreateKernelsInProgram");
            var kernel = kernels[0];

            var workGroupSize = Cl.GetKernelWorkGroupInfo(kernel, device, KernelWorkGroupInfo.WorkGroupSize, out errorCode).CastTo<int>();
            errorCode.Check("GetKernelWorkGroupInfo(KernelWorkGroupInfo.WorkGroupSize)");
            Console.WriteLine($"WorkGroupSize: {workGroupSize}");

            var localMemSize = Cl.GetKernelWorkGroupInfo(kernel, device, KernelWorkGroupInfo.LocalMemSize, out errorCode).CastTo<int>();
            errorCode.Check("GetKernelWorkGroupInfo(KernelWorkGroupInfo.LocalMemSize)");
            Console.WriteLine($"LocalMemSize: {localMemSize}");

            var compileWorkGroupSize = Cl.GetKernelWorkGroupInfo(kernel, device, KernelWorkGroupInfo.CompileWorkGroupSize, out errorCode).CastTo<int>();
            errorCode.Check("GetKernelWorkGroupInfo(KernelWorkGroupInfo.CompileWorkGroupSize)");
            Console.WriteLine($"CompileWorkGroupSize: {compileWorkGroupSize}");

            var preferredWorkGroupSizeMultiple = Cl.GetKernelWorkGroupInfo(kernel, device, (KernelWorkGroupInfo)0x11B3, out errorCode).CastTo<int>();
            errorCode.Check("GetKernelWorkGroupInfo(KernelWorkGroupInfo.PreferredWorkGroupSizeMultiple)");
            Console.WriteLine($"PreferredWorkGroupSizeMultiple: {preferredWorkGroupSizeMultiple}");

            var privateMemSize = Cl.GetKernelWorkGroupInfo(kernel, device, (KernelWorkGroupInfo)0x11B4, out errorCode).CastTo<int>();
            errorCode.Check("GetKernelWorkGroupInfo(KernelWorkGroupInfo.PrivateMemSize)");
            Console.WriteLine($"PrivateMemSize: {privateMemSize}");

            const int size = 1024;

            var floatsA = Enumerable.Range(1, size).Select(n => (float)n).ToArray();
            var floatsB = Enumerable.Range(1, size).Select(n => (float)n).ToArray();
            var floatsC = new float[size];

            using (var mem1 = MyPinnedArrayOfStruct.Create(context, floatsA))
            using (var mem2 = MyPinnedArrayOfStruct.Create(context, floatsB))
            using (var mem3 = MyPinnedArrayOfStruct.Create(context, floatsC, MemMode.WriteOnly))
            {
                var commandQueue = Cl.CreateCommandQueue(context, device, CommandQueueProperties.ProfilingEnable, out errorCode);
                errorCode.Check("CreateCommandQueue");

                errorCode = Cl.SetKernelArg(kernel, 0, mem1.Buffer); errorCode.Check();
                errorCode = Cl.SetKernelArg(kernel, 1, mem2.Buffer); errorCode.Check();
                errorCode = Cl.SetKernelArg(kernel, 2, mem3.Buffer); errorCode.Check();

                var globalWorkSize = new[] { (IntPtr)size };
                Event e1;
                errorCode = Cl.EnqueueNDRangeKernel(
                    commandQueue,
                    kernel,
                    (uint)globalWorkSize.Length, // workDim
                    null, // globalWorkOffset
                    globalWorkSize,
                    null, // localWorkSize
                    0, // numEventsInWaitList
                    null, // eventWaitList
                    out e1);
                errorCode.Check("EnqueueNDRangeKernel");

                Event e2;
                errorCode = Cl.EnqueueReadBuffer(
                    commandQueue,
                    mem3.Buffer,
                    Bool.False, // blockingRead
                    IntPtr.Zero, // offsetInBytes
                    (IntPtr)mem3.Size,
                    mem3.Handle,
                    0, // numEventsInWaitList
                    null, // eventWaitList
                    out e2);
                errorCode.Check("EnqueueReadBuffer");

                var evs = new[] { e2 };
                errorCode = Cl.WaitForEvents((uint)evs.Length, evs);
                errorCode.Check("WaitForEvents");
            }

            Console.WriteLine($"floatsC[0]: {floatsC[0]}");
            Console.WriteLine($"floatsC[1023]: {floatsC[1023]}");
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
