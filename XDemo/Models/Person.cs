using Dallus;

namespace XDemo.Models
{
    public class Person : IRepoModel
    {
        public int Id { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? MiddleName { get; set; }
        public Address Address { get; set; }
    }
}
