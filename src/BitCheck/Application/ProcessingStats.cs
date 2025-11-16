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
        public int FilesProcessed { get; set; }

        /// <summary>
        /// Number of new entries inserted into the database.
        /// </summary>
        public int FilesAdded { get; set; }

        /// <summary>
        /// Number of existing entries whose hashes were updated.
        /// </summary>
        public int FilesUpdated { get; set; }

        /// <summary>
        /// Files compared against existing hashes.
        /// </summary>
        public int FilesChecked { get; set; }

        /// <summary>
        /// Files that failed integrity verification.
        /// </summary>
        public int FilesMismatched { get; set; }

        /// <summary>
        /// Files skipped due to errors, inaccessibility, or user options.
        /// </summary>
        public int FilesSkipped { get; set; }

        /// <summary>
        /// Database entries pointing to files that no longer exist on disk.
        /// </summary>
        public int FilesMissing { get; set; }

        /// <summary>
        /// Database entries removed because the backing files were missing.
        /// </summary>
        public int FilesRemoved { get; set; }
    }
}
