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
                Dump.DeviceDetails(device);

            Console.WriteLine();

            foreach (var device in devices)
                DumpWorkGroupInfo(context, device);

            Console.WriteLine();
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
