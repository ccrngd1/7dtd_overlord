using System.Configuration.Install;
using System.Diagnostics;
using System.Threading;

namespace lawsoncs.htg.sdtd.AdminServer
{
    using System; 

    class Client : ServiceBase
    {
        private DateTime _currentDate;
        private WorkerClass _mainWorker;

        #region Performance counters
        private PerformanceCounter TransactionsPerSecond { get; set; } 
        #endregion

        #region CounterCategory properties
        private string CounterCategory
        {
            get
            {
                return "SDTDServerMonitor";
            }
        }
        private string CounterCategoryHelp
        {
            get
            {
                return "SDTD Server Monitor";
            }
        }
        #endregion

        private static void Main(string[] args)
        {
#if (DEBUG)
            long i = 0;
            Interlocked.Increment(ref i);
            //uncomment the next line if you don't have the performance counters installed
            //args = new string[] { "/a:install", "/dn:ZirMed: Validator", "/desc:ZirMed Claims Validator - Its the Validator way to do it!", "/sn:ZMValidator", "/starttype:Manual" };
            //args = new string[ ] { "/a:uninstall", "/sn:ZMValidator" };
            args = new[] { "/a:debug" };

            foreach (string t in args)
            {
                Console.WriteLine(t);
            }
#endif

            var startUp = new Client();

            startUp.Execute(args, startUp.OnStart, startUp.OnStop);
        }
        private Client()
        {
            AppDomain.CurrentDomain.UnhandledException +=(CurrentDomain_UnhandledException);

            _currentDate = DateTime.Now; 

            if(log4net.LogManager.GetLogger("log").IsInfoEnabled)
                log4net.LogManager.GetLogger("log").Info("Application Initializing...");

            // Setup our install and uninstall event traps to get performance counter installation and uninstallation.
            ServiceInstalled += (Startup_ServiceInstalled);
            ServiceUninstalled += (Startup_ServiceUninstalled);
        }

        void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var exc = e.ExceptionObject as Exception;

            if (exc == null) return;

            if(log4net.LogManager.GetLogger("log").IsFatalEnabled)
                log4net.LogManager.GetLogger("log").Fatal(string.Format("The following unhandled exception has occurred: {0} - {1}", exc.Message, exc.StackTrace));
        } 

        #region Install and Uninstall Event Handler methods
        /// <summary>
        /// Method used to fire when the application install takes place. Used to perform any task that should happen when an application is installed as a service.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The <see cref="InstallEventArgs"/> object used as part of the installation process.</param>
        void Startup_ServiceInstalled(object sender, InstallEventArgs e)
        {
            //InstallPerformanceCounters();
        }

        /// <summary>
        /// Method used to fire when the application uninstall takes place. Used to perform any task that should happen when an application is uninstalled as a service.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The <see cref="InstallEventArgs"/> object used as part of the installation process.</param>
        void Startup_ServiceUninstalled(object sender, InstallEventArgs e)
        {
            //DeletePerformanceCounters();
        }
        #endregion

        #region OnStart and OnStop service methods
        protected override void OnStart(string[] args)
        {
            if (log4net.LogManager.GetLogger("log").IsInfoEnabled)
                log4net.LogManager.GetLogger("log").Info("Service Starting...");

            _mainWorker = new WorkerClass();  

            _mainWorker.Start();

            if(log4net.LogManager.GetLogger("log").IsInfoEnabled)
                log4net.LogManager.GetLogger("log").Info("Service Started and running...");

            //InitializePerformanceCounters();
        }

         

        protected override void OnStop()
        {
            if(log4net.LogManager.GetLogger("log").IsInfoEnabled)
                log4net.LogManager.GetLogger("log").Info("Service Stop Requested...");

            //ask to stop... this method will not return until all of the child tasks have completed.
            _mainWorker.Stop();
        }
        #endregion

        #region Event Handlers 

        private void InstallPerformanceCounters()
        {
            try
            {
                var counterCreationDataCollection = new CounterCreationDataCollection();
                var transactionsPerSecond = new CounterCreationData("Transactions/Second", "Transactions per second,", PerformanceCounterType.RateOfCountsPerSecond32);
                var totalTransactions = new CounterCreationData("Total Transactions", "Total number of transactions the application has processed since it started,", PerformanceCounterType.NumberOfItems32);
                var dailyTransactions = new CounterCreationData("Daily Transactions", "Total number of transactions the application has processed today,", PerformanceCounterType.NumberOfItems32);
                var runningThreads = new CounterCreationData("Running Threads", "The current number of executing threads,", PerformanceCounterType.NumberOfItems32);

                counterCreationDataCollection.Add(transactionsPerSecond);
                counterCreationDataCollection.Add(totalTransactions);
                counterCreationDataCollection.Add(dailyTransactions);
                counterCreationDataCollection.Add(runningThreads);

                if (!PerformanceCounterCategory.Exists(CounterCategory))
                {
                    PerformanceCounterCategory.Create(CounterCategory, CounterCategoryHelp,
                        PerformanceCounterCategoryType.SingleInstance, counterCreationDataCollection);

                }
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.ToString());
                throw;
            }
        }

        private void DeletePerformanceCounters()
        {
            if (PerformanceCounterCategory.Exists(CounterCategory))
            {
                PerformanceCounterCategory.Delete(CounterCategory);
            }
        }

        private void InitializePerformanceCounters()
        {
            try
            {
                #region Setup the performance counter objects
                // If the custom performance counter exists, go ahead and setup the actual
                //  performance counter objects.  If this 
                if (!PerformanceCounterCategory.Exists(CounterCategory)) return;

                TransactionsPerSecond = new PerformanceCounter(CounterCategory, "Transactions/Second", false) { RawValue = 0 }; 

                #endregion
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.ToString());
                throw;
            }
        } 
        #endregion
    }
}
