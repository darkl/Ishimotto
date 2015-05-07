using System;
using System.Linq;
using Ishimotto.NuGet;
using log4net;
using log4net.Config;
using SharpConfig;

namespace Ishimotto.Console
{

    internal class Program
    {
        private static ILog mLogger;

        private static void Main(string[] args)
        {

            System.Console.WriteLine("Ishimotto 1.0 2014-2015");

            InitializeLogger();

        
            mLogger.Info("Start Ishimotto process");
            
            Configuration conf = Configuration.LoadFromFile("Configuration.xml");

            DateTime lastFetchTime = conf["Ishimotto.General"]["LastFetchTime"].GetValue<DateTime>();

            var nugetSettings = new NuGetSettings(conf["Ishimotto.NuGet"]);
            
            var repositoryConf = conf[nugetSettings.DependenciesRepositoryType.Name];

            var repositoryInfo = new DependenciesRepostoryInfo(nugetSettings.DependenciesRepositoryType,repositoryConf.Select(setting => setting.Value).ToArray());
            
            var nugetTask = new NuGetDownloadAsyncTask(nugetSettings, repositoryInfo, lastFetchTime);

            nugetTask.ExecuteAsync().Wait();

            mLogger.Info("Updating LastFetchTime to " + DateTime.Now.ToShortDateString());

            conf["Ishimotto.General"]["LastFetchTime"].SetValue(DateTime.Now.ToShortDateString());

            conf.Save("configuration.xml");
            
            mLogger.Debug("Finish ishimotto process");
        }




        private static void InitializeLogger()
        {
            XmlConfigurator.Configure();

            mLogger = LogManager.GetLogger("Ishimotto.Console.Program");

            AppDomain.CurrentDomain.UnhandledException += LogUnhandledException;

            mLogger.Debug("Start Ishimotto process");
        }

        /// <summary>
        /// Logging the exception before terminating the program
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void LogUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            mLogger.Fatal("An unhandled exception occured", e.ExceptionObject as Exception);
        }



    }
}