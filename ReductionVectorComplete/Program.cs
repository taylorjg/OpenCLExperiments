using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ClUtils;
using OpenCL.Net;
using Environment = OpenCL.Net.Environment;

namespace ReductionVectorComplete
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

        private static void EnumerateDevices(Context context, IEnumerable<Device> devices)
        {
            foreach (var device in devices)
            {
                Reduction(context, device);
            }

            Console.WriteLine();
        }

        private static void Reduction(Context context, Device device)
        {
            const string resourceName = "ReductionVectorComplete.reduction.cl";

            var source = ProgramUtils.GetProgramSourceFromResource(Assembly.GetExecutingAssembly(), resourceName);
            var program = ProgramUtils.BuildProgramForDevice(context, device, source);

            ErrorCode errorCode;
            var kernels = Cl.CreateKernelsInProgram(program, out errorCode);
            errorCode.Check("CreateKernelsInProgram");

            var kernel = kernels[0];

            const int numValues = 1024 * 1024;
            const int numValuesPerWorkItem = 4;
            const int globalWorkSize = numValues/numValuesPerWorkItem;
            var localWorkSize = Cl.GetKernelWorkGroupInfo(kernel, device, KernelWorkGroupInfo.WorkGroupSize, out errorCode).CastTo<int>();
            errorCode.Check("GetKernelWorkGroupInfo(KernelWorkGroupInfo.WorkGroupSize)");
            Console.WriteLine($"localWorkSize: {localWorkSize}");
            var numWorkGroups = globalWorkSize/localWorkSize;

            const int value = 42;
            const int correctAnswer = numValues * value;

            var data = Enumerable.Repeat(value, numValues).Select(n => (float)n).ToArray();
            var workGroupResults = new float[numWorkGroups*numValuesPerWorkItem];

            using (var mem1 = new PinnedArrayOfStruct<float>(context, data))
            using (var mem2 = new PinnedArrayOfStruct<float>(context, workGroupResults, MemMode.WriteOnly))
            {
                var commandQueue = Cl.CreateCommandQueue(context, device, CommandQueueProperties.ProfilingEnable, out errorCode);
                errorCode.Check("CreateCommandQueue");

                errorCode = Cl.SetKernelArg(kernel, 0, mem1.Buffer);
                errorCode.Check("SetKernelArg(0)");

                errorCode = Cl.SetKernelArg<float>(kernel, 1, localWorkSize * 4);
                errorCode.Check("SetKernelArg(1)");

                errorCode = Cl.SetKernelArg(kernel, 2, mem2.Buffer);
                errorCode.Check("SetKernelArg(2)");

                Event e1;
                errorCode = Cl.EnqueueNDRangeKernel(
                    commandQueue,
                    kernel,
                    1, // workDim
                    null, // globalWorkOffset
                    new[] {(IntPtr) globalWorkSize},
                    new[] {(IntPtr) localWorkSize},
                    0, // numEventsInWaitList
                    null, // eventWaitList
                    out e1);
                errorCode.Check("EnqueueNDRangeKernel");

                Event e2;
                errorCode = Cl.EnqueueReadBuffer(
                    commandQueue,
                    mem2.Buffer,
                    Bool.False, // blockingRead
                    IntPtr.Zero, // offsetInBytes
                    (IntPtr)mem2.Size,
                    mem2.Handle,
                    0, // numEventsInWaitList
                    null, // eventWaitList
                    out e2);
                errorCode.Check("EnqueueReadBuffer");

                var evs = new[] { e2 };
                errorCode = Cl.WaitForEvents((uint)evs.Length, evs);
                errorCode.Check("WaitForEvents");

                var start1 = Cl.GetEventProfilingInfo(e1, ProfilingInfo.Start, out errorCode).CastTo<long>();
                errorCode.Check("GetEventProfilingInfo(ProfilingInfo.Start)");
                var end1 = Cl.GetEventProfilingInfo(e1, ProfilingInfo.End, out errorCode).CastTo<long>();
                errorCode.Check("GetEventProfilingInfo(ProfilingInfo.End)");

                var start2 = Cl.GetEventProfilingInfo(e2, ProfilingInfo.Start, out errorCode).CastTo<long>();
                errorCode.Check("GetEventProfilingInfo(ProfilingInfo.Start)");
                var end2 = Cl.GetEventProfilingInfo(e2, ProfilingInfo.End, out errorCode).CastTo<long>();
                errorCode.Check("GetEventProfilingInfo(ProfilingInfo.End)");

                Console.WriteLine($"e1 elapsed time: {end1 - start1:N0}ns");
                Console.WriteLine($"e2 elapsed time: {end2 - start2:N0}ns");
            }

            var finalAnswer = Math.Truncate(workGroupResults.Sum());
            Console.WriteLine($"OpenCL final answer: {finalAnswer:N0}; Correct answer: {correctAnswer:N0}");
        }
    }
}
