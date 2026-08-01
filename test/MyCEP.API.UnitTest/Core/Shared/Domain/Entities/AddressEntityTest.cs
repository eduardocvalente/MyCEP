using FluentAssertions;
using MyCEP.API.Core.Shared.Domain.Entities.Address;
using MyCEP.API.Core.Shared.Infrastructure.CrossCutting.NotificationError;

namespace MyCEP.API.UnitTest.Core.Shared.Domain.Entities
{
    public class AddressEntityTests
    {
        [Fact(DisplayName = "Deve retornar erro de validação se os campos obrigatórios estiverem vazios")]
        public void IsValid_WithEmptyMandatoryFields_ShouldReturnValidationErrors()
        {
            var address = new AddressEntity
            {
                Zipcode = string.Empty,
                Street = "Rua Teste",
                Complement = "Complemento",
                Neighborhood = "",
                City = "",
                State = "",
                IbgeCode = "",
                GiaCode = "GiaTeste",
                Ddd = "11",
                SiafiCode = "1234"
            };

            // Act
            var validationErrors = address.IsValid();

            // Assert — bairro e código IBGE deixaram de ser obrigatórios:
            // CEPs gerais de municípios pequenos vêm sem eles no ViaCEP.
            validationErrors.Error.Errors.Should().HaveCount(3);
            validationErrors.Error.Errors.Should().Contain(e => e.PropertyName == nameof(address.Zipcode) && e.PropertyErrors.Any(x => x == DefaultMessage.StringIsNullOrEmptyMessage.ToString()));
            validationErrors.Error.Errors.Should().Contain(e => e.PropertyName == nameof(address.City) && e.PropertyErrors.Any(x => x == DefaultMessage.StringIsNullOrEmptyMessage.ToString()));
            validationErrors.Error.Errors.Should().Contain(e => e.PropertyName == nameof(address.State) && e.PropertyErrors.Any(x => x == DefaultMessage.StringIsNullOrEmptyMessage.ToString()));
        }

        [Fact(DisplayName = "Deve validar com sucesso se os campos obrigatórios forem preenchidos")]
        public void IsValid_WithValidMandatoryFields_ShouldNotReturnValidationErrors()
        {
            var address = new AddressEntity
            {
                Zipcode = "12345678",
                Street = "Rua Teste",
                Complement = "Complemento",
                Neighborhood = "Bairro Teste",
                City = "Cidade Teste",
                State = "TS",
                IbgeCode = "54321",
                GiaCode = "GiaTeste",
                Ddd = "11",
                SiafiCode = "1234"
            };

            // Act
            var validationErrors = address.IsValid();

            // Assert
            validationErrors.Error.Errors.Should().BeNullOrEmpty();
            validationErrors.Error.Errors.Should().HaveCount(0);
        }

        [Fact(DisplayName = "Deve restaurar a AddressEntity corretamente")]
        public void Restore_ShouldCreateAddressEntityWithProperData()
        {
            // Arrange
            var id = Guid.NewGuid();
            var createdAt = DateTimeOffset.Now;
            var updatedAt = DateTimeOffset.Now;
            DateTimeOffset? deletedAt = null;

            // Act
            var restoredAddress = AddressEntity.Restore(
                id,
                "12345678",
                "Rua Restaurada",
                "Complemento Restaurado",
                "Bairro Restaurado",
                "Cidade Restaurada",
                "RS",
                "98765",
                "GiaRestaurada",
                "21",
                "4321",
                deletedAt,
                createdAt,
                updatedAt
            );

            // Assert
            restoredAddress.Should().NotBeNull();
            restoredAddress.Id.Should().Be(id);
            restoredAddress.Zipcode.Should().Be("12345678");
            restoredAddress.Street.Should().Be("Rua Restaurada");
            restoredAddress.Complement.Should().Be("Complemento Restaurado");
            restoredAddress.Neighborhood.Should().Be("Bairro Restaurado");
            restoredAddress.City.Should().Be("Cidade Restaurada");
            restoredAddress.State.Should().Be("RS");
            restoredAddress.IbgeCode.Should().Be("98765");
            restoredAddress.GiaCode.Should().Be("GiaRestaurada");
            restoredAddress.Ddd.Should().Be("21");
            restoredAddress.SiafiCode.Should().Be("4321");
        }

        [Fact(DisplayName = "Deve criar a AddressEntity conceitualmente corretamente")]
        public void ConceptualCreate_ShouldCreateAddressEntityProperly()
        {
            // Arrange
            var id = Guid.NewGuid();

            // Act
            var createdAddress = AddressEntity.ConceptualCreate(
                id,
                "87654321",
                "Rua Conceitual",
                "Complemento Conceitual",
                "Bairro Conceitual",
                "Cidade Conceitual",
                "CC",
                "12345",
                "GiaConceitual",
                "31",
                "5678"
            );

            // Assert
            createdAddress.Should().NotBeNull();
            createdAddress.Id.Should().Be(id);
            createdAddress.Zipcode.Should().Be("87654321");
            createdAddress.Street.Should().Be("Rua Conceitual");
            createdAddress.Complement.Should().Be("Complemento Conceitual");
            createdAddress.Neighborhood.Should().Be("Bairro Conceitual");
            createdAddress.City.Should().Be("Cidade Conceitual");
            createdAddress.State.Should().Be("CC");
            createdAddress.IbgeCode.Should().Be("12345");
            createdAddress.GiaCode.Should().Be("GiaConceitual");
            createdAddress.Ddd.Should().Be("31");
            createdAddress.SiafiCode.Should().Be("5678");
        }
    }
}
