using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using MyCEP.API.Core.Shared.Domain.Entities.Address;
using MyCEP.API.Core.Shared.Domain.Repository;
using MyCEP.API.Core.Shared.Domain.Repository.Base;
using MyCEP.API.Core.Shared.Domain.Service.ViaCep;
using MyCEP.API.Core.Shared.Infrastructure.CrossCutting.NotificationError;
using MyCEP.API.Core.Shared.Infrastructure.CrossCutting.Result;
using MyCEP.API.Core.Shared.Infrastructure.CrossCutting.UnitofWork;
using MyCEP.API.Core.Shared.Infrastructure.Entities.Gateways.ViaCep.Response;
using MyCEP.API.Core.Shared.Infrastructure.Gateways;
using MyCEP.API.Core.Shared.Infrastructure.Schemas;
using Refit;

namespace MyCEP.API.UnitTest.Core.Shared.Service
{
    public class ViaCepServiceTests
    {
        private readonly Mock<ILogger<ViaCepService>> _loggerMock;
        private readonly Mock<IGenericRepository<AddressEntity, AddressInfra>> _repositoryMock;
        private readonly Mock<IViaCepApiGateway> _viaCepApiGatewayMock;
        private readonly Mock<IAddressRepository> _addressRepositoryMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly IViaCepService _viaCepService;

        public ViaCepServiceTests()
        {
            _loggerMock = new Mock<ILogger<ViaCepService>>();
            _repositoryMock = new Mock<IGenericRepository<AddressEntity, AddressInfra>>();
            _viaCepApiGatewayMock = new Mock<IViaCepApiGateway>();
            _addressRepositoryMock = new Mock<IAddressRepository>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();

            _viaCepService = new ViaCepService(
                _loggerMock.Object,
                _repositoryMock.Object,
                _viaCepApiGatewayMock.Object,
                _addressRepositoryMock.Object,
                _unitOfWorkMock.Object
            );
        }

        private void SetupDbMiss(string zipCode)
        {
            var failResultDB = ResultFactory.CreateFail<AddressEntity>(
                NotificationErrors.Create("db", "Não encontrado"),
                ErrorType.BusinessError
            );

            _addressRepositoryMock
                .Setup(a => a.GetAddressZipCode(zipCode, It.IsAny<CancellationToken>()))
                .ReturnsAsync(failResultDB);
        }

