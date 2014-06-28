using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Ishimotto.Core
{
    /// <summary>
    /// Download data using Aria2 tool
    /// </summary>
    public class AriaDownloader
    {
        #region Constants
        /// <summary>
        /// Represent a CMD success code
        /// </summary>
        public const int SUCCESS_EXIST_CODE = 0; 
        #endregion

        #region Properties
        /// <summary>
        /// Directory to save the downloads
        /// </summary>
        public string DownloadsDirectory { get; private set; }

        /// <summary>
        /// Indicates if the <see cref="AriaDownloader"/> should delete temporary files after using them
        /// </summary>
        public bool DeleteTempFiles { get; private set; } 
        #endregion

        #region Constructor
        /// <summary>
        /// Creates new instance of <see cref="AriaDownloader"/>
        /// </summary>
        /// <param name="downloadsDirectory"><see cref="DownloadsDirectory"/></param>
        /// <param name="deleteTempFiles"><see cref="DeleteTempFiles"/></param>
        public AriaDownloader(string downloadsDirectory, bool deleteTempFiles = true)
        {

            DownloadsDirectory = downloadsDirectory;

            DeleteTempFiles = deleteTempFiles;

        } 
        #endregion

        #region Public Methods
        /// <summary>
        /// Download file from given Url
        /// </summary>
        /// <param name="url">The url of the file to download</param>
        public void Download(string url)
        {
            Process.Start(GetProcessStartInformation(url));
        }

        /// <summary>
        /// Download mulitple files 
        /// </summary>
        /// <param name="urls">The urls of the files to download</param>
        public void Download(IEnumerable<string> urls)
        {
            if (urls == null)
            {
                throw new ArgumentException("The urls argument can not be null");
            }

            using (var fileWriter = new FileWriter(DownloadsDirectory, "links", 1, "txt"))
            {
                fileWriter.WriteToFiles(urls);

                Parallel.ForEach(fileWriter.FilesPaths, DownloadFromFile);
            }


        } 
        #endregion

        #region Private methods
        /// <summary>
        /// Download urls from given file
        /// </summary>
        /// <param name="filePath">File contains a list of urls</param>
        private void DownloadFromFile(string filePath)
        {
            var arguments = String.Concat( " -x 7","  -i ", filePath , " -d ",DownloadsDirectory );

            var processStartInfo = GetProcessStartInformation(arguments);

            var downloadProcess = Process.Start(processStartInfo);

            if (DeleteTempFiles)
            {
                downloadProcess.WaitForExit();
                if (downloadProcess.ExitCode == SUCCESS_EXIST_CODE)
                    File.Delete(filePath);
            }
        }

        /// <summary>
        /// Creates information of the aria process to execute
        /// </summary>
        /// <param name="arguments">Aria arguments to run</param>
        /// <returns>information to start the process</returns>
        private ProcessStartInfo GetProcessStartInformation(string arguments)
        {
            var processStartInfo = new ProcessStartInfo();

            processStartInfo.UseShellExecute = true;

            processStartInfo.FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "aria2c.exe");

            processStartInfo.Arguments = arguments;
            return processStartInfo;
        } 
        #endregion
    }
}
