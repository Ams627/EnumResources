using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace EnumResources
{
    class Program
    {
        //        BOOL CALLBACK EnumResTypeProc(
        //  _In_opt_ HMODULE  hModule,
        //  _In_ LPTSTR   lpszType,
        //  _In_ LONG_PTR lParam
        //);

        public delegate bool EnumResTypeProc(long hModule, string lpType, long lParam);
        public delegate bool EnumResNameProcType(long hModule, string type, long name, long lParam);

        [DllImport("kernel32.dll")]
        static extern bool EnumResourceTypes(IntPtr hModule, EnumResTypeProc lpEnumFunc, long lParam);

        [DllImport("kernel32")]
        public static extern IntPtr LoadLibrary(string path);

        [DllImport("kernel32.dll", SetLastError = true)]
        public extern static bool EnumResourceNames(IntPtr hModule, string lpszType, EnumResNameProcType lpEnumFunc, long lParam);


        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr LoadResource(long hModule, IntPtr hResInfo);

        [DllImport("kernel32.dll")]
        static extern IntPtr FindResourceEx(IntPtr hModule, IntPtr lpType, IntPtr lpName, ushort wLanguage);

        [DllImport("kernel32.dll")]
        static extern IntPtr FindResource(long hModule, string lpName, string type);

        [DllImport("kernel32.dll")]
        static extern IntPtr FindResource(long hModule, int rid, string type);

        [DllImport("kernel32.dll")]
        static extern IntPtr LockResource(IntPtr hResData);

        [DllImport("Kernel32.dll", EntryPoint = "SizeofResource", SetLastError = true)]
        private static extern uint SizeofResource(long hModule, IntPtr hResource);

        static bool EnumResourcesProc(long hModule, string lpType, long lParam)
        {
            Console.WriteLine($"TYPE: {lpType}");
            return true;
        }

        static bool EnumResourceNamesProc(long hModule, string type, long name, long lParam)
        {
            Console.WriteLine($"NAME: {name}");

            var handle = FindResource(hModule, "#1", "REGISTRY");
            var resource = LoadResource(hModule, handle);
            var mem = LockResource(resource);
            var numberOfBytes = SizeofResource(hModule, handle);
            byte[] arr = new byte[numberOfBytes];
            Marshal.Copy(mem, arr, 0, arr.Length);
            File.WriteAllBytes("res1.tlb", arr);
            return true;
        }

        private static void Main(string[] args)
        {
            try
            {
                foreach (var filename in args)
                {
                    var lib = LoadLibrary(filename);

                    EnumResourceTypes(lib, EnumResourcesProc, 0L);
                    PrintTypeLibs((long)lib);
                    PrintTypeLibs((long)lib, "REGISTRY");
                    var res = EnumResourceNames(lib, "REGISTRY", EnumResourceNamesProc, 3456789012345L);
                    Console.WriteLine();
                    //EnumResourceNames(lib, "REGISTRY", EnumResourceNamesProc, 0L);
                }
            }
            catch (Exception ex)
            {
                var fullname = System.Reflection.Assembly.GetEntryAssembly().Location;
                var progname = Path.GetFileNameWithoutExtension(fullname);
                Console.Error.WriteLine($"{progname} Error: {ex.Message}");
            }

        }

        private static void PrintTypeLibs(long lib, string type = "TYPELIB")
        {
            for (int i = 1; ;i++)
            {
                var handle = FindResource(lib, i, type);
                if (handle == IntPtr.Zero)
                {
                    break;
                }

                var resource = LoadResource(lib, handle);
                var mem = LockResource(resource);
                var numberOfBytes = SizeofResource(lib, handle);
                byte[] arr = new byte[numberOfBytes];
                Marshal.Copy(mem, arr, 0, arr.Length);
                File.WriteAllBytes($"res{i}.tlb", arr);
            }
        }
    }
}