        [Fact(DisplayName = "Deve retornar endereço do banco de dados se existente")]
        public async Task GetAddressByZipCodeAsync_WhenAddressExistsInDB_ShouldReturnSuccess()
        {
            // Arrange
            string zipCode = "12345678";
            var addressEntity = new AddressEntity
            {
                Zipcode = "12345678",
                Street = "Rua Teste",
                Complement = "Complemento",
                Neighborhood = "Bairro",
                City = "Cidade",
                State = "ST",
                IbgeCode = "Ibge",
                GiaCode = "Gia",
                Ddd = "12",
                SiafiCode = "34"
            };

            var successResult = ResultFactory.CreateSuccess(addressEntity);
            _addressRepositoryMock
                .Setup(a => a.GetAddressZipCode(zipCode, It.IsAny<CancellationToken>()))
                .ReturnsAsync(successResult);

            // Act
            var result = await _viaCepService.GetAddressByZipCodeAsync(zipCode, CancellationToken.None);

            // Assert
            result.Should().BeAssignableTo<ISuccessResult<AddressEntity>>();
            var success = result as ISuccessResult<AddressEntity>;
            success!.Data.Should().BeEquivalentTo(addressEntity);

            // Cache hit não deve tocar o gateway nem persistir nada
            _viaCepApiGatewayMock.Verify(
                v => v.GetAddressByZipCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
            _unitOfWorkMock.Verify(u => u.Commit(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact(DisplayName = "Deve retornar falha se a chamada à API ViaCep lançar ApiException")]
        public async Task GetAddressByZipCodeAsync_WhenApiThrowsApiException_ShouldReturnFailResult()
        {
            // Arrange
            string zipCode = "12345678";
            SetupDbMiss(zipCode);

            var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com");
            var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                RequestMessage = request,
                ReasonPhrase = "Bad Request",
                Content = new StringContent("Invalid request data.")
            };

            var refitSettings = new RefitSettings();

            var apiException = await ApiException.Create(
                "Custom exception message",
                request,
                HttpMethod.Get,
                response,
                refitSettings
            );

            _viaCepApiGatewayMock
                .Setup(v => v.GetAddressByZipCodeAsync(zipCode, It.IsAny<CancellationToken>()))
                .ThrowsAsync(apiException);

            // Act
            var result = await _viaCepService.GetAddressByZipCodeAsync(zipCode, CancellationToken.None);

            // Assert
            result.Should().BeAssignableTo<IFailResult<AddressEntity>>();
            var fail = result as IFailResult<AddressEntity>;
            fail!.ErrorType.Should().Be(ErrorType.ExceptionError);
            fail.NotificationErrors.Error.Errors
                .Any(e => e.PropertyName == "apiEx")
                .Should().BeTrue();
        }

        [Fact(DisplayName = "Deve retornar erro de validação se a API ViaCep responder 400 (CEP malformado)")]
        public async Task GetAddressByZipCodeAsync_WhenApiReturnsBadRequest_ShouldReturnValidationError()
        {
            // Arrange
            string zipCode = "12345678";
            SetupDbMiss(zipCode);

            var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.BadRequest);
            var apiResponse = new ApiResponse<AddressResponse>(httpResponseMessage, null, null!, null);

            _viaCepApiGatewayMock
                .Setup(v => v.GetAddressByZipCodeAsync(zipCode, It.IsAny<CancellationToken>()))
                .ReturnsAsync(apiResponse);

            // Act
            var result = await _viaCepService.GetAddressByZipCodeAsync(zipCode, CancellationToken.None);

            // Assert
            result.Should().BeAssignableTo<IFailResult<AddressEntity>>();
            var fail = result as IFailResult<AddressEntity>;
            fail!.ErrorType.Should().Be(ErrorType.ValidationError);
            fail.NotificationErrors.Error.Errors
                .Any(e => e.PropertyName == "contentAddressResponse" && e.PropertyErrors.Any(x => x.Equals(DefaultMessage.NoContentFromViaCep)))
                .Should().BeTrue();
        }

        [Fact(DisplayName = "Deve retornar erro de negócio se a API ViaCep responder 200 com erro=true (CEP inexistente)")]
        public async Task GetAddressByZipCodeAsync_WhenApiReturnsErroTrue_ShouldReturnBusinessError()
        {
            // Arrange
            string zipCode = "99999999";
            SetupDbMiss(zipCode);

            // Comportamento real do ViaCEP para CEP bem-formado que não existe:
            // HTTP 200 com corpo {"erro": true} e demais campos ausentes.
            var addressResponse = new AddressResponse { Error = true };
            var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK);
            var apiResponse = new ApiResponse<AddressResponse>(httpResponseMessage, addressResponse, null!, null);

            _viaCepApiGatewayMock
                .Setup(v => v.GetAddressByZipCodeAsync(zipCode, It.IsAny<CancellationToken>()))
                .ReturnsAsync(apiResponse);

            // Act
            var result = await _viaCepService.GetAddressByZipCodeAsync(zipCode, CancellationToken.None);

            // Assert
            result.Should().BeAssignableTo<IFailResult<AddressEntity>>();
            var fail = result as IFailResult<AddressEntity>;
            fail!.ErrorType.Should().Be(ErrorType.BusinessError);

            // Nada deve ser persistido para CEP inexistente
            _repositoryMock.Verify(
                r => r.CreateAsync(It.IsAny<AddressEntity>(), It.IsAny<CancellationToken>()), Times.Never);
            _unitOfWorkMock.Verify(u => u.Commit(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact(DisplayName = "Deve retornar falha se o repositório falhar ao validar/salvar o endereço")]
        public async Task GetAddressByZipCodeAsync_WhenRepositoryCreateFails_ShouldReturnFailResult()
        {
            // Arrange
            string zipCode = "12345678";
            SetupDbMiss(zipCode);

            var addressResponse = new AddressResponse
            {
                PostalCode = "12345-678",
                Street = "Rua X",
                Complement = "Comp",
                Neighborhood = "Bairro",
                City = "Cidade",
                State = "ST",
                IbgeCode = "Ibge",
                GiaCode = "Gia",
                Ddd = "12",
                SiafiCode = "34"
            };

            var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK);
            var apiResponse = new ApiResponse<AddressResponse>(httpResponseMessage, addressResponse, null!, null);
            _viaCepApiGatewayMock
                .Setup(v => v.GetAddressByZipCodeAsync(zipCode, It.IsAny<CancellationToken>()))
                .ReturnsAsync(apiResponse);

            var failRepoResult = ResultFactory.CreateFail<AddressEntity>(
                NotificationErrors.Create("Zipcode", "Erro ao salvar"),
                ErrorType.ExceptionError
            );
            _repositoryMock
                .Setup(r => r.CreateAsync(It.IsAny<AddressEntity>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(failRepoResult);

            // Act
            var result = await _viaCepService.GetAddressByZipCodeAsync(zipCode, CancellationToken.None);

            // Assert — a falha de persistência não pode mais ser engolida
            result.Should().BeAssignableTo<IFailResult<AddressEntity>>();
            var fail = result as IFailResult<AddressEntity>;
            fail!.ErrorType.Should().Be(ErrorType.ExceptionError);
            fail.NotificationErrors.Error.Errors
                .Any(e => e.PropertyName == "Zipcode")
                .Should().BeTrue();

            _unitOfWorkMock.Verify(u => u.Commit(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact(DisplayName = "Deve retornar sucesso e commitar se o endereço for buscado na API e salvo corretamente")]
        public async Task GetAddressByZipCodeAsync_WhenApiAndRepositorySucceed_ShouldReturnSuccessResult()
        {
            // Arrange
            string zipCode = "12345678";
            SetupDbMiss(zipCode);

            var addressResponse = new AddressResponse
            {
                PostalCode = "12345-678",
                Street = "Rua X",
                Complement = "Comp",
                Neighborhood = "Bairro",
                City = "Cidade",
                State = "ST",
                IbgeCode = "Ibge",
                GiaCode = "Gia",
                Ddd = "12",
                SiafiCode = "34"
            };
            var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK);
            var apiResponse = new ApiResponse<AddressResponse>(httpResponseMessage, addressResponse, null!, null);

            _viaCepApiGatewayMock
                .Setup(v => v.GetAddressByZipCodeAsync(zipCode, It.IsAny<CancellationToken>()))
                .ReturnsAsync(apiResponse);

            var expectedAddress = ViaCepService.FromResponse(apiResponse);

            var successRepoResult = ResultFactory.CreateSuccess(expectedAddress);
            _repositoryMock
                .Setup(r => r.CreateAsync(It.IsAny<AddressEntity>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(successRepoResult);

            // Act
            var result = await _viaCepService.GetAddressByZipCodeAsync(zipCode, CancellationToken.None);

            // Assert
            result.Should().BeAssignableTo<ISuccessResult<AddressEntity>>();
            var success = result as ISuccessResult<AddressEntity>;
            success!.Data.Should().BeEquivalentTo(expectedAddress);

            _unitOfWorkMock.Verify(u => u.Commit(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact(DisplayName = "Deve retornar falha com erro interno se ocorrer exceção inesperada")]
        public async Task GetAddressByZipCodeAsync_WhenUnexpectedExceptionOccurs_ShouldReturnInternalError()
        {
            // Arrange
            string zipCode = "12345678";

            _addressRepositoryMock
                .Setup(a => a.GetAddressZipCode(zipCode, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Erro inesperado"));

            // Act
            var result = await _viaCepService.GetAddressByZipCodeAsync(zipCode, CancellationToken.None);

            // Assert
            result.Should().BeAssignableTo<IFailResult<AddressEntity>>();
            var fail = result as IFailResult<AddressEntity>;
            fail!.ErrorType.Should().Be(ErrorType.InternalError);

            fail.NotificationErrors.Error.Errors
                .Any(e => e.PropertyName == "ex" && e.PropertyErrors.Any(x => x == DefaultMessage.ErrorFetchingAddress))
                .Should().BeTrue();
        }
    }
}
