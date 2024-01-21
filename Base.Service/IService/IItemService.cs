using Base.Repository.Entity;
using Base.Service.ViewModel.RequestVM.Item;
using Base.Service.ViewModel.ResponseVM;

namespace Base.Service.IService;

public interface IItemService
{
    Task<Item?> GetById(int id);
    Task<IEnumerable<Item>> Get(int startPage, int endPage, int? quantity, string? filter, bool? pickUp, int? locationId);
    Task<ServiceResponseVM> Delete(int id);
    Task<ServiceResponseVM<Item>> Create(Item newItem);
    Task<ServiceResponseVM<Item>> Update(UpdateItemVM updateItem, int id);
}
