using System.Text.Json.Serialization;
using MyCEP.API.Core.Shared.Domain.Entities.Base;
using MyCEP.API.Core.Shared.Infrastructure.CrossCutting.NotificationError;

namespace MyCEP.API.Core.Shared.Domain.Entities.Address
{
    public sealed class AddressEntity : DomainBase
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

        public override NotificationErrors IsValid()
        {
            var notificationErrors = NotificationErrors.Empty;

            // Apenas CEP, cidade e UF são garantidos pelo ViaCEP: CEPs gerais
            // de municípios pequenos vêm sem logradouro e sem bairro, e ainda
            // assim são endereços válidos a persistir.
            if (string.IsNullOrWhiteSpace(Zipcode))
                notificationErrors.AddError(nameof(Zipcode), DefaultMessage.StringIsNullOrEmptyMessage);

            if (string.IsNullOrWhiteSpace(State))
                notificationErrors.AddError(nameof(State), DefaultMessage.StringIsNullOrEmptyMessage);

            if (string.IsNullOrWhiteSpace(City))
                notificationErrors.AddError(nameof(City), DefaultMessage.StringIsNullOrEmptyMessage);

            return notificationErrors;
        }

        public static AddressEntity Restore(
            in Guid id,
            string zipcode,
            string street,
            string complement,
            string neighborhood,
            string city,
            string state,
            string ibgeCode,
            string giaCode,
            string ddd,
            string siafiCode,
            in DateTimeOffset? deletedAt,
            in DateTimeOffset createdAt,
            in DateTimeOffset? updatedAt)
        {
            var result = new AddressEntity()
            {
                Zipcode = zipcode,
                Street = street,
                Complement = complement,
                Neighborhood = neighborhood,
                City = city,
                State = state,
                IbgeCode = ibgeCode,
                GiaCode = giaCode,
                Ddd = ddd,
                SiafiCode = siafiCode
            };

            return Restore(result, id, deletedAt, createdAt, updatedAt);
        }

        public static AddressEntity ConceptualCreate(
            in Guid id,
            string zipcode,
            string street,
            string complement,
            string neighborhood,
            string city,
            string state,
            string ibgeCode,
            string giaCode,
            string ddd,
            string siafiCode)
        {
            var result = new AddressEntity()
            {
                Id = id,
                Zipcode = zipcode,
                Street = street,
                Complement = complement,
                Neighborhood = neighborhood,
                City = city,
                State = state,
                IbgeCode = ibgeCode,
                GiaCode = giaCode,
                Ddd = ddd,
                SiafiCode = siafiCode
            };

            return result;
        }

    }
}
