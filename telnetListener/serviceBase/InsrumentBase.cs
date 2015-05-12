using System;
using System.Diagnostics;

namespace lawsoncs.htg.sdtd.AdminServer
{
    /// <summary>
    /// A generic class used to provide basic instrumentation functionality.
    /// </summary>
    /// <threadsafety>
    /// Public static (Shared in Visual Basic) members of this type are safe for multithreaded operations. Instance members are <b>not</b> guaranteed to be thread-safe.
    /// </threadsafety>
    public class InstrumentationBase
    {
        #region Protected CounterCategory Properties
        /// <summary>
        /// Gets or sets the name of the custom performance counter category used for instrumentation.
        /// </summary>
        protected string CounterCategory
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the description of the custom category.
        /// </summary>
        protected string CounterCategoryHelp
        {
            get;
            set;
        }
        #endregion

        #region Protected PerformanceCounter Properties
        /// <summary>
        /// A <see cref="PerformanceCounterType.RateOfCountsPerSecond32"/> type of <see cref="PerformanceCounter"/> 
        /// </summary>
        public virtual PerformanceCounter TransactionsPerSecond
        {
            get;
            set;
        }
        /// <summary>
        /// A <see cref="PerformanceCounterType.NumberOfItems32"/> type of <see cref="PerformanceCounter"/> 
        /// </summary>
        public virtual PerformanceCounter TotalTransactions
        {
            get;
            set;
        }
        /// <summary>
        /// A <see cref="PerformanceCounterType.NumberOfItems32"/> type of <see cref="PerformanceCounter"/> 
        /// </summary>
        public virtual PerformanceCounter DailyTransactions
        {
            get;
            set;
        }
        /// <summary>
        /// A <see cref="PerformanceCounterType.NumberOfItems32"/> type of <see cref="PerformanceCounter"/> 
        /// </summary>
        public virtual PerformanceCounter RunningThreads
        {
            get;
            set;
        }
        #endregion

        /// <summary>
        /// Instantiates a new instance of the <see cref="InstrumentationBase"/> class.
        /// </summary>
        /// <param name="counterCategory">The name of the custom performance counter category used for instrumentation.</param>
        /// <param name="counterCategoryHelp">A description of the custom category.</param>
        public InstrumentationBase(string counterCategory, string counterCategoryHelp)
        {
            CounterCategory = counterCategory;
            CounterCategoryHelp = counterCategoryHelp;
        }

        /// <summary>
        /// This routine installs the performance counters on the local machine.  Requires administrative privledges to run.
        /// </summary>
        public virtual void InstallPerformanceCounters()
        {
            try
            {
                CounterCreationDataCollection counterCreationDataCollection = new CounterCreationDataCollection();
                CounterCreationData transactionsPerSecond = new CounterCreationData("Transactions/Second", "Transactions per second,", PerformanceCounterType.RateOfCountsPerSecond32);
                CounterCreationData totalTransactions = new CounterCreationData("Total Transactions", "Total number of transactions the application has processed since it started,", PerformanceCounterType.NumberOfItems32);
                CounterCreationData dailyTransactions = new CounterCreationData("Daily Transactions", "Total number of transactions the application has processed today,", PerformanceCounterType.NumberOfItems32);
                CounterCreationData runningThreads = new CounterCreationData("Running Threads", "The current number of executing threads,", PerformanceCounterType.NumberOfItems32);

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
                System.Diagnostics.Trace.TraceError(ex.ToString());
                throw;
            }
        }

        /// <summary>
        /// This routine uninstalls the performance counters on the local machine.  Requires administrative privledges to run.
        /// </summary>
        public virtual void DeletePerformanceCounters()
        {
            if (PerformanceCounterCategory.Exists(CounterCategory))
            {
                PerformanceCounterCategory.Delete(CounterCategory);
            }
        }

        /// <summary>
        /// Initializes the performance counters in the given category.
        /// </summary>
        public virtual void InitializePerformanceCounters()
        {
            try
            {
                #region Setup the performance counter objects
                // If the custom performance counter exists, go ahead and setup the actual
                //  performance counter objects.  If this 
                if (PerformanceCounterCategory.Exists(CounterCategory))
                {
                    TransactionsPerSecond = new PerformanceCounter(CounterCategory, "Transactions/Second", false);
                    TransactionsPerSecond.RawValue = 0;

                    TotalTransactions = new PerformanceCounter(CounterCategory, "Total Transactions", false);
                    TotalTransactions.RawValue = 0;

                    DailyTransactions = new PerformanceCounter(CounterCategory, "Daily Transactions", false);
                    DailyTransactions.RawValue = 0;

                    RunningThreads = new PerformanceCounter(CounterCategory, "Running Threads", false);
                    RunningThreads.RawValue = 0;
                }
                #endregion
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError(ex.ToString());
                throw;
            }

        }
    }
}
