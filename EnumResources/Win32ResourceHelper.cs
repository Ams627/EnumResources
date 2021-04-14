using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace EnumResources
{
    public class Win32ResourceHelper
    {
        private readonly string _filename = "";
        private readonly IntPtr _moduleHandle;

        private Dictionary<ResourceTypes, string> _resourceTypeToFileExtension = new Dictionary<ResourceTypes, string>
        {
            { ResourceTypes.Cursor,  "cursor" },
            { ResourceTypes.Bitmap,  "bitmap" },
            { ResourceTypes.Icon,  "ico" },
            { ResourceTypes.Menu,  "menu" },
            { ResourceTypes.Dialog,  "dlg" },
            { ResourceTypes.String,  "stringTable" },
            { ResourceTypes.Fontdir,  "fontdir" },
            { ResourceTypes.Font,  "font" },
            { ResourceTypes.Accelerator,  "accel" },
            { ResourceTypes.Rcdata,  "rc" },
            { ResourceTypes.Messagetable,  "mtable" },
            { ResourceTypes.GroupCursor,  "gcursor" },
            { ResourceTypes.GroupIcon,  "gicon" },
            { ResourceTypes.Version,  "version" },
            { ResourceTypes.DlgInclude,  "dlgInclude" },
            { ResourceTypes.PlugPlay,  "plugPlay" },
            { ResourceTypes.Vxd,  "vxd" },
            { ResourceTypes.AniCursor,  "anicursor" },
            { ResourceTypes.AniIcon,  "aniIcon" },
            { ResourceTypes.Html,  "html" },
            { ResourceTypes.Manifest,  "manifest" },
        };

        public Win32ResourceHelper(IntPtr moduleHandle)
        {
            _moduleHandle = moduleHandle;
        }

        public Win32ResourceHelper(string filename)
        {
            _filename = filename;
            _moduleHandle = Native.LoadLibraryEx(_filename, IntPtr.Zero, Native.LoadLibraryFlags.LOAD_LIBRARY_AS_IMAGE_RESOURCE);
        }

        public enum ResourceTypes
        {
            Cursor = 1,
            Bitmap = 2,
            Icon = 3,
            Menu = 4,
            Dialog = 5,
            String = 6,
            Fontdir = 7,
            Font = 8,
            Accelerator = 9,
            Rcdata = 10,
            Messagetable = 11,
            GroupCursor = 12,
            GroupIcon = 14,
            Version = 16,
            DlgInclude = 17,
            PlugPlay = 19,
            Vxd = 20,
            AniCursor = 21,
            AniIcon = 22,
            Html = 23,
            Manifest = 24
        }

        public IntPtr ModuleHandle => _moduleHandle;

        public IEnumerable<object> GetResourceLanguages(object resourceType, object resourceName)
        {
            var resourceLanguages = new List<short>();
            var mem = GCHandle.Alloc(resourceLanguages);

            var resourceTypeIntPtr = ResourceObjectToIntPtr(resourceType);
            var resourceNameIntPtr = ResourceObjectToIntPtr(resourceName);

            Native.EnumResourceLanguages(_moduleHandle, resourceTypeIntPtr, resourceNameIntPtr, CallbackGetResourceLanguages, (IntPtr)mem);
            mem.Free();
            foreach (var language in resourceLanguages)
            {
                yield return language;
            }
        }


        /// <summary>
        /// Returns a list of resources of the specified resource type (GetResourceTypes can be called first
        /// to retrieve the list of resource types in the module).
        /// </summary>
        /// <param name="resourceType">resource type: string or small integer from the RT_XXX enumeration in WinUser.h</param>
        /// <returns>a list of resources: each resource is either a string or small integer</returns>
        public IEnumerable<object> GetResourceNames(object resourceType)
        {
            var resourceTypes = new List<object>();
            var mem = GCHandle.Alloc(resourceTypes);
            Native.EnumResourceNames(_moduleHandle, ResourceObjectToIntPtr(resourceType), CallbackGetResourceNames, (IntPtr)mem);
            mem.Free();
            foreach (var r in resourceTypes)
            {
                yield return r;
            }
        }

        /// <summary>
        /// Returns a list of resource types in the module (DLL or EXE). Resource types are small integers
        /// defined in WinUser.h (see Win32 API docs). Short summary: 
        ///   RT_CURSOR=1, RT_BITMAP=2, RT_ICON=3, RT_MENU=4, RT_DIALOG=5, RT_STRING=6, RT_FONTDIR=7, 
        ///   RT_FONT=8, RT_ACCELERATOR=9, RT_RCDATA=10, RT_MESSAGETABLE=11, RT_GROUP_CURSOR=12,
        ///   RT_GROUP_ICON=14, T_VERSION=16, RT_DLGINCLUDE=17, RT_PLUGPLAY=19, RT_VXD=20, RT_ANICURSOR=21, 
        ///   RT_ANIICON=22, RT_HTML=23
        /// NOTE THAT A RESOURCE TYPE CAN ALSO BE A STRING: for example "TYPELIB" or "REGISTRY"
        /// </summary>
        /// <returns>The resource types defined in the module as a small integer or a string</returns>
        public IEnumerable<object> GetResourceTypes()
        {
            var resourceTypes = new List<object>();
            var mem = GCHandle.Alloc(resourceTypes);
            Native.EnumResourceTypes(_moduleHandle, CallbackGetResourceTypes, (IntPtr)mem);
            mem.Free();
            foreach (var resourceType in resourceTypes)
            {
                yield return resourceType;
            }
        }

        public void SaveResource(string filename, object type, object name, ushort languageId = 0)
        {
            var typeIntPtr = ResourceObjectToIntPtr(type);
            var nameIntPtr = ResourceObjectToIntPtr(name);
            var handle = languageId > 0 ?
                Native.FindResourceEx(_moduleHandle, typeIntPtr, nameIntPtr, languageId) :
                Native.FindResource(_moduleHandle, nameIntPtr, typeIntPtr);
            var resource = Native.LoadResource(_moduleHandle, handle);
            var mem = Native.LockResource(resource);
            var numberOfBytes = Native.SizeofResource(_moduleHandle, handle);
            byte[] arr = new byte[numberOfBytes];
            Marshal.Copy(mem, arr, 0, arr.Length);
            File.WriteAllBytes(filename, arr);
        }

        private static bool CallbackGetResourceLanguages(IntPtr hModule, IntPtr type, IntPtr name, short languageId, IntPtr lParam)
        {
            var listObj = ((GCHandle)lParam).Target;
            if (listObj is List<short> list)
            {
                list.Add(languageId);
            }
            return true;
        }

        private static bool CallbackGetResourceNames(IntPtr hModule, IntPtr type, IntPtr name, IntPtr lParam)
        {
            var rname = ResourceToObject(name);

            var listObj = ((GCHandle)lParam).Target;
            if (listObj is List<object> list)
            {
                list.Add(rname);
            }
            return true;
        }

        private static bool CallbackGetResourceTypes(IntPtr hModule, IntPtr type, IntPtr lParam)
        {
            var rtype = ResourceToObject(type);

            var listObj = ((GCHandle)lParam).Target;
            if (listObj is List<object> list)
            {
                list.Add(rtype);
            }
            return true;
        }

        /// <summary>
        /// The enumeration callback functions return an unmanaged pointer to a null-terminated string
        /// but this pointer can also be a small integer. It is treated as an integer if all bits
        /// above the first 16 are zero.
        /// </summary>
        /// <param name="lpType"></param>
        /// <returns>either a string (as object) or an integer (as object)</returns>
        private static object ResourceToObject(IntPtr lpType)
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
        /// <summary>
        /// From a resource object (which contains either a string or an integer), create an IntPtr suitable
        /// for passing to the Win32 API functions EnumResourceXXX
        /// </summary>
        /// <param name="resourceType">The C# object containing a resource string or integer ID</param>
        /// <returns>An IntPtr suitable for passing to Win32 API</returns>
        private IntPtr ResourceObjectToIntPtr(object resourceType)
        {
            IntPtr type = IntPtr.Zero;
            if (resourceType is string str)
            {
                type = Marshal.StringToHGlobalAnsi(str);
            }
            else if (resourceType is int i)
            {
                type = (IntPtr)i;
            }
            else if (resourceType is ResourceTypes rtype)
            {
                type = (IntPtr)(int)rtype;
            }
            else
            {
                throw new ArgumentException("parameter is of the wrong type", nameof(resourceType));
            }
            return type;
        }
        /// <summary>
        /// Win32 Native methods
        /// </summary>
        class Native
        {
            public delegate bool EnumResourceLanguagesProcType(IntPtr hModule, IntPtr type, IntPtr name, short languageId, IntPtr lParam);

            public delegate bool EnumResourceNamesProcType(IntPtr hModule, IntPtr type, IntPtr name, IntPtr lParam);

            public delegate bool EnumResourceTypeProc(IntPtr hModule, IntPtr lpType, IntPtr lParam);

            [System.Flags]
            public enum LoadLibraryFlags : uint
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
            [DllImport("kernel32.dll")]
            public static extern bool EnumResourceLanguages(IntPtr hModule, IntPtr lpszType, IntPtr lpName, EnumResourceLanguagesProcType lpEnumFunc, IntPtr lParam);

            [DllImport("kernel32.dll", SetLastError = true)]
            public extern static bool EnumResourceNames(IntPtr hModule, IntPtr lpszType, EnumResourceNamesProcType lpEnumFunc, IntPtr lParam);

            [DllImport("kernel32.dll")]
            public static extern bool EnumResourceTypes(IntPtr hModule, EnumResourceTypeProc lpEnumFunc, IntPtr lParam);

            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern IntPtr LoadLibraryEx(string lpFileName, IntPtr hReservedNull, LoadLibraryFlags dwFlags);

            [DllImport("kernel32.dll")]
            public static extern IntPtr FindResourceEx(IntPtr hModule, IntPtr lpType, IntPtr lpName, ushort wLanguage);

            [DllImport("kernel32.dll")]
            public static extern IntPtr FindResource(IntPtr hModule, IntPtr lpName, IntPtr type);

            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern IntPtr LoadResource(IntPtr hModule, IntPtr hResInfo);

            [DllImport("kernel32.dll")]
            public static extern IntPtr LockResource(IntPtr hResData);

            [DllImport("Kernel32.dll", SetLastError = true)]
            public static extern uint SizeofResource(IntPtr hModule, IntPtr hResource);

        }
    }
}
