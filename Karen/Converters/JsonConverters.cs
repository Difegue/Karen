using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Karen.Converters
{
    public class ObjectToInferredTypesConverter : JsonConverter<object>
    {
        public override object Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => reader.TokenType switch
        {
            JsonTokenType.True => true,
            JsonTokenType.False => false,
            JsonTokenType.Number when reader.TryGetInt32(out int l) => l,
            JsonTokenType.Number when reader.TryGetInt64(out long l) => l,
            JsonTokenType.Number => reader.GetDouble(),
            JsonTokenType.String when reader.TryGetDateTime(out DateTime datetime) => datetime,
            JsonTokenType.String => reader.GetString()!,
            _ => JsonDocument.ParseValue(ref reader).RootElement.Clone()
        };

        [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("Trimming", "IL2026")]
        [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("AOT", "IL3050")]
        public override void Write(Utf8JsonWriter writer, object objectToWrite, JsonSerializerOptions options) => JsonSerializer.Serialize(writer, objectToWrite, objectToWrite.GetType(), options);
    }

}
