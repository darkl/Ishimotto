using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using log4net;

namespace Ishimotto.Core.Aria
{
    /// <summary>
    /// Download data using Aria2 tool
    /// </summary>
    public class AriaDownloader
    {
        #region Constants
        
        private const string ARIA_EXE = "aria2c.exe";

        #endregion

        #region Members

        private readonly ILog mLogger = LogManager.GetLogger(typeof (AriaDownloader));
        private string mFileListProcessArguments;
        private string mSingleFileProcessArguments;
        private int mMaxUrlsPerProcess;

        #endregion

        #region Properties

        /// <summary>
        /// Get or sets the directory to save the downloads
        /// </summary>
        public string DownloadsDirectory { get; private set; }

        /// <summary>
        /// Gets or sets a value indicating if the <see cref="AriaDownloader"/> should delete temporary files after using them
        /// </summary>
        public bool DeleteTempFiles { get; private set; }

        /// <summary>
        /// Gets or sets the file path to the aria process logs to, if the path is String.Empty no log would be saved
        /// </summary>
        public string AriaLogPath { get; private set; }

        /// <summary>
        /// Gets or sets the severity of aria2 log write
        /// </summary>
        public AriaSeverity Severity { get; private set; }

        /// <summary>
        /// Get or sets the max connections to open aganist the NuGet server
        /// </summary>
        public int MaxConnections { get; private set; }

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
        public AriaDownloader(string downloadsDirectory, bool deleteTempFiles, int maxConnections, string logPath = "", AriaSeverity severity = AriaSeverity.None, int maxUrlsPerProcess = 5000)
        {
            HandleArguments(downloadsDirectory, maxConnections, logPath, severity);

            DeleteTempFiles = deleteTempFiles;
            mMaxUrlsPerProcess = maxUrlsPerProcess;

            BuildArgumentsString();
        }

        private void BuildArgumentsString()
        {
            string arguments = string.Format(" -x {0}  -i {1} -d {2}", MaxConnections, "{0}", DownloadsDirectory);

            if (!string.IsNullOrEmpty(AriaLogPath))
            {
                arguments += string.Format(" --log={0} --log-level={1}", AriaLogPath, Severity.ToString().ToLower());
            }

            mFileListProcessArguments = arguments;

            mSingleFileProcessArguments =
                string.Format("{0} -d {1}", "{0}", DownloadsDirectory);
        }

        /// <summary>
        /// Creates new instance of <see cref="AriaDownloader"/>
        /// </summary>
        /// <param name="downloadsDirectory"><see cref="DownloadsDirectory"/></param>
        public AriaDownloader(string downloadsDirectory)
            : this(downloadsDirectory, false, 10, String.Empty)
        {
        }

        /// <summary>
        /// Creates new instance of <see cref="AriaDownloader"/>
        /// </summary>
        /// <param name="downloadsDirectory"><see cref="DownloadsDirectory"/></param>
        /// <param name="deleteTempFiles"><see cref="DeleteTempFiles"/></param>
        public AriaDownloader(string downloadsDirectory, bool deleteTempFiles)
            : this(downloadsDirectory, deleteTempFiles, 10, String.Empty)
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
            mLogger.InfoFormat("Start downloading file from url {0}", url);

            string arguments = GetSingleFileProcessArguments(url);

            RunAria2(arguments);
        }

        /// <summary>
        /// Download mulitple files 
        /// </summary>
        /// <param name="urls">The urls of the files to download</param>
        public void Download(ICollection<string> urls)
        {
            if (urls == null)
            {
                mLogger.Fatal("Got null as urls to download");

                throw new ArgumentException("The urls to download can not be null", "urls");
            }

            if (!urls.Any())
            {
                mLogger.Warn("No files to download . . . cycle is done");
            }

            mLogger.InfoFormat("Start downloading {0} file(s)", urls.Count);

            mLogger.Debug("splitting the urls into files");

            IEnumerable<string> paths = CreateLinkFiles(urls);

            mLogger.Debug("splitting completed");

            Parallel.ForEach(paths, filePath => DownloadFromFile(filePath));
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Create files contains all the urls for Atia to download
        /// </summary>
        /// <param name="urls">The urls to insert to the files</param>
        /// <returns>collection of all created link files</returns>
        private IEnumerable<string> CreateLinkFiles(ICollection<string> urls)
        {
            IEnumerable<string> filePaths;

            int numOfFiles = Math.Max(urls.Count()/mMaxUrlsPerProcess, 1);

            using (AriaFilesWriter ariaFilesWriter = new AriaFilesWriter(DownloadsDirectory, "links", numOfFiles, "txt"))
            {
                ariaFilesWriter.WriteToFiles(urls);

                filePaths = ariaFilesWriter.FilesPaths;
            }

            return filePaths;
        }

        private string GetSingleFileProcessArguments(string url)
        {
            return string.Format(mSingleFileProcessArguments, url);
        }

        /// <summary>
        /// Download urls from given file
        /// </summary>
        /// <param name="filePath">File contains a list of urls</param>
        private void DownloadFromFile(string filePath)
        {
            mLogger.InfoFormat("Start downloading urls from {0}", filePath);

            string arguments = GetArgumentsForFilepath(filePath);

            Process downloadProcess = RunAria2(arguments);

            downloadProcess.WaitForExit();

            mLogger.InfoFormat("Finish downloaded files from {0}, for download information check aria log", filePath);

            if (DeleteTempFiles)
            {
                mLogger.DebugFormat("Deleting file {0}", filePath);

                File.Delete(filePath);
            }
        }

        private string GetArgumentsForFilepath(string filePath)
        {
            return string.Format(mFileListProcessArguments, filePath);
        }

        private Process RunAria2(string processArguments)
        {
            ProcessStartInfo processStartInfo = GetProcessStartInformation(processArguments);

            Process downloadProcess = Process.Start(processStartInfo);

            return downloadProcess;
        }

        /// <summary>
        /// Creates information of the aria process to execute
        /// </summary>
        /// <param name="arguments">Aria arguments to run</param>
        /// <returns>information to start the process</returns>
        private ProcessStartInfo GetProcessStartInformation(string arguments)
        {
            ProcessStartInfo processStartInfo = new ProcessStartInfo();

            processStartInfo.UseShellExecute = false;
            processStartInfo.CreateNoWindow = true;
            processStartInfo.FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ARIA_EXE);

            processStartInfo.Arguments = arguments;

            return processStartInfo;
        }

