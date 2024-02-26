using Base.Service.GoogleEntity;
using Base.Service.IService;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Base.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RouteController : ControllerBase
    {
        private readonly IGoogleService _googleService;
        public RouteController(IGoogleService googleService)
        {
            _googleService = googleService;
        }
        [HttpPost]
        public async Task<IActionResult> RouteCalculate([FromBody] List<Waypoint> locations, string travelMode, string routingPreference, bool avoidHighways, bool avoidTolls, bool avoidFerries)
        {
            try
            {
                if (ModelState.IsValid && locations.Count >= 2)
                {
                    var routes = await _googleService.CalculateRouteAsync(locations, travelMode, routingPreference, avoidHighways, avoidTolls, avoidFerries);
                    return Ok(routes);
                }

                return BadRequest(new
                {
                    Title = "Can not Create Routes"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
