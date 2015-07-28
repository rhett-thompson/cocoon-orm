# Cocoon ORM
Cocoon ORM is a simple to use .Net open source alternative to the Entity Framework and nHibernate created for SQL Server 2008/2012/20014/+, SQL Azure (but workable in other T-SQL database environments), and MySQL.  It is an ORM toolset that performs: automapping, auto CRUD (including select joins), auto SQL, auto stored procedure parameter mapping, and more. It creates easy to inspect parametrized SQL that is execution plan and cache friendly.  The SQL that Cocoon ORM generates is the same SQL you yourself might write had you the time.  

Cocoon ORM is for developers who are unwilling to trust the Entity Framework or NHibernate to create and manage their database and database code.  Cocoon ORM developers should be comfortable with SQL Server and MySQL as well.  

NOTE. Some features will be disabled in the MySQL database adapter.

Webpage: http://guidelinetech.github.io/cocoon-orm/

### The goals of Cocoon ORM 
- Leverage simplicity and elegance.
- Save time
- Reduce bugs
- Massivly reduce the amount of code required for database access.  

### Features of Cocoon ORM
- Requires no special training; examples are plentiful and directly applicable to your code today.  
- Includes regression and benchmarking tools for the truly performance paranoid.  
- Directly compare performance between Cocoon dynamic parametrized SQL and that of traditional stored procedure.  
- Leverages data annotations similar to that of the Entity Framework.  
- Can be used in a simplified code first or a concise code second environment.
- Rudementary table generation tools are provided primarily as a time saver; not to remove the need for the developer to understand the underlying database structure.

### Example Models
```cs
[Table]
class Customer
{
    [Column, PrimaryKey, IgnoreOnInsert, IgnoreOnUpdate, Identity(1, 1)]
    public int CustomerID { get; set; }
    [Column, NotNull]
    public string LoginEmail { get; set; }
    [Column, NotNull]
    public string Password { get; set; }
    [Column]
    public string FirstName { get; set; }
    [Column]
    public string LastName { get; set; }
    [Column]
    public string Address1 { get; set; }
    [Column]
    public string Address2 { get; set; }
    [Column]
    public string City { get; set; }
    [Column]
    public string State { get; set; }
    [Column]
    public string PostalCode { get; set; }
    [Column]
    public string Country { get; set; }
    [Column]
    public string Phone { get; set; }
    [Column(DefaultValue: "getutcdate()"), IgnoreOnInsert, IgnoreOnUpdate, NotNull]
    public DateTime CreateDate { get; set; }
    
    //an example of lazy loading: loads customer orders when accessed.
    public List<Order> orders
    {
        get
        {
            return Program.db.GetList<Order>(where: new { CustomerID = CustomerID });
        }
        set { }
    }
    
}

[Table]
class OrderType
{
    [Column("OrderTypeID", "varchar(50)"), PrimaryKey, NotNull]
    public const string INTERNAL = "internal";
    [Column("OrderTypeID")]
    public const string WEBSITE = "website";
    [Column("OrderTypeID")]
    public const string PHONE = "phone";
    [Column("OrderTypeID")]
    public const string ONSITE = "onsite";     
}

[Table]
class Order
{
    [Column, PrimaryKey, IgnoreOnInsert, IgnoreOnUpdate, Identity(1, 1)]
    public int OrderID { get; set; }
    [Column, ForeignKey(typeof(Customer)), NotNull]
    public int CustomerID { get; set; }
    [Column(DataType: "varchar(50)"), ForeignKey(typeof(OrderType)), NotNull]
    public string OrderTypeID { get; set; }
    [Column(DefaultValue: "getutcdate()"), IgnoreOnInsert, IgnoreOnUpdate, NotNull]
    public DateTime CreateDate { get; set; }
    [ForeignColumn(ForeignKey: "CustomerID", ObjectModel:typeof(Customer), OverrideName: "FirstName")]
    public string CustomerFirstName { get; set; }
}
```

