using System.Diagnostics.CodeAnalysis;
using MyCEP.API.Core.Shared.Infrastructure.CrossCutting.NotificationError;
using MyCEP.API.Core.Shared.Infrastructure.CrossCutting.Utils;

namespace MyCEP.API.Core.Shared.Domain.Entities.Base
{
    [ExcludeFromCodeCoverage]
    public abstract class DomainBase
    {
        public Guid Id { get; protected set; }
        public DateTimeOffset? DeletedAt { get; private set; }
        public DateTimeOffset CreatedAt { get; private set; }
        public DateTimeOffset? UpdatedAt { get; private set; }

        protected static TBaseEntity Restore<TBaseEntity>(TBaseEntity entity, Guid id, DateTimeOffset? deletedAt = null, DateTimeOffset createdAt = default, DateTimeOffset? updatedAt = null)
            where TBaseEntity : DomainBase
        {
            if (entity is null)
                return null!;

            entity.Id = id;
            entity.DeletedAt = deletedAt;
            entity.CreatedAt = createdAt;
            entity.UpdatedAt = updatedAt;

            return entity;
        }

        internal virtual void SetId(Guid id)
        {
            Id = id;
        }

        public void SetDateTimeOffsetUpdate()
        {
            UpdatedAt = DateTimeOffsetUtils.GetDateTimeOffsetInDefaultTimezone();
        }

        public bool CreateDateToInit()
        {
            if (Id != Guid.Empty)
                return false;

            CreatedAt = DateTimeOffsetUtils.GetDateTimeOffsetInDefaultTimezone();
            return true;
        }

        public bool Delete()
        {
            if (!CanDelete())
                return false;

            DeletedAt = DateTimeOffsetUtils.GetDateTimeOffsetInDefaultTimezone();
            return true;
        }

        public bool IsDeleted()
        {
            if (DeletedAt == null)
                return false;

            return DateTimeOffsetUtils.IsValidDateTimeOffset(DeletedAt.Value);
        }

        private bool CanDelete()
        {
            if (DeletedAt == null)
                return true;

            return !DateTimeOffsetUtils.IsValidDateTimeOffset(DeletedAt.Value);
        }

        public virtual NotificationErrors IsValid()
        {
            return NotificationErrors.Empty;
        }
    }
}
