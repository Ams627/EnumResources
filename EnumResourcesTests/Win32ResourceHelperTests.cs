using Microsoft.VisualStudio.TestTools.UnitTesting;
using EnumResources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using FluentAssertions;

namespace EnumResources.Tests
{
    [TestClass()]
    public class Win32ResourceHelperTests
    {
        [TestMethod()]
        public void GetResourceNamesTest()
        {
            var w32Rh = new Win32ResourceHelper("TestData/ShellExtensionX64.dll");
            w32Rh.Should().NotBe(null);
            w32Rh.ModuleHandle.Should().NotBe(IntPtr.Zero);
            
            var groupIcons = w32Rh.GetResourceNames(Win32ResourceHelper.ResourceTypes.GroupIcon).ToList();
            var expectedGroupIcons = new int[] { 208, 209 };
            groupIcons.Should().BeEquivalentTo(expectedGroupIcons);

            var registryResources = w32Rh.GetResourceNames("REGISTRY").ToList();
            var expectedRegistryResources = new[] { 102 };
            registryResources.Should().BeEquivalentTo(expectedRegistryResources);

            var typelibs = w32Rh.GetResourceNames("TYPELIB").ToList();
            var expectedtypelibs = new[] { 1 };
            typelibs.Should().BeEquivalentTo(expectedtypelibs);

            var versions = w32Rh.GetResourceNames(Win32ResourceHelper.ResourceTypes.Version).ToList();
            var expectedversions = new[] { 1 };
            versions.Should().BeEquivalentTo(expectedversions);
        }

        [TestMethod()]
        public void GetResourceTypesTest()
        {
            var w32Rh = new Win32ResourceHelper("TestData/ShellExtensionX64.dll");
            w32Rh.Should().NotBe(null);
            w32Rh.ModuleHandle.Should().NotBe(IntPtr.Zero);
            var types = w32Rh.GetResourceTypes();
            types.Should().HaveCount(7);
            var expected = new object[] { "REGISTRY", "TYPELIB", 3, 6, 14, 16, 24 };
            types.Should().BeEquivalentTo(expected);
        }

        [TestMethod()]
        public void GetResourceLanguagesTest()
        {
            var w32Rh = new Win32ResourceHelper("TestData/ShellExtensionX64.dll");
            w32Rh.Should().NotBe(null);
            w32Rh.ModuleHandle.Should().NotBe(IntPtr.Zero);

            var stringResources = w32Rh.GetResourceNames(Win32ResourceHelper.ResourceTypes.String).ToList();
            stringResources.Should().NotBeNull().And.HaveCount(1);

            var languages = w32Rh.GetResourceLanguages(Win32ResourceHelper.ResourceTypes.String, stringResources.First());
            languages.Should().HaveCount(35);
        }

        class Native
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
        }
    }
}