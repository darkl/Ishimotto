using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ishimotto.NuGet.NuGetGallery;

namespace Ishimotto.NuGet
{
    public class NuGetFetcher : IEnumerable<V2FeedPackage>
    {
        private readonly int mPageSize;
        private readonly IQueryable<V2FeedPackage> mPackages;
        private readonly TimeSpan mTimeout;

        public NuGetFetcher(IQueryable<V2FeedPackage> packages, int pageSize, TimeSpan timeout)
        {
            mPageSize = pageSize;
            mTimeout = timeout;
            mPackages = packages;
        }

        private IEnumerable<V2FeedPackage> FetchPackages()
        {
            bool stop = false;

            for (int i = 0; !stop; i++)
            {
                IEnumerable<V2FeedPackage> current = GetPage(i);

                stop = true;

                foreach (V2FeedPackage v2FeedPackage in current)
                {
                    stop = false;
                    yield return v2FeedPackage;
                }
            }
        }

        private IEnumerable<V2FeedPackage> GetPage(int number)
        {
            IList<V2FeedPackage> current = null;

            while (current == null)
            {
                try
                {
                    current = TryGetPage(number);
                }
                catch (Exception)
                {
                    current = null;
                }
            }

            return current;
        }

        private IList<V2FeedPackage> TryGetPage(int number)
        {
            IQueryable<V2FeedPackage> packages =
                mPackages.Skip(mPageSize * number).Take(mPageSize);

            var task =
                Task<IList<V2FeedPackage>>.Run(() => packages.ToList());
            

            bool finished =Task.WaitAll(new Task[] { task }, mTimeout);


            if (finished)
            {
                return task.Result;
            }

            throw new TimeoutException();
        }

        public IEnumerator<V2FeedPackage> GetEnumerator()
        {
            return FetchPackages().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}