## Annotations
#### Annotations are used to specify columns in models, and define the properties and relationships of those columns
| Annotation  | Description |
| ------------- | ------------- |
| Column  | Signifies that a property in this class is mirrored as a field in the database   |
| Table  | Signifies that this class is mirrored in the database as a table  |
| PrimaryKey  | Signifies that this column/property is a primary key  |
| ForeignKey  | Signifies that this column/property is a foreign key  |
| IgnoreOnInsert  | Tells Cocoon to ignore this column when inserting objects/records; usually used for columns with default fields or identity columns that are automatically incremented etc.  |
| IgnoreOnUpdate  | Tells Cocoon to ignore this column in update.  |
| IgnoreOnSelect  | Tells Cocoon to ignore this column on data retrieval operations.  |
| ForeignColumn  | Signifies that this field should be set from a table/object other than this one; based on a foreign key in this class/table.  |
| NotNull  | Signifies that this property/column should not be null.  |
| Identity  | Signifies that this column is an identity column and should be incremented on each insert.  |


## Basic Example
#### This is a minimal example.
```cs
SQLServerAdapter adapter = new SQLServerAdapter();
//MySQLServerAdapter adapter = new MySQLServerAdapter();

DBConnection db = new DBConnection("Server={your server};Database={your database};Uid={user id};Pwd={password};", adapter);

//Retrieves a single order
db.GetList<Order>(where:new { OrderID = 123 });
```

## CRUD Operations
#### Basic CRUD operations
```cs
SQLServerAdapter adapter = new SQLServerAdapter();
//MySQLServerAdapter adapter = new MySQLServerAdapter();

DBConnection db = new DBConnection("Server={your server};Database={your database};Uid={user id};Pwd={password};", adapter);

//insert a new customer into the database
Customer newCustomer = db.Insert<customer>(new Customer() { LoginEmail = "customer@email.com", FirstName = "bob" });

//retrieve a single customer from the database
Customer someCustomer = db.GetSingle<customer>(new { CustomerID = newCustomer.CustomerID });

//change the customers last name
someCustomer.LastName = "barker";
db.Update(someCustomer, where: new { CustomerID = someCustomer.CustomerID });

//delete the customer from the database
db.Delete(typeof(Customer), where:new { CustomerID = someCustomer.CustomerID });
```

## Stored Procedures
```cs
SQLServerAdapter adapter = new SQLServerAdapter();
//MySQLServerAdapter adapter = new MySQLServerAdapter();

DBConnection db = new DBConnection("Server={your server};Database={your database};Uid={user id};Pwd={password};", adapter);

//retrieve a single order from a stored procedure
db.ExecuteSProcSingle<Order>("OrderGet", new { OrderID = 5 });

//get a list of orders from a stored procedure that returns a list of orders
List<Order> listOfOrders = db.ExecuteSProcList<Order>("OrderList");
```

## Parameterized SQL
```cs
SQLServerAdapter adapter = new SQLServerAdapter();
//MySQLServerAdapter adapter = new MySQLServerAdapter();

DBConnection db = new DBConnection("Server={your server};Database={your database};Uid={user id};Pwd={password};", adapter);

//@OrderID is parameterized from the OrderID in the new { OrderID = 5 }
db.ExecuteSQLSingle<Order>("select * from Orders where OrderID = @OrderID", new { OrderID = 5 });

//a basic SQL list
List<Order> listOfOrders = db.ExecuteSQLList<Order>("select * from Orders");
```

## Transactions
Cocoon ORM supports ambient transactions.
You can read more about them here: https://msdn.microsoft.com/en-us/library/System.Transactions(v=vs.110).aspx
```cs
SQLServerAdapter adapter = new SQLServerAdapter();
//MySQLServerAdapter adapter = new MySQLServerAdapter();

DBConnection db = new DBConnection("Server={your server};Database={your database};Uid={user id};Pwd={password};", adapter);
using (TransactionScope tran = new TransactionScope())
{
  db.Update(typeof(Order), where:new { OrderTypeID = OrderType.ONSITE }, new { OrderID = 2 });
  tran.Complete();
}
```

## Utility Functions
| Function  | Description |
| ------------- | ------------- |
| GenerateSequentialGuid  | Generates a sequential COMB GUID.  It's based on the number of 10 nanosecond intervals that have elapsed since 1/1/1990 UTC.   |
| GenerateSequentialUID | Generates a sequential Base36 unique identifier.  It's based on the number of 10 nanosecond intervals that have elapsed since 1/1/1990 UTC.  |
