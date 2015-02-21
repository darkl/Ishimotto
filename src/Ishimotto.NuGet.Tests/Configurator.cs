using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Ishimotto.NuGet.Tests
{
    [SetUpFixture]
    class Configurator
    {
        [SetUp]
        public void CreateRemoteRepository()
        {

            DestroyRpository(PackageManagerDownloaderTest.TESTS_DITRECTORY_NAME);

            Directory.CreateDirectory(PackageManagerDownloaderTest.TESTS_DITRECTORY_NAME);

            SetupRepository();
        }

        private void SetupRepository()
        {
           


            var remoteRepositoryPath = PackageManagerDownloaderTest.REMOTE_DIRECTORY_PERFIX;

            if (Directory.Exists(remoteRepositoryPath))
            {
                DestroyRpository(remoteRepositoryPath);
            }

            Directory.CreateDirectory(remoteRepositoryPath);

            DirectoryInfo binDirectory = new DirectoryInfo(Path.Combine(Environment.CurrentDirectory,"Packages"));

            var filesToCopy = binDirectory.GetFiles("*.nupkg", SearchOption.AllDirectories);

            foreach (var file in filesToCopy)
            {
                file.CopyTo(Path.Combine(Environment.CurrentDirectory, remoteRepositoryPath, file.Name));
            }
        }



        [TearDown]
        private static void DestroyRpository(string repositoryPath)
        {
            
            if (Directory.Exists(repositoryPath))
            {
                Directory.Delete(repositoryPath,true);
            }
        }
    }
}
