namespace TeamFlow.Importing.FileExtensions;

public enum FileExtension
{
    [FileExtensionValue("")]
    Unknown,

    [FileExtensionValue(".json")]
    Json,

    [FileExtensionValue(".csv")]
    Csv
}
