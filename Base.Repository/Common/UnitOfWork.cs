using Base.Repository.IRepository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Base.Repository.Common;

public interface IUnitOfWork
{
    IUserRepository UserRepository { get; }
    IRoleRepository RoleRepository { get; }
    IItemRepository ItemRepository { get; }
    ILocationRepository LocationRepository { get; }
    IRouteRepository RouteRepository { get; }
    ITripRepository TripRepository { get; }
    IBillRepository BillRepository { get; }
    IPackageRepository PackageRepository { get; }
    Task<bool> SaveChangesAsync();
}

public class UnitOfWork : IUnitOfWork, IDisposable
{
    private readonly ApplicationDbContext _applicationDbContext;
    
    public IUserRepository UserRepository { get; private set; }
    public IRoleRepository RoleRepository { get; private set; }
    public IItemRepository ItemRepository { get; private set; }
    public ILocationRepository LocationRepository { get; private set; }
    public IRouteRepository RouteRepository { get; private set; }
    public ITripRepository TripRepository { get; private set; }
    public IBillRepository BillRepository { get; private set; }
    public IPackageRepository PackageRepository { get; private set; }

    public UnitOfWork(ApplicationDbContext applicationDbContext, 
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IItemRepository itemRepository,
        ILocationRepository locationRepository,
        IRouteRepository routeRepository,
        ITripRepository tripRepository,
        IBillRepository billRepository,
        IPackageRepository packageRepository)
    {
        _applicationDbContext = applicationDbContext;
        UserRepository = userRepository;
        RoleRepository = roleRepository;
        ItemRepository = itemRepository;
        LocationRepository = locationRepository;
        RouteRepository = routeRepository;
        TripRepository = tripRepository;
        BillRepository = billRepository;
        PackageRepository = packageRepository;
    }

    public async Task<bool> SaveChangesAsync()
    {
        return (await _applicationDbContext.SaveChangesAsync() > 0);
    }

    public void Dispose()
    {
        _applicationDbContext.Dispose();
    }
}
