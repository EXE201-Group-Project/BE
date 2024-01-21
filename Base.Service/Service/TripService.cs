using Base.Repository.Common;
using Base.Repository.Entity;
using Base.Service.Common;
using Base.Service.IService;
using Base.Service.Validation;
using Base.Service.ViewModel.RequestVM.Trip;
using Base.Service.ViewModel.ResponseVM;
using Duende.IdentityServer.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Base.Service.Service;

internal class TripService : ITripService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidateGet _validateGet;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUploadFile _uploadFile;
    public TripService(IUnitOfWork unitOfWork, IValidateGet validateGet, ICurrentUserService currentUserService, IUploadFile uploadFile)
    {
        _unitOfWork = unitOfWork;
        _validateGet = validateGet;
        _currentUserService = currentUserService;
        _uploadFile = uploadFile;
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
    public async Task<ServiceResponseVM<Trip>> Create(Trip newTrip)
    {
        var existedUser = await _unitOfWork.UserRepository.FindAsync(newTrip.UserId);
        if (existedUser is null)
        {
            return new ServiceResponseVM<Trip>
            {
                IsSuccess = false,
                Title = "Create trip failed",
                Errors = new string[1] { "User not found" }
            };
        }

        if(newTrip.Locations.Count() > 0)
        {
            var errors = new ConcurrentQueue<string>();

            var options = new ParallelOptions
            {
                MaxDegreeOfParallelism = Convert.ToInt32(Math.Ceiling(Environment.ProcessorCount * 0.3 * 2))
            };

            Parallel.ForEach(newTrip.Locations, options, async (location, state) =>
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

            if (!errors.IsNullOrEmpty())
            {
                return new ServiceResponseVM<Trip>
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
                return new ServiceResponseVM<Trip>
                {
                    IsSuccess = true,
                    Title = "Create trip successfully",
                    Result = newTrip
                };
            }
            else
            {
                return new ServiceResponseVM<Trip>
                {
                    IsSuccess = false,
                    Title = "Create trip failed",
                    Errors = new string[1] { "Save changes failed" }
                };
            }
        }
        catch (DbUpdateException ex)
        {
            return new ServiceResponseVM<Trip>
            {
                IsSuccess = false,
                Title = "Create trip failed",
                Errors = new string[1] { ex.Message }
            };
        }
        catch (OperationCanceledException ex)
        {
            return new ServiceResponseVM<Trip>
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
}
