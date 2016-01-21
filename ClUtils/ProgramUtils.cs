using System;
using System.IO;
using System.Linq;
using System.Reflection;
using OpenCL.Net;

namespace ClUtils
{
    public static class ProgramUtils
    {
        public static string GetProgramSourceFromResource(Assembly assembly, string resourceName)
        {
            using (var stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null) throw new ApplicationException($"Failed to load resource {resourceName}");
                var streamReader = new StreamReader(stream);
                return streamReader.ReadToEnd();
            }
        }

        public static Program BuildProgramForDevice(Context context, Device device, string source)
        {
            var strings = new[] { source };
            var lengths = new[] { (IntPtr)source.Length };

            ErrorCode errorCode;

            var program = Cl.CreateProgramWithSource(context, (uint)strings.Length, strings, lengths, out errorCode);
            errorCode.Check("CreateProgramWithSource");

            errorCode = Cl.BuildProgram(program, 1, new[] { device }, string.Empty, null, IntPtr.Zero);
            if (errorCode == ErrorCode.BuildProgramFailure)
            {
                var log = Cl.GetProgramBuildInfo(program, device, ProgramBuildInfo.Log, out errorCode).ToString();
                throw new ApplicationException($"BuildProgram failed:{System.Environment.NewLine}{log}");
            }
            errorCode.Check("BuildProgram");

            return program;
        }

        public static void SaveDisassembly(Program program, string fileName)
        {
            ErrorCode errorCode;

            var numDevices = Cl.GetProgramInfo(program, ProgramInfo.NumDevices, out errorCode).CastTo<int>();
            errorCode.Check("GetProgramInfo(ProgramInfo.NumDevices)");

            var binSizes = Cl.GetProgramInfo(program, ProgramInfo.BinarySizes, out errorCode).CastToArray<int>(numDevices);
            errorCode.Check("GetProgramInfo(ProgramInfo.BinarySizes)");

            var binSize = binSizes[0];
            Console.WriteLine($"binSize: {binSize}");

            var bufferArray = new InfoBufferArray(binSizes.Select(bs => new InfoBuffer((IntPtr) bs)).ToArray());
            IntPtr ret;
            errorCode = Cl.GetProgramInfo(program, ProgramInfo.Binaries, bufferArray.Size, bufferArray, out ret);
            errorCode.Check("GetProgramInfo(ProgramInfo.Binaries)");

            var baseFileName = Path.GetFileNameWithoutExtension(fileName);
            var extension = Path.GetExtension(fileName);

            foreach (var deviceNum in Enumerable.Range(0, numDevices))
            {
                var disassemblyBytes = bufferArray[deviceNum].CastToArray<byte>(binSize);
                File.WriteAllBytes($"{baseFileName}_{deviceNum}{extension}", disassemblyBytes);
            }
        }
    }
}
