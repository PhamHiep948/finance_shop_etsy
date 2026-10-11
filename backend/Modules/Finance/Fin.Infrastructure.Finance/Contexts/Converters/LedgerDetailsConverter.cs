using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Fin.Domain.Finance.Entities;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Fin.Infrastructure.Finance.Contexts.Converters
{
    public static class LedgerDetailsConverter
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
        {
            Converters = { new LenientDecimalConverter() },
        };

        public static ValueConverter<LedgerDetails, string> Converter { get; } = new(
            details => Serialize(details),
            json => Deserialize(json));

        public static ValueComparer<LedgerDetails> Comparer { get; } = new(
            (left, right) => Serialize(left!) == Serialize(right!),
            details => Serialize(details).GetHashCode(StringComparison.Ordinal),
            details => Deserialize(Serialize(details)));

        public static string Serialize(LedgerDetails details) => JsonSerializer.Serialize(details, JsonOptions);

        public static LedgerDetails Deserialize(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return new LedgerDetails();
            }

            try
            {
                return JsonSerializer.Deserialize<LedgerDetails>(json, JsonOptions) ?? new LedgerDetails();
            }
            catch (JsonException)
            {
                // Rows written by the legacy backend accepted any JSON; a malformed blob must not break the whole list.
                return new LedgerDetails();
            }
        }

        private sealed class LenientDecimalConverter : JsonConverter<decimal?>
        {
            public override decimal? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                switch (reader.TokenType)
                {
                    case JsonTokenType.Number:
                        return reader.GetDecimal();
                    case JsonTokenType.String:
                        var text = reader.GetString();
                        return decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var value) ? value : null;
                    default:
                        reader.Skip();
                        return null;
                }
            }

            public override void Write(Utf8JsonWriter writer, decimal? value, JsonSerializerOptions options)
            {
                if (value is null)
                {
                    writer.WriteNullValue();
                }
                else
                {
                    writer.WriteNumberValue(value.Value);
                }
            }
        }
    }
}
