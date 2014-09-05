using System;
using System.Collections.Generic;
using Ishimotto.Core;
using Ishimotto.Core.Legacy;
using Ishimotto.NuGet.NuGetGallery;

namespace Ishimotto.NuGet
{
    public class NuGetDownloader : Downloader<V2FeedPackage>
    {
        public NuGetDownloader(IEnumerable<V2FeedPackage> downloadLinks,
                               string downloadPath,
                               int parallelDownloads,
                               int numberOfRetries) :
                                   base(downloadLinks,
                                        downloadPath,
                                        parallelDownloads,
                                        numberOfRetries)
        {
        }

        public static string GetUri(string galleryUrl)
        {
            return galleryUrl.Replace("http://www.nuget.org/packages/", "http://nuget.org/api/v2/package/");
        }

        protected override string GetDownloadLink(V2FeedPackage item)
        {
            return GetUri(item.GalleryDetailsUrl);
        }

        protected override string GetDownloadFileName(V2FeedPackage item)
        {
            string destination = String.Format("{0}.{1}.nupkg", item.Id, item.Version);

            return destination;
        }
    }
}