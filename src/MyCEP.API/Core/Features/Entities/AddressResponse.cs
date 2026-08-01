using System.Text.Json.Serialization;
using MyCEP.API.Core.Features.Entities.Base;
using MyCEP.API.Core.Shared.Domain.Entities.Address;
using MyCEP.API.Core.Shared.Infrastructure.CrossCutting.Result;

namespace MyCEP.API.Core.Features.Entities
{
    public class AddressResponse : BaseResponse
    {
        [JsonPropertyName("zipcode")]
        public string Zipcode { get; set; }

        [JsonPropertyName("street")]
        public string Street { get; set; }

        [JsonPropertyName("complement")]
        public string Complement { get; set; }

        [JsonPropertyName("neighborhood")]
        public string Neighborhood { get; set; }

        [JsonPropertyName("city")]
        public string City { get; set; }

        [JsonPropertyName("state")]
        public string State { get; set; }

        [JsonPropertyName("ibgeCode")]
        public string IbgeCode { get; set; }

        [JsonPropertyName("giaCode")]
        public string GiaCode { get; set; }

        [JsonPropertyName("areaCode")]
        public string Ddd { get; set; }

        [JsonPropertyName("siafiCode")]
        public string SiafiCode { get; set; }

        public static AddressResponse FromResponse(ISuccessResult<AddressEntity> domainObject)
        {
            var entity = new AddressResponse()
            {
                Id = domainObject.Data.Id,
                Zipcode = domainObject.Data.Zipcode,
                Street = domainObject.Data.Street,
                Complement = domainObject.Data.Complement,
                Neighborhood = domainObject.Data.Neighborhood,
                City = domainObject.Data.City,
                State = domainObject.Data.State,
                IbgeCode = domainObject.Data.IbgeCode,
                GiaCode = domainObject.Data.GiaCode,
                Ddd = domainObject.Data.Ddd,
                SiafiCode = domainObject.Data.SiafiCode
            };

            return entity;
        }
    }
}