
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Base.Service.GoogleEntity
{
    public class Waypoint
    {
        public string? description { get; set; }
        public string? place_id { get; set; }
        public Location? location { get; set; }
    }
}
