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

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr LoadLibraryEx(string lpFileName, IntPtr hReservedNull, LoadLibraryFlags dwFlags);

        private static void Main(string[] args)
        {
            try
            {
                Console.WriteLine("plok");
                var plural = args.Length > 1;
                foreach (var filename in args)
                {
                    var lib = LoadLibraryEx(filename, IntPtr.Zero, LoadLibraryFlags.LOAD_LIBRARY_AS_IMAGE_RESOURCE);
                    if (lib == IntPtr.Zero)
                    {
                        Console.Error.WriteLine($"Cannot read from file {filename}.");
                    }

                    if (plural)
                    {
                        Console.WriteLine($"{filename}");
                    }
                    var helper = new Win32ResourceHelper(lib);
                    var types = helper.GetResourceTypes().ToList();
                    foreach (var type in types)
                    {
                        var indent = plural ? "    " : "";
                        Console.WriteLine($"{indent}resource type: {type}");
                        var names = helper.GetResourceNames(type).ToList();
                        foreach (var name in names)
                        {
                            Console.WriteLine($"{indent}    resource id: {type}");

                            if (type is string str && str == "TYPELIB")
                            {
                                var outputFilename = $"resource-{name}.tlb";
                                helper.SaveResource(outputFilename, type, name);
                            }
                            else if (type is string str2 && str2 == "REGISTRY")
                            {
                                var outputFilename = $"resource-{name}.rgs";
                                helper.SaveResource(outputFilename, type, name);
                            }
                            var languages = helper.GetResourceLanguages(type, name).ToList();
                            foreach (var l in languages)
                            {
                                Console.WriteLine($"{indent}        language id: {l}");
                            }
                        }   
                    }
                }
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
