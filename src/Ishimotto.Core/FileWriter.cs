using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using log4net;

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
        /// Collection of all the paths creates by the <see cref="FileWriter"/>
        /// </summary>
        private List<string> mFilePaths;

        /// <summary>
        /// The name of the file to create
        /// </summary>
        private string mFileName;

        /// <summary>
        /// A Logger
        /// </summary>
        private ILog mLogger;

        private int mFileIndex;

        private ISubject<string> mLineSubject;

        private IDisposable mSubscripitonDisposable;

        #endregion

        #region Properties

        /// <summary>
        /// <see cref="mFilePaths"/>
        /// </summary>
        public IEnumerable<string> FilesPaths
        {
            get { return mFilePaths.AsEnumerable(); }
        }

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
        /// <param name="maxLinesInFile">Number of files to create</param>
        /// <param name="extension"><see cref="Extension"/></param>
        public FileWriter(string outputDirectory, string fileName, int maxLinesInFile, string extension)
        {
            mLogger = LogManager.GetLogger(this.GetType());

            ValidateArguemnts(fileName, maxLinesInFile, extension);

            mOutputDirectory = outputDirectory;

            mFilePaths = new List<string>();

            CreateDirectory();

            mFileName = fileName;

            Extension = extension;

            mFileIndex = 0;

             mLineSubject = new Subject<string>();

            mSubscripitonDisposable = mLineSubject.Distinct().Buffer(maxLinesInFile).Subscribe(WriteLinesToFile);
            
        }


        private void WriteLinesToFile(IList<string> linesToWrite)
        {
            int index;

            lock (mLineSubject)
            {
                index = mFileIndex;

                mFileIndex++;
            }

            var filePath = Path.Combine(mOutputDirectory, String.Concat(mFileName, index, ".", Extension));

            File.WriteAllLines(filePath, linesToWrite);

            mFilePaths.Add(filePath);

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
        public FileWriter(string outputDirectory, string fileName, int numOfFiles, string extension, string perfix,
            string suffix)
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
        public void Write(IEnumerable<string> lines)
        {
            //Checking if lines argument is null
            if (lines == null)
            {

                mLogger.Error("The lines argument is null, no further actions will take place");

                throw new ArgumentNullException("The lines argument can not be null");
            }

            //Checking if there anything to write
            if (!lines.Any())
            {
                mLogger.Warn("The lines Enumrable is empty, no further actions will take place");
                return;
            }

            mLogger.Debug("Start writing lines to files");

            foreach (var line in lines)
            {
                Write(line);
            }
        }

        /// <summary>
        /// Disposes the instance of <see cref="FileWriter"/>
        /// </summary>
        public void Dispose()
        {
            mLogger.Debug("Disposing Writers");

            mLineSubject.OnCompleted();
            mSubscripitonDisposable.Dispose();
        }

        public void Flush()
        {
            mLineSubject.OnCompleted();
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
        /// Validate that the arguments are valid
        /// </summary>
        /// <param name="fileName"><see cref="mFileName"/></param>
        /// <param name="maxLinesInFile">Number of files to create</param>
        /// <param name="extension"><see cref="Extension"/></param>
        private void ValidateArguemnts(string fileName, int maxLinesInFile, string extension)
        {
            Exception exception = null;

            if (String.IsNullOrEmpty(fileName))
            {
                exception = new ArgumentNullException("The file name can not be null");

                mLogger.Fatal("The given file name is null", exception);
            }

            if (String.IsNullOrEmpty(extension))
            {

                exception = new ArgumentNullException("The extension can not be null");

                mLogger.Fatal("The given extension is null", exception);
            }

            if (maxLinesInFile <= 0)
            {
                exception = new ArgumentOutOfRangeException("The maxLinesInFile nust be grather than 0");

                mLogger.Fatal("InvalidParameter: maxLinesInFile", exception);
            }


            if (exception != null)
            {
                throw exception;
            }
        }
        #endregion

        public void Write(string link)
        {
            mLineSubject.OnNext(link);
        }
    }
}
