using AutoMapper;
using Base.Repository.Entity;
using Base.Service.IService;
using Base.Service.ViewModel.RequestVM.Item;
using Base.Service.ViewModel.RequestVM.Trip;
using Base.Service.ViewModel.ResponseVM;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Base.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class TripController : ControllerBase
{
	private readonly IMapper _mapper;
	private readonly ITripService _tripService;

	public TripController(IMapper mapper, ITripService tripService)
	{
		_mapper = mapper;
		_tripService = tripService;
    }

    [HttpGet("{id}", Name = nameof(GetTripById))]
    public async Task<IActionResult> GetTripById(int id)
    {
        if (ModelState.IsValid)
        {
            var trip = await _tripService.GetById(id);
            if (trip is null)
            {
                return NotFound(new
                {
                    Title = "Trip not found"
                });
            }
            return Ok(new
            {
                Result = _mapper.Map<TripResponseVM>(trip)
            });
        }

        return BadRequest(new
        {
            Title = "Get trip information failed",
            Errors = new string[1] { "Invalid input" }
        });
    }

    [HttpGet]
    public async Task<IActionResult> GetTrips([FromQuery] int startPage, [FromQuery] int endPage, [FromQuery] int? quantity, [FromQuery] int? travelMode, [FromQuery] int? status, [FromQuery] Guid? userId, [FromQuery] DateTime? date)
    {
        if (ModelState.IsValid)
        {
            var trips = await _tripService.Get(startPage, endPage, quantity, travelMode, status, userId, date);
            return Ok(new
            {
                Result = _mapper.Map<IEnumerable<TripResponseVM>>(trips)
            });
        }

        return BadRequest(new
        {
            Title = "Get trips information failed",
            Errors = new string[1] { "Invalid input" }
        });
    }

    [HttpPost]
    public async Task<IActionResult> CreateTrip([FromForm] TripVM resource)
    {
        if (ModelState.IsValid)
        {
            var result = await _tripService.Create(_mapper.Map<Trip>(resource));
            if (result.IsSuccess)
            {
                return CreatedAtAction(nameof(GetTripById), new
                {
                    id = result.Result!.Id,
                }, _mapper.Map<TripResponseVM>(result.Result!));
            }

            return BadRequest(result);
        }

        return BadRequest(new
        {
            Title = "Create trip failed",
            Errors = new string[1] { "Invalid input" }
        });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateTrip([FromForm] UpdateTripVM resource, int id)
    {
        if (ModelState.IsValid)
        {
            var result = await _tripService.Update(resource, id);
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
            Title = "Update trip failed",
            Errors = new string[1] { "Invalid input" }
        });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteItem(int id)
    {
        if (ModelState.IsValid)
        {
            var result = await _tripService.Delete(id);
            if (result.IsSuccess)
            {
                return Ok(result);
            }

            return BadRequest(result);
        }

        return BadRequest(new
        {
            Title = "Delete trip failed",
            Errors = new string[1] { "Invalid input" }
        });
    }
}
