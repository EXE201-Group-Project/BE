using Base.Service.IService;
using Base.Service.Service;
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
        #endregion

        #region Validation
        #endregion

        return services;
    }
}
