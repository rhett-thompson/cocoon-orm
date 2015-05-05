# Cocoon ORM
Cocoon ORM is a simple to use open source alternative to the Entity Framework and nHibernate created for SQL Server 2008/2010/20012/+ and SQL Azure (but workable in other T-SQL database environments).  It is an ORM toolset that performs: automapping, auto CRUD (including joins), auto SQL, auto stored procedure parameter mapping, and more. It creates easy to inspect parametrized SQL that is execution plan and cache friendly.  The SQL that Coocoon ORM generates is the same SQL you yourself might write had you the time.  

Cocoon ORM is for developers who are unwilling to trust the Entity Framework or NHibernate to create and manage their database and database code.  Cocoon ORM developers should be comfortable with SQL Server as well.  

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

## Basic Example
#### This is a minimal example.
```cs
DBConnection db = new DBConnection("Server={your server};Database={your database};Uid={user id};Pwd={password};");
db.GetList<Order>(where:new { OrderID = 123 });
```

## CRUD Operations
#### Basic CRUD operations
```cs
DBConnection db = new DBConnection("Server={your server};Database={your database};Uid={user id};Pwd={password};");
Customer newCustomer = db.Insert<customer>(new Customer() { LoginEmail = "customer@email.com", FirstName = "bob" });
Customer someCustomer = db.GetSingle<customer>(new { CustomerID = newCustomer.CustomerID });
someCustomer.LastName = "barker";
db.Update(someCustomer, where: new { CustomerID = someCustomer.CustomerID });
db.Delete(typeof(Customer), where:new { CustomerID = someCustomer.CustomerID });
```

## Stored Procedures
```cs
DBConnection db = new DBConnection("Server={your server};Database={your database};Uid={user id};Pwd={password};");
db.ExecuteSProcSingle<order>("OrderGet", new { OrderID = 5 });
List<order> listOfOrders = db.ExecuteSProcList<order>("OrderList");
```

## Parameterized SQL
```cs
DBConnection db = new DBConnection("Server={your server};Database={your database};Uid={user id};Pwd={password};");
db.ExecuteSQLSingle<order>("select * from Orders where OrderID = @OrderID", new { OrderID = 5 });
List<order> listOfOrders = db.ExecuteSQLList<order>("select * from Orders");
```

## Transactions
```cs
DBConnection db = new DBConnection("Server={your server};Database={your database};Uid={user id};Pwd={password};");
using (TransactionScope tran = new TransactionScope())
{
  db.Update(typeof(EDBOrder), where:new { OrderTypeID = EDBOrderType.ONSITE }, new { OrderID = 2 });
  tran.Complete();
}
```
