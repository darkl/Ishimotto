using System;
using System.Collections.Generic;
using System.Linq;
using Ishimotto.NuGet.Dependencies;
using NUnit.Framework;

namespace Ishimotto.NuGet.Tests
{
    /// <summary>
    /// Test handling with NuGet dependncies extraction
    /// </summary>
    [TestFixture]
    public class NuGetDependenciesTestser
    {
        #region Tests
        [Test(Description = "Check if the Depnency is built correctly")]
        public void Check_Dependnecies_Constructions()
        {
            //Arrange
            var id = "TempPackage";
            var version = "version";
            string encodedDependency = String.Format("{0}:{1}", id, version);

            //Act

            var dependency = new NuGetDependency(encodedDependency);

            //Assert

            Assert.That(dependency.PackageId, Is.EqualTo(id));
            Assert.That(dependency.Version, Is.EqualTo(version));

        }

        [Test(Description = "Check if the Depdendency constructor can handle null version")]
        public void Check_Null_Version()
        {
            //Arrange
            var id = "TempPackage";

            string encodedDependency = String.Format("{0}:", id);

            //Act
            var dependency = new NuGetDependency(encodedDependency);

            //Assert

            Assert.That(dependency.PackageId, Is.EqualTo(id));
            Assert.That(dependency.Version, Is.EqualTo(string.Empty));

        }

        [Test(Description = "Check if the DepndencyExtractor extract encoded dependencies correctly")]
        public void Check_Dependency_Extractor()
        {
            //Arange
            var dependencies = GetDependencies(7);

            //Act
            var result = NuGetDependencyExtractor.Extract(String.Join("|", dependencies.Select(depndency => depndency.ToString())));

            //Assert
            Assert.That(result, Is.EquivalentTo(dependencies));
        }

        [Test(Description = "Check if the DepndencyExtractor extract encoded dependencies with null version correctly")]
        public void Check_Dependency_Extractor_With_Null_Version()
        {

            //Arange
            var dependencies = GetDependencies(7);

            dependencies.Concat(new[] { new NuGetDependency("package", string.Empty), });

            //Act
            var result = NuGetDependencyExtractor.Extract(String.Join("|", dependencies.Select(depndency => depndency.ToString())));

            //Assert
            Assert.That(result, Is.EquivalentTo(dependencies));
        } 
        #endregion

        /// <summary>
        /// Build collection of <see cref="NuGetDependency"/>
        /// </summary>
        /// <param name="count">How many <see cref="Dependencies"/> should be built</param>
        /// <returns>new collection of <see cref="NuGetDependency"/></returns>
        private IEnumerable<NuGetDependency> GetDependencies(int count)
        {
            return Enumerable.Repeat(new NuGetDependency("package", "version"), count);
        }
    }
}
