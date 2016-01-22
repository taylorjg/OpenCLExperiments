using System;
using System.Collections.Generic;
using ClUtils;
using OpenCL.Net;
using Environment = OpenCL.Net.Environment;

namespace VersionInfo
{
    internal static class Program
    {
        private static void Main(/* string[] args */)
        {
            ErrorCode errorCode;
            var platformIds = Cl.GetPlatformIDs(out errorCode);
            errorCode.Check("GetPlatformIDs");

            foreach (var platformId in platformIds)
            {
                var platformName = Cl.GetPlatformInfo(platformId, PlatformInfo.Name, out errorCode).ToString();
                errorCode.Check("GetPlatformInfo(PlatformInfo.Name)");
                Console.WriteLine($"Platform name: {platformName}");

                var vendor = Cl.GetPlatformInfo(platformId, PlatformInfo.Vendor, out errorCode).ToString();
                errorCode.Check("GetPlatformInfo(PlatformInfo.Vendor)");
                Console.WriteLine($"Platform vendor: {vendor}");

                var version = Cl.GetPlatformInfo(platformId, PlatformInfo.Version, out errorCode).ToString();
                errorCode.Check("GetPlatformInfo(PlatformInfo.Version)");
                Console.WriteLine($"Platform version: {version}");

                var environment = new Environment(platformName);
                EnumerateDevices(environment.Devices);
            }
        }

        private static void EnumerateDevices(IEnumerable<Device> devices)
        {
            foreach (var device in devices)
            {
                DumpDeviceVersionInfo(device);
            }

            Console.WriteLine();
        }

        private static void DumpDeviceVersionInfo(Device device)
        {
            ErrorCode errorCode;

            var deviceName = Cl.GetDeviceInfo(device, DeviceInfo.Name, out errorCode).ToString();
            errorCode.Check("GetDeviceInfo(DeviceInfo.Name)");
            Console.WriteLine($"Device name: {deviceName}");

            var version = Cl.GetDeviceInfo(device, DeviceInfo.Version, out errorCode).ToString();
            errorCode.Check("GetDeviceInfo(DeviceInfo.Version)");
            Console.WriteLine($"Device version: {version}");

            var driverVersion = Cl.GetDeviceInfo(device, DeviceInfo.DriverVersion, out errorCode).ToString();
            errorCode.Check("GetDeviceInfo(DeviceInfo.DriverVersion)");
            Console.WriteLine($"Device driver version: {driverVersion}");

            var openClCVersion = Cl.GetDeviceInfo(device, (DeviceInfo)0x103D, out errorCode).ToString();
            errorCode.Check("GetDeviceInfo(CL_DEVICE_OPENCL_C_VERSION = 0x103D)");
            Console.WriteLine($"Device OpenCL C version: {openClCVersion}");

            var type = Cl.GetDeviceInfo(device, DeviceInfo.Type, out errorCode).CastTo<int>();
            errorCode.Check("GetDeviceInfo(DeviceInfo.Type)");
            switch (type)
            {
                case 1 << 0:
                    Console.WriteLine("Device type: CL_DEVICE_TYPE_DEFAULT");
                    break;
                case 1 << 1:
                    Console.WriteLine("Device type: CL_DEVICE_TYPE_CPU");
                    break;
                case 1 << 2:
                    Console.WriteLine("Device type: CL_DEVICE_TYPE_GPU");
                    break;
                case 1 << 3:
                    Console.WriteLine("Device type: CL_DEVICE_TYPE_ACCELERATOR");
                    break;
                case 1 << 4:
                    Console.WriteLine("Device type: CL_DEVICE_TYPE_CUSTOM");
                    break;
                default:
                    Console.WriteLine("Device type: ?");
                    break;
            }

            var localMemSize = Cl.GetDeviceInfo(device, DeviceInfo.LocalMemSize, out errorCode).CastTo<long>();
            errorCode.Check("GetDeviceInfo(DeviceInfo.LocalMemSize)");
            Console.WriteLine($"Device local mem size: {localMemSize:N0}");

            var globalMemSize = Cl.GetDeviceInfo(device, DeviceInfo.GlobalMemSize, out errorCode).CastTo<long>();
            errorCode.Check("GetDeviceInfo(DeviceInfo.GlobalMemSize)");
            Console.WriteLine($"Device global mem size: {globalMemSize:N0}");
        }
    }
}
