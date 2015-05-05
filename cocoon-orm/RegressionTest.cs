using System;
using System.Collections.Generic;
using System.Linq;

namespace Cocoon
{

    /// <summary>
    /// 
    /// </summary>
    public abstract class RegressionTest
    {

        /// <summary>
        /// 
        /// </summary>
        protected DBConnection db;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="db"></param>
        public RegressionTest(DBConnection db)
        {

            this.db = db;

            generateData();

        }

        /// <summary>
        /// 
        /// </summary>
        public abstract void generateData();

        /// <summary>
        /// 
        /// </summary>
        public abstract void runTests();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="iterations"></param>
        public abstract void runBenchmark(uint iterations);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="method"></param>
        /// <param name="tag"></param>
        /// <param name="condition"></param>
        public abstract void testOutput(string method, string tag, bool condition);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tag"></param>
        /// <param name="totalTime"></param>
        /// <param name="averageTime"></param>
        public abstract void benchmarkOutput(string tag, double totalTime, double averageTime);

        /// <summary>
        /// 
        /// </summary>
        public abstract void checkMethodsTested();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tag"></param>
        /// <param name="benchmarkAction"></param>
        /// <param name="itertations"></param>
        public virtual void performBenchmark(string tag, uint itertations, Action benchmarkAction)
        {

            DateTime start = DateTime.Now;
            List<TimeSpan> callTimes = new List<TimeSpan>();
            for (int i = 0; i < itertations; i++)
            {
                DateTime callStart = DateTime.Now;
                benchmarkAction();
                callTimes.Add(DateTime.Now.Subtract(callStart));
            }
            benchmarkOutput(tag, DateTime.Now.Subtract(start).TotalMilliseconds, callTimes.Average(t => t.TotalMilliseconds));

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="method"></param>
        /// <param name="tag"></param>
        /// <param name="testAction"></param>
        public virtual void performTest(string method, string tag, Func<bool> testAction)
        {

            try
            {

                testOutput(method, tag, testAction());

            }
            catch
            {

                testOutput(method, tag, false);

            }

        }

    }
}
