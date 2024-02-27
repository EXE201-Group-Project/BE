using Base.Service.GoogleEntity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Base.Service.IService
{
    public interface IGoogleService
    {
        Task<List<RouteResponseDTO>> CalculateRouteAsync(List<Waypoint> locations, string travelModel, string routingPreference, bool avoidHighways, bool avoidTolls, bool avoidFerries);
    }
}
