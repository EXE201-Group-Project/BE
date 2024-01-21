using Base.Repository.Common;
using Base.Repository.Entity;
using Base.Service.Common;
using Base.Service.IService;
using Base.Service.Validation;
using Base.Service.ViewModel.RequestVM.Item;
using Base.Service.ViewModel.ResponseVM;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Reflection;
using Expression = System.Linq.Expressions.Expression;

namespace Base.Service.Service;

internal class ItemService : IItemService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidateGet _validateGet;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUploadFile _uploadFile;

    public ItemService(IUnitOfWork unitOfWork, IValidateGet validateGet, ICurrentUserService currentUserService, IUploadFile uploadFile)
    {
        _unitOfWork = unitOfWork;
        _validateGet = validateGet;
        _currentUserService = currentUserService;
        _uploadFile = uploadFile;
    }

    public async Task<Item?> GetById(int id)
    {
        return await _unitOfWork.ItemRepository.Get(i => !i.Deleted && i.Id == id).FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<Item>> Get(int startPage, int endPage, int? quantity, string? filter, bool? pickUp, int? locationId)
    {
        int quantityResult = 0;
        _validateGet.ValidateGetRequest(ref startPage, ref endPage, quantity, ref quantityResult);
        if (quantityResult == 0)
        {
            throw new ArgumentException("Error when get quantity per page");
        }

        var expressions = new List<Expression>();
        ParameterExpression pe = Expression.Parameter(typeof(Item), "r");
        MethodInfo? containsMethod = typeof(string).GetMethod("Contains", new[] { typeof(string) });

        if (containsMethod is null)
        {
            throw new ArgumentNullException("Method Contains can not found from string type");
        }

        expressions.Add(Expression.Equal(Expression.Property(pe, nameof(Item.Deleted)), Expression.Constant(false)));

        if(filter is not null)
        {
            expressions.Add(
                Expression.Or(
                    Expression.Call(Expression.Property(pe, nameof(Item.Name)), containsMethod, Expression.Constant(filter)), 
                    Expression.Call(Expression.Property(pe, nameof(Item.Description)), containsMethod, Expression.Constant(filter))
                ));
        }

        if(pickUp is not null)
        {
            expressions.Add(Expression.Equal(Expression.Property(pe, nameof(Item.PickUp)), Expression.Constant(pickUp)));
        }

        if(locationId is not null)
        {
            expressions.Add(Expression.Equal(Expression.Property(pe, nameof(Item.LocationId)), Expression.Constant(locationId)));
        }

        Expression combined = expressions.Aggregate((accumulate, next) => Expression.AndAlso(accumulate, next));
        Expression<Func<Item, bool>> where = Expression.Lambda<Func<Item, bool>>(combined, pe);

        return await _unitOfWork.ItemRepository
            .Get(where)
            .AsNoTracking()
            .Skip((startPage - 1) * quantityResult)
            .Take((endPage - startPage + 1) * quantityResult)
            .ToArrayAsync();
    }

    public async Task<ServiceResponseVM> Delete(int id)
    {
        var existedItem = await _unitOfWork.ItemRepository.Get(i => !i.Deleted && i.Id == id).FirstOrDefaultAsync();
        if(existedItem is null)
        {
            return new ServiceResponseVM
            {
                IsSuccess = false,
                Title = "Delete item failed",
                Errors = new string[1] { "Item not found" }
            };
        }

        existedItem.Deleted = true;
        try
        {
            var result = await _unitOfWork.SaveChangesAsync();
            if (result)
            {
                return new ServiceResponseVM
                {
                    IsSuccess = true,
                    Title = "Delete item successfully"
                };
            }
            else
            {
                return new ServiceResponseVM
                {
                    IsSuccess = false,
                    Title = "Delete item failed",
                    Errors = new string[1] { "Save changes failed" }
                };
            }
        }
        catch (DbUpdateException ex)
        {
            return new ServiceResponseVM
            {
                IsSuccess = false,
                Title = "Delete item failed",
                Errors = new string[1] { ex.Message }
            };
        }
        catch (OperationCanceledException ex)
        {
            return new ServiceResponseVM
            {
                IsSuccess = false,
                Title = "Delete item failed",
                Errors = new string[2] { "The operation has been cancelled", ex.Message }
            };
        }
    }

    public async Task<ServiceResponseVM<Item>> Create(Item newItem)
    {
        var existedLocation = await _unitOfWork.LocationRepository.FindAsync(newItem.LocationId);
        if (existedLocation == null)
        {
            return new ServiceResponseVM<Item>
            {
                IsSuccess = false,
                Title = "Create item failed",
                Errors = new string[1] { "Location not found" }
            };
        }

        if(newItem.Image is not null && newItem.Image.Length > 0)
        {
            var uploadResult = await _uploadFile.UploadImage(newItem.Image);
            if(uploadResult.Error is not null)
            {
                return new ServiceResponseVM<Item>
                {
                    IsSuccess = false,
                    Title = "Create item failed",
                    Errors = new string[1] { uploadResult.Error.Message }
                };
            }

            newItem.ImageUrl = new KeyValuePair<string, string>(newItem.Image.FileName, uploadResult.SecureUrl.ToString());
        }

        newItem.CreatedAt = DateTime.UtcNow;
        newItem.CreatedBy = _currentUserService.UserId;

        await _unitOfWork.ItemRepository.AddAsync(newItem);

        try
        {
            var result = await _unitOfWork.SaveChangesAsync();
            if (result)
            {
                return new ServiceResponseVM<Item>
                {
                    IsSuccess = true,
                    Title = "Create item successfully",
                    Result = newItem
                };
            }
            else
            {
                return new ServiceResponseVM<Item>
                {
                    IsSuccess = false,
                    Title = "Create item failed",
                    Errors = new string[1] { "Save changes failed" }
                };
            }
        }
        catch (DbUpdateException ex)
        {
            return new ServiceResponseVM<Item>
            {
                IsSuccess = false,
                Title = "Create item failed",
                Errors = new string[1] { ex.Message }
            };
        }
        catch (OperationCanceledException ex)
        {
            return new ServiceResponseVM<Item>
            {
                IsSuccess = false,
                Title = "Create item failed",
                Errors = new string[2] { "The operation has been cancelled", ex.Message }
            };
        }
    }

    public async Task<ServiceResponseVM<Item>> Update(UpdateItemVM updateItem, int id)
    {
        var existedItem = await _unitOfWork.ItemRepository.Get(i => !i.Deleted && i.Id == id).FirstOrDefaultAsync();
        if(existedItem is null)
        {
            return new ServiceResponseVM<Item>
            {
                IsSuccess = false,
                Title = "Update item failed",
                Errors = new string[1] { "Item not found" }
            };
        }

        if(updateItem.LocationId is not null)
        {
            var existedLocation = await _unitOfWork.LocationRepository.FindAsync(updateItem.LocationId ?? 0);
            if (existedLocation == null)
            {
                return new ServiceResponseVM<Item>
                {
                    IsSuccess = false,
                    Title = "Update item failed",
                    Errors = new string[1] { "Location not found" }
                };
            }
        }

        if(updateItem.Image is not null && updateItem.Image.Length > 0)
        {
            var uploadResult = await _uploadFile.UploadImage(updateItem.Image);
            if (uploadResult.Error is not null)
            {
                return new ServiceResponseVM<Item>
                {
                    IsSuccess = false,
                    Title = "Update item failed",
                    Errors = new string[1] { uploadResult.Error.Message }
                };
            }

            existedItem.ImageUrl = new KeyValuePair<string, string>(updateItem.Image.FileName, uploadResult.SecureUrl.ToString());
        }

        existedItem.Name = updateItem.Name ?? existedItem.Name;
        existedItem.Quantity = updateItem.Quantity ?? existedItem.Quantity;
        existedItem.Description = updateItem.Description ?? existedItem.Description;
        existedItem.PickUp = updateItem.PickUp ?? existedItem.PickUp;

        try
        {
            var result = await _unitOfWork.SaveChangesAsync();
            if (result)
            {
                return new ServiceResponseVM<Item>
                {
                    IsSuccess = true,
                    Title = "Update item successfully"
                };
            }
            else
            {
                return new ServiceResponseVM<Item>
                {
                    IsSuccess = false,
                    Title = "Update item failed",
                    Errors = new string[1] { "Save changes failed" }
                };
            }
        }
        catch (DbUpdateException ex)
        {
            return new ServiceResponseVM<Item>
            {
                IsSuccess = false,
                Title = "Update item failed",
                Errors = new string[1] { ex.Message }
            };
        }
        catch (OperationCanceledException ex)
        {
            return new ServiceResponseVM<Item>
            {
                IsSuccess = false,
                Title = "Update item failed",
                Errors = new string[2] { "The operation has been cancelled", ex.Message }
            };
        }
    }
}
