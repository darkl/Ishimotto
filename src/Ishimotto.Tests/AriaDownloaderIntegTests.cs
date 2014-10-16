using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Ishimotto.Core.Aria;
using log4net.Config;
using NUnit.Framework;

namespace Ishimotto.Tests
{
    /// <summary>
    /// Test functioning of <see cref="AriaDownloader"/>
    /// </summary>
    [TestFixture]
    public class AriaDownloaderIntegTests
    {
        #region Constants
        private const string DOWNLOADS_DIRECTORY = @"C:\Ishimotto\Tests\Downloads";

        private const string TEST_DIRECTORY = @"C:\Ishimotto\Tests\";

        /// <summary>
        /// Url to random Nupkg to download
        /// </summary>
        private const string SINGLE_DOWNLOAD_URL = @"http://nuget.org/api/v2/package/_Atrico.Lib.CommonAssemblyInfo/1.0.0";

        /// <summary>
        /// The name of <see cref="SINGLE_DOWNLOAD_URL"/> file
        /// </summary>
        private const string SINGLE_FILE_NAME = "_atrico.lib.commonassemblyinfo.1.0.0.nupkg";


        /// <summary>
        /// Path to the file that contains all the links to downloads
        /// </summary>
        private const string LINKS_FILE_PATH = "links.txt";
        #endregion

        #region Initialization
        [TestFixtureSetUp]
        public void Init()
        {
            XmlConfigurator.Configure();
        }

        [SetUp]
        public void CreateTestsDirectory()
        {
            if (!Directory.Exists(DOWNLOADS_DIRECTORY))
            {
                Directory.CreateDirectory(DOWNLOADS_DIRECTORY);
            }
        } 
        #endregion
        
        [Test]
        public void Test_Single_Download()
        {
            //Arrange

            var downloader = new AriaDownloader(DOWNLOADS_DIRECTORY);

            //Act

            downloader.Download(SINGLE_DOWNLOAD_URL);

            var filePath = Path.Combine(DOWNLOADS_DIRECTORY, SINGLE_FILE_NAME);


            //Assert

            Assert.That(File.Exists(filePath), Is.True);
        }

        [Test]
        public void Download_From_Number_of_Threads()
        {

            //Arrange

            var downloader = new AriaDownloader(DOWNLOADS_DIRECTORY, false, 10);

            //Getting all links

            var linksFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, LINKS_FILE_PATH);

            string[] urls = File.ReadAllLines(linksFilePath);

            //Act

            downloader.Download(urls);

            var numOfFiles = Directory.GetFiles(DOWNLOADS_DIRECTORY, "*.nupkg").Length;

            //Assert

            Assert.That(numOfFiles, Is.EqualTo(urls.Count()));



        }

        [Test]
        public void Check_That_Temp_Files_Are_Deleted()
        {
            //Arragne

            var downloader = new AriaDownloader(DOWNLOADS_DIRECTORY, true);


            var urls = GetUrls(2);

            //Act 

            downloader.Download(urls.ToList());

            var tempFilePath = Path.Combine(DOWNLOADS_DIRECTORY, "links1.txt");


            //Assert

            Assert.That(File.Exists(tempFilePath), Is.False);

        }

        /// <summary>
        /// Fetchs urls from <see cref="LINKS_FILE_PATH"/>
        /// </summary>
        /// <param name="numOfLinksToFetch">Number of links to fetch</param>
        /// <returns>Enumrable of the first <see cref="numOfLinksToFetch"/> from the <see cref="LINKS_FILE_PATH"/></returns>
        private IEnumerable<string> GetUrls(int numOfLinksToFetch)
        {
            var allLinks = File.ReadAllLines(LINKS_FILE_PATH);

            return allLinks.Take(numOfLinksToFetch);
        }
        
        [Test]
        public void Check_If_Aria_Logs_Exist()
        {
            //Arragne

            var ariaLogPath = Path.Combine(DOWNLOADS_DIRECTORY, "aria.log");

            var downloader = new AriaDownloader(DOWNLOADS_DIRECTORY, true, 1, ariaLogPath, AriaSeverity.Debug);


            var urls = GetUrls(2);

            //Act 

            downloader.Download(urls.ToList());


            //Assert

            Assert.That(File.Exists(ariaLogPath), Is.True);

        }

        #region TearDown
        [TearDown]
        public void DeleteDirectory()
        {
            var retries = 5;

            for (int tryNo = 0; tryNo < retries; tryNo++)
            {
                try
                {
                    if (Directory.Exists(TEST_DIRECTORY))
                    {
                        Directory.Delete(TEST_DIRECTORY, true);
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

        #endregion
    }
}
