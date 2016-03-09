using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ishimotto.Core;
using Ishimotto.NuGet.Dependencies;
using Moq;
using Ninject;
using Ninject.Infrastructure.Language;
using NUnit.Framework;

namespace Ishimotto.NuGet.Tests
{
    [TestFixture]
    public class NuGetDownloadAsyncTaskTests
    {

        private Mock<INuGetSettings> mSettingsMock;

        private Mock<IAriaDownloader> mDownloader;

        private Mock<IDependenciesContainer> mDependenciesContainerMock;

        [SetUp]
        public void Init()
        {
        
            mDownloader = new Mock<IAriaDownloader>();

            mSettingsMock = new Mock<INuGetSettings>();

            mDependenciesContainerMock = new Mock<IDependenciesContainer>();

        }

        [Test]
        public async void Check_Dependencies_Resolving()
        {
            var source = new PackageDto("source", "1.0.0");

            var dep1 = new PackageDto("dependency", "1.0.0");

            var dep2 = new PackageDto("dependency2", "1.0.0");
            
            mDependenciesContainerMock.Setup(x => x.GetDependenciesAsync(source, It.IsAny<bool>())).ReturnsAsync(new [] {dep1}.ToEnumerable());

            mDependenciesContainerMock.Setup(x => x.GetDependenciesAsync(dep1, It.IsAny<bool>())).ReturnsAsync(new[] { dep2 }.ToEnumerable());
            
            var task = new NuGetDownloadAsyncTask(mSettingsMock.Object, mDependenciesContainerMock.Object,mDownloader.Object);
            
            //Act
            await task.ResolveDependnecies(source).ConfigureAwait(false);

            mDependenciesContainerMock.Verify(x => x.GetDependenciesAsync(It.IsAny<PackageDto>(), It.IsAny<bool>()), Times.Exactly(3));

            mDownloader.Verify(x => x.AddLinks(It.IsAny<IEnumerable<string>>()),Times.Exactly(2));

            
        }
    }
}
