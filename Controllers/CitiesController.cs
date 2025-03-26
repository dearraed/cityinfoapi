using Asp.Versioning;
using AutoMapper;
using CityInfo.API.Entities;
using CityInfo.API.Models;
using CityInfo.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace CityInfo.API.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/v{version:apiVersion}/cities")]
    [ApiVersion("1.0")]
    [ApiVersion("2.0")]
    public class CitiesController : ControllerBase
    {
        private ICityInfoRepository _cityInfoRepository;
        private readonly IMapper _mapper;
        const int maxCitiesPageSize = 20;

        public CitiesController(ICityInfoRepository cityInfoRepository, IMapper mapper)
        {
            _cityInfoRepository = cityInfoRepository ?? throw new ArgumentNullException(nameof(cityInfoRepository));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<CityWithoutPointOfInterestDto>>> GetCities([FromQuery] string? name, [FromQuery] string? searchQuery,
            int pageNumber = 1, int pageSize = 10)
        {
            try
            {
                if (pageSize > maxCitiesPageSize)
                {
                    pageSize = maxCitiesPageSize;
                }
                var (cityEntities, paginationMetadata) = await _cityInfoRepository.GetCitiesAsync(name, searchQuery, pageNumber, pageSize);

                Response.Headers.Add("X-Pagination", JsonSerializer.Serialize(paginationMetadata));

                return Ok(_mapper.Map<IEnumerable<CityWithoutPointOfInterestDto>>(cityEntities));
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Database Failure");
            }
           
        }

        /// <summary>
        /// Get a city by id
        /// </summary>
        /// <param name="cityId">The id of the city to get</param>
        /// <param name="includePointsOfInterest">Whether or not to include the points of interest</param>
        /// <response code="200">Returns the requested city</response>
        /// <returns>A city with or without points of interest</returns>
        [HttpGet("{cityId}", Name = "GetCity")]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<CityWithoutPointOfInterestDto>>> GetCity(int cityId, 
            bool includePointsOfInterest = false)
        {
            City cityEntity = await _cityInfoRepository.GetCityAsync(cityId, includePointsOfInterest);

            if(cityEntity == null)
            {
                return NotFound();
            }

            if (includePointsOfInterest)
            {
                return Ok(_mapper.Map<CityDto>(cityEntity));
            }
            return Ok(_mapper.Map<CityWithoutPointOfInterestDto>(cityEntity));
            
        }

        [HttpPost]
        public async Task<ActionResult<CityDto>> CreateCity(CityDto city)
        {
            var finalCity = _mapper.Map<Entities.City>(city);
            await _cityInfoRepository.addCity(finalCity);
            await _cityInfoRepository.SaveChangesAsync();
            var createdCityToReturn = _mapper.Map<CityDto>(finalCity);
            return CreatedAtRoute("GetCity",
            new
            {
                cityId = createdCityToReturn.Id,
                includePointOfInerest = true
            },
            createdCityToReturn);
        }
    }
}
