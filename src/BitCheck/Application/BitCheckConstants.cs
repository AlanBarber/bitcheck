namespace BitCheck.Application
{
    /// <summary>
    /// Shared constant values used across the BitCheck application.
    /// </summary>
    public static class BitCheckConstants
    {
        /// <summary>
        /// Default database file name that is created within scanned directories.
        /// </summary>
        public const string DatabaseFileName = ".bitcheck.db";

        /// <summary>
        /// Name of the per-directory ignore file, auto-discovered like .gitignore.
        /// </summary>
        public const string IgnoreFileName = ".bitcheckignore";
    }
}
