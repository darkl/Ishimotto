using System;
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

    /// <summary>
    /// Tests for <see cref="DependencyContainer"/>
    /// </summary>
    [TestFixture]
    internal class PackageManagerDownloaderTest
    {
        //Todo: Create simple remote repository

        //Todo: Arrange configuration (maybe search package like Infra.Configuration

        #region Consts
        /// <summary>
        /// Ralative path to the tests directory to create (creating relativly to <see cref="Environment.CurrentDirectory"/>
        /// </summary>
        public const string TESTS_DITRECTORY_NAME = @"Tests";

        /// <summary>
        /// Ralative path to the localrepository directory to create (creating relativly to <see cref="Environment.CurrentDirectory"/>
        /// </summary>
        public const string LOCAL_REPOSITORY_PERFIX = @"Tests\Local";

        /// <summary>
        /// Ralative path to the remote directory to create (creating relativly to <see cref="Environment.CurrentDirectory"/>
        /// </summary>
        public const string REMOTE_DIRECTORY_PERFIX = @"Tests\Remote";

        /// <summary>
        /// Path to NuGetGallery repository
        /// </summary>
        public const string REMOTE_NUGET_REPOSITORY = @"https://www.nuget.org/api/v2/";

        /// <summary>
        /// Name of package that contains dependencies 
        /// </summary>
        private const string PACKAGE_WITH_DEPENDENCIES_ID = "Microsoft.AspNet.Mvc";

        /// <summary>
        /// The version of <see cref="PACKAGE_WITH_DEPENDENCIES_ID"/>
        /// </summary>
        private const string PACKAGE_VERSION = "5.2.3";

        /// <summary>
        /// Collection of the depndencies of <see cref="PACKAGE_WITH_DEPENDENCIES_ID"/>
        /// </summary>
        private static readonly IEnumerable<string> DEPNDENDCIES_LIST = new[] { "Microsoft.AspNet.WebPages", "Microsoft.Web.Infrastructure", "Microsoft.AspNet.Razor" };
        #endregion

        /// <summary>
        /// The object to test
        /// </summary>
        private DependencyContainer mDownloader;

        #region Initialize Tests

        /// <summary>
        /// Creates necessary repositories and configure the <see cref="mDownloader"/>
        /// </summary>
        [SetUp]
        public void Init()
        {
            SetupTestDirectory();

            var localRepository = Path.Combine(Environment.CurrentDirectory, LOCAL_REPOSITORY_PERFIX);

            var remoteRepository = REMOTE_NUGET_REPOSITORY;

            mDownloader = new DependencyContainer(remoteRepository, localRepository, null);
        }

        /// <summary>
        /// Create the local repository directory
        /// </summary>
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

        #endregion

        /// <summary>
        /// Sanity test, the simplest use case to examine
        /// </summary>
        [Test]
        public async void Test_Simple_Download()
        {
            //Arrange
            //var repositoryMock = CreateRepositoryMock();

            //mDownloader.DependenciesRepostory = repositoryMock.Object;

            //var packagesToDownload = new List<PackageDto>
            //{
            //    new PackageDto("Log4Net", "2.0.3"),
            //    new PackageDto("NUnit", "2.6.4"),
                
            //};

            ////Act
            //await mDownloader.GetDependenciesAsync(packagesToDownload);

            //await mDownloader.Dispose();

            ////Assert
            //AssertExistance("NUnit", "log4net");

            //repositoryMock.Verify(rep => rep.AddDepndenciesAsync(It.IsAny<IEnumerable<PackageDto>>()), Times.Exactly(1));

        }

        /// <summary>
        /// Checks that the <see cref="DependencyContainer"/> can handle downloading dependencies of certain package
        /// </summary>
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
        
        /// <summary>
        /// Checks that the <see cref="DependencyContainer"/> can handle downloading dependencies of dependencies
        /// </summary>
        [Test]
        public async void Test_Download_All_Depndencies()
        {
            //Arrange

            var mockRepository = new Mock<IDependenciesRepostory>();

            mockRepository.Setup(rep => rep.ShouldDownload(It.IsAny<PackageDependency>())).Returns(true);

            mDownloader.DependenciesRepostory = mockRepository.Object;

            //Act

            await mDownloader.GetDependenciesAsync(new PackageDto(PACKAGE_WITH_DEPENDENCIES_ID, PACKAGE_VERSION));

            await mDownloader.Dispose();


            //Assert
            mockRepository.Verify(rep => rep.ShouldDownload(It.IsAny<PackageDependency>()), Times.AtLeast(4));

            mockRepository.Verify(rep => rep.AddDepndenciesAsync(It.IsAny<IEnumerable<PackageDto>>()), Times.AtLeastOnce);

            AssertExistance(DEPNDENDCIES_LIST.Concat(new[] { PACKAGE_WITH_DEPENDENCIES_ID }).ToArray());
        }

        /// <summary>
        /// Checks that the <see cref="DependencyContainer"/> can handle downloading more than one <see cref="PackageDependencySet"/>
        /// </summary>
        [Test]
        public void Test_Download_Depndencies_Sets()
        {
            //Arrange

            var dependencies = BuildDependencies();

            var set1 = BuildDependenciesSet(dependencies.Take(dependencies.Count() / 2));

            var set2 = BuildDependenciesSet(dependencies.Skip(dependencies.Count() / 2));

            var packageMock = CreatePackageMock(new[] { set1, set2 });

            var repositoryMock = CreateRepositoryMock();

            mDownloader.DependenciesRepostory = repositoryMock.Object;

            //Act

            mDownloader.DownloadDependencies(null, new PackageOperationEventArgs(packageMock.Object, null, string.Empty));

            //Assert

            AssertExistance(dependencies.Select(dep => dep.Id).ToArray());

        }

        /// <summary>
        /// Checks the <see cref="DependencyContainer"/> does not download dependencies that already exist in the <see cref="IDependenciesRepostory"/>
        /// </summary>
        [Test]
        public async void Test_Download_Depndencies_When_Should_Download_Is_False()
        {
            //Arrange

            var mockRepository = new Mock<IDependenciesRepostory>();

            mockRepository.Setup(rep => rep.ShouldDownload(It.IsAny<PackageDependency>())).Returns(false);

            mDownloader.DependenciesRepostory = mockRepository.Object;

            //Act

            await mDownloader.GetDependenciesAsync(new PackageDto(PACKAGE_WITH_DEPENDENCIES_ID, PACKAGE_VERSION));

            await mDownloader.Dispose();


            //Assert
            mockRepository.Verify(rep => rep.AddDependnecyAsync(It.IsAny<PackageDto>()), Times.Exactly(1));

            mockRepository.Verify(rep => rep.AddDepndenciesAsync(It.IsAny<IEnumerable<PackageDto>>()), Times.Never);

            AssertExistance(PACKAGE_WITH_DEPENDENCIES_ID);

            AssertMissing(DEPNDENDCIES_LIST.ToArray());

        }

        #region Private Methods
        //Todo: When upgrading to C# 6.0 all the calls to this function may return the enumerable and not cast it to array
        /// <summary>
        /// Checks that packages exists in the local repository
        /// </summary>
        /// <param name="packagesIds">The ids of the packages that soppuse to exist in the repository</param>
        private static void AssertExistance(params string[] packagesIds)
        {
            var files = Directory.GetFiles(LOCAL_REPOSITORY_PERFIX, "*.nupkg", SearchOption.AllDirectories);

            Assert.That(files.Length, Is.EqualTo(packagesIds.Length), "pakages in repository: " + string.Join(",", files.Select(file => file.Substring(file.LastIndexOf(@"\")))));
            foreach (var expectedFile in packagesIds)
            {
                Assert.IsTrue(files.Any(filePath => filePath.ToLower().Contains(expectedFile.ToLower())), "Missing package id: " + expectedFile);
            }

        }

        /// <summary>
        /// Checks that packages missing in the local repository
        /// </summary>
        /// <param name="packagesIds">The ids of the packages that does not soppuse to exist in the repository</param>
        private static void AssertMissing(params string[] packagesIds)
        {

            var files = Directory.GetFiles(LOCAL_REPOSITORY_PERFIX, "*.nupkg", SearchOption.AllDirectories);

            foreach (var expectedFile in packagesIds)
            {
                Assert.IsFalse(files.Any(file => file.Contains(expectedFile)), "The file: {0} does not suppose to exsit in the repository", expectedFile);
            }

        }

        /// <summary>
        /// Creates new <see cref="PackageDependencySet"/>
        /// </summary>
        /// <param name="dependencies"><see cref="PackageDependency"/> to contain inside the <see cref="PackageDependencySet"/></param>
        /// <returns></returns>
        private PackageDependencySet BuildDependenciesSet(IEnumerable<PackageDependency> dependencies)
        {
            return new PackageDependencySet(new FrameworkName(".NETFramework,Version=v4.5"), dependencies);
        }

        /// <summary>
        /// Creates new <see cref="PackageDependency"/>'s
        /// </summary>
        /// <returns>New dependencies</returns>
        private IEnumerable<PackageDependency> BuildDependencies()
        {
            yield return new PackageDependency("log4net", new VersionSpec(SemanticVersion.Parse("2.0.3")));
            yield return new PackageDependency("NUnit", new VersionSpec(SemanticVersion.Parse("2.6.3")));
            yield return new PackageDependency("Newtonsoft.Json", new VersionSpec(SemanticVersion.Parse("6.0.8")));
        }

        /// <summary>
        /// Creates mock for <see cref="IDependenciesRepostory"/>
        /// </summary>
        /// <returns>Mock object, that returns always true on the method ShouldDownload</returns>
        private static Mock<IDependenciesRepostory> CreateRepositoryMock()
        {
            var repository = new Mock<IDependenciesRepostory>();

            repository.Setup(repo => repo.ShouldDownload(It.IsAny<PackageDependency>())).Returns(true);
            return repository;
        }

        /// <summary>
        /// Creates mock for <see cref="IPackage"/>
        /// </summary>
        /// <param name="sets"><see cref=PackageDependencySet""/> to add to the package, those dependencies will be downloaded</param>
        /// <returns>mock of <see cref="IPackage"/> with the embeded <see cref="sets"/></returns>
        private Mock<IPackage> CreatePackageMock(IEnumerable<PackageDependencySet> sets)
        {
            var package = new Mock<IPackage>();

            package.SetupGet(p => p.DependencySets).Returns(sets);
            return package;
        }

        /// <summary>
        /// Creates mock for <see cref="IPackage"/>
        /// </summary>
        /// <param name="depdendencies"><see cref=PackageDependency""/> to add to the package, those dependencies will be downloaded</param>
        /// <returns>mock of <see cref="IPackage"/> with the embeded <see cref="depdendencies"/></returns>
        private Mock<IPackage> CreatePackageMock(IEnumerable<PackageDependency> depdendencies)
        {
            var package = new Mock<IPackage>();

            package.SetupGet(p => p.DependencySets).Returns(new[] { new PackageDependencySet(new FrameworkName(".NETFramework,Version=v4.5"), depdendencies) });
            return package;
        } 
        #endregion

        [TearDown]
        public async void TearDown()
        {

            await Task.Delay(TimeSpan.FromSeconds(1));

            Directory.Delete(LOCAL_REPOSITORY_PERFIX);

        }


    }
}
