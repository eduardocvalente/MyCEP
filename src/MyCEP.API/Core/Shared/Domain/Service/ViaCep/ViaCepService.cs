using Microsoft.EntityFrameworkCore;
using MyCEP.API.Core.Shared.Domain.Entities.Address;
using MyCEP.API.Core.Shared.Domain.Repository;
using MyCEP.API.Core.Shared.Domain.Repository.Base;
using MyCEP.API.Core.Shared.Infrastructure.CrossCutting.NotificationError;
using MyCEP.API.Core.Shared.Infrastructure.CrossCutting.Result;
using MyCEP.API.Core.Shared.Infrastructure.CrossCutting.UnitofWork;
using MyCEP.API.Core.Shared.Infrastructure.CrossCutting.Utils;
using MyCEP.API.Core.Shared.Infrastructure.Entities.Gateways.ViaCep.Response;
using MyCEP.API.Core.Shared.Infrastructure.Gateways;
using MyCEP.API.Core.Shared.Infrastructure.Schemas;
using Npgsql;
using Refit;

namespace MyCEP.API.Core.Shared.Domain.Service.ViaCep
{
    public class ViaCepService : IViaCepService
    {
        private readonly ILogger<ViaCepService> _logger;
        private readonly IGenericRepository<AddressEntity, AddressInfra> _repository;
        private readonly IViaCepApiGateway _viaCepApiGateway;
        private readonly IAddressRepository _addressRepository;
        private readonly IUnitOfWork _unitOfWork;

        public ViaCepService(
            ILogger<ViaCepService> logger,
            IGenericRepository<AddressEntity, AddressInfra> repository,
            IViaCepApiGateway viaCepApiGateway,
            IAddressRepository addressRepository,
            IUnitOfWork unitOfWork)
        {
            _logger = logger;
            _repository = repository;
            _viaCepApiGateway = viaCepApiGateway;
            _addressRepository = addressRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<IAnalysisResult<AddressEntity>> GetAddressByZipCodeAsync(string zipCode, CancellationToken cancellationToken)
        {
            try
            {
                var dbResponse = await _addressRepository.GetAddressZipCode(zipCode, cancellationToken);
                if (dbResponse is ISuccessResult<AddressEntity> successResponse)
                {
                    _logger.LogInformation("CEP {ZipCode} encontrado no banco de dados.", zipCode);
                    return ResultFactory.CreateSuccess(successResponse.Data);
                }

                ApiResponse<AddressResponse> contentAddressResponse;
                try
                {
                    contentAddressResponse = await _viaCepApiGateway.GetAddressByZipCodeAsync(zipCode, cancellationToken);
                }
                catch (ApiException apiEx)
                {
                    _logger.LogWarning(apiEx, "Erro ao consumir a API ViaCEP para o CEP {ZipCode}", zipCode);
                    return ResultFactory.CreateFail<AddressEntity>(
                        NotificationErrors.Create(nameof(apiEx), DefaultMessage.FormatViaCepApiErrorMessage(apiEx.StatusCode.ToString())),
                        ErrorType.ExceptionError);
                }

                if (!contentAddressResponse.IsSuccessStatusCode)
                {
                    // 400 = CEP malformado (validação); 404 = não encontrado
                    // (negócio); o resto é falha do serviço externo.
                    var errorType = contentAddressResponse.StatusCode switch
                    {
                        System.Net.HttpStatusCode.BadRequest => ErrorType.ValidationError,
                        System.Net.HttpStatusCode.NotFound => ErrorType.BusinessError,
                        _ => ErrorType.ExceptionError
                    };

                    _logger.LogWarning("ViaCEP respondeu {StatusCode} para o CEP {ZipCode}",
                        (int)contentAddressResponse.StatusCode, zipCode);
                    return ResultFactory.CreateFail<AddressEntity>(
                        NotificationErrors.Create(nameof(contentAddressResponse), DefaultMessage.NoContentFromViaCep),
                        errorType);
                }

                // O ViaCEP responde 200 com {"erro": true} para CEP bem-formado
                // que não existe — sem este teste a API devolveria um endereço
                // inteiro de nulls com status 200.
                if (contentAddressResponse.Content is null
                    || contentAddressResponse.Content.Error
                    || string.IsNullOrWhiteSpace(contentAddressResponse.Content.PostalCode))
                {
                    _logger.LogInformation("CEP {ZipCode} não existe na base do ViaCEP.", zipCode);
                    return ResultFactory.CreateFail<AddressEntity>(
                        NotificationErrors.Create(nameof(zipCode), DefaultMessage.AddressNotFoundForZipCodeMessage),
                        ErrorType.BusinessError);
                }

                var contentAddress = FromResponse(contentAddressResponse);

                var saveResponse = await _repository.CreateAsync(contentAddress, cancellationToken);
                if (saveResponse is IFailResult<AddressEntity> failResult)
                {
                    _logger.LogError("Erro ao validar/salvar o endereço do CEP {ZipCode}: {Errors}",
                        zipCode, failResult.NotificationErrors.Error);
                    return ResultFactory.CreateFailFromOtherFailType<AddressEntity, AddressEntity>(failResult);
                }

                try
                {
                    await _unitOfWork.Commit(cancellationToken);
                }
                catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
                {
                    // Duas requisições simultâneas do mesmo CEP: a outra inseriu
                    // primeiro. O registro já está no banco — devolve o existente.
                    _logger.LogInformation("Inserção concorrente detectada para o CEP {ZipCode}; usando o registro existente.", zipCode);

                    var existing = await _addressRepository.GetAddressZipCode(zipCode, cancellationToken);
                    if (existing is ISuccessResult<AddressEntity> existingSuccess)
                        return ResultFactory.CreateSuccess(existingSuccess.Data);

                    throw;
                }

                return ResultFactory.CreateSuccess(contentAddress);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar o CEP {ZipCode} na API ViaCEP ou no banco de dados.", zipCode);
                return ResultFactory.CreateFail<AddressEntity>(
                    NotificationErrors.Create(nameof(ex), DefaultMessage.ErrorFetchingAddress),
                    ErrorType.InternalError);
            }
        }

        public static AddressEntity FromResponse(ApiResponse<AddressResponse> response)
        {
            return new AddressEntity
            {
                Zipcode = response.Content!.PostalCode.OnlyNumbers(),
                Street = response.Content.Street ?? string.Empty,
                Complement = response.Content.Complement ?? string.Empty,
                Neighborhood = response.Content.Neighborhood ?? string.Empty,
                City = response.Content.City ?? string.Empty,
                State = response.Content.State ?? string.Empty,
                IbgeCode = response.Content.IbgeCode ?? string.Empty,
                GiaCode = response.Content.GiaCode ?? string.Empty,
                Ddd = response.Content.Ddd ?? string.Empty,
                SiafiCode = response.Content.SiafiCode ?? string.Empty
            };
        }
    }
}
