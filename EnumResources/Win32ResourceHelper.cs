using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace EnumResources
{
    class Win32ResourceHelper
    {
        public delegate bool EnumResourceLanguagesProcType(IntPtr hModule, IntPtr type, IntPtr name, short languageId, IntPtr lParam);

        public delegate bool EnumResourceNamesProcType(IntPtr hModule, IntPtr type, IntPtr name, IntPtr lParam);

        public delegate bool EnumResourceTypeProc(IntPtr hModule, IntPtr lpType, IntPtr lParam);

        [DllImport("kernel32.dll", SetLastError = true)]
        public extern static bool EnumResourceNames(IntPtr hModule, IntPtr lpszType, EnumResourceNamesProcType lpEnumFunc, IntPtr lParam);

        public IEnumerable<object> GetResourceLanguages(IntPtr moduleHandle, object resourceType, object resourceName)
        {
            var resourceLanguages = new List<short>();
            var mem = GCHandle.Alloc(resourceLanguages);

            var resourceTypeIntPtr = ResourceObjectToIntPtr(resourceType);
            var resourceNameIntPtr = ResourceObjectToIntPtr(resourceName);

            EnumResourceLanguages(moduleHandle, resourceTypeIntPtr, resourceNameIntPtr, CallbackGetResourceLanguages, (IntPtr)mem);
            mem.Free();
            foreach (var language in resourceLanguages)
            {
                yield return language;
            }
        }

        public IEnumerable<object> GetResourceNames(IntPtr moduleHandle, object resourceType)
        {
            var resourceTypes = new List<object>();
            var mem = GCHandle.Alloc(resourceTypes);
            EnumResourceNames(moduleHandle, ResourceObjectToIntPtr(resourceType), CallbackGetResourceNames, (IntPtr)mem);
            mem.Free();
            foreach (var r in resourceTypes)
            {
                yield return r;
            }
        }

        //public IEnumerable<object> GetResourceLanguages(IntPtr moduleHandle, object resourceType, object resourceName)
        //{
        //    var resourceLanguages = new List<object>();
        //    var mem = GCHandle.Alloc(resourceLanguages);
        //    EnumResourceNames(moduleHandle, ResourceObjectToIntPtr(resourceType), GetResourceNamesCallback, (IntPtr)mem);
        //    mem.Free();
        //    foreach (var l in resourceLanguages)
        //    {
        //        yield return l;
        //    }
        //}
        public IEnumerable<object> GetResourceTypes(IntPtr moduleHandle)
        {
            var resourceTypes = new List<object>();
            var mem = GCHandle.Alloc(resourceTypes);
            EnumResourceTypes(moduleHandle, CallbackGetResourceTypes, (IntPtr)mem);
            mem.Free();
            foreach (var resourceType in resourceTypes)
            {
                yield return resourceType;
            }
        }

        [DllImport("kernel32.dll")]
        static extern bool EnumResourceLanguages(IntPtr hModule, IntPtr lpszType, IntPtr lpName, EnumResourceLanguagesProcType lpEnumFunc, IntPtr lParam);
        [DllImport("kernel32.dll")]
        static extern bool EnumResourceTypes(IntPtr hModule, EnumResourceTypeProc lpEnumFunc, IntPtr lParam);

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

        private bool CallbackGetResourceLanguages(IntPtr hModule, IntPtr type, IntPtr name, short languageId, IntPtr lParam)
        {
            var listObj = ((GCHandle)lParam).Target;
            if (listObj is List<short> list)
            {
                list.Add(languageId);
            }
            return true;
        }
        bool CallbackGetResourceNames(IntPtr hModule, IntPtr type, IntPtr name, IntPtr lParam)
        {
            var rname = ResourceToObject(name);

            var listObj = ((GCHandle)lParam).Target;
            if (listObj is List<object> list)
            {
                list.Add(rname);
            }
            return true;
        }

        private bool CallbackGetResourceTypes(IntPtr hModule, IntPtr type, IntPtr lParam)
        {
            var rtype = ResourceToObject(type);

            var listObj = ((GCHandle)lParam).Target;
            if (listObj is List<object> list)
            {
                list.Add(rtype);
            }
            return true;
        }

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
            else
            {
                throw new ArgumentException("parameter is of the wrong type", nameof(resourceType));
            }
            return type;
        }
    }
}
