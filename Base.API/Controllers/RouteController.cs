using Base.Service.GoogleEntity;
using Base.Service.IService;
using Base.Service.ViewModel.ResponseVM;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using JsonSerializer = System.Text.Json.JsonSerializer;
using Duende.IdentityServer.Extensions;

namespace Base.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RouteController : ControllerBase
    {
        private string modelAiUrl = "";
        private readonly HttpClient modelAiClient = default!;
        private readonly IGoogleService _googleService;
        public RouteController(IGoogleService googleService)
        {
            _googleService = googleService;
            modelAiUrl = "http://modelAi:9999/api/v1/shortest_path";
            modelAiClient = new HttpClient();
            var contentType = new MediaTypeWithQualityHeaderValue("application/json");
            modelAiClient.DefaultRequestHeaders.Accept.Add(contentType);
        }
        [HttpPost]
        public async Task<IActionResult> RouteCalculate([FromBody] List<Waypoint> locations, string travelMode, string routingPreference, bool avoidHighways, bool avoidTolls, bool avoidFerries)
        {
            try
            {
                if (ModelState.IsValid && locations.Count >= 2)
                {
                    var waypoints = new List<Waypoint>();

                    #region Call Model AI API
                    var addresses = new List<object>();
                    var count = 1;
                    foreach (var location in locations)
                    {
                        location.place_id = count.ToString();
                        addresses.Add(new
                        {
                            id = location.place_id,
                            latitude = location.location?.latLng?.latitude,
                            longitude = location.location?.latLng?.longitude,
                        });
                        count++;
                    }
                    var requestContent = new
                    {
                        addresses = addresses
                    };
                    string strData = JsonSerializer.Serialize(requestContent);
                    var contentData = new StringContent(strData, System.Text.Encoding.UTF8, "application/json");

                    HttpResponseMessage response = await modelAiClient.PostAsync(modelAiUrl, contentData);
                    if (response.IsSuccessStatusCode)
                    {
                        string responseBody = await response.Content.ReadAsStringAsync();
                        var orderedResult = (JArray?)JsonConvert.DeserializeObject(responseBody);

                        if (orderedResult.IsNullOrEmpty())
                        {
                            return BadRequest(new
                            {
                                IsSuccess = false,
                                Title = "Calculate routes failed",
                                Errors = new string[2] { "Can not calculate the order of routes", "The ordered result is null or empty" }
                            });
                        }

                        foreach (var item in orderedResult ?? Enumerable.Empty<JToken>())
                        {
                            if (item.Type.Equals(JTokenType.Object))
                            {
                                var location = locations.FirstOrDefault(l => l.place_id == item["id"]?.Value<string>());
                                if (location is null)
                                {
                                    return BadRequest(new
                                    {
                                        IsSuccess = false,
                                        Title = "Calculate routes failed",
                                        Errors = new string[1] { "Data responsed from model ai not correct" }
                                    });
                                }
                                waypoints.Add(location);
                            }
                        }
                    }
                    else
                    {
                        return BadRequest(new
                        {
                            IsSuccess = false,
                            Title = "Calculate routes failed",
                            Errors = new string[2] { "Can not calculate the order of routes", "The http response is unsuccessfully" }
                        });
                    }
                    #endregion

                    var routes = await _googleService.CalculateRouteAsync(waypoints, travelMode, routingPreference, avoidHighways, avoidTolls, avoidFerries);
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