        #endregion

        #region Argument Validation

        /// <summary>
        /// Verify that the given arguemnts are valid
        /// </summary>
        /// <param name="downloadsDirectory"><see cref="DownloadsDirectory"/></param>
        /// <param name="maxConnections"><see cref="MaxConnections"/></param>
        /// <param name="logPath"><see cref="AriaLogPath"/></param>
        /// <param name="severity"><see cref="Severity"/></param>
        private void HandleArguments(string downloadsDirectory, int maxConnections, string logPath, AriaSeverity severity)
        {
            HandleDownloadDirectory(downloadsDirectory);

            HandleAriaLogPath(logPath);

            HandleSeverity(logPath, severity);

            ValidateMaxConnections(maxConnections);
        }

        private void ValidateMaxConnections(int maxConnections)
        {
            if (maxConnections != 0)
            {
                MaxConnections = maxConnections;
            }
            else
            {
                mLogger.Error
                    ("Could not set MaxConnection, The maximum connections value must be greater than zero, MaxConnections will be set to 1");

                MaxConnections = 1;
            }
        }

        /// <summary>
        /// Validate that the severity is set to NONE when a log file path is not provided
        /// </summary>
        /// <param name="logPath"><see cref="AriaLogPath"/></param>
        /// <param name="severity"><see cref="Severity"/></param>
        private void HandleSeverity(string logPath, AriaSeverity severity)
        {
            if (string.IsNullOrEmpty(logPath))
            {
                if (severity != AriaSeverity.None)
                {
                    mLogger.Error
                        ("The severity must be set to NONE unless a log path is provided, no logs will be written from aria2c.exe");

                    Severity = AriaSeverity.None;
                }
            }
            else
            {
                if (severity != AriaSeverity.None)
                {
                    Severity = severity;
                }
                else
                {
                    mLogger.Warn(
                        "The severity must not be set to NONE when a log path is present, severity will be NOTICE level");

                    Severity = AriaSeverity.Notice;
                }
            }
        }

        /// <summary>
        /// Varify that a file path is valid, and create the file if it does not exist
        /// </summary>
        /// <param name="logPath">path to varify</param>
        private void HandleAriaLogPath(string logPath)
        {
            // If logPath is empty there is nothing to check
            if (string.IsNullOrEmpty(logPath))
            {
                return;
            }

            if (HelperMethods.IsPathValid(logPath))
            {
                AriaLogPath = logPath;
            }
            else
            {
                mLogger.ErrorFormat("The AriaLogPath is invalid, no logs will be written");

                AriaLogPath = string.Empty;
            }
        }

        /// <summary>
        /// Varify that the directory path is valid, and creates it if it does not exist
        /// </summary>
        /// <param name="downloadsDirectory">The directory path to check if is valid</param>
        private void HandleDownloadDirectory(string downloadsDirectory)
        {
            if (!HelperMethods.IsPathValid(downloadsDirectory))
            {
                mLogger.FatalFormat("The path {0} for download directory is not valid", downloadsDirectory);

                throw new ArgumentException(String.Format("The path {0} is not valid", downloadsDirectory),
                                            "downloadsDirectory");
            }

            // If the directory does not exist, create it
            if (!Directory.Exists(downloadsDirectory))
            {
                mLogger.InfoFormat("The directory at {0} doesn't exist, ishimotto will create it", downloadsDirectory);

                Directory.CreateDirectory(downloadsDirectory);
            }

            DownloadsDirectory = downloadsDirectory;
        }

        #endregion
    }
}