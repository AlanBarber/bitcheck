using System.IO.Hashing;

namespace BitCheck.Application
{
    /// <summary>
    /// Provides shared helpers for enumerating directories, filtering files, and computing hashes.
    /// </summary>
    public static class FileSystemUtilities
    {
        /// <summary>
        /// Returns the non-hidden, non-database files located directly in the supplied directory.
        /// </summary>
        /// <param name="directory">Absolute or relative directory path.</param>
        /// <returns>An array of eligible file paths.</returns>
        public static string[] GetEligibleFiles(string directory) =>
            Directory.GetFiles(directory)
                .Where(f => !ShouldSkipFile(f))
                .ToArray();

        /// <summary>
        /// Returns subdirectories that are not hidden.
        /// </summary>
        /// <param name="directory">Absolute or relative directory path.</param>
        /// <returns>An array of eligible directory paths.</returns>
        public static string[] GetEligibleDirectories(string directory) =>
            Directory.GetDirectories(directory)
                .Where(d => !IsHidden(d))
                .ToArray();

        /// <summary>
        /// Determines whether the application can safely read the specified file.
        /// </summary>
        /// <param name="filePath">Absolute or relative file path.</param>
        /// <param name="errorReason">Outputs the reason when the file cannot be read.</param>
        /// <returns><c>true</c> if the file can be read; otherwise <c>false</c>.</returns>
        public static bool CanReadFile(string filePath, out string? errorReason)
        {
            errorReason = null;

            try
            {
                if (!File.Exists(filePath))
                {
                    errorReason = "File not found";
                    return false;
                }

                var fileInfo = new FileInfo(filePath);
                if (fileInfo.Length == 0)
                {
                    return true;
                }

                using var testStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                return true;
            }
            catch (UnauthorizedAccessException)
            {
                errorReason = "Access denied";
                return false;
            }
            catch (IOException ex)
            {
                errorReason = $"I/O error: {ex.Message}";
                return false;
            }
            catch (Exception ex)
            {
                errorReason = $"Error: {ex.Message}";
                return false;
            }
        }

        /// <summary>
        /// Indicates whether the file should be skipped due to being hidden or matching the database file name.
        /// </summary>
        /// <param name="filePath">Absolute or relative file path.</param>
        /// <returns><c>true</c> if the file should be skipped, otherwise <c>false</c>.</returns>
        public static bool ShouldSkipFile(string filePath)
        {
            var fileName = Path.GetFileName(filePath);
            if (string.Equals(fileName, BitCheckConstants.DatabaseFileName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return IsHidden(filePath);
        }

        /// <summary>
        /// Determines whether the provided path represents a hidden file or directory.
        /// </summary>
        /// <param name="path">Absolute or relative path.</param>
        /// <returns><c>true</c> if the path is hidden, otherwise <c>false</c>.</returns>
        public static bool IsHidden(string path)
        {
            try
            {
                var fileInfo = new FileInfo(path);
                var dirInfo = new DirectoryInfo(path);
                bool exists = fileInfo.Exists || dirInfo.Exists;
                if (!exists)
                {
                    return false;
                }

                var name = fileInfo.Exists ? fileInfo.Name : dirInfo.Name;
                if (name.StartsWith('.'))
                {
                    return true;
                }

                if (OperatingSystem.IsWindows())
                {
                    var attributes = fileInfo.Exists ? fileInfo.Attributes : dirInfo.Attributes;
                    return (attributes & FileAttributes.Hidden) == FileAttributes.Hidden;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Computes the XXHash64 hash of the specified file.
        /// </summary>
        /// <param name="path">Absolute or relative file path.</param>
        /// <returns>A hex string when successful; otherwise <c>null</c>.</returns>
        public static string? ComputeHash(string path)
        {
            try
            {
                using var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                var hasher = new XxHash64();
                hasher.Append(fileStream);
                var rawHash = hasher.GetCurrentHash();
                return Convert.ToHexString(rawHash);
            }
            catch
            {
                return null;
            }
        }
    }
}
