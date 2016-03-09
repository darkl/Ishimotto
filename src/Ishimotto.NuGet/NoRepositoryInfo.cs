using System;
using Ishimotto.NuGet.Dependencies.Repositories;

namespace Ishimotto.NuGet
{
    public class NoRepositoryInfo : IDependenciesRepostoryInfo
    {
        public IDependenciesRepostory Build()
        {
            return new EmptyRepository();
        }

        public Type RepositoryType
        {
            get { return typeof (EmptyRepository); }
            set { throw new NotImplementedException(); }
        }

        public string[] Properties
        {
            get { return new string[0]; }
            set { throw new NotImplementedException(); }
        }
    }
}