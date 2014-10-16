using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using log4net;

namespace Ishimotto.Core.Aria
{
    /// <summary>
    /// Writes large amounts of data to files
    /// </summary>
    public class AriaFilesWriter : IDisposable
    {
        #region Data Members

        /// <summary>
        /// The directory to save the files to
        /// </summary>
        private readonly string mOutputDirectory;

        /// <summary>
        /// Streams to write the files
        /// </summary>
        private readonly StreamWriter[] mWriters;

        /// <summary>
        /// Indicates if the streams are initialized
        /// </summary>
        private bool mAreStreamsInitialized = false;

        /// <summary>
        /// Represents a collection of all the paths creates by the <see cref="AriaFilesWriter"/>
        /// </summary>
        private readonly List<string> mFilePaths;

        /// <summary>
        /// The name of the file to create
        /// </summary>
        private readonly string mFileName;

        private readonly ILog mLogger = LogManager.GetLogger(typeof (AriaFilesWriter));
        private readonly object mLock = new object();

        #endregion

        #region Constructors

        /// <summary>
        /// Creates new instance of <see cref="AriaFilesWriter"/>
        /// </summary>
        /// <param name="outputDirectory"><see cref="mOutputDirectory"/></param>
        /// <param name="fileName"><see cref="mFileName"/></param>
        /// <param name="numOfFiles">Number of files to create</param>
        /// <param name="extension"><see cref="Extension"/></param>
        public AriaFilesWriter(string outputDirectory, string fileName, int numOfFiles, string extension)
        {
            ValidateArguemnts(fileName, numOfFiles, extension);

            mOutputDirectory = outputDirectory;

            CreateDirectory();

            mFileName = fileName;
            Extension = extension;

            mFilePaths = new List<string>(GetFilePaths(numOfFiles));

            mWriters = new StreamWriter[numOfFiles];
        }

        /// <summary>
        /// Creates new instance of <see cref="AriaFilesWriter"/>
        /// </summary>
        /// <param name="outputDirectory"><see cref="mOutputDirectory"/></param>
        /// <param name="fileName"><see cref="mFileName"/></param>
        /// <param name="numOfFiles">Number of files to create</param>
        /// <param name="perfix">perfix to append to the text</param>
        /// <param name="suffix">suffix to append to the text</param>
        /// <param name="extension"><see cref="Extension"/></param>
        public AriaFilesWriter(string outputDirectory, string fileName, int numOfFiles, string extension, string perfix, string suffix)
            : this(outputDirectory, fileName, numOfFiles, extension)
        {
            Suffix = suffix;
            Prefix = perfix;
        }

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
        public string Prefix { get; set; }

        /// <summary>
        /// Optional a suffix to append to the text
        /// </summary>
        public string Suffix { get; set; }

        #endregion

        #region Public Methods

        /// <summary>
        /// Write texts to <see cref="FilesPaths"/>, adding perfix and suffix if exists
        /// </summary>
        /// <param name="lines">Lines to write</param>
        public void WriteToFiles(ICollection<string> lines)
        {
            // Checking if lines argument is null
            if (lines == null)
            {
                mLogger.Error("The lines argument is null, no further actions will take place");

                throw new ArgumentNullException("lines", "The lines argument can not be null");
            }

            // Checking if there anything to write
            if (!lines.Any())
            {
                mLogger.Warn("The lines Enumrable is empty, no further actions will take place");

                return;
            }

            mLogger.Debug("Start writing lines to files");

            if (!mAreStreamsInitialized)
            {
                mLogger.Debug("Initalizing writers");

                InitializeWriters();
            }

            mLogger.InfoFormat("Start splitting {0} lines into {1} files", lines.Count, mWriters.Length);

            InnerWriteFiles(lines);

            mLogger.Debug("finsidh spliting lines to files");
        }

        private void InnerWriteFiles(IEnumerable<string> lines)
        {
            int lineIndex = 0;

            // TODO: Maybe there is a way to make it parallel with Take and Skip to make it Thread safe

            IEnumerable<string> formattedLines = 
                lines.Select(text => String.Concat(Prefix, text, Suffix));

            IEnumerable<IGrouping<int, string>> groupedLines =
                formattedLines.Select((line, index) =>
                                      new
                                          {
                                              Line = line,
                                              Index = index
                                          })
                              .GroupBy(x => x.Index%mWriters.Length,
                                       x => x.Line);

            Parallel.ForEach(groupedLines, group =>
                                           WriteGroup(group.Key, group));
        }

        private void WriteGroup(int writerIndex, IEnumerable<string> lines)
        {
            StreamWriter currentWriter = mWriters[writerIndex];

            foreach (string line in lines)
            {
                currentWriter.WriteLine(line);                
            }
        }

        /// <summary>
        /// Disposes the instance of <see cref="AriaFilesWriter"/>
        /// </summary>
        public void Dispose()
        {
            mLogger.Debug("Disposing Writers");

            IEnumerable<StreamWriter> writersToDispose = 
                mWriters.Where(writer => writer != null);
            
            foreach (StreamWriter streamWriter in writersToDispose)
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
                ArgumentException arumentException =
                    new ArgumentException("The output directory consist of invalid chars",
                                          "outputDirectory");

                mLogger.Fatal("Could not create output directory", arumentException);

                throw arumentException;
            }

            if (!Directory.Exists(mOutputDirectory))
            {
                mLogger.InfoFormat("The directory at {0} does not exist, ishimotto will create the directory", mOutputDirectory);

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
            List<string> files = new List<string>(numOfFiles);

            int index = 1;

            for (int fileNo = 0; fileNo < numOfFiles; fileNo++)
            {
                string filePath = Path.Combine(mOutputDirectory, string.Format("{0}{1}.{2}", mFileName, index, Extension));

                while (File.Exists(filePath))
                {
                    index++;

                    string previousFilePath = filePath;

                    filePath = Path.Combine(mOutputDirectory, String.Concat(mFileName, index, ".", Extension));

                    mLogger.InfoFormat("The file {0} already exist, tring to create file {1}", previousFilePath, filePath);
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

            mAreStreamsInitialized = true;
        }

        /// <summary>
        /// Validates that the arguments are valid
        /// </summary>
        /// <param name="fileName"><see cref="mFileName"/></param>
        /// <param name="numOfFiles">Number of files to create</param>
        /// <param name="extension"><see cref="Extension"/></param>
        private void ValidateArguemnts(string fileName, int numOfFiles, string extension)
        {
            Exception exception = null;

            if (string.IsNullOrEmpty(fileName))
            {
                exception = new ArgumentNullException("fileName", "The file name can not be null");
                mLogger.Fatal("The given file name is null", exception);
            }

            if (string.IsNullOrEmpty(extension))
            {
                exception = new ArgumentNullException("extension", "The extension can not be null");
                mLogger.Fatal("The given extension is null", exception);
            }

            if (numOfFiles <= 0)
            {
                exception = new ArgumentOutOfRangeException("numOfFiles", "The numOfFiles nust be greater than 0");
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