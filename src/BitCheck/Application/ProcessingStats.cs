namespace BitCheck.Application
{
    /// <summary>
    /// Aggregates metrics captured during a BitCheck run for summary reporting.
    /// </summary>
    public class ProcessingStats
    {
        /// <summary>
        /// Total number of files successfully processed (hashed/checked).
        /// </summary>
        public long FilesProcessed { get; set; }

        /// <summary>
        /// Number of new entries inserted into the database.
        /// </summary>
        public long FilesAdded { get; set; }

        /// <summary>
        /// Number of existing entries whose hashes were updated.
        /// </summary>
        public long FilesUpdated { get; set; }

        /// <summary>
        /// Files compared against existing hashes.
        /// </summary>
        public long FilesChecked { get; set; }

        /// <summary>
        /// Files that failed integrity verification.
        /// </summary>
        public long FilesMismatched { get; set; }

        /// <summary>
        /// Files skipped due to errors, inaccessibility, or user options.
        /// </summary>
        public long FilesSkipped { get; set; }

        /// <summary>
        /// Database entries pointing to files that no longer exist on disk.
        /// </summary>
        public long FilesMissing { get; set; }

        /// <summary>
        /// Database entries removed because the backing files were missing.
        /// </summary>
        public long FilesRemoved { get; set; }

        /// <summary>
        /// Total size in bytes of all files processed.
        /// </summary>
        public long TotalBytesProcessed { get; set; }
    }
}
