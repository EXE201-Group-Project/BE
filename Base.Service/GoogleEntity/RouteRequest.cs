using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Base.Service.GoogleEntity
{
    public class RouteRequest
    {
        public Origins? origin { get; set; }
        public Destinations? destination { get; set; }

        public string? travelMode { get; set; }
        public string? routingPreference { get; set; }

        public RouteModifiers? routeModifiers { get; set; }
    }
}
