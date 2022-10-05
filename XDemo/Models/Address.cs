using Dallus;

namespace XDemo.Models
{
    public class Address : IRepoModel
    {
        public int Id { get; set; }
        public string? Street { get; set; }
        public string? UnitNumber { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? Zip { get; set; }
    }
}
