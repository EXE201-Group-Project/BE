using Base.Repository.Common;
using Base.Repository.Entity;
using Base.Service.Common;
using Base.Service.IService;
using Base.Service.Validation;
using Base.Service.ViewModel.ResponseVM;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Reflection;

namespace Base.Service.Service;

internal class LocationService : ILocationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidateGet _validateGet;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUploadFile _uploadFile;

    public LocationService(IUnitOfWork unitOfWork, IValidateGet validateGet, ICurrentUserService currentUserService, IUploadFile uploadFile)
    {
        _unitOfWork = unitOfWork;
        _validateGet = validateGet;
        _currentUserService = currentUserService;
        _uploadFile = uploadFile;
    }

    public async Task<Location?> GetById(int id)
    {
        return await _unitOfWork.LocationRepository.Get(l => !l.Deleted && l.Id == id).FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<Location>> Get(int startPage, int endPage, int? quantity, string? address, int? order, int? tripId)
    {
        int quantityResult = 0;
        _validateGet.ValidateGetRequest(ref startPage, ref endPage, quantity, ref quantityResult);
        if (quantityResult == 0)
        {
            throw new ArgumentException("Error when get quantity per page");
        }

        var expressions = new List<Expression>();
        ParameterExpression pe = Expression.Parameter(typeof(Location), "l");
        MethodInfo? containsMethod = typeof(string).GetMethod("Contains", new[] { typeof(string) });

        if (containsMethod is null)
        {
            throw new ArgumentNullException("Method Contains can not found from string type");
        }

        expressions.Add(Expression.Equal(Expression.Property(pe, nameof(Location.Deleted)), Expression.Constant(false)));

        if(address is not null)
        {
            expressions.Add(Expression.Call(Expression.Property(pe, nameof(Location.Address)), containsMethod ,Expression.Constant(address)));
        }

        if(order is not null)
        {
            expressions.Add(Expression.Equal(Expression.Property(pe, nameof(Location.Order)), Expression.Constant(order)));
        }

        if(tripId is not null)
        {
            expressions.Add(Expression.Equal(Expression.Property(pe, nameof(Location.TripId)), Expression.Constant(tripId)));
        }

        Expression combined = expressions.Aggregate((accumulate, next) => Expression.AndAlso(accumulate, next));
        Expression<Func<Location, bool>> where = Expression.Lambda<Func<Location, bool>>(combined, pe);

        return await _unitOfWork.LocationRepository
            .Get(where)
            .AsNoTracking()
            .Skip((startPage - 1) * quantityResult)
            .Take((endPage - startPage + 1) * quantityResult)
            .ToArrayAsync();
    }

    public async Task<ServiceResponseVM> Delete(int id)
    {
        var existedLocation = await _unitOfWork.LocationRepository.Get(l => !l.Deleted && l.Id == id).FirstOrDefaultAsync();
        if(existedLocation is null)
        {
            return new ServiceResponseVM
            {
                IsSuccess = false,
                Title = "Delete location failed",
                Errors = new string[1] { "Location not found" }
            };
        }

        existedLocation.Deleted = true;
        try
        {
            var result = await _unitOfWork.SaveChangesAsync();
            if (result)
            {
                return new ServiceResponseVM
                {
                    IsSuccess = true,
                    Title = "Delete location successfully"
                };
            }
            else
            {
                return new ServiceResponseVM
                {
                    IsSuccess = false,
                    Title = "Delete location failed",
                    Errors = new string[1] { "Save changes failed" }
                };
            }
        }
        catch (DbUpdateException ex)
        {
            return new ServiceResponseVM
            {
                IsSuccess = false,
                Title = "Delete location failed",
                Errors = new string[1] { ex.Message }
            };
        }
        catch (OperationCanceledException ex)
        {
            return new ServiceResponseVM
            {
                IsSuccess = false,
                Title = "Delete location failed",
                Errors = new string[2] { "The operation has been cancelled", ex.Message }
            };
        }
    }

    public async Task<ServiceResponseVM<Location>> Create(Location newLocation)
    {
        var existedTrip = await _unitOfWork.TripRepository.FindAsync(newLocation.TripId);
        if (existedTrip is null)
        {
            return new ServiceResponseVM<Location>
            {
                IsSuccess = false,
                Title = "Create location failed",
                Errors = new string[1] { "Trip not found" }
            };
        }

        if (newLocation.Address is null)
        {
            return new ServiceResponseVM<Location>
            {
                IsSuccess = false,
                Title = "Create location failed",
                Errors = new string[1] { "Address is required" }
            };
        }

        if(newLocation.Items.Count() > 0)
        {
            foreach(var item in newLocation.Items)
            {
                if(item.Image is not null && item.Image.Length > 0)
                {
                    var uploadResult = await _uploadFile.UploadImage(item.Image);
                    if (uploadResult.Error is not null)
                    {
                        return new ServiceResponseVM<Location>
                        {
                            IsSuccess = false,
                            Title = "Create location failed",
                            Errors = new string[2] { uploadResult.Error.Message, $"Can not upload the image '{item.Image.FileName}'" }
                        };
                    }

                    item.ImageUrl = new KeyValuePair<string, string>(item.Image.FileName, uploadResult.SecureUrl.ToString());
                }
            }
        }

        newLocation.CreatedAt = DateTime.UtcNow;
        newLocation.CreatedBy = _currentUserService.UserId;

        await _unitOfWork.LocationRepository.AddAsync(newLocation);

        try
        {
            var result = await _unitOfWork.SaveChangesAsync();
            if (result)
            {
                return new ServiceResponseVM<Location>
                {
                    IsSuccess = true,
                    Title = "Create location successfully",
                    Result = newLocation
                };
            }
            else
            {
                return new ServiceResponseVM<Location>
                {
                    IsSuccess = false,
                    Title = "Create location failed",
                    Errors = new string[1] { "Save changes failed" }
                };
            }
        }
        catch (DbUpdateException ex)
        {
            return new ServiceResponseVM<Location>
            {
                IsSuccess = false,
                Title = "Create location failed",
                Errors = new string[1] { ex.Message }
            };
        }
        catch (OperationCanceledException ex)
        {
            return new ServiceResponseVM<Location>
            {
                IsSuccess = false,
                Title = "Create location failed",
                Errors = new string[2] { "The operation has been cancelled", ex.Message }
            };
        }
    }
}
