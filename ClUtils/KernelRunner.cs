using System;
using System.Collections.Generic;
using System.Linq;
using OpenCL.Net;

namespace ClUtils
{
    public static class KernelRunner
    {
        public static void RunKernel(
            Context context,
            Device device,
            Kernel kernel,
            int size,
            IEnumerable<int> indicesOfPinnedArraysToReadBack,
            params IPinnedArrayOfStruct[] pinnedArrays)
        {
            ErrorCode errorCode;

            var commandQueue = Cl.CreateCommandQueue(context, device, CommandQueueProperties.ProfilingEnable, out errorCode);
            errorCode.Check("CreateCommandQueue");

            var setKernelArgErrorCodes = pinnedArrays.Select((pinnedArray, index) =>
            {
                var innerErrorCode = Cl.SetKernelArg(kernel, (uint)index, pinnedArray.Buffer);
                innerErrorCode.Check();
                return innerErrorCode;
            }).ToList();

            var globalWorkSize = new[] {(IntPtr) size};

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

            var eventsToWaitFor = new List<Event>();

            foreach (var index in indicesOfPinnedArraysToReadBack)
            {
                Event e2;
                errorCode = Cl.EnqueueReadBuffer(
                    commandQueue,
                    pinnedArrays[index].Buffer,
                    Bool.False, // blockingRead
                    IntPtr.Zero, // offsetInBytes
                    (IntPtr)pinnedArrays[index].Size,
                    pinnedArrays[index].Handle,
                    0, // numEventsInWaitList
                    null, // eventWaitList
                    out e2);
                errorCode.Check("EnqueueReadBuffer");

                eventsToWaitFor.Add(e2);
            }

            var evs = eventsToWaitFor.ToArray();
            errorCode = Cl.WaitForEvents((uint)evs.Length, evs);
            errorCode.Check("WaitForEvents");
        }
    }
}
