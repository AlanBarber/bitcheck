namespace BitCheck.Database;

/// <summary>
/// Defines CRUD and persistence operations for BitCheck's lightweight file hash database.
/// </summary>
public interface IDatabaseService : IDisposable
{
    /// <summary>
    /// Retrieves a single <see cref="FileEntry"/> by its file name key.
    /// </summary>
    /// <param name="filename">Logical file name or relative path used as the primary key.</param>
    /// <returns>The matching <see cref="FileEntry"/>, or <c>null</c> if not found.</returns>
    FileEntry GetFileEntry(string filename);

    /// <summary>
    /// Returns all tracked entries currently stored in the database.
    /// </summary>
    /// <returns>An enumeration of <see cref="FileEntry"/> objects.</returns>
    IEnumerable<FileEntry> GetAllEntries();

    /// <summary>
    /// Adds a new entry to the database.
    /// </summary>
    /// <param name="fileEntry">The entry to insert.</param>
    /// <returns>The persisted <see cref="FileEntry"/>.</returns>
    FileEntry InsertFileEntry(FileEntry fileEntry);

    /// <summary>
    /// Updates an existing entry with new hash/timestamp data.
    /// </summary>
    /// <param name="fileEntry">The entry containing updated values.</param>
    /// <returns>The updated <see cref="FileEntry"/>.</returns>
    FileEntry UpdateFileEntry(FileEntry fileEntry);

    /// <summary>
    /// Removes an entry from the database.
    /// </summary>
    /// <param name="filename">Primary key of the entry to delete.</param>
    /// <returns>The removed <see cref="FileEntry"/>, or <c>null</c> if it did not exist.</returns>
    FileEntry DeleteFileEntry(string filename);

    /// <summary>
    /// Flushes pending changes to the underlying storage medium.
    /// </summary>
    void Flush();

    /// <summary>
    /// Clears any in-memory cache so the next access reloads from disk.
    /// </summary>
    void InvalidateCache();
}