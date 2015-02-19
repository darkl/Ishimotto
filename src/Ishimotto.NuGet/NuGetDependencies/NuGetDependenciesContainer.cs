using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ishimotto.Core;
using NuGet;

namespace Ishimotto.NuGet.NuGetDependencies
{
//    public class NuGetDependenciesContainer
//    {
//        private INuGetDependenciesRepostory mRepostory;

//        private string mNuGetUrl;

//        public NuGetDependenciesContainer(INuGetDependenciesRepostory repository, string nugetUrl)
//        {
//            mRepostory = repository;

//            mNuGetUrl = nugetUrl;
//        }

//        public Task DownloadDependenciesAsync(IEnumerable<NuGetDependency> dependencies)
//        {
//            var depndenciesToFetch = dependencies.Where(dependency => !mRepostory.IsExist(dependency));

//            return Task.WhenAll(DownloadAsync(depndenciesToFetch), UpdateRepositoryAsync(depndenciesToFetch));
//        }

//        private async Task UpdateRepositoryAsync(IEnumerable<NuGetDependency> depndenciesToFetch)
//        {
//            throw new NotImplementedException();
//        }

//        private async Task DownloadAsync(IEnumerable<NuGetDependency> depndenciesToFetch)
//        {
//            var querier = new NuGetQuerier(mNuGetUrl);

//            var links =
//                querier.FetchSpecific(depndenciesToFetch).Select(package => NuGetDownloader.GetUri(package.GalleryDetailsUrl));

//            AriaDownloader downloader = new AriaDownloader("dd");

//            //TODO:Make downalod method awaitable
//            /*await */
//            downloader.Download(links);
//        }
//    }



public interface INuGetDependenciesRepostory
    {
        bool IsExist(PackageDependency dependency);
        Task AddDepndencies(IEnumerable<PackageDto> items);
    }
}
