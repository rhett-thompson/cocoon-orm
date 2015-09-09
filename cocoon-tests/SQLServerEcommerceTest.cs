using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;

namespace Cocoon.Tests
{

    public class SQLServerEcommerceTest : RegressionTest
    {

        private const int dataCount = 5;
        private List<string> testedMethods = new List<string>();
        private uint passed = 0;
        private uint failed = 0;

        public SQLServerEcommerceTest(DBConnection db) : base(db) { }

        public override void generateData()
        {

            Console.WriteLine("*** Generating Test Data ***");

            DateTime startTime = DateTime.Now;

            db.DropTable(typeof(Payment));
            db.DropTable(typeof(OrderLine));
            db.DropTable(typeof(Order));
            db.DropTable(typeof(Customer));
            db.DropTable(typeof(OrderType));

            db.CreateLookupTable(typeof(OrderType));
            db.CreateTable(typeof(Customer));
            db.CreateTable(typeof(Order));
            db.CreateTable(typeof(OrderLine));
            db.CreateTable(typeof(Payment));

            db.ExecuteSQLVoid(@"if exists (select name from sysobjects where name = 'OrderGet') drop procedure OrderGet;");
            db.ExecuteSQLVoid(@"
                create procedure OrderGet 
                    @OrderID as int
                as
                begin

	                set nocount on;
	                select * from [Order] where OrderID = @OrderID

                end
            ");

            db.ExecuteSQLVoid(@"if exists (select name from sysobjects where name = 'OrderList') drop procedure OrderList;");
            db.ExecuteSQLVoid(@"
                create procedure OrderList
                as
                begin

	                set nocount on;
	                select * from [Order]

                end
            ");

            Random rng = new Random();

            for (int i = 1; i <= dataCount; i++)
            {

                Customer newCustomer = null;
                Order newOrder = null;
                OrderLine newOrderLine = null;
                Payment newPayment = null;
 
                try
                {

                    //customer
                    newCustomer = db.Insert<Customer>(new Customer()
                    {
                        WebsiteID = 1,
                        Address1 = "address 1",
                        Address2 = "address 2",
                        City = "city",
                        Country = "country",
                        FirstName = "first name",
                        LastName = "last name",
                        LoginEmail = "email@email.com",
                        Password = string.Format("({0}{0}{0}) {0}{0}{0}-{0}{0}{0}{0}", rng.Next(10)),
                        Phone = "phone",
                        PostalCode = "postal code",
                        State = "state",
                        CreateDate = DateTime.UtcNow
                    });

                    //order
                    newOrder = db.Insert<Order>(new Order()
                    {
                        WebsiteID = 1,
                        CustomerID = newCustomer.CustomerID,
                        OrderTypeID = OrderType.WEBSITE,
                        CreateDate = DateTime.UtcNow

                    });

                    //orderline
                    newOrderLine = db.Insert<OrderLine>(new OrderLine()
                    {
                        OrderID = newOrder.OrderID,
                        SKU = "SKU_" + rng.Next(1000),
                        UnitPrice = (decimal)(rng.NextDouble() * 1000),
                        Quantity = rng.Next(10),
                        CreateDate = DateTime.UtcNow

                    });

                    //payment
                    newPayment = db.Insert<Payment>(new Payment()
                    {
                        OrderID = newOrder.OrderID,
                        PaymentAmount = newOrderLine.UnitPrice * newOrderLine.Quantity,
                        CreateDate = DateTime.UtcNow

                    });

                }
                catch { }

            }

            Console.WriteLine(string.Format("Generated data in {0}ms\r\n\r\n", DateTime.Now.Subtract(startTime).TotalMilliseconds));

        }

        public override void testOutput(string method, string tag, bool condition)
        {

            if (!testedMethods.Contains(method))
                testedMethods.Add(method);

            Console.WriteLine(string.Format("{0} - {1} passed: {2}", method, tag, condition));

            if (condition)
                passed++;
            else
                failed++;

        }

        public override void benchmarkOutput(string tag, double totalTime, double averageTime)
        {
            Console.WriteLine(string.Format("{0} - Total: {1}ms, Average: {2}ms", tag, totalTime, averageTime));
        }

        public override void runTests()
        {

            Console.WriteLine("*** Table Verification ***");

            DateTime startTime = DateTime.Now;

            testOutput("VerifyLookupTable", "OrderType verification", db.VerifyLookupTable(typeof(OrderType)));
            testOutput("VerifyTable", "Customer verification", db.VerifyTable(typeof(Customer)));
            testOutput("VerifyTable", "Order verification", db.VerifyTable(typeof(Order)));
            testOutput("VerifyTable", "OrderLine verification", db.VerifyTable(typeof(OrderLine)));
            testOutput("VerifyTable", "Payment verification", db.VerifyTable(typeof(Payment)));

            Console.WriteLine(string.Format("Table verification complete in {0}ms\r\n\r\n", DateTime.Now.Subtract(startTime).TotalMilliseconds));

            Console.WriteLine("*** Running Tests ***");

            startTime = DateTime.Now;

            Random rng = new Random();

            //GetSingle
            performTest("GetSingle", "", () => { return db.GetSingle<Order>(new { WebsiteID = 1, OrderID = 1 }).OrderID == 1; });

            //Update
            performTest("Update", "", () => {
                Order order = db.GetSingle<Order>(new { WebsiteID = 1, OrderID = 1 });
                order.OrderTypeID = OrderType.PHONE;
                db.Update(order, new { WebsiteID = 1, OrderID = 1 });
                order = db.GetSingle<Order>(new { WebsiteID = 1, OrderID = 1 });
                return order.OrderTypeID == OrderType.PHONE; 
            });

            //GetList
            performTest("GetList", "", () => { return db.GetList<Order>(new { WebsiteID = 1 }).Count == dataCount; });

            //GetScalarList
            performTest("GetScalarList", "", () => { return db.GetScalarList<string>("OrderType").Count == 4; });

            //GetScalar
            performTest("GetScalar", "", () => { return db.GetScalar<string>(typeof(Customer), "LastName", new { WebsiteID = 1, CustomerID = 1 }) == "last name"; });

            //ExecuteSQLSingle
            performTest("ExecuteSQLSingle", "", () => { return db.ExecuteSQLSingle<Order>("select * from [Order] where OrderID = @OrderID", new { WebsiteID = 1, OrderID = 1 }).OrderID == 1; });

            //ExecuteSQLList
            performTest("ExecuteSQLList", "", () => { return db.ExecuteSQLList<Order>("select * from [Order]").Count == dataCount; });

            //ExecuteSQLDataSet
            performTest("ExecuteSQLDataSet", "", () => { return db.ExecuteSQLDataSet("select * from [Order]").Tables[0].Rows.Count == dataCount; });

            //ExecuteSQLScalar
            performTest("ExecuteSQLScalar", "", () => { return db.ExecuteSQLScalar<string>("select LastName from Customer where CustomerID = @CustomerID", new { CustomerID = 1 }) == "last name"; });

            //ExecuteSQLVoid
            performTest("ExecuteSQLVoid", "", () => { db.ExecuteSQLVoid("select * from [Order]"); return true; });

            //ExecuteSProcSingle
            performTest("ExecuteSProcSingle", "", () => { return db.ExecuteSProcSingle<Order>("OrderGet", new { OrderID = 1 }).OrderID == 1; });

            //ExecuteSProcList
            performTest("ExecuteSProcList", "", () => { return db.ExecuteSProcList<Order>("OrderList").Count == dataCount; });

            //ExecuteSProcDataSet
            performTest("ExecuteSProcDataSet", "", () => { return db.ExecuteSProcDataSet("OrderList").Tables[0].Rows.Count == dataCount; });

            //ExecuteSProcScalar
            performTest("ExecuteSProcScalar", "", () => { return db.ExecuteSProcScalar<int>("OrderGet", new { OrderID = 1 }) == 1; });

            //ExecuteSProcVoid
            performTest("ExecuteSProcVoid", "", () => { db.ExecuteSProcVoid("OrderGet", new { OrderID = 1 }); return true; });

            //Delete
            performTest("Delete", "", () =>
            {
                db.Delete(typeof(Payment), new { OrderID = 1 });
                db.Delete(typeof(OrderLine), new { OrderID = 1 });
                db.Delete(typeof(Order), new { WebsiteID = 1, OrderID = 1 });
                return db.GetList<Order>(new { WebsiteID = 1 }).Count == dataCount - 1;
            });

            //DropTable
            performTest("DropTable", "", () => { 
                db.DropTable(typeof(TestTable));
                db.DropTable(typeof(TestLookupTable)); 
                return true; 
            });

            //CreateTable
            performTest("CreateTable", "", () => {
                db.CreateTable(typeof(TestTable));
                return true; });

            //CreateLookupTable
            performTest("CreateLookupTable", "", () =>
            {
                db.CreateLookupTable(typeof(TestLookupTable));
                return true;
            });

            //Insert
            performTest("Insert", "", () => {

                var insert1 = db.Insert<TestTable>(new TestTable() { Prim1 = Guid.NewGuid(), Prim2 = "Prim2", Prim3 = 123, Name = "asd" });

                List<TestTable> tt = new List<TestTable>();
                tt.Add(new TestTable() { Name = "a", Prim1 = Guid.NewGuid(), Prim2 = "a", Prim3 = 1 });
                tt.Add(new TestTable() { Name = "b", Prim1 = Guid.NewGuid(), Prim2 = "b", Prim3 = 2 });
                tt.Add(new TestTable() { Name = "c", Prim1 = Guid.NewGuid(), Prim2 = "c", Prim3 = 3 });

                var insert2 = db.Insert(tt);

                return insert1 != null && insert2.Count() > 0;

            });

            //TableExists
            performTest("TableExists", "", () => {
                return db.TableExists(typeof(TestTable)) && db.TableExists(typeof(TestLookupTable)); 
            });

            //GetCSV
            performTest("GetCSV", "", () =>
            {
                string a = db.GetCSV(db.ExecuteSProcDataSet("OrderList"));
                db.GetCSV<Order>(db.ExecuteSProcList<Order>("OrderList"));
                return true;
            });

            //GetHTML
            performTest("GetHTML", "", () =>
            {
                db.GetHTML(db.ExecuteSProcDataSet("OrderList"));
                db.GetHTML<Order>(db.ExecuteSProcList<Order>("OrderList"));
                return true;
            });

            Console.WriteLine(string.Format("Tests completed in {0}ms. {1} methods passed. {2} methods failed.\r\n\r\n", DateTime.Now.Subtract(startTime).TotalMilliseconds, passed, failed));



        }

        public override void runBenchmark(uint iterations)
        {

            Console.WriteLine("*** Benchmark ***");

            DateTime startTime = DateTime.Now;

            Console.WriteLine(string.Format("{0} iterations.", iterations));

            Console.WriteLine("* Get single row or object *");

            //GetSingle
            performBenchmark("GetSingle", iterations, () => { Order order = db.GetSingle<Order>(new { WebsiteID = 1, OrderID = 2 }); });

            //ExecuteSingle
            performBenchmark("ExecuteSProcSingle", iterations, () => { Order layout = db.ExecuteSProcSingle<Order>("OrderGet", new { OrderID = 2 }); });

            //SQL raw select single
            performBenchmark("Classic SQL Select Single", iterations, () =>
            {

                using (var connection = new SqlConnection(db.ConnectionString))
                using (var cmd = connection.CreateCommand())
                using (var da = new SqlDataAdapter(cmd))
                {

                    //set sql
                    cmd.CommandText = string.Format("select * from [Order] where [Order].OrderID = {0}", 2);

                    //fill data set
                    DataSet ds = new DataSet();
                    da.Fill(ds);

                }

            });

            //Classic Stored Proc Single
            performBenchmark("Classic Stored Proc Single", iterations, () =>
            {
                using (var connection = new SqlConnection(db.ConnectionString))
                using (var cmd = connection.CreateCommand())
                using (var da = new SqlDataAdapter(cmd))
                {

                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandText = "OrderGet";
                    connection.Open();

                    cmd.Parameters.AddWithValue("OrderID", 2);

                    DataSet ds = new DataSet();
                    da.Fill(ds);

                }
            });

            Console.WriteLine("* Get list of rows or list of object *");

            //GetList
            performBenchmark("GetList", iterations, () => { List<Order> orders = db.GetList<Order>(new { WebsiteID = 1, OrderID = 2 }); });

            //Execute
            performBenchmark("ExecuteSProcList", iterations, () => { List<Order> layout = db.ExecuteSProcList<Order>("OrderList"); });

            //SQL raw select list
            performBenchmark("Classic SQL Select List", iterations, () =>
            {

                using (var connection = new SqlConnection(db.ConnectionString))
                using (var cmd = connection.CreateCommand())
                using (var da = new SqlDataAdapter(cmd))
                {

                    //set sql
                    cmd.CommandText = "select * from [Order]";

                    //fill data set
                    DataSet ds = new DataSet();
                    da.Fill(ds);

                }

            });

            //Classic Stored Proc Single
            performBenchmark("Classic Stored Proc List", iterations, () =>
            {
                using (var connection = new SqlConnection(db.ConnectionString))
                using (var cmd = connection.CreateCommand())
                using (var da = new SqlDataAdapter(cmd))
                {

                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandText = "OrderList";
                    connection.Open();

                    DataSet ds = new DataSet();
                    da.Fill(ds);

                }
            });

            Console.WriteLine(string.Format("Benchmark completed in {0}ms\r\n\r\n", DateTime.Now.Subtract(startTime).TotalMilliseconds));

        }

        public override void checkMethodsTested()
        {

            MethodInfo[] methods = db.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance);

            foreach (MethodInfo m in methods)
            {

                if (!testedMethods.Contains(m.Name) && m.Name != "ToString" && m.Name != "Equals" && m.Name != "GetType" && m.Name != "GetHashCode")
                    Console.WriteLine("Didn't test method: " + m.Name);

            }

        }

    }
}
