using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ClUtils;
using OpenCL.Net;
using Environment = OpenCL.Net.Environment;

namespace RunKernel
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
                RunKernel(context, device);
            }
        }

        private static void RunKernel(Context context, Device device)
        {
            const string resourceName = "RunKernel.sum.cl";

            var source = ProgramUtils.GetProgramSourceFromResource(Assembly.GetExecutingAssembly(), resourceName);
            var program = ProgramUtils.BuildProgramForDevice(context, device, source);

            ErrorCode errorCode;
            var kernel = Cl.CreateKernel(program, "sum", out errorCode);
            errorCode.Check("CreateKernel");

            const int size = 1024;

            var floatsA = Enumerable.Range(1, size).Select(n => (float)n).ToArray();
            var floatsB = Enumerable.Range(1, size).Select(n => (float)n).ToArray();
            var floatsC = new float[size];

            using (var mem1 = new PinnedArrayOfStruct<float>(context, floatsA))
            using (var mem2 = new PinnedArrayOfStruct<float>(context, floatsB))
            using (var mem3 = new PinnedArrayOfStruct<float>(context, floatsC, MemMode.WriteOnly))
            {
                KernelRunner.RunKernel(context, device, kernel, size, new[] { 2 }, mem1, mem2, mem3);
            }

            Console.WriteLine($"floatsC[0]: {floatsC[0]}");
            Console.WriteLine($"floatsC[1023]: {floatsC[1023]}");
        }
    }
}
