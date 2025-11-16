using System.Text.Json.Serialization;

namespace BitCheck.Database
{
    /// <summary>
    /// JSON source generation context for FileEntry serialization.
    /// Required for trimmed/AOT builds.
    /// </summary>
    [JsonSerializable(typeof(List<FileEntry>))]
    [JsonSerializable(typeof(FileEntry))]
    internal partial class FileEntryJsonContext : JsonSerializerContext
    {
    }
}
