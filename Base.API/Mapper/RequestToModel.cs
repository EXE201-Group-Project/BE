using AutoMapper;
using Base.Service.Common;

namespace Base.API.Mapper
{
    public class RequestToModel : Profile
    {
        private readonly ICurrentUserService _currentUserService;
        public RequestToModel(ICurrentUserService currentUserService)
        {
            _currentUserService = currentUserService;
        }
    }
}
