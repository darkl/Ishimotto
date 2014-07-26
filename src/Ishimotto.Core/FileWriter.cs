using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using log4net.Repository.Hierarchy;

namespace Ishimotto.Core
{
    /// <summary>
    /// Writes large amounts of data to files
    /// </summary>
    public class FileWriter : IDisposable
    {
        #region Data Members
        /// <summary>
        /// The directory to save the files to
        /// </summary>
        private readonly string mOutputDirectory;

        /// <summary>
        /// Streams to write the files
        /// </summary>
        private StreamWriter[] mWriters;

        /// <summary>
        /// Indicates if the streams are initialized
        /// </summary>
        private bool mAreStreamInitialized = false;

        /// <summary>
        /// Collection of all the paths creates by the <see cref="FileWriter"/>
        /// </summary>
        private List<string> mFilePaths;

        /// <summary>
        /// The name of the file to create
        /// </summary>
        private string mFileName;

        private ILog mLogger;

        #endregion

        #region Properties
        /// <summary>
        /// <see cref="mFilePaths"/>
        /// </summary>
        public IEnumerable<string> FilesPaths { get { return mFilePaths.AsEnumerable(); } }

        /// <summary>
        /// The extension of the document to create
        /// </summary>
        public string Extension { get; private set; }

        /// <summary>
        /// Optional a perfix to append to the text
        /// </summary>
        public string Perfix { get; set; }

        /// <summary>
        /// Optional a suffix to append to the text
        /// </summary>
        public string Suffix { get; set; }

        #endregion

        #region Constructors
        /// <summary>
        /// Creates new instance of <see cref="FileWriter"/>
        /// </summary>
        /// <param name="outputDirectory"><see cref="mOutputDirectory"/></param>
        /// <param name="fileName"><see cref="mFileName"/></param>
        /// <param name="numOfFiles">Number of files to create</param>
        /// <param name="extension"><see cref="Extension"/></param>
        public FileWriter(string outputDirectory, string fileName, int numOfFiles, string extension)
        {

            mLogger = LogManager.GetLogger(this.GetType());

            ValidateArguemnts(fileName, numOfFiles, extension);

            mOutputDirectory = outputDirectory;

            CreateDirectory();

            mFileName = fileName;
            Extension = extension;

            mFilePaths = new List<string>(GetFilePaths(numOfFiles));

            mWriters = new StreamWriter[numOfFiles];

        }


