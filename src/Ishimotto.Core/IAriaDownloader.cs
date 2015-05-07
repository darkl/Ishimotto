using System.Collections.Generic;

namespace Ishimotto.Core
{
    public interface IAriaDownloader
    {
        /// <summary>
        /// Directory to save the downloads
        /// </summary>
        string DownloadsDirectory { get; }

        /// <summary>
        /// Indicates if the <see cref="AriaDownloader"/> should delete temporary files after using them
        /// </summary>
        bool DeleteTempFiles { get; }

        /// <summary>
        /// Log file to log the aria process, iff the log is String.Empty no log would be saved
        /// </summary>
        string AriaLogPath { get; }

        /// <summary>
        /// The severity of the aria's log to write
        /// </summary>
        AriaSeverity Severity { get; }

        /// <summary>
        /// The max connections to open aganist the NuGet server
        /// </summary>
        uint MaxConnections { get; }

        /// <summary>
        /// Download file from given Url
        /// </summary>
        /// <param name="url">The url of the file to download</param>
        void Download(string url);

        /// <summary>
        /// Add links to download 
        /// </summary>
        /// <remarks>these links will be downloaded when the Download method is invoked</remarks>
        /// <param name="links">A collection of links to add </param>
        void AddLinks(IEnumerable<string> links);

        /// <summary>
        /// Download mulitple files 
        /// </summary>
        /// <param name="urls">The urls of the files to download</param>
        void Download(IEnumerable<string> urls);

        /// <summary>
        /// Downloads all the packages retrieved from the <see cref="AriaDownloader.AddLinks"/> method
        /// </summary>
        void Download();

        /// <summary>
        /// Add link to download
        /// </summary>
        /// <remarks>
        /// this method add link to the <see cref="mLinks"/> collections.
        /// the link will be downloaded when the Download method is invoked
        /// </remarks>
        /// <param name="link"></param>
        void AddLink(string link);
    }
}