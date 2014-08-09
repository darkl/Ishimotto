using System;
using System.IO;
using System.Linq;
using System.Threading;
using Ishimotto.Core;
using log4net.Config;
using NUnit.Framework;

namespace Ishimotto.Tests
{
    [TestFixture]
    public class AriaDownloaderTests
    {
        private const string TESTS_DIRECTORY = @"C:\Ishimotto\Tests\";

        private const string DOWNLOADS_DIRECTORY = @"C:\Ishimotto\Tests\Downloads";

        private const string ARIA_LOG_PATH = TESTS_DIRECTORY + "aria.log";

        [TestFixtureSetUp]
        public void Init()
        {
            XmlConfigurator.Configure();
        }

        [SetUp]
        public void CreateTestsDirectory()
        {
            if (!Directory.Exists(TESTS_DIRECTORY))
            {
                Directory.CreateDirectory(TESTS_DIRECTORY);
            }
        }

        [TearDown]
        public void DeleteDirectory()
        {
            var retries = 5;

            for (int tryNo = 0; tryNo < retries; tryNo++)
            {
                try
                {
                    if (Directory.Exists(TESTS_DIRECTORY))
                    {
                        Directory.Delete(TESTS_DIRECTORY, true);
                    }

                    tryNo = retries;
                }
                catch (Exception)
                {

                    tryNo++;
                    Thread.Sleep(500);
                }

            }


        }

        [TestFixtureTearDown]
        public void CleanUp()
        {
            DeleteDirectory();
        }


        [Test]
        public void Download_Directory_Is_Created()
        {
            //Act

            var directoryPath = Path.Combine(DOWNLOADS_DIRECTORY);
            var downloader = new AriaDownloader(directoryPath);

            //Assert

            Assert.That(Directory.Exists(directoryPath), Is.True);

        }

        [Test]
        public void Downloads_Directory_Is_Set()
        {
            //When directory does not exist
            AssertDirectoryExist();

            //When directory is exist
            AssertDirectoryExist();
        }

        private static void AssertDirectoryExist()
        {
//Act

            var downloader = new AriaDownloader(DOWNLOADS_DIRECTORY);

            //Assert

            Assert.That(downloader.DownloadsDirectory, Is.EqualTo(DOWNLOADS_DIRECTORY));
        }

        [Test]
        public void Downloads_Directory_Contains_Invalid_Chars()
        {
            //Arrange


            string invalidPath = TESTS_DIRECTORY + Path.GetInvalidPathChars().First();

            //Act + Assert

            Assert.Throws<Exception>(() => new AriaDownloader(invalidPath));
        }

        [Test]
        public void Aria_Log_Path_Contains_Invalid_Chars()
        {
            //Arrange


            string invalidPath = TESTS_DIRECTORY + Path.GetInvalidPathChars().First();

            //Act

            var downloader = new AriaDownloader(DOWNLOADS_DIRECTORY, false, 2, invalidPath, AriaSeverity.Error);

            //Assert

            Assert.That(downloader.AriaLogPath, Is.Empty);
        }

        [Test]
        public void Aria_Log_Created()
        {

            //Arrange



            //Act
            var downloader = new AriaDownloader(DOWNLOADS_DIRECTORY, false, 2, ARIA_LOG_PATH, AriaSeverity.Error);


            //Assert

            Assert.That(File.Exists(downloader.AriaLogPath), Is.True);

        }

        [Test]
        public void When_Aria_Log_Is_Empty_Severity_Set_To_None()
        {

            //Arrange

            var ariaLogPath = string.Empty;

            //Act
            var downloader = new AriaDownloader(DOWNLOADS_DIRECTORY, false, 2, ariaLogPath, AriaSeverity.Error);


            //Assert

            Assert.That(downloader.Severity, Is.EqualTo(AriaSeverity.None));

        }


        [Test]
        public void When_Aria_Log_Is_Not_Empty_Severity_Is_Not_None()
        {

            //Act
            var downloader = new AriaDownloader(DOWNLOADS_DIRECTORY, false, 2, ARIA_LOG_PATH, AriaSeverity.None);


            //Assert
            Assert.That(downloader.Severity, Is.EqualTo(AriaSeverity.Notice));

        }

        [Test]
        public void When_Max_Connections_Is_Zero()
        {
            //Act

            var downloader = new AriaDownloader(DOWNLOADS_DIRECTORY, false, 0);

            //Assert

            Assert.That(downloader.MaxConnections, Is.EqualTo(1));

        }


        [Test]
        public void Max_Connections_Is_Set()
        {
            //Act

            var downloader = new AriaDownloader(DOWNLOADS_DIRECTORY, false, 7);

            //Assert

            Assert.That(downloader.MaxConnections, Is.EqualTo(7));

        }

        [Test]
        public void Set_Severity()
        {
            //Act

            var downloader = new AriaDownloader(DOWNLOADS_DIRECTORY, false, 5, ARIA_LOG_PATH, AriaSeverity.Warn);

            //Assert

            Assert.That(downloader.Severity,Is.EqualTo(AriaSeverity.Warn));
        }

    }
}
