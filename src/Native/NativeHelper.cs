using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace ImageLibrary.Native
{
    static class NativeHelper
    {
        private static bool _delegateSet;

        public static bool IsPresent(string library)
        {
            var platform = GetPlatformMonicker();
            var architecture = platform == "osx" ? "64" : GetArchitecture();
            var extension = GetExtension();

            var root = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);

            var libraryPath = Path.Combine(root, "runtimes", $"{platform}_x{architecture}", $"{library}.{extension}");
            return File.Exists(libraryPath);
        }

        public static void SetDllImportResolver()
        {
            // Set delegate once
            if (_delegateSet)
                return;

            NativeLibrary.SetDllImportResolver(typeof(NativeHelper).Assembly, NativeHelper.ResolveImport);
            _delegateSet = true;
        }

        public static IntPtr ResolveImport(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
        {
            // If resolve is requested by any other assembly than Kanvas
            var currentAssembly = typeof(NativeHelper).Assembly;
            if (assembly != currentAssembly)
                return NativeLibrary.Load(libraryName, assembly, searchPath);

            var platform = GetPlatformMonicker();
            var architecture = platform == "osx" ? "64" : GetArchitecture();
            var extension = GetExtension();

            var root = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);

            // Try get resource depending on the platform
            var libraryPath = Path.Combine(root, "runtimes", $"{platform}-x{architecture}", $"{libraryName}.{extension}");
            if (!File.Exists(libraryPath))
                throw new InvalidOperationException($"The file '{libraryPath}' could not be found.");

            // Load extracted library
            return NativeLibrary.Load(libraryPath);
        }

        public static GCHandle PinObject(object obj)
        {
            return GCHandle.Alloc(obj, GCHandleType.Pinned);
        }

        public static void FreePinnedObject(GCHandle handle)
        {
            handle.Free();
        }

        public static IntPtr MarshalObject(object obj)
        {
            var objSize = Marshal.SizeOf(obj);
            var ptr = Marshal.AllocHGlobal(objSize);
            Marshal.StructureToPtr(obj, ptr, true);

            return ptr;
        }

        public static void FreeObject(IntPtr ptr)
        {
            Marshal.FreeHGlobal(ptr);
        }

        private static string GetPlatformMonicker()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return "win";

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return "linux";

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return "osx";

            throw new InvalidOperationException($"Unsupported platform {RuntimeInformation.OSDescription}.");
        }

        private static string GetArchitecture()
        {
            if (RuntimeInformation.OSArchitecture.HasFlag(Architecture.X64))
                return "64";

            if (RuntimeInformation.OSArchitecture.HasFlag(Architecture.X86))
                return "86";

            throw new InvalidOperationException($"Unsupported architecture {RuntimeInformation.OSArchitecture}.");
        }

        private static string GetExtension()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return "dll";

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return "so";

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return "dylib";

            throw new InvalidOperationException($"Unsupported platform {RuntimeInformation.OSDescription}.");
        }
    }
}
