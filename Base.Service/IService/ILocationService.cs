using Base.Repository.Entity;
using Base.Service.ViewModel.ResponseVM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Base.Service.IService;

public interface ILocationService
{
    Task<Location?> GetById(int id);
    Task<IEnumerable<Location>> Get(int startPage, int endPage, int? quantity, string? address, int? order, int? tripId);
    Task<ServiceResponseVM> Delete(int id);
    Task<ServiceResponseVM<Location>> Create(Location newLocation);
}
