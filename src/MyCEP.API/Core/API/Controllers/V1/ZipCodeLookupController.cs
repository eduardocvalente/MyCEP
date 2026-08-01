using Microsoft.AspNetCore.Mvc;
using MyCEP.API.Core.Features.Entities;
using MyCEP.API.Core.Features.UseCase.IZipCodeLookup;
using MyCEP.API.Core.Shared.Domain.Entities.Address;
using MyCEP.API.Core.Shared.Infrastructure.CrossCutting.Result;

namespace MyCEP.API.Core.API.Controllers.V1
{
    [ApiController]
    [Route("api/[controller]")]
    public class ZipCodeLookupController : ControllerBase
    {
        private readonly IZipCodeLookupUseCase _zipCodeLookupUseCase;

        public ZipCodeLookupController(IZipCodeLookupUseCase zipCodeLookupUseCase)
        {
            _zipCodeLookupUseCase = zipCodeLookupUseCase;
        }

        [HttpGet("{zipCode}")]
        [ProducesResponseType(typeof(AddressResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAddressByZipCode(string zipCode, CancellationToken cancellationToken)
        {
            var result = await _zipCodeLookupUseCase.ExecAsync(zipCode, cancellationToken);

            return result switch
            {
                ISuccessResult<AddressResponse> success => Success(success.Data),
                IFailResult<AddressEntity> fail => FailToActionResult(fail),
                _ => Problem(statusCode: StatusCodes.Status500InternalServerError)
            };
        }

        private IActionResult Success(AddressResponse address)
        {
            // Endereço de CEP é praticamente imutável e público: deixar a CDN e
            // o browser cachearem por 24h corta a maioria dos round-trips até a
            // aplicação — a forma mais barata de escalar este domínio.
            Response.Headers.CacheControl = "public, max-age=86400";
            return Ok(address);
        }

        private IActionResult FailToActionResult(IFailResult<AddressEntity> fail)
        {
            var statusCode = fail.ErrorType switch
            {
                ErrorType.ValidationError => StatusCodes.Status400BadRequest,
                ErrorType.BusinessError => StatusCodes.Status404NotFound,
                _ => StatusCodes.Status500InternalServerError
            };

            var problem = ProblemDetailsFactory.CreateProblemDetails(
                HttpContext,
                statusCode: statusCode,
                title: statusCode == StatusCodes.Status404NotFound
                    ? "Endereço não encontrado."
                    : statusCode == StatusCodes.Status400BadRequest
                        ? "Requisição inválida."
                        : "Erro interno no servidor.");

            // Em 5xx o detalhe fica só no log; nos demais casos as mensagens de
            // negócio/validação ajudam o consumidor a corrigir a chamada.
            if (statusCode < StatusCodes.Status500InternalServerError)
                problem.Extensions["errors"] = fail.NotificationErrors.Error.Errors;

            return new ObjectResult(problem)
            {
                StatusCode = statusCode,
                ContentTypes = { "application/problem+json" }
            };
        }
    }
}
