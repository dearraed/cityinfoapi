namespace CityInfo.API.Entities
{
    public class Country
    {
        public Country(int id, string name, string description)
        {
            Id = id;
            Name = name;
            Description = description;
        }

        int Id { get; set; }    
        string Name { get; set; }
        string Description { get; set; }    
    }
}
