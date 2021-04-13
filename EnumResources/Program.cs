using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Security.AccessControl;

namespace EnumResources
{
    class Program
    {
        [System.Flags]
        enum LoadLibraryFlags : uint
        {
            None = 0,
            DONT_RESOLVE_DLL_REFERENCES = 0x00000001,
            LOAD_IGNORE_CODE_AUTHZ_LEVEL = 0x00000010,
            LOAD_LIBRARY_AS_DATAFILE = 0x00000002,
            LOAD_LIBRARY_AS_DATAFILE_EXCLUSIVE = 0x00000040,
            LOAD_LIBRARY_AS_IMAGE_RESOURCE = 0x00000020,
            LOAD_LIBRARY_SEARCH_APPLICATION_DIR = 0x00000200,
            LOAD_LIBRARY_SEARCH_DEFAULT_DIRS = 0x00001000,
            LOAD_LIBRARY_SEARCH_DLL_LOAD_DIR = 0x00000100,
            LOAD_LIBRARY_SEARCH_SYSTEM32 = 0x00000800,
            LOAD_LIBRARY_SEARCH_USER_DIRS = 0x00000400,
            LOAD_WITH_ALTERED_SEARCH_PATH = 0x00000008
        }

        public delegate bool EnumResTypeProc(IntPtr hModule, IntPtr lpType, IntPtr lParam);
        public delegate bool EnumResourceTypeProcType(long hModule, IntPtr type, long name, IntPtr lParam);

        [DllImport("kernel32.dll")]
        static extern bool EnumResourceTypes(IntPtr hModule, EnumResTypeProc lpEnumFunc, IntPtr lParam);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr LoadLibraryEx(string lpFileName, IntPtr hReservedNull, LoadLibraryFlags dwFlags);

        [DllImport("kernel32.dll", SetLastError = true)]
        public extern static bool EnumResourceNames(IntPtr hModule, IntPtr lpszType, EnumResourceTypeProcType lpEnumFunc, IntPtr lParam);


        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr LoadResource(long hModule, IntPtr hResInfo);

        [DllImport("kernel32.dll")]
        static extern IntPtr FindResourceEx(IntPtr hModule, IntPtr lpType, IntPtr lpName, ushort wLanguage);

        [DllImport("kernel32.dll")]
        static extern IntPtr FindResource(long hModule, string lpName, IntPtr type);

        [DllImport("kernel32.dll")]
        static extern IntPtr FindResource(long hModule, int rid, string type);

        [DllImport("kernel32.dll")]
        static extern IntPtr LockResource(IntPtr hResData);

        [DllImport("Kernel32.dll", EntryPoint = "SizeofResource", SetLastError = true)]
        private static extern uint SizeofResource(long hModule, IntPtr hResource);

        [DllImport("kernel32.dll")]
        static extern uint GetLastError();

        static bool EnumResourceTypeProc(IntPtr hModule, IntPtr lpType, IntPtr lParam)
        {
            var rtype = GetResourceType(lpType);

            var listObj = ((GCHandle)lParam).Target;
            if (listObj is List<object> list)
            {
                list.Add(rtype);
            }
            Console.WriteLine(rtype);
            return true;
        }

        private static object GetResourceType(IntPtr lpType)
        {
            var i = lpType.ToInt64();
            if ((i >> 16) == 0)
            {
                return (int)i;
            }
            else
            {
                return Marshal.PtrToStringAnsi(lpType);
            }
        }

        static bool EnumResourceNamesProc(long hModule, IntPtr type, long name, IntPtr lParam)
        {
            var resourceType = GetResourceType(type);
            string outputFilename;
            if (resourceType is string str)
            {
                outputFilename = str == "TYPELIB" ? $"{name}.tlb" : str == "REGISTRY" ? $"{name}.rgs" : $"{name}.bin";
            }
            else
            {
                var resourceName  = $"{ResourceTypeHelper.GetResourceTypename((ResourceTypeHelper.ResourceTypes)type)}-{name}";
                outputFilename = $"{resourceName}.bin";
            }

            var handle = FindResource(hModule, $"#{name}", type);
            var resource = LoadResource(hModule, handle);
            var mem = LockResource(resource);
            var numberOfBytes = SizeofResource(hModule, handle);
            byte[] arr = new byte[numberOfBytes];
            Marshal.Copy(mem, arr, 0, arr.Length);
            Console.WriteLine($"Writing resource to {outputFilename}");
            File.WriteAllBytes(outputFilename, arr);
            return true;
        }

        private static void Main(string[] args)
        {
            try
            {
                var plural = args.Length > 1;
                foreach (var filename in args)
                {
                    var lib = LoadLibraryEx(filename, IntPtr.Zero, LoadLibraryFlags.LOAD_LIBRARY_AS_IMAGE_RESOURCE);
                    if (lib == IntPtr.Zero)
                    {
                        Console.Error.WriteLine($"Cannot read from file {filename}.");
                    }

                    var helper = new Win32ResourceHelper();
                    var types = helper.GetResourceTypes(lib).ToList();
                    foreach (var type in types)
                    {
                        var names = helper.GetResourceNames(lib, type).ToList();
                        foreach (var name in names)
                        {
                            var languages = helper.GetResourceLanguages(lib, type, name).ToList();
                        }   
                    }
                }

                //    var resourceTypes = new List<object>();
                //    var mem = GCHandle.Alloc(resourceTypes);
                //    EnumResourceTypes(lib, EnumResourceTypeProc, (IntPtr)mem);
                //    mem.Free();
                    
                //    foreach (var r in resourceTypes)
                //    {
                //        var toPrint = r is int i ? ResourceTypeHelper.GetResourceTypenameForPrinting(i) : r;
                //        Console.WriteLine($"{toPrint}");
                //    }
                //    foreach (var resourceType in resourceTypes)
                //    {
                //        IntPtr type = IntPtr.Zero;

                //        if (resourceType is string str)
                //        {
                //            type = Marshal.StringToHGlobalAnsi(str);
                //        }
                //        else if (resourceType is int i)
                //        {
                //            type = (IntPtr)i;
                //        }

                //        var res = EnumResourceNames(lib, type, EnumResourceNamesProc, IntPtr.Zero);
                //        if (!res)
                //        {
                //            var error = GetLastError();
                //        }
                //        else
                //        {
                //            Console.WriteLine();
                //        }
                //    }
                // }
            }
            catch (Exception ex)
            {
                var fullname = System.Reflection.Assembly.GetEntryAssembly().Location;
                var progname = Path.GetFileNameWithoutExtension(fullname);
                Console.Error.WriteLine($"{progname} Error: {ex.Message}");
            }

        }
    }
}
