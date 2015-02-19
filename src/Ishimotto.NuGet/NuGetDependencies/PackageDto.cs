using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using NuGet;

namespace Ishimotto.NuGet.NuGetDependencies
{
    public class PackageDto
    {
        public PackageDto(PackageDependency package): this(package.Id)
        {
            if (package.VersionSpec.IsMaxInclusive)
            {
                Version = package.VersionSpec.MaxVersion;
            }
        }

        public PackageDto(IPackageName package)
            : this(package.Id,package.Version)
        {
           
        }

        public string ID { get; private set; }

        public SemanticVersion Version { get; private set; }

        public PackageDto(string id, SemanticVersion version):this(id)
        {
            ID = id;
            Version = version;
        }

        public PackageDto(string id, string version):this(id)
        {
            
            //Todo: Handle failed parse
            Version = SemanticVersion.Parse(version);
        }

        private PackageDto(string id)
        {
            ID = id;
        }
    }
}
