using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Mars.Admin.Framework.Models {
    public class Map2GisMarker
    {
        public string location { get; set; } = default!;
        public string id { get; set; } = default!;
        public string title { get; set; } = default!;
        public string desc { get; set; } = default!;
    }
}
