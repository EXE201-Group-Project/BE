using AutoMapper;
using Base.Repository.Entity;
using Base.Service.IService;
using Base.Service.ViewModel.RequestVM.Item;
using Base.Service.ViewModel.RequestVM.Location;
using Base.Service.ViewModel.ResponseVM;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Base.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LocationController : ControllerBase
    {
        private readonly ILocationService _locationService;
        private readonly IMapper _mapper;

        public LocationController(ILocationService locationService, IMapper mapper)
        {
            _locationService = locationService;
            _mapper = mapper;
        }

        [HttpGet("{id}", Name = nameof(GetLocationById))]
        public async Task<IActionResult> GetLocationById(int id)
        {
            if (ModelState.IsValid)
            {
                var item = await _locationService.GetById(id);
                if (item is null)
                {
                    return NotFound(new
                    {
                        Title = "Location not found"
                    });
                }
                return Ok(new
                {
                    Result = _mapper.Map<ItemResponseVM>(item)
                });
            }

            return BadRequest(new
            {
                Title = "Get location information failed",
                Errors = new string[1] { "Invalid input" }
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetLocations([FromQuery] int startPage, [FromQuery] int endPage, [FromQuery] int? quantity, [FromQuery] string? address, [FromQuery] int? order, [FromQuery] int? tripId)
        {
            if (ModelState.IsValid)
            {
                var items = await _locationService.Get(startPage, endPage, quantity, address, order, tripId);
                return Ok(new
                {
                    Result = _mapper.Map<IEnumerable<LocationResponseVM>>(items)
                });
            }

            return BadRequest(new
            {
                Title = "Get locations information failed",
                Errors = new string[1] { "Invalid input" }
            });
        }

        [HttpPost]
        public async Task<IActionResult> CreateLocation([FromForm] LocationVM resource)
        {
            if (ModelState.IsValid)
            {
                var result = await _locationService.Create(_mapper.Map<Location>(resource));
                if (result.IsSuccess)
                {
                    return CreatedAtAction(nameof(GetLocationById), new
                    {
                        id = result.Result!.Id,
                    }, _mapper.Map<LocationResponseVM>(result.Result!));
                }

                return BadRequest(result);
            }

            return BadRequest(new
            {
                Title = "Create location failed",
                Errors = new string[1] { "Invalid input" }
            });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteLocation(int id)
        {
            if (ModelState.IsValid)
            {
                var result = await _locationService.Delete(id);
                if (result.IsSuccess)
                {
                    return Ok(result);
                }

                return BadRequest(result);
            }

            return BadRequest(new
            {
                Title = "Delete location failed",
                Errors = new string[1] { "Invalid input" }
            });
        }
    }
}
