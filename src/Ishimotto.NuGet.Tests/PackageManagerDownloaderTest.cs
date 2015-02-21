﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;
using Ishimotto.NuGet.Dependencies;
using Ishimotto.NuGet.Dependencies.Repositories;
using Moq;
using NuGet;
using NUnit.Framework;

namespace Ishimotto.NuGet.Tests
{

    [TestFixture]
    internal class PackageManagerDownloaderTest
    {

        //Todo: Create simple remote repository

        //Todo: Create tests for MongoRepository (check if staesfy function works, and handle null Version, existance depdendenciex are not being added)

        //Todo: Arrange configuration (maybe search package like Infra.Configuration


        public const string TESTS_DITRECTORY_NAME = @"Tests";

        public const string LOCAL_REPOSITORY_PERFIX = @"Tests\Local";

        public const string REMOTE_DIRECTORY_PERFIX = @"Tests\Remote";

        public const string REMOTE_NUGET_REPOSITORY = @"https://www.nuget.org/api/v2/"; 

        private const string PACKAGE_WITH_DEPENDENCIES_ID = "rx-main";

        private static readonly IEnumerable<string> DEPNDENDCIES_LIST = new[] {"Rx-Interfaces", "Rx-Core", "Rx-Linq", "Rx-PlatformServices"};

        private PackageManagerDownloader mDownloader;
        


        [SetUp]
        public void Init()
        {
            SetupTestDirectory();

            var localRepository = Path.Combine(Environment.CurrentDirectory, LOCAL_REPOSITORY_PERFIX);

            var remoteRepository = REMOTE_NUGET_REPOSITORY;

            mDownloader = new PackageManagerDownloader(remoteRepository, localRepository);
        }

        private static void SetupTestDirectory()
        {
            if (!Directory.Exists(LOCAL_REPOSITORY_PERFIX))
            {
                Directory.CreateDirectory(LOCAL_REPOSITORY_PERFIX);
            }
            else
            {
                Directory.Delete(LOCAL_REPOSITORY_PERFIX, true);

                Directory.CreateDirectory(LOCAL_REPOSITORY_PERFIX);
            }
        }
        
        [Test]
        public async void Test_Simple_Download()
        {
            //Arrange
            var repositoryMock = CreateRepositoryMock();

            mDownloader.DependenciesRepostory = repositoryMock.Object;

            var packagesToDownload = new List<PackageDto>
            {
                new PackageDto("Log4Net", "2.0.3"),
                new PackageDto("NUnit", "2.6.4"),
                
            };

            //Act
            await mDownloader.DownloadPackagesAsync(packagesToDownload);

            await mDownloader.Dispose();

            //Assert
            AssertExistance("NUnit", "log4net");

            repositoryMock.Verify(rep => rep.AddDepndenciesAsync(It.IsAny<IEnumerable<PackageDto>>()),Times.Exactly(1));

        }

        //Todo: When upgrading to C# 6.0 all the calls to this function may return the enumerable and not cast it to array
        private static void AssertExistance(params string[] expectedFiles)
        {

            var localRepository = Path.Combine(Environment.CurrentDirectory, LOCAL_REPOSITORY_PERFIX);

            var files = Directory.GetFiles(LOCAL_REPOSITORY_PERFIX, "*.nupkg", SearchOption.AllDirectories);

            Assert.That(files.Length,Is.EqualTo(expectedFiles.Length), "pakages in repository: " + string.Join(",", files));
            foreach (var expectedFile in expectedFiles)
            {
                Assert.IsTrue(files.Any(filePath => filePath.ToLower().Contains(expectedFile.ToLower())),expectedFile);
            }

        }

        private static void AssertMissing(params string[] expectedFiles)
        {

            var files = Directory.GetFiles(LOCAL_REPOSITORY_PERFIX, "*.nupkg", SearchOption.AllDirectories);
            
            foreach (var expectedFile in expectedFiles)
            {
                Assert.IsFalse(files.Any(file => file.Contains(expectedFile)));
            }

        }

        [Test]
        public async void Test_Dependencies_Download()
        {
            //Arrange
            IEnumerable<PackageDependency> dependencies = BuildDependencies();

            var packageMock = CreatePackageMock(dependencies);

            var repositoryMock = CreateRepositoryMock();

            mDownloader.DependenciesRepostory = repositoryMock.Object;

            //Act
            mDownloader.DownloadDependencies(null, new PackageOperationEventArgs(packageMock.Object, null, null));

            await mDownloader.Dispose();

            //Assert

            repositoryMock.Verify(rep => rep.AddDepndenciesAsync(It.IsAny<IEnumerable<PackageDto>>()), Times.AtLeastOnce());

            repositoryMock.Verify(rep => rep.ShouldDownload(It.IsAny<PackageDependency>()), Times.AtLeast(dependencies.Count()));

            packageMock.VerifyGet(package => package.DependencySets, Times.AtLeastOnce());

            AssertExistance(dependencies.Select(d => d.Id).ToArray());

        }

