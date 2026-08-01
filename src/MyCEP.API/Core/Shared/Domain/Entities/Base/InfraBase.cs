using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyCEP.API.Core.Shared.Domain.Entities.Base
{
    public abstract record InfraBase
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; }

        [Column("deleted_at")]
        public DateTimeOffset? DeletedAt { get; set; }

        [Column("created_at")]
        public DateTimeOffset CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTimeOffset? UpdatedAt { get; set; }
    }
}