        /// <summary>
        /// Creates new instance of <see cref="FileWriter"/>
        /// </summary>
        /// <param name="outputDirectory"><see cref="mOutputDirectory"/></param>
        /// <param name="fileName"><see cref="mFileName"/></param>
        /// <param name="numOfFiles">Number of files to create</param>
        /// <param name="perfix">perfix to append to the text</param>
        /// <param name="suffix">suffix to append to the text</param>
        /// <param name="extension"><see cref="Extension"/></param>
        public FileWriter(string outputDirectory, string fileName, int numOfFiles, string extension, string perfix, string suffix)
            : this(outputDirectory, fileName, numOfFiles, extension)
        {
            Suffix = suffix;
            Perfix = perfix;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Write texts to <see cref="FilesPaths"/>, adding perfix and suffix if exists
        /// </summary>
        /// <param name="lines">Lines to write</param>
        public async Task WriteToFiles(IEnumerable<string> lines)
        {

            //Checking if lines argument is null
            if (lines == null)
            {
                if (mLogger.IsErrorEnabled)
                {
                    mLogger.Error("The lines argument is null, no further actions will take place");

                    throw new ArgumentNullException("The lines argument can not be null");
                }
            }

            //Checking if there anything to write
            if (!lines.Any())
            {
                if (mLogger.IsWarnEnabled)
                {
                    mLogger.Warn("The lines Enumrable is empty, no further actions will take place");
                }

                return;
            }

            if (mLogger.IsDebugEnabled)
            {
                mLogger.Debug("Start writing lines to files");
            }


            if (!mAreStreamInitialized)
            {
                if (mLogger.IsDebugEnabled)
                {
                    mLogger.Debug("Initalize writers");
                }

                InitializeWriters();
            }

            int lineIndex = 0;


            if (mLogger.IsInfoEnabled)
            {
                mLogger.InfoFormat("Start splitting {0} lines into {1} files", lines.Count(), mWriters.Length);
            }

            await Task.Factory.StartNew(() =>
                {
                    foreach (var text in lines)
                    {
                        var line = String.Concat(Perfix, text, Suffix);
                        mWriters[lineIndex % mWriters.Length].WriteLine(line);
                        Interlocked.Increment(ref lineIndex);
                    }
                });

            if (mLogger.IsDebugEnabled)
            {
                mLogger.Debug("finsidh spliting lines to files");
            }
        }

        /// <summary>
        /// Disposes the instance of <see cref="FileWriter"/>
        /// </summary>
        public void Dispose()
        {


            if (mLogger.IsDebugEnabled)
            {
                mLogger.Debug("Disposing Writers");
            }

            var writersToDispose = mWriters.Where(writer => writer != null);
            foreach (var streamWriter in writersToDispose)
            {
                streamWriter.Close();
                streamWriter.Dispose();
            }

        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Creates the <see cref="mOutputDirectory"/> if doesn't exist
        /// </summary>
        private void CreateDirectory()
        {
            if (!HelperMethods.IsPathValid(mOutputDirectory))
            {
                var arumentException = new ArgumentException("The output directory consist of invalid chars");

                if (mLogger.IsFatalEnabled)
                {
                    mLogger.Fatal("Could not create output directory", arumentException);

                }

                throw arumentException;
            }

            if (!Directory.Exists(mOutputDirectory))
            {

                if (mLogger.IsInfoEnabled)
                {
                    mLogger.InfoFormat("The directory at {0} does not exist, ishimotto will create the directory", mOutputDirectory);
                }

                Directory.CreateDirectory(mOutputDirectory);
            }
        }

        /// <summary>
        /// Gets the paths of the files to write
        /// </summary>
        /// <param name="numOfFiles">Number of files to create</param>
        /// <returns>Enumrable of all the files paths</returns>
        private IEnumerable<string> GetFilePaths(int numOfFiles)
        {
            var files = new List<string>(numOfFiles);

            int index = 1;

            for (int fileNo = 0; fileNo < numOfFiles; fileNo++)
            {

                string filePath = Path.Combine(mOutputDirectory, String.Concat(mFileName, index, ".", Extension));

                while (File.Exists(filePath))
                {
                    index++;

                    var previousFilePath = filePath;

                    filePath = Path.Combine(mOutputDirectory, String.Concat(mFileName, index, ".", Extension));

                    if (mLogger.IsInfoEnabled)
                    {
                        mLogger.InfoFormat("The file {0} already exist, tring to create file {1}", previousFilePath, filePath);
                    }
                }

                files.Add(filePath);

                index++;
            }

            return files;
        }

        /// <summary>
        /// Initialize the streams to write to <see cref="FilesPaths"/>
        /// </summary>
        private void InitializeWriters()
        {

            for (int writerPosition = 0; writerPosition < mWriters.Length; writerPosition++)
            {

                mWriters[writerPosition] = new StreamWriter(mFilePaths[writerPosition], false);

            }

            mAreStreamInitialized = true;

        }

        /// <summary>
        /// Validate that the arguments are valid
        /// </summary>
        /// <param name="fileName"><see cref="mFileName"/></param>
        /// <param name="numOfFiles">Number of files to create</param>
        /// <param name="extension"><see cref="Extension"/></param>
        private void ValidateArguemnts(string fileName, int numOfFiles, string extension)
        {

            Exception exception = null;

            if (String.IsNullOrEmpty(fileName))
            {

                exception = new ArgumentNullException("The file name can not be null");

                if (mLogger.IsFatalEnabled)
                    mLogger.Fatal("The given file name is null", exception);


            }

            if (String.IsNullOrEmpty(extension))
            {

                exception = new ArgumentNullException("The extension can not be null");

                if (mLogger.IsFatalEnabled)
                    mLogger.Fatal("The given extension is null", exception);

            }

            if (numOfFiles <= 0)
            {

                exception = new ArgumentOutOfRangeException("The numOfFiles nust be grather than 0");

                if (mLogger.IsFatalEnabled)
                    mLogger.Fatal("InvalidParameter: numOfFiles", exception);
            }


            if (exception != null)
            {
                throw exception;
            }
        }
        #endregion
    }
}
