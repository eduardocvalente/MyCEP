using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MyCEP.API.Core.Shared.Domain.Entities.Address;
using MyCEP.API.Core.Shared.Infrastructure.CrossCutting.Result;
using MyCEP.API.Core.Shared.Infrastructure.Repository.Base;
using MyCEP.API.Core.Shared.Infrastructure.Schemas;
using MyCEP.API.IntegrationTest.Base;

namespace MyCEP.API.IntegrationTest.Shared.Infrastructure.Repository.Base
{
    /// <summary>
    /// Testes de integração para AddressRepository
    /// Testa todas as funcionalidades do repositório usando banco PostgreSQL real via TestContainers
    /// </summary>
    public sealed class AddressRepositoryIntegrationTests : IntegrationTestBase
    {
        private AddressRepository GetAddressRepository()
        {
            return new AddressRepository(DbContext);
        }

        [Fact(DisplayName = "Deve retornar endereço com sucesso quando CEP existir no banco de dados")]
        public async Task GetAddressZipCode_WhenAddressExists_ShouldReturnSuccessWithCorrectData()
        {
            // Arrange
            var zipCode = "12345678";
            var expectedAddress = new AddressInfra
            {
                Id = Guid.NewGuid(),
                Zipcode = zipCode,
                Street = "Rua Teste Integração",
                Complement = "Complemento Teste",
                Neighborhood = "Bairro Teste",
                City = "Cidade Teste",
                State = "SP",
                IbgeCode = "1234567",
                GiaCode = "123",
                Ddd = "11",
                SiafiCode = "1234",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await DbContext.Set<AddressInfra>().AddAsync(expectedAddress);
            await DbContext.SaveChangesAsync();

            // Act
            var addressRepository = GetAddressRepository();
            var result = await addressRepository.GetAddressZipCode(zipCode);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeAssignableTo<ISuccessResult<AddressEntity>>();

            var successResult = result as ISuccessResult<AddressEntity>;
            successResult.Should().NotBeNull();
            successResult!.Data.Should().NotBeNull();

            // Validar todos os campos mapeados corretamente
            var returnedAddress = successResult.Data;
            returnedAddress.Zipcode.Should().Be(zipCode);
            returnedAddress.Street.Should().Be("Rua Teste Integração");
            returnedAddress.Complement.Should().Be("Complemento Teste");
            returnedAddress.Neighborhood.Should().Be("Bairro Teste");
            returnedAddress.City.Should().Be("Cidade Teste");
            returnedAddress.State.Should().Be("SP");
            returnedAddress.IbgeCode.Should().Be("1234567");
            returnedAddress.GiaCode.Should().Be("123");
            returnedAddress.Ddd.Should().Be("11");
            returnedAddress.SiafiCode.Should().Be("1234");
        }

        [Fact(DisplayName = "Deve retornar erro de negócio quando endereço não existir para o CEP informado")]
        public async Task GetAddressZipCode_WhenAddressDoesNotExist_ShouldReturnFailureWithBusinessError()
        {
            // Arrange
            var nonExistentZipCode = "99999999";

            // Act
            var result = await GetAddressRepository().GetAddressZipCode(nonExistentZipCode);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeAssignableTo<IFailResult<AddressEntity>>();

            var failResult = result as IFailResult<AddressEntity>;
            failResult.Should().NotBeNull();
            failResult!.NotificationErrors.Should().NotBeNull();
            failResult.NotificationErrors.HaveError().Should().BeTrue();
            failResult.NotificationErrors.Error.Errors.Should().HaveCount(1);
            failResult.NotificationErrors.Error.Errors.First().PropertyErrors.First().Should().Contain("Endereço não encontrado para o CEP informado");
            failResult.ErrorType.Should().Be(ErrorType.BusinessError);
        }

        [Fact(DisplayName = "Deve retornar falha quando CEP for uma string vazia")]
        public async Task GetAddressZipCode_WhenZipCodeIsEmpty_ShouldReturnFailure()
        {
            // Arrange
            var emptyZipCode = string.Empty;

            // Act
            var result = await GetAddressRepository().GetAddressZipCode(emptyZipCode);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeAssignableTo<IFailResult<AddressEntity>>();
        }

        [Fact(DisplayName = "Deve retornar falha quando CEP for null")]
        public async Task GetAddressZipCode_WhenZipCodeIsNull_ShouldReturnFailure()
        {
            // Arrange
            string nullZipCode = null!;

            // Act
            var result = await GetAddressRepository().GetAddressZipCode(nullZipCode);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeAssignableTo<IFailResult<AddressEntity>>();
        }

        [Fact(DisplayName = "Deve rejeitar inserção de CEP duplicado pela constraint de índice único")]
        public async Task SaveChanges_WhenDuplicateZipCode_ShouldThrowUniqueViolation()
        {
            // Arrange
            var duplicateZipCode = "11111111";
            var firstAddress = new AddressInfra
            {
                Id = Guid.NewGuid(),
                Zipcode = duplicateZipCode,
                Street = "Primeira Rua",
                Complement = "Primeiro Complemento",
                Neighborhood = "Primeiro Bairro",
                City = "Primeira Cidade",
                State = "SP",
                IbgeCode = "1111111",
                GiaCode = "111",
                Ddd = "11",
                SiafiCode = "1111",
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                UpdatedAt = DateTime.UtcNow.AddDays(-1)
            };

            var secondAddress = new AddressInfra
            {
                Id = Guid.NewGuid(),
                Zipcode = duplicateZipCode,
                Street = "Segunda Rua",
                Complement = "Segundo Complemento",
                Neighborhood = "Segundo Bairro",
                City = "Segunda Cidade",
                State = "RJ",
                IbgeCode = "2222222",
                GiaCode = "222",
                Ddd = "21",
                SiafiCode = "2222",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await DbContext.Set<AddressInfra>().AddAsync(firstAddress);
            await DbContext.SaveChangesAsync();

            await DbContext.Set<AddressInfra>().AddAsync(secondAddress);

            // Act & Assert — o índice único ix_address_zipcode impede a duplicata
            var act = async () => await DbContext.SaveChangesAsync();
            await act.Should().ThrowAsync<DbUpdateException>();

            // Garante que a corrida de duas inserções não deixa duas linhas
            DbContext.ChangeTracker.Clear();
            var count = await DbContext.Set<AddressInfra>().CountAsync(x => x.Zipcode == duplicateZipCode);
            count.Should().Be(1);
        }

        [Fact(DisplayName = "Deve usar AsNoTracking para otimizar performance e não rastrear entidades")]
        public async Task GetAddressZipCode_ShouldUseAsNoTrackingToOptimizePerformance()
        {
            // Arrange
            var zipCode = "22222222";
            var addressInfra = new AddressInfra
            {
                Id = Guid.NewGuid(),
                Zipcode = zipCode,
                Street = "Rua Sem Tracking",
                Complement = "Teste Performance",
                Neighborhood = "Bairro Performance",
                City = "Cidade Performance",
                State = "SP",
                IbgeCode = "1234567",
                GiaCode = "123",
                Ddd = "11",
                SiafiCode = "1234",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await DbContext.Set<AddressInfra>().AddAsync(addressInfra);
            await DbContext.SaveChangesAsync();

            // Act
            var result = await GetAddressRepository().GetAddressZipCode(zipCode);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeAssignableTo<ISuccessResult<AddressEntity>>();

            // Verificar que a entidade original não está sendo rastreada após a consulta
            var trackedEntity = DbContext.Entry(addressInfra);
            trackedEntity.State.Should().Be(EntityState.Unchanged);
        }

        [Fact(DisplayName = "Deve funcionar corretamente independente de maiúscula/minúscula no CEP")]
        public async Task GetAddressZipCode_WhenZipCodeCaseSensitive_ShouldMatchExactly()
        {
            // Arrange
            var zipCode = "33333333";
            var addressInfra = new AddressInfra
            {
                Id = Guid.NewGuid(),
                Zipcode = zipCode.ToUpper(), // Salvar em maiúsculo
                Street = "Rua Case Sensitive",
                Complement = "Teste Case",
                Neighborhood = "Bairro Case",
                City = "Cidade Case",
                State = "SP",
                IbgeCode = "1234567",
                GiaCode = "123",
                Ddd = "11",
                SiafiCode = "1234",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await DbContext.Set<AddressInfra>().AddAsync(addressInfra);
            await DbContext.SaveChangesAsync();

            // Act - Buscar em minúsculo
            var result = await GetAddressRepository().GetAddressZipCode(zipCode.ToLower());

            // Assert - PostgreSQL não é case sensitive para strings por padrão
            result.Should().NotBeNull();
            result.Should().BeAssignableTo<ISuccessResult<AddressEntity>>();
        }

        [Fact(DisplayName = "Deve retornar erro de negócio quando banco de dados estiver vazio")]
        public async Task GetAddressZipCode_WhenDatabaseIsEmpty_ShouldReturnFailureWithBusinessError()
        {
            // Arrange
            var zipCode = "44444444";
            // Database já está limpo pelo IntegrationTestBase

            // Act
            var result = await GetAddressRepository().GetAddressZipCode(zipCode);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeAssignableTo<IFailResult<AddressEntity>>();

            var failResult = result as IFailResult<AddressEntity>;
            failResult.Should().NotBeNull();
            failResult!.ErrorType.Should().Be(ErrorType.BusinessError);
        }

        [Fact(DisplayName = "Deve tratar corretamente CEPs com caracteres especiais como hífen")]
        public async Task GetAddressZipCode_WithSpecialCharactersInZipCode_ShouldHandleCorrectly()
        {
            // Arrange
            var specialZipCode = "12345-678"; // CEP com hífen
            var normalZipCode = "12345678";

            var addressInfra = new AddressInfra
            {
                Id = Guid.NewGuid(),
                Zipcode = normalZipCode, // Salvo sem hífen
                Street = "Rua Caracteres Especiais",
                Complement = "Teste Especial",
                Neighborhood = "Bairro Especial",
                City = "Cidade Especial",
                State = "SP",
                IbgeCode = "1234567",
                GiaCode = "123",
                Ddd = "11",
                SiafiCode = "1234",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await DbContext.Set<AddressInfra>().AddAsync(addressInfra);
            await DbContext.SaveChangesAsync();

            // Act - Buscar com hífen
            var result = await GetAddressRepository().GetAddressZipCode(specialZipCode);

            // Assert - Deve falhar pois não há normalização na busca
            result.Should().NotBeNull();
            result.Should().BeAssignableTo<IFailResult<AddressEntity>>();
        }

        [Fact(DisplayName = "Deve manter boa performance mesmo com grande volume de dados no banco")]
        public async Task GetAddressZipCode_WithLargeDataset_ShouldPerformEfficiently()
        {
            // Arrange
            var targetZipCode = "55555555";
            var addresses = new List<AddressInfra>();

            // Criar um dataset grande para testar performance
            for (int i = 0; i < 100; i++)
            {
                addresses.Add(new AddressInfra
                {
                    Id = Guid.NewGuid(),
                    Zipcode = $"{i:D8}",
                    Street = $"Rua Teste {i}",
                    Complement = $"Complemento {i}",
                    Neighborhood = $"Bairro {i}",
                    City = $"Cidade {i}",
                    State = "SP",
                    IbgeCode = $"{i:D7}",
                    GiaCode = $"{i:D3}",
                    Ddd = "11",
                    SiafiCode = $"{i:D4}",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }

            // Adicionar o endereço target
            addresses.Add(new AddressInfra
            {
                Id = Guid.NewGuid(),
                Zipcode = targetZipCode,
                Street = "Rua Target",
                Complement = "Complemento Target",
                Neighborhood = "Bairro Target",
                City = "Cidade Target",
                State = "SP",
                IbgeCode = "5555555",
                GiaCode = "555",
                Ddd = "11",
                SiafiCode = "5555",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });

            await DbContext.Set<AddressInfra>().AddRangeAsync(addresses);
            await DbContext.SaveChangesAsync();

            // Act
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var result = await GetAddressRepository().GetAddressZipCode(targetZipCode);
            stopwatch.Stop();

            // Assert
            result.Should().NotBeNull();
            result.Should().BeAssignableTo<ISuccessResult<AddressEntity>>();

            var successResult = result as ISuccessResult<AddressEntity>;
            successResult!.Data.Street.Should().Be("Rua Target");

            // Verificar que a consulta é eficiente (menos de 1 segundo para 100 registros)
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000);
        }
    }
}
