using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Base.Service.GoogleEntity
{
    public class RouteModifiers
    {
        public bool? avoidFerries { get; set; }

        public bool? avoidTolls { get; set; }

        public bool? avoidHighways { get; set; }
    }
}
