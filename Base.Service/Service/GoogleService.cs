using Base.Service.GoogleEntity;
using Base.Service.IService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Base.Service.Service
{
    public class GoogleService : IGoogleService
    {
        private readonly HttpClient client = null!;
        private string ApiUrl = "";

        public GoogleService()
        {
            client = new HttpClient();
            var contentType = new MediaTypeWithQualityHeaderValue("application/json");
            client.DefaultRequestHeaders.Accept.Add(contentType);
            client.DefaultRequestHeaders.Add("X-Goog-Api-Key", "AIzaSyDcbUoHJwHi56IPSDE08doXUMtyYZLyd1g");
            client.DefaultRequestHeaders.Add("X-Goog-FieldMask", "routes.duration,routes.distanceMeters,routes.polyline.encodedPolyline");
            ApiUrl = "https://routes.googleapis.com/directions/v2:computeRoutes";
        }
        public async Task<List<RouteResponseDTO>> CalculateRouteAsync(List<Waypoint> locations, string travelMode, string routingPreference, bool avoidHighways, bool avoidTolls, bool avoidFerries)
        {
            try
            {
                List<RouteResponse> routes = new List<RouteResponse>();
                List<RouteResponseDTO> routeDTOs = new List<RouteResponseDTO>();
                var commonParams = new
                {
                    travelMode,
                    routingPreference,
                    routeModifiers = new
                    {
                        avoidHighways,
                        avoidTolls,
                        avoidFerries,
                    }
                };
                for (int i = 0; i < locations.Count - 1; i++)
                {
                    var startLatLng = locations[i].location?.latLng;
                    var endLatLng = locations[i + 1].location?.latLng;

                    var routeRequest = new RouteRequest()
                    {
                        origin = new Origins()
                        {
                            location = new Location()
                            {
                                latLng = startLatLng,
                            }
                        },

                        destination = new Destinations()
                        {
                            location = new Location()
                            {
                                latLng = endLatLng
                            }
                        },
                        travelMode = commonParams.travelMode,
                        routingPreference = commonParams.routingPreference,
                        routeModifiers = new RouteModifiers()
                        {
                            avoidTolls = commonParams.routeModifiers.avoidTolls,
                            avoidHighways = commonParams.routeModifiers.avoidHighways,
                            avoidFerries = commonParams.routeModifiers.avoidFerries,
                        }
                    };

                    string strData = JsonSerializer.Serialize(routeRequest);
                    var contentData = new StringContent(strData, System.Text.Encoding.UTF8, "application/json");

                    HttpResponseMessage response = await client.PostAsync(ApiUrl, contentData);
                    if (response.IsSuccessStatusCode)
                    {
                        string responseBody = await response.Content.ReadAsStringAsync();

                        var options = new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        };
                        var distanceResults = JsonSerializer.Deserialize<RouteResponse>(responseBody, options)!;
                        routes.Add(distanceResults);

                        var routeDTO = new RouteResponseDTO()
                        {
                            startPoint = new StartPoint()
                            {
                                description = locations[i].description,
                                place_id = locations[i].place_id
                            },
                            endPoint = new EndPoint()
                            {
                                description = locations[i + 1].description,
                                place_id = locations[i + 1].place_id
                            },

                            routes = distanceResults
                        };
                        routeDTOs.Add(routeDTO);
                    }
                }
                return routeDTOs;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
    }
}
