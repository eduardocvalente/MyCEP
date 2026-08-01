using MyCEP.API.Core.Features.Entities;
using MyCEP.API.Core.Shared.Domain.Entities.Address;
using MyCEP.API.Core.Shared.Domain.Service.ViaCep;
using MyCEP.API.Core.Shared.Infrastructure.CrossCutting.NotificationError;
using MyCEP.API.Core.Shared.Infrastructure.CrossCutting.Result;
using MyCEP.API.Core.Shared.Infrastructure.CrossCutting.Utils;

namespace MyCEP.API.Core.Features.UseCase.IZipCodeLookup
{
    public sealed class ZipCodeLookupUseCase : IZipCodeLookupUseCase
    {
        private const int ZipCodeLength = 8;

        private readonly IViaCepService _viaCepService;
        private readonly ILogger<ZipCodeLookupUseCase> _logger;

        public ZipCodeLookupUseCase(IViaCepService viaCepService, ILogger<ZipCodeLookupUseCase> logger)
        {
            _viaCepService = viaCepService;
            _logger = logger;
        }

        public async Task<IAnalysisResult> ExecAsync(string zipCode, CancellationToken cancellationToken)
        {
            string onlyNumbersZipCode = zipCode.OnlyNumbers();
            if (string.IsNullOrWhiteSpace(onlyNumbersZipCode))
            {
                _logger.LogWarning("CEP inválido ou vazio recebido: {ZipCode}", zipCode);
                return ResultFactory.CreateFail<AddressEntity>(
                    NotificationErrors.Create(nameof(onlyNumbersZipCode), DefaultMessage.ZipCodeCannotBeNullOrEmpty),
                    ErrorType.ValidationError);
            }

            if (onlyNumbersZipCode.Length != ZipCodeLength)
            {
                _logger.LogWarning("CEP com tamanho inválido recebido: {ZipCode}", onlyNumbersZipCode);
                return ResultFactory.CreateFail<AddressEntity>(
                    NotificationErrors.Create(nameof(onlyNumbersZipCode), DefaultMessage.ZipCodeMustHaveEightDigits),
                    ErrorType.ValidationError);
            }

            try
            {
                var result = await _viaCepService.GetAddressByZipCodeAsync(onlyNumbersZipCode, cancellationToken);
                if (result is IFailResult<AddressEntity> operationFail)
                {
                    _logger.LogWarning("Falha ao obter endereço para o CEP {ZipCode}: {ErrorType}",
                        onlyNumbersZipCode, operationFail.ErrorType);
                    return operationFail;
                }

                var response = AddressResponse.FromResponse((ISuccessResult<AddressEntity>)result);
                return ResultFactory.CreateSuccess(response);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar o endereço para o CEP {ZipCode}", onlyNumbersZipCode);
                return ResultFactory.CreateFail<AddressEntity>(
                    NotificationErrors.Create(nameof(ex), DefaultMessage.ErrorFetchingAddress),
                    ErrorType.InternalError);
            }
        }
    }
}
