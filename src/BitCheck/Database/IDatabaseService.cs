namespace BitCheck.Database;

/// <summary>
/// 
/// </summary>
public interface IDatabaseService : IDisposable
{
    FileEntry GetFileEntry(string filename);
    FileEntry InsertFileEntry(FileEntry fileEntry);
    FileEntry UpdateFileEntry(FileEntry fileEntry);
    FileEntry DeleteFileEntry(string filename);
    void Flush();
    void InvalidateCache();
}