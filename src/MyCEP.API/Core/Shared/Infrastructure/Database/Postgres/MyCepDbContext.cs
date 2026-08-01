using Microsoft.EntityFrameworkCore;
using MyCEP.API.Core.Shared.Infrastructure.Schemas;
using MyCEP.API.Core.Shared.Infrastructure.Schemas.Map;

namespace MyCEP.API.Core.Shared.Infrastructure.Database.Postgres
{
    public class MyCepDbContext : DbContext
    {
        public MyCepDbContext(DbContextOptions<MyCepDbContext> options) : base(options)
        {
        }

        public DbSet<AddressInfra> Addresses { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new AddressMap());

            base.OnModelCreating(modelBuilder);
        }
    }
}