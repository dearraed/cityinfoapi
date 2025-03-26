using CityInfo.API.Entities;
using CityInfo.API.Models;

namespace CityInfo.API.Services
{
    public interface ICityInfoRepository
    {
        Task<IEnumerable<City>> GetCitiesAsync();

        Task<(IEnumerable<City>, PaginationMetadata)> GetCitiesAsync(string? name, string? searchQuery, int pageNumber, int pageSize);
        Task<City?> GetCityAsync(int cityId, bool includePointsOfInterest);
        Task<PointOfInterest?> GetPointOfInterestForCityAsync(int cityId, int pointOfInterestId);

        Task<IEnumerable<PointOfInterest>> GetPointsOfInterestForCityAsync(int cityId);

        Task<bool> CityExistsAsync(int cityId);
        Task AddPointOfInterestForCityAsync(int cityId, PointOfInterest finalPointOfInterest);

        Task<bool> SaveChangesAsync();

        void DeletePointOfInterest(PointOfInterest pointOfInterest);

        Task addCity(City city);

        Task<bool> CityNameMatchesCityId(string? cityName, int cityId);
    }
}
