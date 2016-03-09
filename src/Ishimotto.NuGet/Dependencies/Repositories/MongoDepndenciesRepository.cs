using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using MongoDB.Driver.Linq;
using NuGet;

namespace Ishimotto.NuGet.Dependencies.Repositories
{
    /// <summary>
    /// An implementation of <see cref="IDependenciesRepostory"/> using MongoDB
    /// </summary>
    public class MongoDepndenciesRepository : IDependenciesRepostory
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
        /// reates new instance of <see cref="MongoDepndenciesRepository"/>
        /// </summary>
        /// <param name="mongoConnection">The connection to the MongoDb</param>
        public MongoDepndenciesRepository(string mongoConnection,string DbName,string collectionName)
        {
            mDepndencies =
                new MongoClient(mongoConnection).GetServer()
                    .GetDatabase(DbName)
                    .GetCollection<PackageDto>(collectionName);

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
                return mDepndencies.Find(Query.EQ("_id", new BsonString(dependency.FormatPackageID()))).Any();
            }
        }

        /// <summary>
        /// Adds single package to the repository
        /// </summary>
        /// <param name="package">item to add</param>
        /// <returns>Task to indicate when the process is completed</returns>
        public Task AddDependnecyAsync(PackageDto package)
        {
            return Task.Run(() =>
                {
                    {
                        var options = new MongoInsertOptions()
                        {
                        
                            Flags = InsertFlags.ContinueOnError,
                            
                        };
                        lock (mSyncObject)
                        {
                            mDepndencies.Save(package, options);    
                        }
                        
                    }
                }
                );

        }

        /// <summary>
        /// Determines whether a depdendency should be download
        /// </summary>
        /// <param name="dependency">Dependency to examine</param>
        /// <returns>Boolean indicating if the <see cref="dependency"/></returns>
        public bool ShouldDownload(PackageDependency dependency)
        {
            PackageDto[] ids;

            lock (mSyncObject)
            {
                //Have to debug this
                ids = mDepndencies.AsQueryable().Where(package => package.ID == dependency.Id).ToArray();
            }

            return !ids.Any(package => dependency.VersionSpec.Satisfies(package.SemanticVersion));
        }

        /// <summary>
        /// Adds new depdendencies to the repository
        /// </summary>
        /// <param name="dependencies">The depdendnecies to the repository</param>
        /// <returns>A task to indicate when the process is done</returns>
        public Task AddDepndenciesAsync(IEnumerable<PackageDto> dependencies)
        {
            return Task.Run(async () =>
            {
                //mDepndencies.InsertBatch(dependencies);

                foreach (var dependency in dependencies)
                {
                    await AddDependnecyAsync(dependency).ConfigureAwait(false);
                }

            });



        #endregion
        }
    }
}