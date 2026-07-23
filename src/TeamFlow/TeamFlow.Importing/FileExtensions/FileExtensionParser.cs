using System.Reflection;

namespace TeamFlow.Importing.FileExtensions;

public static class FileExtensionParser
{
    private static readonly IReadOnlyDictionary<string, FileExtension> Values =
       Enum.GetValues<FileExtension>()
           .Select(enumValue => new
           {
               EnumValue = enumValue,
               Attribute = typeof(FileExtension)
                  .GetField(enumValue.ToString())?
                  .GetCustomAttribute<FileExtensionValueAttribute>()
           })
           .Where(x => x.Attribute is not null)
           .ToDictionary(
               x => x.Attribute!.ExtensionValue,
               x => x.EnumValue,
               StringComparer.OrdinalIgnoreCase);

    public static bool TryParse(string? value, out FileExtension fileExtension)
    {
        if (string.IsNullOrEmpty(value))
        {
            fileExtension = default;
            return false;
        }

        return Values.TryGetValue(value.Trim(), out fileExtension);
    }
}
