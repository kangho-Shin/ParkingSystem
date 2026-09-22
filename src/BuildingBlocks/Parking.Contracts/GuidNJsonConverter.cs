using Newtonsoft.Json;

namespace Parking.Contracts;

public sealed class GuidNJsonConverter : JsonConverter<Guid>
{
    public override void WriteJson(
        JsonWriter writer,
        Guid value,
        JsonSerializer serializer) =>
        writer.WriteValue(value.ToString("N"));

    public override Guid ReadJson(
        JsonReader reader,
        Type objectType,
        Guid existingValue,
        bool hasExistingValue,
        JsonSerializer serializer)
    {
        string? value = reader.Value?.ToString();
        if (Guid.TryParse(value, out Guid result))
            return result;
        throw new JsonSerializationException("EventId 형식이 올바르지 않습니다.");
    }
}
