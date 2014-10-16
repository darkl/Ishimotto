using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using log4net;

namespace Ishimotto.Core.Aria
{
    public static class HelperMethods
    {
        /// <summary>
        /// All chars that can not be used in directory path
        /// </summary>
        private static readonly char[] INVALID_PATH_CHARS = Path.GetInvalidPathChars();

        /// <summary>
        /// All chars that can not be used in directory path
        /// </summary>
        private static readonly char[] INVALID_FILE_NAME_CHARS = Path.GetInvalidFileNameChars();

        private static readonly ILog mLog = LogManager.GetLogger(typeof(HelperMethods));

        /// <summary>
        /// Checks if a path isa a valid path
        /// </summary>
        /// <param name="path">Path to check</param>
        /// <returns>A boolean indicating if the path is valid</returns>
        public static bool IsPathValid(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                ArgumentException exception = new ArgumentException("Path can not be null", "path");

                mLog.Fatal("Path is null", exception);

                return false;
            }

            bool isPathValid = !path.Any(pathLetter => INVALID_PATH_CHARS.Contains(pathLetter));

            if (!isPathValid)
            {
                return false;
            }

            // Checking that all names (of directories and files are correct)
            IEnumerable<string> names = 
                path.Split(Path.DirectorySeparatorChar).Skip(1);

            // If the directory conatins invalid chars, throw exception
            bool result =
                names.All(name =>
                          !name.Any(pathLetter => INVALID_FILE_NAME_CHARS.Contains(pathLetter)));

            return result;
        }
    }
}
