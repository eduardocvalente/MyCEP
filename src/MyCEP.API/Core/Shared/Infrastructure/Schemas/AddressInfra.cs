using MyCEP.API.Core.Shared.Domain.Entities.Base;

namespace MyCEP.API.Core.Shared.Infrastructure.Schemas
{
    public sealed record AddressInfra : InfraBase
    {
        public string Zipcode { get; set; }
        public string Street { get; set; }
        public string Complement { get; set; }
        public string Neighborhood { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string IbgeCode { get; set; }
        public string GiaCode { get; set; }
        public string Ddd { get; set; }
        public string SiafiCode { get; set; }
    }
}
