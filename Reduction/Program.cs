using System;
using System.Collections.Generic;
using System.Linq;
using ClUtils;
using OpenCL.Net;
using Environment = OpenCL.Net.Environment;

namespace Reduction
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
            {
                Reduction(context, device);
            }

            Console.WriteLine();
        }

        private static void Reduction(Context context, Device device)
        {
        }
    }
}
