using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using NuGet;

namespace Ishimotto.NuGet.Dependencies.Repositories
{
    /// <summary>
    /// An implementation of <see cref="IDependenciesRepostory"/> using MongoDB
    /// </summary>
    class MongoDepndnciesRepository : IDependenciesRepostory
    {
        //Todo: consider moving this class to a diffrent dll, so this dll would not be depndended on Mongo

        #region Data Members
        
        /// <summary>
        /// repositorie's depdendencies
        /// </summary>
        private MongoCollection<PackageDto> mDepndencies;
        
        private object mSyncObject = new object();

        #endregion

        #region Constructors

        /// <summary>
        /// reates new instance of <see cref="MongoDepndnciesRepository"/>
        /// </summary>
        /// <param name="mongoConnection">The connection to the MongoDb</param>
        public MongoDepndnciesRepository(string mongoConnection)
        {
            //Todo: should extract those parameters to configuration, wish I had Infra.Configuraiton
            mDepndencies =
                new MongoClient(mongoConnection).GetServer().GetDatabase("Ishimotto").GetCollection<PackageDto>("Dependencies");

        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Determines wheter a dependency exist in the repository
        /// </summary>
        /// <param name="dependency">The dependency to check</param>
        /// <returns>Boolean indicating if <see cref="dependency"/> exists in the repository</returns>
        public bool IsExist(PackageDto dependency)
        {
            lock (mSyncObject)
            {
                return mDepndencies.AsQueryable().Contains(dependency);
            }

        }


        /// <summary>
        /// Determines whether a depdendency should be download
        /// </summary>
        /// <param name="dependency">Dependency to examine</param>
        /// <returns>Boolean indicating if the <see cref="dependency"/></returns>
        public bool ShouldDownload(PackageDependency dependency)
        {
            lock (mSyncObject)
            {

                //Have to debug this
                return mDepndencies.AsQueryable().Any(package => package.ID == dependency.Id &&
                                                                 !dependency.VersionSpec.Satisfies(package.Version));
            }
        }

        /// <summary>
        /// Adds new depdendencies to the repository
        /// </summary>
        /// <param name="dependencies">The depdendnecies to the repository</param>
        /// <returns>A task to indicate when the process is done</returns>
        public Task AddDepndencies(IEnumerable<PackageDto> dependencies)
        {
            return Task.Run(() =>
            {
                lock (mSyncObject)
                {
                    mDepndencies.InsertBatch(dependencies);
                    
                }
            });

        }

        #endregion
    }
}
