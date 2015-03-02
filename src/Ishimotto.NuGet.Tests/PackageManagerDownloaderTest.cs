using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;
using Ishimotto.NuGet.Dependencies;
using Ishimotto.NuGet.Dependencies.Repositories;
using Moq;
using Ninject;
using Ninject.Modules;
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
        //Todo: Arrange configuration (maybe search package like Infra.Configuration

        #region Consts

        /// <summary>
        /// Path to NuGetGallery repository
        /// </summary>
        public const string REMOTE_NUGET_REPOSITORY = @"https://www.nuget.org/api/v2/";

        public const string PACKAGE_ID = "MockPackage";

        public const string DUMMY_DEPENDENCY_ID = "DummyDependency";

        public const string DUMMY_DEPENDENCY_VERSION = "2.6.0.7";

        #endregion

        /// <summary>
        /// The object to test
        /// </summary>
        private DependencyContainer mContainer;

        /// <summary>
        /// Checks that the <see cref="DependencyContainer"/> can handle downloading dependencies of certain package
        /// </summary>
        [Test]
        public async void Test_Get_Dependencies()
        {
            //Arrange
            IEnumerable<PackageDependency> dependencies = BuildDependencies();

            var packageMock = CreatePackageMock(dependencies);

            var remoteRepositoryMock = new Mock<IPackageRepository>();


            var kernel = new StandardKernel();

            kernel.Bind<IPackage>().ToConstant(packageMock.Object);


            foreach (var packageDependency in dependencies)
            {
                var mock = CreatePackageMock(packageDependency);

                remoteRepositoryMock.Setup(r => r.FindPackage(packageDependency.Id)).Returns(mock.Object);
            }


            remoteRepositoryMock.Setup(mock => mock.FindPackage(PACKAGE_ID)).Returns(packageMock.Object);
            var repositoryMock = CreateRepositoryMock();

            mContainer.DependenciesRepostory = repositoryMock.Object;

            kernel.Bind<IDependenciesRepostory>().ToConstant(repositoryMock.Object);

            var container = kernel.Get<DependencyContainer>();

            //Act
            var result = await container.GetDependenciesAsync(PACKAGE_ID);

            //Assert

            repositoryMock.Verify(rep => rep.AddDepndenciesAsync(It.IsAny<IEnumerable<PackageDto>>()), Times.AtLeastOnce());

            repositoryMock.Verify(rep => rep.ShouldDownload(It.IsAny<PackageDependency>()), Times.AtLeast(dependencies.Count()));

            packageMock.VerifyGet(package => package.DependencySets, Times.AtLeastOnce());


            var ids = result.Select(package => package.ID);

            Assert.That(ids, Is.EquivalentTo(dependencies.Select(d => d.Id)));


        }

        private IMock<IPackage> CreatePackageMock(PackageDependency packageDependency)
        {
            var mock = new Mock<IPackage>();

            mock.SetupGet(p => p.Id).Returns(packageDependency.Id);

            return mock;
        }

        /// <summary>
        /// Checks the <see cref="DependencyContainer"/> does not download dependencies that already exist in the <see cref="IDependenciesRepostory"/>
        /// </summary>
        [Test]
        public async void Test_Download_Depndencies_When_Should_Download_Is_False()
        {
            ////Arrange

            //var mockRepository = new Mock<IDependenciesRepostory>();

            //mockRepository.Setup(rep => rep.ShouldDownload(It.IsAny<PackageDependency>())).Returns(false);

            //mContainer.DependenciesRepostory = mockRepository.Object;

            ////Act

            //await mContainer.GetDependenciesAsync(new PackageDto(PACKAGE_WITH_DEPENDENCIES_ID, PACKAGE_VERSION));

            //await mContainer.Dispose();


            ////Assert
            //mockRepository.Verify(rep => rep.AddDependnecyAsync(It.IsAny<PackageDto>()), Times.Exactly(1));

            //mockRepository.Verify(rep => rep.AddDepndenciesAsync(It.IsAny<IEnumerable<PackageDto>>()), Times.Never);

            //AssertExistance(PACKAGE_WITH_DEPENDENCIES_ID);

            //AssertMissing(DEPNDENDCIES_LIST.ToArray());

        }

        #region Private Methods

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
            int count = 1;
            yield return new PackageDependency(DUMMY_DEPENDENCY_ID, new VersionSpec(SemanticVersion.Parse(DUMMY_DEPENDENCY_VERSION)));

            count++;

            yield return new PackageDependency(DUMMY_DEPENDENCY_ID + count, new VersionSpec(SemanticVersion.Parse(DUMMY_DEPENDENCY_VERSION)));

            count++;

            yield return new PackageDependency("Newtonsoft.Json", new VersionSpec(SemanticVersion.Parse(DUMMY_DEPENDENCY_VERSION)));
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

    }
}
