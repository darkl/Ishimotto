using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Ishimotto.Core;
using log4net.Config;
using NUnit.Framework;

namespace Ishimotto.Tests
{

    /// <summary>
    /// Test the functioning of <see cref="FileWriter"/> 
    /// </summary>
    [TestFixture]
    public class FileWriterTests
    {

        #region Constants
        /// <summary>
        /// the Tests directory
        /// </summary>
        private const string TESTS_DIRECTORY = @"C:\Ishimotto\Tests\";

        private const string DUMMY_STRING = "DUMMY";

        private static readonly string[] THINGS_TO_WRITE = new[] { "12", "243", "2354" };
        #endregion

        #region Initialization
        /// <summary>
        /// Configurate the logger before all tests
        /// </summary>
        [TestFixtureSetUp]
        public void Init()
        {
            XmlConfigurator.Configure();
        }

        /// <summary>
        /// Creating the tests directory in if it doesd not exist
        /// </summary>
        /// 
        [SetUp]
        public void CreateTestsDirectory()
        {
            if (!Directory.Exists(TESTS_DIRECTORY))
            {
                Directory.CreateDirectory(TESTS_DIRECTORY);
            }
        }
        #endregion

        #region Handle Invalid Arguments
        [Test]
        public void Check_That_Directory_Is_Created()
        {
            //Arrange

            DeleteDirectory();

            //Act

            FileWriter writer = new FileWriter(TESTS_DIRECTORY, DUMMY_STRING, 2, DUMMY_STRING);

            bool isDirectoyExist = Directory.Exists(TESTS_DIRECTORY);

            writer.Dispose();

            //Assert

            Assert.That(isDirectoyExist, Is.True);

        }

        [Test]
        public void Handle_Null_Directory()
        {
            //Act + Assert

            Assert.Throws<ArgumentNullException>(() => new FileWriter(null, DUMMY_STRING, 2, DUMMY_STRING));

        }

        [Test]
        public void Handle_Zero_Writers()
        {

            Assert.Throws<ArgumentOutOfRangeException>(
                () => new FileWriter(TESTS_DIRECTORY, DUMMY_STRING, 0, DUMMY_STRING));

        }

        [Test]
        public void Handle_Null_File_Name()
        {
            //Act + Assert

            Assert.Throws<ArgumentNullException>(() => new FileWriter(TESTS_DIRECTORY, null, 2, DUMMY_STRING));

        }

        [Test]
        public void Handle_Null_Extension()
        {
            //Act + Assert

            Assert.Throws<ArgumentNullException>(() => new FileWriter(TESTS_DIRECTORY, DUMMY_STRING, 2, null));

        }

        [Test]
        public void Handle_Invalid_Directory_Name()
        {
            //Arrange

            var invalidDirecotPath = String.Concat(@"c:\ishimotto\", Path.GetInvalidPathChars().First(),
                Path.GetInvalidFileNameChars()[0]);




            //Act + Assert

            Assert.Throws<ArgumentException>(() => new FileWriter(invalidDirecotPath, DUMMY_STRING, 1, DUMMY_STRING));

        }

        [Test]
        public void Handle_Null_To_Write()
        {
            //Arrange

            var writer = new FileWriter(TESTS_DIRECTORY, DUMMY_STRING, 1, DUMMY_STRING);


            //Act + Assert

            Assert.Throws<ArgumentNullException>(() => writer.Write((IEnumerable<string>) null));

            AssertFilesDoesNotExists(writer);

        }

        [Test]
        public void Handle_Empty_Enumrable_To_Write()
        {
            //Arrange

            var writer = new FileWriter(TESTS_DIRECTORY, DUMMY_STRING, 1, DUMMY_STRING);


            //Act + Assert

            Assert.DoesNotThrow(() => writer.Write(Enumerable.Empty<string>()));

            writer.Dispose();

            AssertFilesDoesNotExists(writer);



        }
        #endregion

        #region Check Fancuation
        [Test]
        public async void Check_That_File_IsCreated()
        {
            //Arrange

            FileWriter writer = new FileWriter(TESTS_DIRECTORY, DUMMY_STRING, 1, DUMMY_STRING);


            //Act

             writer.Write(THINGS_TO_WRITE);


            var result = writer.FilesPaths.Any(path => !File.Exists(path));

            writer.Dispose();

            //Assert

            Assert.That(result, Is.False);
        }

        [Test]
        public async void Check_That_File_Conatins_Data()
        {
            //Arrange

            var fileWriter = new FileWriter(TESTS_DIRECTORY, DUMMY_STRING, THINGS_TO_WRITE.Length, DUMMY_STRING);

            //Act

             fileWriter.Write(THINGS_TO_WRITE);

            fileWriter.Dispose();

            var values = new List<string>(THINGS_TO_WRITE.Length);

            using (var reader = new StreamReader(fileWriter.FilesPaths.First()))
            {

                while (!reader.EndOfStream)
                {
                    values.Add(reader.ReadLine());
                }

            }


            //Assert

            Assert.That(values, Is.EqualTo(THINGS_TO_WRITE));
        }

        /// <summary>
        /// Checks that all the paths of <see cref="FileWriter.FilesPaths"/> does not exist in <see cref="TESTS_DIRECTORY"/>
        /// </summary>
        /// <param name="writer"></param>
        private  static void AssertFilesDoesNotExists(FileWriter writer)
        {
            foreach (var path in writer.FilesPaths)
            {
                Assert.That(File.Exists(path), Is.False);
            }
        }

        [Test]
        public async void Handle_Existing_File()
        {
            //Arrange

            var filePath = Path.Combine(TESTS_DIRECTORY, DUMMY_STRING + "1." + DUMMY_STRING);

            if (!File.Exists(filePath))
            {
                File.Create(filePath);
            }

            //Act 

            FileWriter writer = new FileWriter(TESTS_DIRECTORY, DUMMY_STRING, 1, DUMMY_STRING);

            //build the path that should be created


             writer.Write(THINGS_TO_WRITE);

            var path = Path.Combine(TESTS_DIRECTORY, DUMMY_STRING + "2." + DUMMY_STRING);

            writer.Dispose();

            //Assert

            Assert.That(File.Exists(path), Is.True);
        }
        #endregion

        #region TearDown
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
        #endregion


    }
}
