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
                Console.WriteLine($"platformName: {platformName}");
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

            var kernel1 = Cl.CreateKernel(program, "reductionVector", out errorCode);
            errorCode.Check("CreateKernelsInProgram");

            var kernel2 = Cl.CreateKernel(program, "reductionComplete", out errorCode);
            errorCode.Check("CreateKernelsInProgram");

            const int numValues = 1024 * 1024;
            const int numValuesPerWorkItem = 4;
            var globalWorkSize = numValues/numValuesPerWorkItem;
            var localWorkSize = Cl.GetKernelWorkGroupInfo(kernel1, device, KernelWorkGroupInfo.WorkGroupSize, out errorCode).CastTo<int>();
            errorCode.Check("GetKernelWorkGroupInfo(KernelWorkGroupInfo.WorkGroupSize)");
            Console.WriteLine($"localWorkSize: {localWorkSize}");

            const int value = 42;
            const int correctAnswer = numValues * value;

            var data = Enumerable.Repeat(value, numValues).Select(n => (float)n).ToArray();
            var sum = new float[1];

            using (var mem1 = new PinnedArrayOfStruct<float>(context, data, MemMode.ReadWrite))
            using (var mem2 = new PinnedArrayOfStruct<float>(context, sum, MemMode.WriteOnly))
            {
                var commandQueue = Cl.CreateCommandQueue(context, device, CommandQueueProperties.ProfilingEnable, out errorCode);
                errorCode.Check("CreateCommandQueue");

                errorCode = Cl.SetKernelArg(kernel1, 0, mem1.Buffer);
                errorCode.Check("SetKernelArg(0)");

                errorCode = Cl.SetKernelArg<float>(kernel1, 1, localWorkSize * 4);
                errorCode.Check("SetKernelArg(1)");

                errorCode = Cl.SetKernelArg(kernel2, 0, mem1.Buffer);
                errorCode.Check("SetKernelArg(0)");

                errorCode = Cl.SetKernelArg<float>(kernel2, 1, localWorkSize * 4);
                errorCode.Check("SetKernelArg(1)");

                errorCode = Cl.SetKernelArg(kernel2, 2, mem2.Buffer);
                errorCode.Check("SetKernelArg(2)");

                var kernel1Events = new List<Event>();

                for(;;)
                {
                    Event e;
                    errorCode = Cl.EnqueueNDRangeKernel(
                        commandQueue,
                        kernel1,
                        1, // workDim
                        null, // globalWorkOffset
                        new[] { (IntPtr)globalWorkSize },
                        new[] { (IntPtr)localWorkSize },
                        0, // numEventsInWaitList
                        null, // eventWaitList
                        out e);
                    errorCode.Check("EnqueueNDRangeKernel");
                    kernel1Events.Add(e);
                    globalWorkSize /= localWorkSize;
                    if (globalWorkSize <= localWorkSize) break;
                }

                Console.WriteLine($"Number of kernel1 invocations: {kernel1Events.Count}");

                Event kernel2Event;
                errorCode = Cl.EnqueueNDRangeKernel(
                    commandQueue,
                    kernel2,
                    1, // workDim
                    null, // globalWorkOffset
                    new[] { (IntPtr)globalWorkSize },
                    null, // localWorkSize
                    0, // numEventsInWaitList
                    null, // eventWaitList
                    out kernel2Event);
                errorCode.Check("EnqueueNDRangeKernel");

                Event readEvent;
                errorCode = Cl.EnqueueReadBuffer(
                    commandQueue,
                    mem2.Buffer,
                    Bool.False, // blockingRead
                    IntPtr.Zero, // offsetInBytes
                    (IntPtr)mem2.Size,
                    mem2.Handle,
                    0, // numEventsInWaitList
                    null, // eventWaitList
                    out readEvent);
                errorCode.Check("EnqueueReadBuffer");

                errorCode = Cl.Finish(commandQueue);
                errorCode.Check("Finish");

                var start1 = Cl.GetEventProfilingInfo(kernel1Events.First(), ProfilingInfo.Start, out errorCode).CastTo<long>();
                errorCode.Check("GetEventProfilingInfo(ProfilingInfo.Start)");
                var end1 = Cl.GetEventProfilingInfo(kernel2Event, ProfilingInfo.End, out errorCode).CastTo<long>();
                errorCode.Check("GetEventProfilingInfo(ProfilingInfo.End)");

                var start2 = Cl.GetEventProfilingInfo(readEvent, ProfilingInfo.Start, out errorCode).CastTo<long>();
                errorCode.Check("GetEventProfilingInfo(ProfilingInfo.Start)");
                var end2 = Cl.GetEventProfilingInfo(readEvent, ProfilingInfo.End, out errorCode).CastTo<long>();
                errorCode.Check("GetEventProfilingInfo(ProfilingInfo.End)");

                Console.WriteLine($"kernel1/kernel2 elapsed time: {end1 - start1:N0}ns");
                Console.WriteLine($"read buffer elapsed time: {end2 - start2:N0}ns");
            }

            Console.WriteLine($"OpenCL final answer: {Math.Truncate(sum[0]):N0}; Correct answer: {correctAnswer:N0}");
        }
    }
}
