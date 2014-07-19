using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net;

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

        #region Constants
        private const string ARIA_EXE = "aria2c.exe"; 
        #endregion

        #region Members
        private ILog mLogger; 
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

        #region Constructors

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
            HandleArguments(downloadsDirectory,maxConnections, logPath, severity);
            
            DeleteTempFiles = deleteTempFiles;

            mLogger = LogManager.GetLogger("Ishimotto.Core.AriaDownloader");
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
            if (mLogger.IsInfoEnabled)
            {
                mLogger.InfoFormat("Start downloading file from url {0}", url);
            }

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
                if (mLogger.IsFatalEnabled)
                {
                    mLogger.Fatal("Got null as url's to download");
                }

                throw new ArgumentException("The urls argument can not be null");
            }

            if (mLogger.IsInfoEnabled)
            {
                mLogger.InfoFormat("Start downloading {0} file(s)", urls.Count());
            }

            if (mLogger.IsDebugEnabled)
            {
                mLogger.Debug("splitting the urls into files");
            }

            var paths = CreateLinkFiles(urls);

            if (mLogger.IsDebugEnabled)
            {
                mLogger.Debug("splitting completed");
            }



            Parallel.ForEach(paths, DownloadFromFile);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Create files contains all the urls for Atia to download
        /// </summary>
        /// <param name="urls">The urls to insert to the files</param>
        /// <returns>collection of all created link files</returns>
        private IEnumerable<string> CreateLinkFiles(IEnumerable<string> urls)
        {

            IEnumerable<string> filePaths;


            //TODO: Test what happen if the file links1.txt already exist

            using (var fileWriter = new FileWriter(DownloadsDirectory, "links", 1, "txt"))
            {
                fileWriter.WriteToFiles(urls);

                filePaths = fileWriter.FilesPath;
            }

            return filePaths;
        }

        /// <summary>
        /// Download urls from given file
        /// </summary>
        /// <param name="filePath">File contains a list of urls</param>
        private void DownloadFromFile(string filePath)
        {
            //TODO: check that downloading from 5 threads is working properly

            if (mLogger.IsInfoEnabled)
            {
                mLogger.InfoFormat("Start downloading urls from {0}", filePath);
            }

            //TODO: make a file with single url and Test if the package is downloaded (Give the urls as Enumrable<string> to the Download method

            StringBuilder arguments = new StringBuilder( String.Concat(" -x ",MaxConnections, "  -i ", filePath, " -d ", DownloadsDirectory));

            AddLogArguments(arguments);
            
            var processStartInfo = GetProcessStartInformation(arguments.ToString());

            var downloadProcess = Process.Start(processStartInfo);

            downloadProcess.WaitForExit();


            if (mLogger.IsInfoEnabled)
            {
                mLogger.InfoFormat("Finish downloaded files from {0}, for download information check aria log",filePath);
            }

            if (DeleteTempFiles)
            {
                //Delete the link files
            
                
                    //TODO: test if the file is deleted

                    if (mLogger.IsDebugEnabled)
                    {
                        mLogger.DebugFormat("Deleting file {0}",filePath);
                    }

                    File.Delete(filePath);
                
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
            processStartInfo.FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ARIA_EXE);

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
        private void HandleArguments(string downloadsDirectory,uint maxConnections, string logPath, AriaSeverity severity)
        {
            HandleDownloadDirectory(downloadsDirectory);

            HandleAriaLogPath(logPath);

            HandleSeverity(logPath, severity);

            ValidateMaxConnections(maxConnections);
        }

        private void ValidateMaxConnections(uint maxConnections)
        {
            if (maxConnections == 0)
            {

                if (mLogger.IsErrorEnabled)
                {
                    mLogger.Error("Could not set MaxConnection, The maximum connections value must be grather than zero, MaxConnection will be set to 1");
                }
                
                MaxConnections = 1;

                //TODO: Test



            }
        }

        /// <summary>
        /// Validate that the severity is set to NONE when a log file path is not provided
        /// </summary>
        /// <param name="logPath"><see cref="AriaLogPath"/></param>
        /// <param name="severity"><see cref="Severity"/></param>
        private void HandleSeverity(string logPath, AriaSeverity severity)
        {
            //TODO: Test when the log file is empty & severity is changed to none
            if (String.IsNullOrEmpty(logPath) && severity != AriaSeverity.None)
            {

                if (mLogger.IsErrorEnabled)
                {
                    mLogger.Error("The severity must be set to NONE unless a log path is provided, no logs will be written from Arira.exe");
                }


                Severity = AriaSeverity.None;

             
            }
        }

        /// <summary>
        /// Varify that a file path is valid, and create the file if it does not exist
        /// </summary>
        /// <param name="filePath">path to varify</param>
        private void HandleAriaLogPath(string filePath)
        {

            //TODO: Test that an invalid path changes the log path to string.Empty

            
            if (!IsPathValid(filePath))
            {

                if(mLogger.IsErrorEnabled)
                {
                    mLogger.ErrorFormat("The AriaLogPath is invalid, no logs will be written");
                }

                AriaLogPath = string.Empty;


            }

            //TODO: Test that a log file is created
            if (!File.Exists(filePath))
            {
                if (mLogger.IsDebugEnabled)
                {
                    mLogger.DebugFormat("The file at {0} does not exist, ishimotto will create it");
                }

                File.Create(filePath);
            }
        }

        /// <summary>
        /// Varify that the directory path is valid, and creates it if it does not exist
        /// </summary>
        /// <param name="downloadsDirectory">The directory path to check if is valid</param>
        private void HandleDownloadDirectory(string downloadsDirectory)
        {
            //TODO: Test if directory is created

            //TODO: Test if exception is thrown


            if (IsPathValid(downloadsDirectory))
            {

                if (mLogger.IsFatalEnabled)
                {
                    mLogger.FatalFormat("The path {0} for download directory is not valid", downloadsDirectory);
                }

                throw new Exception(String.Format("The path {0} is not valid", downloadsDirectory));
                
            }

            //If the directory does not exist, create it
            if (!Directory.Exists(downloadsDirectory))
            {
                //TODO: Test that directory is created

                if (mLogger.IsDebugEnabled)

                {
                    mLogger.DebugFormat("The directory at {0} is not exist, ishimotto will create it");
                }
                Directory.CreateDirectory(downloadsDirectory);

                DownloadsDirectory = downloadsDirectory;
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
