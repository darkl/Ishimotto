using System;
using System.Reflection;
using Ishimotto.NuGet.Dependencies.Repositories;
using SharpConfig;

namespace Ishimotto.NuGet
{
    public interface IDependenciesRepostoryInfo
    {
        IDependenciesRepostory Build();

         Type RepositoryType { get; set; }

         string [] Properties { get; set; }
    }

    public class DependenciesRepostoryInfo : IDependenciesRepostoryInfo
    {
        public DependenciesRepostoryInfo(Type type, string[] properties)
        {
            RepositoryType = type;
            Properties = properties;
        }

       
        public IDependenciesRepostory Build()
        {
            return Activator.CreateInstance(RepositoryType, Properties) as IDependenciesRepostory;
        }

        public Type RepositoryType
        {
            get;
            set;
        }

        public string[] Properties
        {
            get;
            set;
        }
    }
}