using MyCEP.API.Core.Shared.Infrastructure.CrossCutting.Result;

namespace MyCEP.API.Core.Features.UseCase.IZipCodeLookup
{
    public interface IZipCodeLookupUseCase
    {
        Task<IAnalysisResult> ExecAsync(string zipCode, CancellationToken cancellationToken);
    }
}
