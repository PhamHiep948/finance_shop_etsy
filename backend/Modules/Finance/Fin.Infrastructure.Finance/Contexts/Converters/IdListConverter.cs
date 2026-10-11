using System.Globalization;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Fin.Infrastructure.Finance.Contexts.Converters
{
    public static class IdListConverter
    {
        public static ValueConverter<List<long>, string> Converter { get; } = new(
            ids => Join(ids),
            text => Split(text));

        public static ValueComparer<List<long>> Comparer { get; } = new(
            (left, right) => Join(left!) == Join(right!),
            ids => Join(ids).GetHashCode(StringComparison.Ordinal),
            ids => ids.ToList());

        private static string Join(IEnumerable<long> ids) =>
            string.Join(",", ids.Select(id => id.ToString(CultureInfo.InvariantCulture)));

        private static List<long> Split(string? text) =>
            (text ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(part => long.TryParse(part, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id) ? id : 0)
                .Where(id => id > 0)
                .ToList();
    }
}
