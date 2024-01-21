using AutoMapper;
using Base.Repository.Entity;
using Base.Service.IService;
using Base.Service.Service;
using Base.Service.ViewModel.RequestVM.Item;
using Base.Service.ViewModel.RequestVM.Role;
using Base.Service.ViewModel.ResponseVM;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Base.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ItemController : ControllerBase
{
    private readonly IItemService _itemService;
    private readonly IMapper _mapper;

    public ItemController(IItemService itemService, IMapper mapper)
    {
        _itemService = itemService;
        _mapper = mapper;
    }

    [HttpGet("{id}", Name = nameof(GetItemById))]
    public async Task<IActionResult> GetItemById(int id)
    {
        if (ModelState.IsValid)
        {
            var item = await _itemService.GetById(id);
            if (item is null)
            {
                return NotFound(new
                {
                    Title = "Item not found"
                });
            }
            return Ok(new
            {
                Result = _mapper.Map<ItemResponseVM>(item)
            });
        }

        return BadRequest(new
        {
            Title = "Get item information failed",
            Errors = new string[1] { "Invalid input" }
        });
    }

    [HttpGet]
    public async Task<IActionResult> GetItems([FromQuery] int startPage, [FromQuery] int endPage, [FromQuery] int? quantity, [FromQuery] string? filter, [FromQuery] bool? pickUp, [FromQuery] int? locationId)
    {
        if (ModelState.IsValid)
        {
            var items = await _itemService.Get(startPage, endPage, quantity, filter, pickUp, locationId);
            return Ok(new
            {
                Result = _mapper.Map<IEnumerable<ItemResponseVM>>(items)
            });
        }

        return BadRequest(new
        {
            Title = "Get items information failed",
            Errors = new string[1] { "Invalid input" }
        });
    }

    [HttpPost]
    public async Task<IActionResult> CreateItem([FromForm] ItemVM resource)
    {
        if (ModelState.IsValid)
        {
            var result = await _itemService.Create(_mapper.Map<Item>(resource));
            if (result.IsSuccess)
            {
                return CreatedAtAction(nameof(GetItemById), new
                {
                    id = result.Result!.Id,
                }, _mapper.Map<ItemResponseVM>(result.Result!));
            }

            return BadRequest(result);
        }

        return BadRequest(new
        {
            Title = "Create item failed",
            Errors = new string[1] { "Invalid input" }
        });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateItem([FromForm] UpdateItemVM resource, int id)
    {
        if (ModelState.IsValid)
        {
            var result = await _itemService.Update(resource, id);
            if (result.IsSuccess)
            {
                return Ok(new
                {
                    Title = result.Title,
                    Result = _mapper.Map<ItemResponseVM>(result.Result)
                });
            }

            return BadRequest(result);
        }

        return BadRequest(new
        {
            Title = "Update item failed",
            Errors = new string[1] { "Invalid input" }
        });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteItem(int id)
    {
        if (ModelState.IsValid)
        {
            var result = await _itemService.Delete(id);
            if (result.IsSuccess)
            {
                return Ok(result);
            }

            return BadRequest(result);
        }

        return BadRequest(new
        {
            Title = "Delete item failed",
            Errors = new string[1] { "Invalid input" }
        });
    }
}
