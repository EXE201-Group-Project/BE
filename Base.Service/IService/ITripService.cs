using Base.Repository.Entity;
using Base.Service.ViewModel.RequestVM.Trip;
using Base.Service.ViewModel.ResponseVM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Base.Service.Service.TripService;

namespace Base.Service.IService;

public interface ITripService
{
    Task<Trip?> GetById(int id);
    Task<IEnumerable<Trip>> Get(int startPage, int endPage, int? quantity, int? travleMode, int? status, Guid? userId, DateTime? date);
    Task<ServiceResponseVM> Delete(int id);
    Task<ServiceResponseVM<TripResponse>> Create(Trip newTrip);
    Task<ServiceResponseVM<Trip>> Update(UpdateTripVM updateTrip, int id);
}
