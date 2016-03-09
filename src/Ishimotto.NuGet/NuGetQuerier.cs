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

        public IEnumerable<V2FeedPackage> FetchFrom(IEnumerable<string> ids, DateTime startTime, int pageSize,
            TimeSpan timeout,bool allowPrerelease)
        {
            var query =
             from package in mFeedContext.Packages
             where package.Published >= startTime &&
                   !package.IsPrerelease && package.IsLatestVersion &&
                   (allowPrerelease || !package.IsPrerelease)&&
                   ids.Contains(package.Id)
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

        /// <summary>
        /// Gets specific packages to download, including prerelease
        /// </summary>
        /// <param name="packagesIds">Packages to fetch</param>
        /// <param name="fetchFrom">Minimum date to fetch from</param>
        /// <param name="pageSize"></param>
        /// <param name="timeout"></param>
        /// <param name="any"></param>
        /// <returns>Enumerable of all desired packages</returns>
        public IEnumerable<V2FeedPackage> FetchSpecificFrom(IEnumerable<string> packagesIds, DateTime fetchFrom, int pageSize, TimeSpan timeout, bool includePreRelease)
        {
            IQueryable<V2FeedPackage> query = null;

            bool queryContainsData = false;

            foreach (var id in packagesIds)
            {
                if (queryContainsData)
                {
                    query = query.Concat(GetQueryForId(fetchFrom,id,includePreRelease));
                }
                else
                {
                    query = GetQueryForId(fetchFrom,id,includePreRelease);
                    queryContainsData = query.Count() >0;
                }
            }

            return new NuGetFetcher(query, pageSize, timeout);
        }

        /// <summary>
        /// Patchi method to avoid using contains which is not supported in v2 of NuGet feed
        /// </summary>
        /// <param name="fetchFrom">The minimum date to fetch the package from</param>
        /// <param name="id">The id of the package to etch</param>
        /// <param name="includePreRelease"></param>
        /// <returns>A query contains the desired package</returns>
        /// <remarks> When NuGet will publish V3 we will be able to use the Contains method and this method will be redunded
        /// </remarks>
        private IQueryable<V2FeedPackage> GetQueryForId(DateTime fetchFrom, string id, bool includePreRelease)
        {
            var query =
                from package in mFeedContext.Packages
                where package.Published >= fetchFrom &&
                      package.Id == id &&
                      package.IsLatestVersion&&
                      package.IsPrerelease == includePreRelease
                      

                select package;
            return query;
        }
    }
}