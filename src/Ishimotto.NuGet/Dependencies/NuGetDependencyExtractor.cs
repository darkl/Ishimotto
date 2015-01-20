using System;
using System.Collections.Generic;

namespace Ishimotto.NuGet.Dependencies
{
    /// <summary>
    /// Helper to get the package depndencies
    /// </summary>
    public class NuGetDependencyExtractor
    {
        //TODO: change it to somthing configurable
        private static string DELIMITER = ":|";

        /// <summary>
        /// Extract <see cref="NuGetDependency"/>
        /// </summary>
        /// <param name="encodedDependencies"><see cref="String"/> representing the package depdendencies</param>
        /// <returns></returns>
        public static IEnumerable<NuGetDependency> Extract(string encodedDependencies)
        {
            if (String.IsNullOrEmpty(encodedDependencies))
            {
               throw new ArgumentNullException("encodedDependencies");
            }

            var stringDependencies = encodedDependencies.Split(new[] {DELIMITER},
                StringSplitOptions.RemoveEmptyEntries);

            foreach (var stringDependency in stringDependencies)
            {
                yield return new NuGetDependency(stringDependency);
            }
        }
    }
}
