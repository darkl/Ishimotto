using System;
using System.Collections.Generic;
using System.Linq;
using Ishimotto.NuGet.NuGetGallery;

namespace Ishimotto.NuGet
{
    public class NuGetQuerier
    {
        private readonly FeedContext_x0060_1 mFeedContext = new FeedContext_x0060_1(new Uri("http://www.nuget.org/api/v2/"));

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
                      !package.IsPrerelease
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
    }
}