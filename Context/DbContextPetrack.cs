using APIPetrack.Models.Users;
using Microsoft.EntityFrameworkCore;

namespace APIPetrack.Context
{
    public class DbContextPetrack : DbContext
    {
        public DbContextPetrack(DbContextOptions<DbContextPetrack> options) : base(options) { }

        public DbSet<PetOwner> PetOwner { get; set; }
        public DbSet<PetStoreShelter> PetStoreShelter { get; set; }
        public DbSet<Veterinarian> Veterinarian { get; set; }
    }
}
