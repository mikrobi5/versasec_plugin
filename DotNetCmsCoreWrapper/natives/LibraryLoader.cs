using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace VSec.DotNet.CmsCore.Wrapper.natives
{
    public class LibraryLoader : System.Runtime.Loader.AssemblyLoadContext
    {
        [DllImport("kernel32")]
        private static extern IntPtr LoadLibrary(string path);

        [DllImport("kernel32.dll")]
        static extern IntPtr GetProcAddress(IntPtr hModule, String procname);

        [DllImport("libdl")]
        private static extern IntPtr dlopen(string path, int flags);

        [DllImport("libdl")]
        static extern IntPtr dlerror();

        [DllImport("libdl")]
        static extern IntPtr dlsym(IntPtr handle, String symbol);

        public delegate IntPtr SymbolLookupDelegate(IntPtr addr, string name);

        public delegate IntPtr LoadLibraryDelegate(string libName, out SymbolLookupDelegate symbolLookup);

        public static LoadLibraryDelegate CustomLoadLibrary { get; set; }

        public static string NativeLibraryPath { get; private set; }

        protected override Assembly Load(AssemblyName assemblyName)
        {
            if (assemblyName.Name == "nng.NETCore")
                return LoadFromAssemblyPath("/Users/jake/nng.NETCore/bin/Debug/netstandard2.0/nng.NETCore.dll");
            // Return null to fallback on default load context
            return null;
        }
        protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
        {
            // Native nng shared library
            return LoadUnmanagedDllFromPath("/Users/jake/nng/build/libnng.dylib");
        }

        static LibraryLoader()
        {
            // Figure out which OS we're on.  Windows or "other".
            if (Environment.OSVersion.Platform == PlatformID.Unix ||
                        Environment.OSVersion.Platform == PlatformID.MacOSX ||
                        // Legacy mono value.  See https://www.mono-project.com/docs/faq/technical/
                        (int)Environment.OSVersion.Platform == 128)
            {
                LoadPosixLibrary();
            }
            else
            {
               // LoadWindowsLibrary();
            }
        }

        static void LoadPosixLibrary()
        {
            const int RTLD_NOW = 2;
            string rootDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string assemblyDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            // Environment.OSVersion.Platform returns "Unix" for Unix or OSX, so use RuntimeInformation here
            var isOsx = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
            string libFile = isOsx ? "libnng.dylib" : "libnng.so";
            // x86 variants aren't in https://docs.microsoft.com/en-us/dotnet/core/rid-catalog
            string arch = (isOsx ? "osx" : "linux") + "-" + (Environment.Is64BitProcess ? "x64" : "x86");

            // Search a few different locations for our native assembly
            var paths = new[]
                {
            // This is where native libraries in our nupkg should end up
            Path.Combine(rootDirectory, "runtimes", arch, "native", libFile),
            // The build output folder
            Path.Combine(rootDirectory, libFile),
            Path.Combine("/usr/local/lib", libFile),
            Path.Combine("/usr/lib", libFile)
        };

            foreach (var path in paths)
            {
                if (path == null)
                {
                    continue;
                }

                if (File.Exists(path))
                {
                    var addr = dlopen(path, RTLD_NOW);
                    if (addr == IntPtr.Zero)
                    {
                        // Not using NanosmgException because it depends on nn_errno.
                        var error = Marshal.PtrToStringAnsi(dlerror());
                        throw new Exception("dlopen failed: " + path + " : " + error);
                    }
                    NativeLibraryPath = path;
                    return;
                }
            }

            throw new Exception("dlopen failed: unable to locate library " + libFile + ". Searched: " + paths.Aggregate((a, b) => a + "; " + b));
        }

        static string calculatexdir(string assemblyPath, string framework, string libFile)
        {
            string dir = assemblyPath;
            if (string.IsNullOrEmpty(dir)) { return null; }

            dir = Path.GetDirectoryName(dir);
            if (string.IsNullOrEmpty(dir)) { return null; }

            dir = Path.GetDirectoryName(dir);
            if (string.IsNullOrEmpty(dir)) { return null; }

            return Path.Combine(dir, "content", framework, Environment.Is64BitProcess ? "x64" : "x86", libFile);
        }

        static IntPtr LoadWindowsLibrary(string libName, out SymbolLookupDelegate symbolLookup)
        {
            string libFile = libName + ".dll";
            string rootDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string assemblyDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            var paths = new[]
                {
                    calculatexdir(assemblyDirectory, "net40", libFile),
                    Path.Combine(assemblyDirectory, "bin", Environment.Is64BitProcess ? "x64" : "x86", libFile),
                    Path.Combine(assemblyDirectory, Environment.Is64BitProcess ? "x64" : "x86", libFile),
                    Path.Combine(assemblyDirectory, libFile),

                    Path.Combine(rootDirectory, "bin", Environment.Is64BitProcess ? "x64" : "x86", libFile),
                    Path.Combine(rootDirectory, Environment.Is64BitProcess ? "x64" : "x86", libFile),
                    Path.Combine(rootDirectory, libFile)
                };

            foreach (var path in paths)
            {
                if (path == null)
                {
                    continue;
                }

                if (File.Exists(path))
                {
                    var addr = LoadLibrary(path);
                    if (addr == IntPtr.Zero)
                    {
                        // Not using NanomsgException because it depends on nn_errno.
                        throw new Exception("LoadLibrary failed: " + path);
                    }
                    symbolLookup = GetProcAddress;
                    NativeLibraryPath = path;
                    return addr;
                }
            }

            throw new Exception("LoadLibrary failed: unable to locate library " + libFile + ". Searched: " + paths.Aggregate((a, b) => a + "; " + b));
        }
    }

}
