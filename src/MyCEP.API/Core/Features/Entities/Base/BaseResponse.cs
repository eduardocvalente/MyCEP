using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using MyCEP.API.Core.Shared.Domain.Entities.Base;
using MyCEP.API.Core.Shared.Infrastructure.CrossCutting.Result;

namespace MyCEP.API.Core.Features.Entities.Base
{
    [ExcludeFromCodeCoverage]
    public abstract class BaseResponse
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }

        [JsonPropertyName("createdAt")]
        public DateTimeOffset CreatedAt { get; set; }

        [JsonPropertyName("updatedAt")]
        public DateTimeOffset? UpdatedAt { get; set; }

        [JsonPropertyName("deletedAt")]
        public DateTimeOffset? DeletedAt { get; set; }

        public static TEntityDto FromDomain<TEntityDto, TDomain>(TEntityDto entity, ISuccessResult<TDomain> result)
          where TEntityDto : BaseResponse
          where TDomain : DomainBase
        {
            if (entity is null)
                return null!;

            return entity;
        }
    }
}
