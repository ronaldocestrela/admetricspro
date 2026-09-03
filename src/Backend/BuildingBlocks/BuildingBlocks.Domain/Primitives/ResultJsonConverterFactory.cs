using System.Text.Json;
using System.Text.Json.Serialization;

namespace BuildingBlocks.Domain.Primitives;

/// <summary>
/// JSON converter factory for serializing and deserializing <see cref="Result"/> and <see cref="Result{TValue}"/> instances,
/// safely handling failed results without triggering exception on <see cref="Result{TValue}.Value"/> access.
/// </summary>
public sealed class ResultJsonConverterFactory : JsonConverterFactory
{
    /// <inheritdoc />
    public override bool CanConvert(Type typeToConvert)
    {
        if (typeToConvert == typeof(Result))
        {
            return true;
        }

        return typeToConvert.IsGenericType &&
               typeToConvert.GetGenericTypeDefinition() == typeof(Result<>);
    }

    /// <inheritdoc />
    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        if (typeToConvert == typeof(Result))
        {
            return new ResultNonGenericJsonConverter();
        }

        var valueType = typeToConvert.GetGenericArguments()[0];
        var converterType = typeof(ResultGenericJsonConverter<>).MakeGenericType(valueType);
        return (JsonConverter)Activator.CreateInstance(converterType)!;
    }

    private sealed class ResultNonGenericJsonConverter : JsonConverter<Result>
    {
        public override Result Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var doc = JsonDocument.ParseValue(ref reader);
            var root = doc.RootElement;
            var isSuccess = false;
            if (root.TryGetProperty("isSuccess", out var isSuccessProp) ||
                root.TryGetProperty("IsSuccess", out isSuccessProp))
            {
                isSuccess = isSuccessProp.GetBoolean();
            }

            if (isSuccess)
            {
                return Result.Success();
            }

            var error = Error.None;
            if (root.TryGetProperty("error", out var errorProp) ||
                root.TryGetProperty("Error", out errorProp))
            {
                error = JsonSerializer.Deserialize<Error>(errorProp.GetRawText(), options) ?? Error.None;
            }

            return Result.Failure(error);
        }

        public override void Write(Utf8JsonWriter writer, Result value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteBoolean("isSuccess", value.IsSuccess);
            writer.WriteBoolean("isFailure", value.IsFailure);
            writer.WritePropertyName("error");
            JsonSerializer.Serialize(writer, value.Error, options);
            writer.WriteEndObject();
        }
    }

    private sealed class ResultGenericJsonConverter<TValue> : JsonConverter<Result<TValue>>
    {
        public override Result<TValue> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var doc = JsonDocument.ParseValue(ref reader);
            var root = doc.RootElement;
            var isSuccess = false;
            if (root.TryGetProperty("isSuccess", out var isSuccessProp) ||
                root.TryGetProperty("IsSuccess", out isSuccessProp))
            {
                isSuccess = isSuccessProp.GetBoolean();
            }

            if (isSuccess)
            {
                TValue? val = default;
                if (root.TryGetProperty("value", out var valProp) ||
                    root.TryGetProperty("Value", out valProp))
                {
                    val = JsonSerializer.Deserialize<TValue>(valProp.GetRawText(), options);
                }

                return val is not null ? Result<TValue>.Success(val) : Result<TValue>.Failure(Error.NullValue);
            }

            var error = Error.None;
            if (root.TryGetProperty("error", out var errorProp) ||
                root.TryGetProperty("Error", out errorProp))
            {
                error = JsonSerializer.Deserialize<Error>(errorProp.GetRawText(), options) ?? Error.None;
            }

            return Result<TValue>.Failure(error);
        }

        public override void Write(Utf8JsonWriter writer, Result<TValue> value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteBoolean("isSuccess", value.IsSuccess);
            writer.WriteBoolean("isFailure", value.IsFailure);
            writer.WritePropertyName("error");
            JsonSerializer.Serialize(writer, value.Error, options);
            writer.WritePropertyName("value");
            if (value.IsSuccess)
            {
                JsonSerializer.Serialize(writer, value.Value, options);
            }
            else
            {
                writer.WriteNullValue();
            }
            writer.WriteEndObject();
        }
    }
}
