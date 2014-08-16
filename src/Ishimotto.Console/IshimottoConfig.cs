using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ishimotto.Core;

namespace Ishimotto.Console
{
    class IshimottoConfig : ConfigurationSection
    {

        public static IshimottoConfig GetConfig()
        {
            return (IshimottoConfig)ConfigurationManager.GetSection("ishimottoConfig") ?? new IshimottoConfig();
        }


        [ConfigurationProperty("DownloadsDirectory")]
        public string DownloadsDirectory
        {
            get
            {
                return this["DownloadsDirectory"].ToString();
            }
        }

        [ConfigurationProperty("DeleteTempFiles")]
        public bool DeleteTempFiles
        {
            get
            {
                return bool.Parse(this["DeleteTempFiles"].ToString());
            }
        }


        [ConfigurationProperty("AriaLogPath")]
        public string AriaLogPath
        {
            get
            {
                return this["AriaLogPath"].ToString();
            }
        }


        [ConfigurationProperty("AriaLogLevel")]
        public AriaSeverity AriaLogLevel
        {
            get
            {
                AriaSeverity severity;

                if (AriaSeverity.TryParse(this["AriaLogLevel"].ToString(), out severity))
                {
                    return severity;
                }

                return AriaSeverity.None;
            }
        }


        [ConfigurationProperty("MaxConnections")]
        public uint MaxConnections
        {
            get
            {
                return uint.Parse(this["MaxConnections"].ToString());
            }
        }

        [ConfigurationProperty("NuGetUrl")]
        public string NuGetUrl
        {
            get
            {
                return this["NuGetUrl"].ToString();
            }
        }


        [ConfigurationProperty("LastFetchFileName")]
        public string LastFetchFileName
        {
            get
            {
                return this["LastFetchFileName"].ToString();
            }
        }

        public DateTime LastFetchTime
        {
            get
            {
                DateTime lastFetchTime;

                var filePath = LastFetchFileName;
                if (!File.Exists(filePath))
                {
                    File.Create(filePath);

                    return DateTime.Now.AddMonths(-1);
                }


                    using (StreamReader reader = new StreamReader(filePath))
                {
                    lastFetchTime = DateTime.Parse(reader.ReadLine());
                }

                return lastFetchTime;
            }
            set
            {

                using (StreamWriter writer = new StreamWriter(LastFetchFileName))
                {
                    writer.Write(value.ToShortDateString());
                }

            }
        }
    }


}
