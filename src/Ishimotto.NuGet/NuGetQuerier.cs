using System;
using System.Collections.Generic;
using System.Linq;
using Ishimotto.NuGet.NuGetGallery;

namespace Ishimotto.NuGet
{
    public class NuGetQuerier
    {
        private readonly V2FeedContext mFeedContext;


        public NuGetQuerier(string NuGetUrl)
        {
            mFeedContext = new V2FeedContext(new Uri(NuGetUrl));
        }

        public IEnumerable<V2FeedPackage> FetchFrom(TimeSpan timespan, int pageSize = 40)
        {
            return FetchFrom(DateTime.Now - timespan, pageSize, TimeSpan.FromSeconds(10));
        }

        public IEnumerable<V2FeedPackage> FetchFrom(DateTime startTime, int pageSize = 40)
        {
            return FetchFrom(startTime, pageSize, TimeSpan.FromSeconds(10));
        }

        public IEnumerable<V2FeedPackage> FetchFrom(DateTime startTime, int pageSize, TimeSpan timeout)
        {
            var query =
                from package in mFeedContext.Packages
                where package.Published >= startTime &&
                      !package.IsPrerelease && package.IsLatestVersion
                select package;

            return new NuGetFetcher(query, pageSize, timeout);
        }
        
        public IEnumerable<V2FeedPackage> FetchEverything(int pageSize, TimeSpan timeout)
        {
            var query =
                from package in mFeedContext.Packages
                where package.IsLatestVersion && package.Dependencies.Length > 0
                orderby package.DownloadCount descending 
                select package;

            return new NuGetFetcher(query, pageSize, timeout);
        }

        public IEnumerable<V2FeedPackage> FetchBetween(DateTime startTime, int pageSize = 40)
        {
            return FetchFrom(startTime, pageSize, TimeSpan.FromSeconds(10));
        }

        public IEnumerable<V2FeedPackage> FetchBetween(DateTime startTime, DateTime endTime, int pageSize, TimeSpan timeout)
        {
            var query =
                from package in mFeedContext.Packages
                where package.Published >= startTime &&
                      package.Published <= endTime &&
                      !package.IsPrerelease
                select package;

            return new NuGetFetcher(query, pageSize, timeout);
        }

        public IEnumerable<V2FeedPackage> FetchAll(HashSet<string> dependencies, int pageSize, TimeSpan timeout)
        {
            foreach (string dependency in dependencies)
            {
                var query =
                    from package in mFeedContext.Packages
                    where !package.IsPrerelease &&
                          package.Id == dependency
                    select package;

                foreach (V2FeedPackage v2FeedPackage in new NuGetFetcher(query, pageSize, timeout))
                {
                    yield return v2FeedPackage;
                }
            }
        }
    }
}