﻿using Base.Service.IService;
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
        #endregion

        #region Validation
        services.AddSingleton<ICheckQuantityTaken, CheckQuantityTaken>();
        services.AddSingleton<IValidateGet, ValidateGet>();
        #endregion

        return services;
    }
}