        private static Mock<IDependenciesRepostory> CreateRepositoryMock()
        {
            var repository = new Mock<IDependenciesRepostory>();

            repository.Setup(repo => repo.ShouldDownload(It.IsAny<PackageDependency>())).Returns(true);
            return repository;
        }

        private Mock<IPackage> CreatePackageMock(IEnumerable<PackageDependencySet> sets)
        {
            var package = new Mock<IPackage>();

            package.SetupGet(p => p.DependencySets).Returns(sets);
            return package;
        }


        private Mock<IPackage> CreatePackageMock(IEnumerable<PackageDependency> depdendencies)
        {
            var package = new Mock<IPackage>();

            package.SetupGet(p => p.DependencySets).Returns(new[] { new PackageDependencySet(new FrameworkName(".NETFramework,Version=v4.5"), depdendencies) });
            return package;
        }

        [Test]
        public async void Test_Download_All_Depndencies()
        {
            //Arrange

            var mockRepository = new Mock<IDependenciesRepostory>();

            mockRepository.Setup(rep => rep.ShouldDownload(It.IsAny<PackageDependency>())).Returns(true);
                
            mDownloader.DependenciesRepostory = mockRepository.Object;

            //Act

            await mDownloader.DownloadPackageAsync(new PackageDto(PACKAGE_WITH_DEPENDENCIES_ID, "2.2.5"));

            await mDownloader.Dispose();


            //Assert
            mockRepository.Verify(rep => rep.ShouldDownload(It.IsAny<PackageDependency>()),Times.AtLeast(4));

            mockRepository.Verify(rep => rep.AddDepndenciesAsync(It.IsAny<IEnumerable<PackageDto>>()), Times.AtLeastOnce);

            AssertExistance(DEPNDENDCIES_LIST.Concat(new [] {PACKAGE_WITH_DEPENDENCIES_ID}).ToArray());
        }

        [Test]
        public void Test_Download_Depndencies_Sets()
        {
            //Arrange

            var dependencies = BuildDependencies();

            var set1 = BuildDependenciesSet(dependencies.Take(dependencies.Count() /2));

            var set2 = BuildDependenciesSet(dependencies.Skip(dependencies.Count() /2));

            var packageMock = CreatePackageMock(new[] {set1, set2});

            var repositoryMock = CreateRepositoryMock();

            mDownloader.DependenciesRepostory = repositoryMock.Object;

            //Act

            mDownloader.DownloadDependencies(null,new PackageOperationEventArgs(packageMock.Object,null,string.Empty));

            //Assert

            AssertExistance(dependencies.Select(dep => dep.Id).ToArray());

        }

        private PackageDependencySet BuildDependenciesSet(IEnumerable<PackageDependency> dependencies)
        {
            return new PackageDependencySet(new FrameworkName(".NETFramework,Version=v4.5"), dependencies);
        }

        private IEnumerable<PackageDependency> BuildDependencies()
        {
            yield return new PackageDependency("log4net", new VersionSpec(SemanticVersion.Parse("2.0.3")));
            yield return new PackageDependency("NUnit", new VersionSpec(SemanticVersion.Parse("2.6.3")));
            yield return new PackageDependency("Newtonsoft.Json", new VersionSpec(SemanticVersion.Parse("6.0.8")));
        }

        [Test]
        public async void Test_Download_Depndencies_When_Should_Download_Is_False()
        {
            //Arrange

            var mockRepository = new Mock<IDependenciesRepostory>();

            mockRepository.Setup(rep => rep.ShouldDownload(It.IsAny<PackageDependency>())).Returns(false);

            mDownloader.DependenciesRepostory = mockRepository.Object;

            //Act

            await mDownloader.DownloadPackageAsync(new PackageDto(PACKAGE_WITH_DEPENDENCIES_ID, "2.2.5"));

            await mDownloader.Dispose();
            

            //Assert
            mockRepository.Verify(rep => rep.AddDependnecyAsync(It.IsAny<PackageDto>()), Times.Exactly(1));

            mockRepository.Verify(rep => rep.AddDepndenciesAsync(It.IsAny<IEnumerable<PackageDto>>()),Times.Never);

            AssertExistance(PACKAGE_WITH_DEPENDENCIES_ID);
           
            AssertMissing(DEPNDENDCIES_LIST.ToArray());
            
        }

        [TearDown]
        public async void TearDown()
        {

            await Task.Delay(TimeSpan.FromSeconds(1));

            Directory.Delete(LOCAL_REPOSITORY_PERFIX);

        }


    }
}