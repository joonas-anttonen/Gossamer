using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Gossamer.Utilities;

/// <summary>
/// Provides utilities for working with JSON.
/// </summary>
public static class JsonUtilities
{
    /// <summary>
    /// Deserializes a JSON string into an object.
    /// </summary>
    /// <typeparam name="T"/>
    /// <param name="data"/>
    /// <exception cref="InvalidDataException"/>
    public static T Deserialize<T>(byte[] data)
    {
        return JsonSerializer.Deserialize<T>(Encoding.UTF8.GetString(data)) ?? throw new InvalidDataException();
    }
}

[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public class JsonVector2Converter : JsonConverter<Vector2>
{
    public override Vector2 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        reader.Read();
        reader.Read();
        var x = reader.GetSingle();
        reader.Read();
        reader.Read();
        var y = reader.GetSingle();
        reader.Read();
        return new Vector2(x, y);
    }

    public override void Write(Utf8JsonWriter writer, Vector2 value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteNumber("X", value.X);
        writer.WriteNumber("Y", value.Y);
        writer.WriteEndObject();
    }
}

[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public class JsonVector3Converter : JsonConverter<Vector3>
{
    public override Vector3 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        reader.Read();
        reader.Read();
        var x = reader.GetSingle();
        reader.Read();
        reader.Read();
        var y = reader.GetSingle();
        reader.Read();
        reader.Read();
        var z = reader.GetSingle();
        reader.Read();
        return new Vector3(x, y, z);
    }

    public override void Write(Utf8JsonWriter writer, Vector3 value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteNumber("X", value.X);
        writer.WriteNumber("Y", value.Y);
        writer.WriteNumber("Z", value.Z);
        writer.WriteEndObject();
    }
}

[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public class JsonVector4Converter : JsonConverter<Vector4>
{
    public override Vector4 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        reader.Read();
        reader.Read();
        var x = reader.GetSingle();
        reader.Read();
        reader.Read();
        var y = reader.GetSingle();
        reader.Read();
        reader.Read();
        var z = reader.GetSingle();
        reader.Read();
        reader.Read();
        var w = reader.GetSingle();
        reader.Read();
        return new Vector4(x, y, z, w);
    }

    public override void Write(Utf8JsonWriter writer, Vector4 value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteNumber("X", value.X);
        writer.WriteNumber("Y", value.Y);
        writer.WriteNumber("Z", value.Z);
        writer.WriteNumber("W", value.W);
        writer.WriteEndObject();
    }
}

[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public class JsonColorConverter : JsonConverter<Color>
{
    public override Color Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return Color.ParseHexString(reader.GetString() ?? throw new InvalidOperationException());
    }

    public override void Write(Utf8JsonWriter writer, Color value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToHexString());
    }
}