using System.Diagnostics.CodeAnalysis;

namespace MyCEP.API.Core.Shared.Infrastructure.CrossCutting.Utils
{
    [ExcludeFromCodeCoverage]
    public class DateTimeOffsetUtils
    {
        public static bool IsValidDateTimeOffset(DateTimeOffset DateTimeOffset)
        {
            return DateTimeOffset != default
                 && DateTimeOffset != DateTimeOffset.MinValue
                 && DateTimeOffset != DateTimeOffset.MaxValue;
        }

        public static DateTimeOffset GetDateTimeOffsetInDefaultTimezone()
        {
            return DateTimeOffset.UtcNow;
        }
    }
}
