using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using ClUtils;
using OpenCL.Net;
using Environment = OpenCL.Net.Environment;

namespace SaveBinaries
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
                SaveBinaries(context, device);

            Console.WriteLine();
        }

        private static void SaveBinaries(Context context, Device device)
        {
            const string resourceName = "SaveBinaries.sum.cl";
            var source = ProgramUtils.GetProgramSourceFromResource(Assembly.GetExecutingAssembly(), resourceName);
            var program = ProgramUtils.BuildProgramForDevice(context, device, source);
            ProgramUtils.SaveBinaries(program, $"{Path.GetFileNameWithoutExtension(resourceName)}_binary.txt");
        }
    }
}
