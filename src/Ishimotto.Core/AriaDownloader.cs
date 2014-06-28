using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ishimotto.Core
{


    /// <summary>
    /// The options to use as Aria log levels
    /// </summary>
    public enum AriaSeverity
    {
        None, // Not an aria valid value, represent a value when the AriaLogPath is String.Empty
        Debug,
        Info,
        Notice,
        Warn,
        Error,

    }

    /// <summary>
    /// Download data using Aria2 tool
    /// </summary>
    public class AriaDownloader
    {


        #region Properties

        /// <summary>
        /// Directory to save the downloads
        /// </summary>
        public string DownloadsDirectory { get; private set; }

        /// <summary>
        /// Indicates if the <see cref="AriaDownloader"/> should delete temporary files after using them
        /// </summary>
        public bool DeleteTempFiles { get; private set; }


        /// <summary>
        /// Log file to log the aria process, iff the log is String.Empty no log would be saved
        /// </summary>
        public string AriaLogPath { get; private set; }

        /// <summary>
        /// The severity of the aria's log to write
        /// </summary>
        public AriaSeverity Severity { get; private set; }

        /// <summary>
        /// The max connections to open aganist the NuGet server
        /// </summary>
        public uint MaxConnections { get; private set; }

        #endregion

        #region Constructor

        /// <summary>
        /// Creates new instance of <see cref="AriaDownloader"/>
        /// </summary>
        /// <param name="downloadsDirectory"><see cref="DownloadsDirectory"/></param>
        /// <param name="deleteTempFiles"><see cref="DeleteTempFiles"/></param>
        /// <param name="maxConnections"><see cref="MaxConnections"/></param>
        /// <param name="logPath"><see cref="AriaLogPath"/></param>
        /// <param name="severity"><see cref=" Severity"/></param>
        public AriaDownloader(string downloadsDirectory, bool deleteTempFiles,uint maxConnections, string logPath, AriaSeverity severity)
        {
            ValidateArguments(downloadsDirectory,maxConnections, logPath, severity);
            
            DownloadsDirectory = downloadsDirectory;

            DeleteTempFiles = deleteTempFiles;

            AriaLogPath = logPath;

            Severity = severity;

            MaxConnections = maxConnections;
        }


     

        /// <summary>
        /// Creates new instance of <see cref="AriaDownloader"/>
        /// </summary>
        /// <param name="downloadsDirectory"><see cref="DownloadsDirectory"/></param>
        public AriaDownloader(string downloadsDirectory)
            : this(downloadsDirectory, false,10, String.Empty, AriaSeverity.None)
        {

        }

        /// <summary>
        /// Creates new instance of <see cref="AriaDownloader"/>
        /// </summary>
        /// <param name="downloadsDirectory"><see cref="DownloadsDirectory"/></param>
        /// <param name="deleteTempFiles"><see cref="DeleteTempFiles"/></param>

        public AriaDownloader(string downloadsDirectory, bool deleteTempFiles)
            : this(downloadsDirectory, deleteTempFiles,10, String.Empty, AriaSeverity.None)
        {

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

            CreateLinkFiles(urls);
        }



        #endregion

        #region Private Methods

        /// <summary>
        /// Create files contains all the urls for Atia to download
        /// </summary>
        /// <param name="urls">The urls to insert to the files</param>
        private void CreateLinkFiles(IEnumerable<string> urls)
        {
            using (var fileWriter = new FileWriter(DownloadsDirectory, "links", 1, "txt"))
            {
                fileWriter.WriteToFiles(urls);

                Parallel.ForEach(fileWriter.FilesPaths, DownloadFromFile);
            }
        }

        /// <summary>
        /// Download urls from given file
        /// </summary>
        /// <param name="filePath">File contains a list of urls</param>
        private void DownloadFromFile(string filePath)
        {

            //TODO: make a file with single url and Test if the package is downloaded (Give the urls as Enumrable<string> to the Download method

            StringBuilder arguments = new StringBuilder( String.Concat(" -x ",MaxConnections, "  -i ", filePath, " -d ", DownloadsDirectory));

            AddLogArguments(arguments);
            
            var processStartInfo = GetProcessStartInformation(arguments.ToString());

            var downloadProcess = Process.Start(processStartInfo);

            if (DeleteTempFiles)
            {
                //Delete the link files
                downloadProcess.WaitForExit();
                {
                    //TODO: test if the file is deleted

                    File.Delete(filePath);
                }
            }
        }

        private void AddLogArguments(StringBuilder arguments)
        {
            if (!String.IsNullOrEmpty(AriaLogPath))
            {
                arguments.Append(" --log=");

                arguments.Append(AriaLogPath);

                arguments.Append("--log-level=");

                arguments.Append(Severity.ToString().ToLower());
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
            processStartInfo.CreateNoWindow = true;
            processStartInfo.FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "aria2c.exe");

            processStartInfo.Arguments = arguments;
            return processStartInfo;
        }

        #region Argument Validation

        /// <summary>
        /// Verify that the given arguemnts are valid
        /// </summary>
        /// <param name="downloadsDirectory"><see cref="DownloadsDirectory"/></param>
        /// <param name="maxConnections"><see cref="MaxConnections"/></param>
        /// <param name="logPath"><see cref="AriaLogPath"/></param>
        /// <param name="severity"><see cref="Severity"/></param>
        private void ValidateArguments(string downloadsDirectory,uint maxConnections, string logPath, AriaSeverity severity)
        {
            VarifyDirectory(downloadsDirectory);

            VarifyFilePath(logPath);

            ValidateSeverity(logPath, severity);

            ValidateMaxConnections(maxConnections);
        }

        private void ValidateMaxConnections(uint maxConnections)
        {
            if (maxConnections == 0)
            {

                //TODO: Test
                throw new ArgumentException("The maximum connections value must be grather than zero");

                //TODO: Log
            }
        }

        /// <summary>
        /// Validate that the severity is set to NONE when a log file path is not provided
        /// </summary>
        /// <param name="logPath"><see cref="AriaLogPath"/></param>
        /// <param name="severity"><see cref="Severity"/></param>
        private void ValidateSeverity(string logPath, AriaSeverity severity)
        {
            //TODO: Test when the log file is empty & severity is not None an exception should be thrown
            if (String.IsNullOrEmpty(logPath) && severity != AriaSeverity.None)
            {
                throw new ArgumentException("The severity must be set to NONE unless a log path is provided");

                //TODO: log
            }
        }

        /// <summary>
        /// Varify that a file path is valid, and create the file if it does not exist
        /// </summary>
        /// <param name="filePath">path to varify</param>
        private void VarifyFilePath(string filePath)
        {

            //TODO: Test that an invalid path throws exception



            if (!IsPathValid(filePath))
            {
                throw new Exception(String.Format("The path {0} is not valid", filePath));

                //TODO: Log
            }

            //TODO: Test that a log file is created
            if (!File.Exists(filePath))
            {
                File.Create(filePath);
            }
        }

        /// <summary>
        /// Varify that the directory path is valid, and creates it if it does not exist
        /// </summary>
        /// <param name="downloadsDirectory">The directory path to check if is valid</param>
        private void VarifyDirectory(string downloadsDirectory)
        {
            //TODO: Test if directory is created

            //TODO: Test if exception is thrown


            if (IsPathValid(downloadsDirectory))
            {
                throw new Exception(String.Format("The path {0} is not valid", downloadsDirectory));
                //TODO: log the exception
            }

            //If the directory does not exist, create it
            if (!Directory.Exists(downloadsDirectory))
            {
                File.Create(downloadsDirectory);
            }
        }

        /// <summary>
        /// Checks if a path isa a valid path
        /// </summary>
        /// <param name="path">Path to check</param>
        /// <returns>A boolean indicating if the path is valid</returns>
        private bool IsPathValid(string path)
        {

            if (String.IsNullOrEmpty(path))
            {
                //TODO: test with null path
                throw new ArgumentException("Path can not be null");
                //TODO: Log
            }

            var invalidChars = Path.GetInvalidFileNameChars();

            var invalidPathChars = Path.GetInvalidPathChars();

            //If the directory conatins invalid chars, throw exception
            return !path.Any(pathLetter => invalidChars.Contains(pathLetter) || invalidPathChars.Contains(pathLetter));
        } 
        #endregion

        #endregion
    }
}
