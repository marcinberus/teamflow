namespace TeamFlow.Application.Common.Validation;

public static class EnumValidation
{
    public static bool IsDefinedValue<TEnum>(string? value)
        where TEnum : struct, Enum
    {
        return Enum.TryParse<TEnum>(value, out var parsedValue)
            && Enum.IsDefined(parsedValue)
            && string.Equals(
                Enum.GetName(parsedValue),
                value,
                StringComparison.Ordinal);
    }
}
