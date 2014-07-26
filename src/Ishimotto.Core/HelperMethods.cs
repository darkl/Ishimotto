using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net;

namespace Ishimotto.Core
{
    public static class HelperMethods
    {


        /// <summary>
        /// All chars that can not be used in directory path
        /// </summary>
        private static char[] INVALID_PATH_CHARS = Path.GetInvalidPathChars();


        /// <summary>
        /// All chars that can not be used in directory path
        /// </summary>
        private static char[] INVALID_FILE_NAME_CHARS = Path.GetInvalidFileNameChars();


        private static ILog logger = LogManager.GetLogger(typeof(HelperMethods));

        /// <summary>
        /// Checks if a path isa a valid path
        /// </summary>
        /// <param name="path">Path to check</param>
        /// <returns>A boolean indicating if the path is valid</returns>
        public static bool IsPathValid(string path)
        {

            if (String.IsNullOrEmpty(path))
            {
                var exception = new ArgumentException("Path can not be null");

                if (logger.IsFatalEnabled)
                {
                    logger.Fatal("Path is null", exception);
                }

            }

        
            bool isValid = !path.Any(pathLetter => INVALID_PATH_CHARS.Contains(pathLetter));

            if (!isValid)
            {
                return false;
            }


            //Checking that all names (of directories and files arre correct)

            var names = path.Split(Path.DirectorySeparatorChar).Skip(1);

            var invalidPathChars = Path.GetInvalidPathChars();

            //If the directory conatins invalid chars, throw exception

            foreach (var name in names)
            {
                if (name.Any(pathLetter => INVALID_FILE_NAME_CHARS.Contains(pathLetter)))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
