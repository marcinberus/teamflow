namespace TeamFlow.Importing.FileExtensions;

[AttributeUsage(
    AttributeTargets.Field,
    AllowMultiple = false,
    Inherited = false)]
public sealed class FileExtensionValueAttribute : Attribute
{
    public string ExtensionValue { get; }

    public FileExtensionValueAttribute(string extensionName)
    {
        ExtensionValue = extensionName;
    }
}
