using Base.Repository.Common;
using Base.Repository.Entity;
using Base.Service.Common;
using Base.Service.GoogleEntity;
using Base.Service.IService;
using Base.Service.Validation;
using Base.Service.ViewModel.RequestVM.Trip;
using Base.Service.ViewModel.ResponseVM;
using Duende.IdentityServer.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Base.Service.Service;

internal class TripService : ITripService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidateGet _validateGet;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUploadFile _uploadFile;
    private readonly HttpClient client = default!;
    private string ApiUrl = "";
    private string modelAiUrl = "";
    private readonly HttpClient modelAiClient = default!;
    private readonly IGoogleService _googleService;

    public TripService(IUnitOfWork unitOfWork, IValidateGet validateGet, ICurrentUserService currentUserService, IUploadFile uploadFile, IGoogleService googleService)
    {
        _unitOfWork = unitOfWork;
        _validateGet = validateGet;
        _currentUserService = currentUserService;
        _uploadFile = uploadFile;

        client = new HttpClient();
        modelAiClient = new HttpClient();
        var contentType = new MediaTypeWithQualityHeaderValue("application/json");
        modelAiClient.DefaultRequestHeaders.Accept.Add(contentType);
        client.DefaultRequestHeaders.Accept.Add(contentType);
        client.DefaultRequestHeaders.Add("X-Goog-Api-Key", "AIzaSyBxuTnT2zRMR3a1xA5NU8z-8orw2ZL6tV0");
        client.DefaultRequestHeaders.Add("X-Goog-FieldMask", "routes.duration,routes.distanceMeters,routes.polyline.encodedPolyline");
        ApiUrl = "https://routes.googleapis.com/directions/v2:computeRoutes";
        modelAiUrl = "http://modelAi:9999/api/v1/shortest_path";
        _googleService = googleService;
    }

    public async Task<Trip?> GetById(int id)
    {
        return await _unitOfWork.TripRepository.Get(l => !l.Deleted && l.Id == id).FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<Trip>> Get(int startPage, int endPage, int? quantity, int? travleMode, int? status, Guid? userId, DateTime? date)
    {
        int quantityResult = 0;
        _validateGet.ValidateGetRequest(ref startPage, ref endPage, quantity, ref quantityResult);
        if (quantityResult == 0)
        {
            throw new ArgumentException("Error when get quantity per page");
        }

        var expressions = new List<Expression>();
        ParameterExpression pe = Expression.Parameter(typeof(Trip), "l");
        MethodInfo? containsMethod = typeof(string).GetMethod("Contains", new[] { typeof(string) });

        if (containsMethod is null)
        {
            throw new ArgumentNullException("Method Contains can not found from string type");
        }

        expressions.Add(Expression.Equal(Expression.Property(pe, nameof(Trip.Deleted)), Expression.Constant(false)));

        if (travleMode is not null)
        {
            expressions.Add(Expression.Equal(Expression.Property(pe, nameof(Trip.TravelMode)), Expression.Constant(travleMode)));
        }

        if (status is not null)
        {
            expressions.Add(Expression.Equal(Expression.Property(pe, nameof(Trip.Status)), Expression.Constant(status)));
        }

        if (userId is not null)
        {
            expressions.Add(Expression.Equal(Expression.Property(pe, nameof(Trip.UserId)), Expression.Constant(userId)));
        }

        if(date is not null)
        {
            expressions.Add(Expression.Equal(Expression.Property(pe, nameof(Trip.StartDate.Date)), Expression.Constant(((DateTime)date).Date)));
        }

        Expression combined = expressions.Aggregate((accumulate, next) => Expression.AndAlso(accumulate, next));
        Expression<Func<Trip, bool>> where = Expression.Lambda<Func<Trip, bool>>(combined, pe);

        return await _unitOfWork.TripRepository
            .Get(where)
            .AsNoTracking()
            .Skip((startPage - 1) * quantityResult)
            .Take((endPage - startPage + 1) * quantityResult)
            .ToArrayAsync();
    }

    public async Task<ServiceResponseVM> Delete(int id)
    {
        var existedTrip = await _unitOfWork.TripRepository.Get(l => !l.Deleted && l.Id == id).FirstOrDefaultAsync();
        if (existedTrip is null)
        {
            return new ServiceResponseVM
            {
                IsSuccess = false,
                Title = "Delete trip failed",
                Errors = new string[1] { "Trip not found" }
            };
        }

        existedTrip.Deleted = true;
        try
        {
            var result = await _unitOfWork.SaveChangesAsync();
            if (result)
            {
                return new ServiceResponseVM
                {
                    IsSuccess = true,
                    Title = "Delete trip successfully"
                };
            }
            else
            {
                return new ServiceResponseVM
                {
                    IsSuccess = false,
                    Title = "Delete trip failed",
                    Errors = new string[1] { "Save changes failed" }
                };
            }
        }
        catch (DbUpdateException ex)
        {
            return new ServiceResponseVM
            {
                IsSuccess = false,
                Title = "Delete trip failed",
                Errors = new string[1] { ex.Message }
            };
        }
        catch (OperationCanceledException ex)
        {
            return new ServiceResponseVM
            {
                IsSuccess = false,
                Title = "Delete trip failed",
                Errors = new string[2] { "The operation has been cancelled", ex.Message }
            };
        }
    }

    [Authorize()]
    public async Task<ServiceResponseVM<TripResponse>> Create(Trip newTrip)
    {
        var existedUser = await _unitOfWork.UserRepository.FindAsync(newTrip.UserId);
        if (existedUser is null)
        {
            return new ServiceResponseVM<TripResponse>
            {
                IsSuccess = false,
                Title = "Create trip failed",
                Errors = new string[1] { "User not found" }
            };
        }

        if(newTrip.Locations.Count() > 0)
        {
            var errors = new ConcurrentQueue<string>();

            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = Convert.ToInt32(Math.Ceiling(Environment.ProcessorCount * 0.3 * 2))
            };

            // Create new trip
            Parallel.ForEach(newTrip.Locations, parallelOptions, async (location, state) =>
            {
                if (location.Items.Count() > 0)
                {
                    foreach (var item in location.Items)
                    {
                        if (item.Image is not null && item.Image.Length > 0)
                        {
                            var uploadResult = await _uploadFile.UploadImage(item.Image);
                            if (uploadResult.Error is not null)
                            {
                                errors.Enqueue($"Can not upload the image '{item.Image.FileName}'");
                                Console.WriteLine(uploadResult.Error.Message);
                                state.Stop();
                                return;
                            }
                            else
                            {
                                item.ImageUrl = new KeyValuePair<string, string>(item.Image.FileName, uploadResult.SecureUrl.ToString());
                            }
                        }
                    }
                }
            });

            #region Test create routes section
            /*var locations = newTrip.Locations.ToArray();
            var routes = new List<Route>();
            for (int i = 0; i < locations.Count() - 1; i++)
            {
                var startLatLng = locations[i];
                var endLatLng = locations[i + 1];

                // Create route object for json serialization
                var routeRequest = new Route()
                {
                    Origin = new Location()
                    {
                        location = new Location()
                        {
                            latLng = new
                            {
                                latitude = startLatLng.Latitude,
                                longitude = startLatLng.Longitude
                            }
                        }
                    },

                    Destination = new Location()
                    {
                        location = new Location()
                        {
                            latLng = new
                            {
                                latitude = endLatLng.Latitude,
                                longitude = endLatLng.Longitude
                            }
                        }
                    },
                    travelMode = newTrip.TravelMode,
                    routingPreference = newTrip.RoutingPreference,
                    routeModifiers = new
                    {
                        avoidTolls = newTrip.AvoidTolls,
                        avoidHighways = newTrip.AvoidHighways,
                        avoidFerries = newTrip.AvoidFerries,
                    }
                };
                string strData = JsonSerializer.Serialize(routeRequest);

                // Gắn vô body content
                var contentData = new StringContent(strData, System.Text.Encoding.UTF8, "application/json");

                HttpResponseMessage response = await client.PostAsync(ApiUrl, contentData);
                if (response.IsSuccessStatusCode)
                {
                    // Get routes result
                    string responseBody = await response.Content.ReadAsStringAsync();
                    var serializeOptions = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };
                    var routesResult = JsonSerializer.Deserialize<List<RouteResponse>>(responseBody, serializeOptions)!;

                    // Create routes
                    foreach(var routeResult in routesResult)
                    {
                        var route = routeRequest;
                        route.CreatedAt = DateTime.Now;
                        route.CreatedBy = _currentUserService.UserId;
                        route.DistanceMeters = routeResult.distanceMeters;
                        route.DurationSeconds = int.Parse(routeResult.duration?.Remove(routeResult.duration.Length - 1) ?? "0");
                        route.EncodedPolylne = routeResult.polyline?.encodedPolyline ?? "";
                        routes.Add(route);
                    }
                }
            }
            newTrip.Routes = routes;*/
            #endregion

            #region test
            /*newTrip.Locations.AsParallel()
                .WithDegreeOfParallelism(Convert.ToInt32(Math.Ceiling(Environment.ProcessorCount * 0.3 * 2)))
                .ForAll(async location =>
                {
                    if(location.Items.Count() > 0)
                    {
                        foreach (var item in location.Items)
                        {
                            if (item.Image is not null && item.Image.Length > 0)
                            {
                                var uploadResult = await _uploadFile.UploadImage(item.Image);
                                if (uploadResult.Error is not null)
                                {
                                    errors.Enqueue($"Can not upload the image '{item.Image.FileName}'");
                                    Console.WriteLine(uploadResult.Error.Message);
                                }
                            }
                        }
                    }
                });*/
            #endregion

            if (!errors.IsNullOrEmpty())
            {
                return new ServiceResponseVM<TripResponse>
                {
                    IsSuccess = false,
                    Title = "Create trip failed",
                    Errors = errors.ToArray()
                };
            }
        }

        newTrip.CreatedAt = DateTime.UtcNow;
        newTrip.CreatedBy = _currentUserService.UserId;

        await _unitOfWork.TripRepository.AddAsync(newTrip);

        try
        {
            var result = await _unitOfWork.SaveChangesAsync();
            if (result)
            {
                var waypoints = new List<Waypoint>();

                #region Call Model AI API
                var addresses = new List<object>();
                foreach(var location in newTrip.Locations)
                {
                    addresses.Add(new
                    {
                        id = location.Id,
                        latitude = location.Latitude,
                        longitude = location.Longitude,
                    });
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
                        return new ServiceResponseVM<TripResponse>
                        {
                            IsSuccess = false,
                            Title = "Calculate routes failed",
                            Errors = new string[2] { "Can not calculate the order of routes", "The ordered result is null or empty" }
                        };
                    }

                    foreach(var item in orderedResult ?? Enumerable.Empty<JToken>())
                    {
                        if (item.Type.Equals(JTokenType.Object)){
                            var location = newTrip.Locations.FirstOrDefault(l => l.Id == item["id"]?.Value<int>());
                            if(location is null)
                            {
                                return new ServiceResponseVM<TripResponse>
                                {
                                    IsSuccess = false,
                                    Title = "Calculate routes failed",
                                    Errors = new string[1] { "Data responsed from model ai not correct" }
                                };
                            }
                            waypoints.Add(new Waypoint
                            {
                                description = location.Address,
                                location = new Base.Service.GoogleEntity.Location
                                {
                                    latLng = new LatLng
                                    {
                                        latitude = location.Latitude,
                                        longitude = location.Longitude
                                    }
                                }
                            });
                        }
                    }
                }
                else
                {
                    return new ServiceResponseVM<TripResponse>
                    {
                        IsSuccess = false,
                        Title = "Calculate routes failed",
                        Errors = new string[2] { "Can not calculate the order of routes", "The http response is unsuccessfully" }
                    };
                }
                #endregion

                #region Call GG Service
                if (waypoints.IsNullOrEmpty())
                {
                    return new ServiceResponseVM<TripResponse>
                    {
                        IsSuccess = false,
                        Title = "Calculate routes failed",
                        Errors = new string[1] { "Setting waypoints failed" }
                    };
                }

                var googleServiceResult = await _googleService.CalculateRouteAsync(waypoints, newTrip.TravelMode, newTrip.RoutingPreference, newTrip.AvoidHighways ?? false, newTrip.AvoidTolls ?? false, newTrip.AvoidFerries ?? false);

                #endregion

                return new ServiceResponseVM<TripResponse>
                {
                    IsSuccess = true,
                    Title = "Create trip successfully",
                    Result = new TripResponse
                    {
                        Trip = newTrip,
                        Routes = googleServiceResult ?? Enumerable.Empty<RouteResponseDTO>()
                    }
                };
            }
            else
            {
                return new ServiceResponseVM<TripResponse>
                {
                    IsSuccess = false,
                    Title = "Create trip failed",
                    Errors = new string[1] { "Save changes failed" }
                };
            }
        }
        catch (DbUpdateException ex)
        {
            return new ServiceResponseVM<TripResponse>
            {
                IsSuccess = false,
                Title = "Create trip failed",
                Errors = new string[1] { ex.Message }
            };
        }
        catch (OperationCanceledException ex)
        {
            return new ServiceResponseVM<TripResponse>
            {
                IsSuccess = false,
                Title = "Create trip failed",
                Errors = new string[2] { "The operation has been cancelled", ex.Message }
            };
        }
    }

    public async Task<ServiceResponseVM<Trip>> Update(UpdateTripVM updateTrip, int id)
    {
        var existedTrip = await _unitOfWork.TripRepository.FindAsync(id);
        if(existedTrip is null)
        {
            return new ServiceResponseVM<Trip>
            {
                IsSuccess = false,
                Title = "Update trip failed",
                Errors = new string[1] { "Trip not found" }
            };
        }

        if(updateTrip.UserId is not null)
        {
            var existedUser = await _unitOfWork.UserRepository.FindAsync(updateTrip.UserId ?? new Guid("00000000-000-0000-0000-000000000000"));
            if(existedUser is null)
            {
                return new ServiceResponseVM<Trip>
                {
                    IsSuccess = false,
                    Title = "Update trip failed",
                    Errors = new string[1] { "User not found" }
                };
            }
            existedTrip.UserId = updateTrip.UserId ?? existedTrip.UserId;
        }

        existedTrip.TravelMode = updateTrip.TravelMode ?? existedTrip.TravelMode;
        existedTrip.Status = updateTrip.Status ?? existedTrip.Status;
        existedTrip.StartDate = updateTrip.StartDate ?? existedTrip.StartDate;

        try
        {
            var result = await _unitOfWork.SaveChangesAsync();
            if (result)
            {
                return new ServiceResponseVM<Trip>
                {
                    IsSuccess = true,
                    Title = "Update trip successfully"
                };
            }
            else
            {
                return new ServiceResponseVM<Trip>
                {
                    IsSuccess = false,
                    Title = "Update trip failed",
                    Errors = new string[1] { "Save changes failed" }
                };
            }
        }
        catch (DbUpdateException ex)
        {
            return new ServiceResponseVM<Trip>
            {
                IsSuccess = false,
                Title = "Update trip failed",
                Errors = new string[1] { ex.Message }
            };
        }
        catch (OperationCanceledException ex)
        {
            return new ServiceResponseVM<Trip>
            {
                IsSuccess = false,
                Title = "Update trip failed",
                Errors = new string[2] { "The operation has been cancelled", ex.Message }
            };
        }
    }


    // Class Route Response for result of route api
    private class RouteResponse
    {
        public int distanceMeters { get; set; }
        public string? duration { get; set; }
        public Polyline? polyline { get; set; }
    }

    private class Polyline
    {
        public string? encodedPolyline { get; set; }
    }
}

