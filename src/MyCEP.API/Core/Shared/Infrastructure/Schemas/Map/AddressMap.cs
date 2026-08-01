using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MyCEP.API.Core.Shared.Infrastructure.Schemas.Map
{
    public class AddressMap : IEntityTypeConfiguration<AddressInfra>
    {
        public void Configure(EntityTypeBuilder<AddressInfra> builder)
        {

            builder.ToTable("address");

            builder.HasKey(a => a.Id);

            // Registros com soft delete somem de todas as consultas de leitura.
            builder.HasQueryFilter(a => a.DeletedAt == null);

            builder.Property(a => a.Zipcode)
                   .HasColumnName("zipcode")
                   .HasMaxLength(20);

            builder.Property(a => a.Street)
                   .HasColumnName("street")
                   .HasMaxLength(200);

            builder.Property(a => a.Complement)
                   .HasColumnName("complement")
                   .HasMaxLength(200);

            builder.Property(a => a.Neighborhood)
                   .HasColumnName("neighborhood")
                   .HasMaxLength(100);

            builder.Property(a => a.City)
                   .HasColumnName("city")
                   .HasMaxLength(100);

            builder.Property(a => a.State)
                   .HasColumnName("state")
                   .HasMaxLength(2);

            builder.Property(a => a.IbgeCode)
                   .HasColumnName("ibgeCode")
                   .HasMaxLength(10);

            builder.Property(a => a.GiaCode)
                   .HasColumnName("giaCode")
                   .HasMaxLength(20);

            builder.Property(a => a.Ddd)
                   .HasColumnName("areaCode")
                   .HasMaxLength(5);

            builder.Property(a => a.SiafiCode)
                   .HasColumnName("siafiCode")
                   .HasMaxLength(10);

            // A busca é sempre por CEP normalizado (somente dígitos). O índice
            // único transforma a corrida de duas requisições simultâneas do
            // mesmo CEP num conflito detectável, em vez de linhas duplicadas.
            //
            // Parcial (WHERE deleted_at IS NULL): a unicidade vale só entre
            // registros ativos, casando com o filtro global de soft delete —
            // um CEP deletado não impede reinserir o mesmo CEP.
            builder.HasIndex(a => a.Zipcode)
                   .IsUnique()
                   .HasDatabaseName("ix_address_zipcode")
                   .HasFilter("deleted_at IS NULL");

            builder.HasData(
                new AddressInfra
                {
                    Id = new Guid("b3b5b50f-8c8f-4f3b-a8fa-cf6e03750df1"),
                    Zipcode = "01000000",
                    Street = "Avenida Paulista",
                    Complement = "Apto 101",
                    Neighborhood = "Bela Vista",
                    City = "São Paulo",
                    State = "SP",
                    IbgeCode = "3550308",
                    GiaCode = "1004",
                    Ddd = "11",
                    SiafiCode = "7107",
                    CreatedAt = new DateTimeOffset(2024, 2, 26, 12, 0, 0, TimeSpan.Zero)
                },

                new AddressInfra
                {
                    Id = new Guid("bba8c410-1f3a-4b5c-93c3-8317ebaa7f9e"),
                    Zipcode = "20000000",
                    Street = "Rua da Assembleia",
                    Complement = "Sala 202",
                    Neighborhood = "Centro",
                    City = "Rio de Janeiro",
                    State = "RJ",
                    IbgeCode = "3304557",
                    GiaCode = "2001",
                    Ddd = "21",
                    SiafiCode = "6001",
                    CreatedAt = new DateTimeOffset(2024, 2, 26, 12, 0, 0, TimeSpan.Zero)
                }
            );
        }
    }
}
