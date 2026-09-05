namespace BitCheck.Application
{
    public record AppOptions(
        bool Recursive,
        bool Add,
        bool Update,
        bool Check,
        bool Verbose,
        bool Strict,
        bool Timestamps,
        bool SingleDatabase,
        string? File,
        bool Delete,
        bool Info,
        bool List,
        IReadOnlyList<string>? IgnorePatterns = null)
    {
        public IReadOnlyList<string> IgnorePatterns { get; init; } = IgnorePatterns ?? Array.Empty<string>();
    }
}
