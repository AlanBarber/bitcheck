using System;

namespace BitCheck.Database
{
    /// <summary>
    /// Represents a file entry in the database with hash and timestamp information.
    /// </summary>
    public class FileEntry
    {
        /// <summary>
        /// The name of the file (not the full path).
        /// </summary>
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// The XXHash64 hex string of the file contents.
        /// </summary>
        public string Hash { get; set; } = string.Empty;

        /// <summary>
        /// When the hash was computed or last updated.
        /// Updated only when the file is added or when the hash changes.
        /// </summary>
        public DateTime HashDate { get; set; }

        /// <summary>
        /// When the file was last checked for integrity.
        /// Updated every time a check is performed, regardless of result.
        /// </summary>
        public DateTime LastCheckDate { get; set; }

        /// <summary>
        /// The file system modification date (LastWriteTimeUtc) when the hash was computed.
        /// Used to distinguish intentional file changes from corruption.
        /// </summary>
        public DateTime LastModified { get; set; }

        /// <summary>
        /// Default constructor for JSON serialization.
        /// </summary>
        public FileEntry() { }
    }
}
