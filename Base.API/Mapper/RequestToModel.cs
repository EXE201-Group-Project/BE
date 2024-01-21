using AutoMapper;
using Base.Repository.Entity;
using Base.Repository.Identity;
using Base.Service.Common;
using Base.Service.ViewModel.RequestVM.Item;
using Base.Service.ViewModel.RequestVM.Location;
using Base.Service.ViewModel.RequestVM.Role;
using Base.Service.ViewModel.RequestVM.Trip;

namespace Base.API.Mapper
{
    public class RequestToModel : Profile
    {
        private readonly ICurrentUserService _currentUserService;
        public RequestToModel(ICurrentUserService currentUserService)
        {
            _currentUserService = currentUserService;

            CreateMap<RoleVM, Role>()
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.RoleName));
            CreateMap<UpdateRoleVM, Role>()
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.RoleName));

            CreateMap<ItemVM, Item>();
            CreateMap<UpdateItemVM, Item>();

            CreateMap<LocationVM, Location>();
            CreateMap<Location_ItemVM, Item>();

            CreateMap<TripVM, Trip>();
            CreateMap<Trip_LocationVM, Location>();
            CreateMap<Trip_ItemVM, Item>();
        }
    }
}
