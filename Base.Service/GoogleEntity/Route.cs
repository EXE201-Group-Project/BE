using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Base.Service.GoogleEntity
{
    public class Route
    {
        public int distanceMeters { get; set; }
        public string? duration { get; set; }
        public Polyline? polyline { get; set; }
    }
}
