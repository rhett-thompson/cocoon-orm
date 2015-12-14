using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;
using System.Linq.Expressions;
using System.Collections;

namespace Cocoon.Tests
{

    public class Program
    {

        public static DBConnection db;
        
        static void Main(string[] args)
        {
            
            //var db = new DBConnection("Server=174.143.28.19;Database=424828_edb_mysql;Uid=424828_edb_mysql;Pwd=espressoDB90;", new MySQLServerAdapter());
            //var db = new DBConnection("Data Source=72.3.204.234,4120;Initial Catalog=424828_espresso_test;User ID=424828_espresso_test;Password=espressoDB90;Connection Timeout=600");

            Console.WriteLine("SQLServer Regression Test");
            SQLServerEcommerceTest sqlServerTest = new SQLServerEcommerceTest(new DBConnection("Data Source=172.99.97.188,4120;Initial Catalog=424828_espresso_test;User ID=424828_espresso_test;Password=espressoDB90;Connection Timeout=600", new SQLServerAdapter(), new Action<string>(log)));
            sqlServerTest.runTests();
            sqlServerTest.runBenchmark(5);
            sqlServerTest.checkMethodsTested();

            //string connectionString = "Data Source=yawa7e7aq0.database.windows.net,1433;Initial Catalog=edbtest;User ID=edbtest@yawa7e7aq0;Password=EspressoDB123;Connection Timeout=600";

            //using (TransactionScope tran = new TransactionScope())
            //{

            //    db.Update(typeof(EDBOrder), new { OrderTypeID = EDBOrderType.ONSITE }, new { OrderID = 2 });

            //    tran.Complete();

            //}
            Console.ReadLine();

            Console.WriteLine("MySQL Regression Test");
            MySQLEcommerceTest mySqlTest = new MySQLEcommerceTest(new DBConnection("Server=174.143.28.19;Database=424828_edb_mysql;Uid=424828_edb_mysql;Pwd=espressoDB90;", new MySQLServerAdapter(), new Action<string>(log)));
            mySqlTest.runTests();
            mySqlTest.runBenchmark(5);
            mySqlTest.checkMethodsTested();

            Console.WriteLine("Finished");

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
