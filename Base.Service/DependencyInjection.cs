using Base.Service.Common;
using Base.Service.IService;
using Base.Service.Service;
using Base.Service.Validation;
using FTask.Service.Validation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Base.Service;

public static class DependencyInjection
{
    public static IServiceCollection AddService(this IServiceCollection services, IConfiguration configuration)
    {
        #region Services
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<IItemService, ItemService>();
        services.AddScoped<ILocationService, LocationService>();
        services.AddScoped<ITripService, TripService>();
        services.AddScoped<IRouteService, RouteService>();
        services.AddScoped<IBillService, BillService>();
        services.AddScoped<IPackageService, PackageService>();
        #endregion

        #region Validation
        services.AddSingleton<ICheckQuantityTaken, CheckQuantityTaken>();
        services.AddSingleton<IValidateGet, ValidateGet>();
        #endregion

        services.AddScoped<IUploadFile, UploadFile>();

        return services;
    }
}
