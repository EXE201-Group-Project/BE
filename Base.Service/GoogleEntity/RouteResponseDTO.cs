using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Base.Service.GoogleEntity
{
    public class RouteResponseDTO
    {
        public StartPoint? startPoint { get; set; }

        public EndPoint? endPoint { get; set; }

        public RouteResponse? routes { get; set; }
    }
}
