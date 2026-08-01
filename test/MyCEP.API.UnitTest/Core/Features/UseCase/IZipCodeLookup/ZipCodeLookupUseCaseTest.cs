using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using MyCEP.API.Core.Features.Entities;
using MyCEP.API.Core.Features.UseCase.IZipCodeLookup;
using MyCEP.API.Core.Shared.Domain.Entities.Address;
using MyCEP.API.Core.Shared.Domain.Service.ViaCep;
using MyCEP.API.Core.Shared.Infrastructure.CrossCutting.NotificationError;
using MyCEP.API.Core.Shared.Infrastructure.CrossCutting.Result;

namespace MyCEP.API.UnitTest.Core.Features.UseCase.IZipCodeLookup
{
    public class ZipCodeLookupUseCaseTests
    {
        private readonly Mock<IViaCepService> _viaCepServiceMock;
        private readonly Mock<ILogger<ZipCodeLookupUseCase>> _loggerMock;
        private readonly IZipCodeLookupUseCase _useCase;

        public ZipCodeLookupUseCaseTests()
        {
            _viaCepServiceMock = new Mock<IViaCepService>();
            _loggerMock = new Mock<ILogger<ZipCodeLookupUseCase>>();

            _useCase = new ZipCodeLookupUseCase(
                _viaCepServiceMock.Object,
                _loggerMock.Object
            );
        }

        [Fact(DisplayName = "Deve retornar erro de validação se o CEP for inválido ou vazio")]
        public async Task ExecAsync_WithInvalidZipCode_ShouldReturnValidationError()
        {
            // Arrange
            string invalidZipCode = "abc";

            // Act
            var result = await _useCase.ExecAsync(invalidZipCode, CancellationToken.None);
            var fail = result as IFailResult<AddressEntity>;

            // Assert
            result.Should().NotBeNull();
            fail!.NotificationErrors.Error.Errors.Should().HaveCount(1);
            fail.NotificationErrors.Error.Errors.Should().Contain(
                x => x.PropertyName == "onlyNumbersZipCode" &&
                     x.PropertyErrors.Contains(DefaultMessage.ZipCodeCannotBeNullOrEmpty)
            );
            fail.ErrorType.Should().Be(ErrorType.ValidationError);
            _viaCepServiceMock.Verify(
                s => s.GetAddressByZipCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Theory(DisplayName = "Deve retornar erro de validação se o CEP não tiver exatamente 8 dígitos")]
        [InlineData("123")]
        [InlineData("1234567")]
        [InlineData("123456789")]
        public async Task ExecAsync_WithWrongLengthZipCode_ShouldReturnValidationError(string zipCode)
        {
            // Act
            var result = await _useCase.ExecAsync(zipCode, CancellationToken.None);
            var fail = result as IFailResult<AddressEntity>;

            // Assert
            fail.Should().NotBeNull();
            fail!.ErrorType.Should().Be(ErrorType.ValidationError);
            fail.NotificationErrors.Error.Errors.Should().Contain(
                x => x.PropertyErrors.Contains(DefaultMessage.ZipCodeMustHaveEightDigits)
            );
            _viaCepServiceMock.Verify(
                s => s.GetAddressByZipCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact(DisplayName = "Deve repassar a falha se o IViaCepService retornar OperationFail")]
        public async Task ExecAsync_WhenViaCepServiceFails_ShouldReturnFailResult()
        {
            // Arrange
            string validZipCode = "12345678";
            var notificationError = NotificationErrors.Create("cep", "Falha ao buscar endereço");
            var failResult = ResultFactory.CreateFail<AddressEntity>(notificationError, ErrorType.InternalError);

            _viaCepServiceMock
                .Setup(s => s.GetAddressByZipCodeAsync(validZipCode, It.IsAny<CancellationToken>()))
                .ReturnsAsync(failResult);

            // Act
            var result = await _useCase.ExecAsync(validZipCode, CancellationToken.None);
            var fail = result as IFailResult<AddressEntity>;

            // Assert
            result.Should().NotBeNull();
            fail!.ErrorType.Should().Be(ErrorType.InternalError);
        }

        [Fact(DisplayName = "Deve retornar sucesso mapeado se o IViaCepService retornar OperationSuccess")]
        public async Task ExecAsync_WhenOperationIsSuccessful_ShouldReturnSuccessResult()
        {
            // Arrange
            string validZipCode = "12345678";
            var addressEntity = new AddressEntity()
            {
                Zipcode = "01001-000",
                Street = "Praça da Sé",
                Complement = "lado ímpar",
                Neighborhood = "Sé",
                City = "São Paulo",
                State = "SP",
                IbgeCode = "3550308",
                GiaCode = "1004",
                Ddd = "11",
                SiafiCode = "7107"
            };

            var successResult = ResultFactory.CreateSuccess(addressEntity);

            _viaCepServiceMock
                .Setup(s => s.GetAddressByZipCodeAsync(validZipCode, It.IsAny<CancellationToken>()))
                .ReturnsAsync(successResult);

            // Act
            var result = await _useCase.ExecAsync(validZipCode, CancellationToken.None);
            var success = result as ISuccessResult<AddressResponse>;

            // Assert
            success.Should().NotBeNull();
            success!.Data.Should().NotBeNull();

            success.Data.Zipcode.Should().Be(addressEntity.Zipcode);
            success.Data.Street.Should().Be(addressEntity.Street);
            success.Data.Complement.Should().Be(addressEntity.Complement);
            success.Data.Neighborhood.Should().Be(addressEntity.Neighborhood);
            success.Data.City.Should().Be(addressEntity.City);
            success.Data.State.Should().Be(addressEntity.State);
            success.Data.IbgeCode.Should().Be(addressEntity.IbgeCode);
            success.Data.GiaCode.Should().Be(addressEntity.GiaCode);
            success.Data.Ddd.Should().Be(addressEntity.Ddd);
            success.Data.SiafiCode.Should().Be(addressEntity.SiafiCode);
        }

        [Fact(DisplayName = "Deve retornar erro interno se ocorrer exceção inesperada")]
        public async Task ExecAsync_WhenExceptionIsThrown_ShouldReturnInternalError()
        {
            // Arrange
            string validZipCode = "12345678";

            _viaCepServiceMock
                .Setup(s => s.GetAddressByZipCodeAsync(validZipCode, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Erro inesperado"));

            // Act
            var result = await _useCase.ExecAsync(validZipCode, CancellationToken.None);
            var fail = result as IFailResult<AddressEntity>;

            // Assert
            result.Should().NotBeNull();
            fail!.ErrorType.Should().Be(ErrorType.InternalError);
            fail.NotificationErrors.Error.Errors.Should().Contain(
                x => x.PropertyName.StartsWith("ex") &&
                     x.PropertyErrors.Contains(DefaultMessage.ErrorFetchingAddress)
            );
        }

        [Fact(DisplayName = "Deve propagar o cancelamento quando o token for cancelado")]
        public async Task ExecAsync_WhenCancelled_ShouldPropagateCancellation()
        {
            // Arrange
            string validZipCode = "12345678";
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            _viaCepServiceMock
                .Setup(s => s.GetAddressByZipCodeAsync(validZipCode, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OperationCanceledException());

            // Act
            var act = async () => await _useCase.ExecAsync(validZipCode, cts.Token);

            // Assert
            await act.Should().ThrowAsync<OperationCanceledException>();
        }
    }
}
