using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace MyCEP.API.Core.Shared.Infrastructure.CrossCutting.Utils
{
    [ExcludeFromCodeCoverage]
    public static partial class UtilsExtends
    {
        private const string PatternToOnlyNumbers = @"[^0-9]";

        public static string OnlyNumbers(this string? text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            return RegexOnlyNumbers().Replace(text, string.Empty);
        }

        [GeneratedRegex(PatternToOnlyNumbers)]
        private static partial Regex RegexOnlyNumbers();
    }
}
