using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace EnumResources
{
    static class ResourceTypeHelper
    {
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

        private static Dictionary<ResourceTypes, string> _map;


        static ResourceTypeHelper()
        {
            _map = (from v in Enum.GetValues(typeof(ResourceTypes)).Cast<ResourceTypes>()
                    select new { E = v, S = v.ToString() }).ToDictionary(x => x.E, x => x.S);
        }

        public static string GetResourceTypename(ResourceTypes r)
        {
            if (!_map.TryGetValue(r, out var s))
            {
                return null;
            }
            return s;
        }

        public static string GetResourceTypename(int i)
        {
            if (!_map.TryGetValue((ResourceTypes)i, out var s))
            {
                return null;
            }
            return s;
        }
        public static string GetResourceTypenameForPrinting(int i)
        {
            if (!_map.TryGetValue((ResourceTypes)i, out var s))
            {
                return null;
            }
            return $"RT_{s.ToUpper()}";
        }

    }
}
