using AutoMapper;

namespace CityInfo.API.Profiles
{
    public class PointOfInterestProfile : Profile
    {
        public PointOfInterestProfile()
        {
            CreateMap<Entities.PointOfInterest, Models.PointOfInterestDto>(); // For GetPointOfInterest : Map entity to DTO
            CreateMap<Models.PointOfInterestForCreationDto, Entities.PointOfInterest>(); //For creating a new point of interest
            CreateMap<Models.PointOfInterestDto, Entities.PointOfInterest>(); //For create a POI under create a new city
            CreateMap<Models.PointOfInterestForUpdateDto, Entities.PointOfInterest>(); //For updating a POI : from 1st param to 2nd param
            CreateMap<Entities.PointOfInterest, Models.PointOfInterestForUpdateDto>(); //For patch a POI : from 1st param to 2nd param
        }
    }
}
