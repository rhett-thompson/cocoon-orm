using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Transactions;
using System.Linq;

namespace Cocoon.Tests
{

    public class Program
    {

        public static DBConnection db;

        static void Main(string[] args)
        {

            //string connectionString = "Data Source=yawa7e7aq0.database.windows.net,1433;Initial Catalog=edbtest;User ID=edbtest@yawa7e7aq0;Password=EspressoDB123;Connection Timeout=600";
            string connectionString = "Data Source=72.3.204.234,4120;Initial Catalog=424828_espresso_test;User ID=424828_espresso_test;Password=espressoDB90;Connection Timeout=600";
            //string connectionString = "Server=174.143.28.19;Database=424828_edb_mysql;Uid=424828_edb_mysql;Pwd=espressoDB90;";
            db = new DBConnection(connectionString, new Action<string>(log));

            //using (TransactionScope tran = new TransactionScope())
            //{

            //    db.Update(typeof(EDBOrder), new { OrderTypeID = EDBOrderType.ONSITE }, new { OrderID = 2 });

            //    tran.Complete();

            //}

            //SQLServerEcommerceTest regression = new SQLServerEcommerceTest(db);
            //regression.runTests();
            //regression.runBenchmark(100);
            //regression.checkMethodsTested();

            Console.ReadLine();

        }

        private static void output(object msg)
        {

            Console.WriteLine(msg);

        }

        private static void log(string msg)
        {

            //string f = string.Format("./log/log-{0}.txt", DateTime.Now.ToString("MM-dd-yyyy"));
            //File.AppendAllText(f, "---------------\r\n" + DateTime.Now.ToString() + "\r\n---------------\r\n" + msg + "\r\n---------------\r\n\r\n");
            //Console.WriteLine(msg);

        }

    }
}
