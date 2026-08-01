using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MyCEP.API.Core.Features.Entities;
using MyCEP.API.Core.Features.UseCase.IZipCodeLookup;
using MyCEP.API.Core.Shared.Domain.Entities.Address;
using MyCEP.API.Core.Shared.Infrastructure.CrossCutting.Result;
using MyCEP.API.IntegrationTest.Base;

namespace MyCEP.API.IntegrationTest.Features.UseCase.IZipCodeLookup;

public class ZipCodeLookupUseCaseIntegrationTests : IntegrationTestBase
{
    private IZipCodeLookupUseCase _useCase = null!;

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        _useCase = GetService<IZipCodeLookupUseCase>();
    }


    [Fact(DisplayName = "Deve retornar endereço válido para CEP existente e processar corretamente")]
    public async Task ExecAsync_ValidZipCode_ReturnsAddressSuccessfully()
    {
        // Arrange
        const string zipCode = "01001-000";
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await _useCase.ExecAsync(zipCode, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeAssignableTo<ISuccessResult<AddressResponse>>();

        var successResult = result as ISuccessResult<AddressResponse>;
        successResult.Should().NotBeNull();
        successResult!.Data.Should().NotBeNull();
        successResult.Data.Street.Should().Be("Praça da Sé");
        successResult.Data.City.Should().Be("São Paulo");
        successResult.Data.State.Should().Be("SP");
        successResult.Data.Zipcode.Should().Be("01001000");

        var addressCount = await DbContext.Addresses.CountAsync(a => a.Zipcode == "01001000");
        addressCount.Should().Be(1, "O endereço deveria estar persistido na base de dados");
    }

    [Fact(DisplayName = "Deve retornar ValidationError para CEP vazio ou nulo")]
    public async Task ExecAsync_EmptyZipCode_ReturnsValidationError()
    {
        // Arrange
        const string invalidZipCode = "   ";
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await _useCase.ExecAsync(invalidZipCode, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeAssignableTo<IFailResult<AddressEntity>>();

        var failResult = result as IFailResult<AddressEntity>;
        failResult.Should().NotBeNull();
        failResult!.ErrorType.Should().Be(ErrorType.ValidationError);
        failResult.NotificationErrors.Should().NotBeNull();
        failResult.NotificationErrors.HaveError().Should().BeTrue();
    }

    [Fact(DisplayName = "Deve retornar BusinessError para CEP inexistente")]
    public async Task ExecAsync_NonExistentZipCode_ReturnsBusinessError()
    {
        // Arrange
        const string nonExistentZipCode = "99999999";
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await _useCase.ExecAsync(nonExistentZipCode, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeAssignableTo<IFailResult<AddressEntity>>();

        var failResult = result as IFailResult<AddressEntity>;
        failResult.Should().NotBeNull();
        failResult!.ErrorType.Should().Be(ErrorType.BusinessError);
    }

    [Theory(DisplayName = "Deve processar diferentes formatos de CEP corretamente")]
    [InlineData("01001-000")]
    [InlineData("01001000")]
    [InlineData(" 01001-000 ")]
    [InlineData("01001 000")]
    public async Task ExecAsync_DifferentZipCodeFormats_ProcessesCorrectly(string zipCodeFormat)
    {
        // Arrange
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await _useCase.ExecAsync(zipCodeFormat, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeAssignableTo<ISuccessResult<AddressResponse>>();

        var successResult = result as ISuccessResult<AddressResponse>;
        successResult.Should().NotBeNull();
        successResult!.Data.Zipcode.Should().Be("01001000");
    }

    [Fact(DisplayName = "Deve garantir que transação seja realizada corretamente")]
    public async Task ExecAsync_ValidZipCode_EnsuresTransactionIntegrity()
    {
        // Arrange
        const string zipCode = "01001-000";
        var cancellationToken = CancellationToken.None;

        // Limpar o banco antes do teste para ter estado conhecido
        await DatabaseManager.CleanDatabaseAsync();
        await DatabaseManager.SeedTestDataAsync();

        var initialCount = await DbContext.Addresses.CountAsync();

        // Act
        var result = await _useCase.ExecAsync(zipCode, cancellationToken);

        // Assert
        result.Should().BeAssignableTo<ISuccessResult<AddressResponse>>();

        var finalCount = await DbContext.Addresses.CountAsync();

        // O CEP 01001-000 já existe no seed, então não deve adicionar novo registro
        // Quando o endereço já existe no banco, não há inserção, apenas retorno do existente
        finalCount.Should().Be(initialCount, "A transação deve manter a integridade dos dados - não deve adicionar registro duplicado");

        // Verificar que o endereço retornado é correto
        var successResult = result as ISuccessResult<AddressResponse>;
        successResult!.Data.Zipcode.Should().Be("01001000");
        successResult.Data.Street.Should().Be("Praça da Sé");
    }

    [Fact(DisplayName = "Deve garantir que transação seja realizada corretamente quando CEP não existe no banco")]
    public async Task ExecAsync_NewZipCode_EnsuresTransactionIntegrityWithNewRecord()
    {
        // Arrange
        const string zipCode = "01001-000"; // CEP que será tratado pelo fake gateway
        var cancellationToken = CancellationToken.None;

        // Limpar o banco completamente - sem seed
        await DatabaseManager.CleanDatabaseAsync();

        var initialCount = await DbContext.Addresses.CountAsync();

        // Act
        var result = await _useCase.ExecAsync(zipCode, cancellationToken);

        // Assert
        result.Should().BeAssignableTo<ISuccessResult<AddressResponse>>();

        var finalCount = await DbContext.Addresses.CountAsync();

        // Como o CEP não existe no banco, deve buscar na API fake e salvar
        finalCount.Should().Be(initialCount + 1, "A transação deve adicionar um novo registro quando CEP não existe");

        // Verificar que o endereço retornado é correto
        var successResult = result as ISuccessResult<AddressResponse>;
        successResult!.Data.Zipcode.Should().Be("01001000");
        successResult.Data.Street.Should().Be("Praça da Sé");
    }
}

