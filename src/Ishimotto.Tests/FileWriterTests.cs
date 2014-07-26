using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
using Ishimotto.Core;
using log4net.Config;
using NUnit.Framework;

namespace Ishimotto.Tests
{
    [TestFixture]
    public class FileWriterTests
    {
        private const string TESTS_DIRECTORY = @"C:\Ishimotto\Tests\";

        private const string DUMMY_STRING = "DUMMY";

        private static readonly string[] THINGS_TO_WRITE = new[] {"12", "243", "2354"};

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

             Assert.Throws<ArgumentNullException>(() =>  new FileWriter(null, DUMMY_STRING, 2, DUMMY_STRING));
            
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

        public void Check_That_File_IsCreated()
        {
            //Arrange

            FileWriter writer = new FileWriter(TESTS_DIRECTORY, DUMMY_STRING, 1, DUMMY_STRING);


            //Act

            writer.WriteToFiles(THINGS_TO_WRITE);


            var result = writer.FilesPaths.Any(path => !File.Exists(path));

            writer.Dispose();

            //Assert
            
            Assert.That(result, Is.False);

            

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
        public void Check_That_File_Conatins_Data()
        {
            //Arrange

            var fileWriter = new FileWriter(TESTS_DIRECTORY, DUMMY_STRING, 1, DUMMY_STRING);
            
          //Act

            fileWriter.WriteToFiles(THINGS_TO_WRITE);
            
            fileWriter.Dispose();
            var values = File.ReadAllLines(fileWriter.FilesPaths.First());

            //Assert

            Assert.That(values, Is.EqualTo(THINGS_TO_WRITE));
        }


        [Test]
        public void Handle_Null_To_Write()
        {
            //Arrange

            var writer = new FileWriter(TESTS_DIRECTORY, DUMMY_STRING, 1, DUMMY_STRING);


            //Act + Assert

            Assert.DoesNotThrow( () =>writer.WriteToFiles(null));

            var numOfFiles = Directory.GetFiles(TESTS_DIRECTORY).Length;

            //No files should be created
            Assert.That(numOfFiles, Is.EqualTo(0));

        }

        [Test]
        public void Handle_Empty_Enumrable_To_Write()
        {
            //Arrange

            var writer = new FileWriter(TESTS_DIRECTORY, DUMMY_STRING, 1, DUMMY_STRING);


            //Act + Assert

            Assert.DoesNotThrow(() => writer.WriteToFiles(Enumerable.Empty<string>()));

            var numOfFiles = Directory.GetFiles(TESTS_DIRECTORY).Length;

            Assert.That(numOfFiles, Is.EqualTo(0));

        }

        [Test]
        public void Handle_Existing_File()
        {
            //Arrange

            using(var stream = new FileStream(Path.Combine(TESTS_DIRECTORY, DUMMY_STRING + "1." + DUMMY_STRING),FileMode.OpenOrCreate));

            //Act 

            FileWriter writer = new FileWriter(TESTS_DIRECTORY, DUMMY_STRING, 1, DUMMY_STRING);

            //build the path that should be created


            writer.WriteToFiles(THINGS_TO_WRITE);

            var path = Path.Combine(TESTS_DIRECTORY, DUMMY_STRING + "2." + DUMMY_STRING);

            writer.Dispose();

            //Assert

            Assert.That(File.Exists(path),Is.True);
        }


        [TearDown]
        public void DeleteDirectory()
        {
            if (Directory.Exists(TESTS_DIRECTORY))
            {
                Directory.Delete(TESTS_DIRECTORY, true);
            }
        }

        [TestFixtureTearDown]
        public void CleanUp()
        {
            DeleteDirectory();
        }

        
    }
}
