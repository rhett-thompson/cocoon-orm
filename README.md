
<img src="https://raw.githubusercontent.com/Guidelinetech/cocoon-orm/master/cocoon_banner.png" width="700">
##
Cocoon ORM is a simple .NET object-relational mapper alternative to Entity Framework or NHibernate. Cocoon ORM supports SQL Server 2008/2012/20014/+, and SQL Azure.  It is an ORM toolset that performs auto mapping, CRUD operations, SQL parameterization, stored procedure parameter mapping, and more. It creates easy to inspect parametrized SQL that is execution plan and cache friendly.  The SQL that Cocoon ORM generates is the same SQL you yourself might write had you the time.  

- Nuget: https://www.nuget.org/packages/cocoon-orm/

### The goals of Cocoon ORM 
- Massivly reduce the amount of code required for database access.  
- Leverage simplicity and elegance.
- Speed up development time.
- Reduce bugs.
- Replace complicated stored procedures with simpler in-code versions.

### Features of Cocoon ORM
- Includes most common database querying tasks in a simple API.
- Very fast and reliable.
- Stored procedures and inline SQL support.
- Requires no special training; examples are plentiful and directly applicable to your code.  
- Includes a model generator to speed up model creation.
- Leverages data attributes similar to that of the Entity Framework and NHibernate in a simpler manner.
- Many utility functions for common database development including: Unique Identifier generation, object mapping, object typing, hashing functions, data encoding and compression.

### Example Models
```cs
class Customer
{
    [Column, PrimaryKey, IgnoreOnInsert, IgnoreOnUpdate]
    public int CustomerID { get; set; }
    
    [Column,]
    public string LoginEmail { get; set; }
    
    [Column]
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
    
    [Column, IgnoreOnInsert, IgnoreOnUpdate]
    public DateTime CreateDate { get; set; }
    
    //an example of lazy loading: loads customer orders when accessed.
    public List<Order> orders
    {
        get
        {
            return Program.db.GetList<Order>(where: o => o.CustomerID == CustomerID });
        }
        set { }
    }
    
}

class Order
{
    [Column, PrimaryKey, IgnoreOnInsert, IgnoreOnUpdate]
    public int OrderID { get; set; }
    
    [Column]
    public int CustomerID { get; set; }
   
    [Column]
    public string OrderTypeID { get; set; }
    
    [Column, IgnoreOnInsert, IgnoreOnUpdate]
    public DateTime CreateDate { get; set; }
    
    public string CustomerFirstName { get; set; }
    
    [Join]
    public static JoinDef[] joins = new JoinDef[] {

        CocoonORM.Join<Order, Customer, string>(customer => customer.FirstName, order => order.CustomerFirstName)
        
    };
    
}
```

## Database connection
You only need to instantiate this one time, prefereably in a central/global area of your application.
```cs
CocoonORM db = new CocoonORM("Server={your server};Database={your database};Uid={user id};Pwd={password};");
```

## Attributes
### Attributes are used to specify columns in models, and define the properties and relationships of columns
| Attribute  | Description |
| ------------- | ------------- |
| Column  | Signifies that a property in this class is mirrored as a field in the database   |
| Join  | This field contains a Join definition; must be a static JoinDef, or IEnumerable<JoinDef>.  |
| PrimaryKey  | Signifies that this column/property is a primary key  |
| IgnoreOnInsert  | Tells Cocoon to ignore this column when inserting objects/records; usually used for columns with default fields or identity columns that are automatically incremented etc.  |
| IgnoreOnUpdate  | Tells Cocoon to ignore this column in update.  |
| IgnoreOnSelect  | Tells Cocoon to ignore this column on data retrieval operations.  |
| OverrideName  | Overrides the name of this column.  |
| AggSQLField  | Signifies that this field should include aggregation SQL (```sql select count(*) from ForeignTable where ForeignTable.Key = ThisTable.ForeignKey```). |

## Foreign Joins
Columns from foreign tables can be joined into a table model on select operations.  Just add a static array/IEnumerable of JoinDef instances and decorate the field with the [Join] attribute.
```cs

[Join]
public static JoinDef[] joins = new JoinDef[] {

    CocoonORM.Join<ThisTable, ForeignTable>(thisTable => thisTable.ForeignKey, foreignTable => foreignTable.PrimaryKey, foreignTable => foreignTable.SourceField, thisTable => thisTable.DestinationField),
    CocoonORM.Join<ThisTable, ForeignTable>(foreignTable => foreignTable.SourceField, thisTable => thisTable.DestinationField)

};

```

## Basic Example
This is a minimal example.
```cs
//Retrieves a single order
Order order = db.GetSingle<Order>(where: o => 0.OrderID == 123);
```

## CRUD Operations
Basic CRUD operations
```cs
//insert a new customer into the database
Customer newCustomer = db.Insert(new Customer() { LoginEmail = "customer@email.com", FirstName = "bob" });

//retrieve a single customer from the database
Customer someCustomer = db.GetSingle<Customer>(where: c => c.CustomerID == newCustomer.CustomerID);

//change the customers last name
someCustomer.LastName = "barker";
db.Update(someCustomer); //no where parameter is needed; Cocoon will automatically use the primary key defined in the model

//delete the customer from the database
db.Delete<Customer>(where: c => c.CustomerID == someCustomer.CustomerID);
```

## Stored Procedures
```cs
//retrieve a single order from a stored procedure
db.ExecuteProcSingle<Order>(procedure: "OrderGet", parameters: new { OrderID = 5 });

//get a list of orders from a stored procedure that returns a list of orders
IEnumerable<Order> listOfOrders = db.ExecuteProcList<Order>(procedure: "OrderList");
```

## Parameterized SQL
```cs
//@OrderID is parameterized from the OrderID in the new { OrderID = 5 }
db.ExecuteSQLSingle<Order>(sql: "select * from Orders where OrderID = @OrderID", parameters: new { OrderID = 5 });

//a basic SQL list
IEnumerable<Order> listOfOrders = db.ExecuteSQLList<Order>(sql: "select * from Orders");
```

## Transactions
Cocoon ORM supports ambient transactions.
You can read more about them here: https://msdn.microsoft.com/en-us/library/System.Transactions(v=vs.110).aspx
```cs
using (TransactionScope tran = new TransactionScope())
{
  db.UpdatePartial<Order>(fieldsToUpdate: new { OrderID = 2 }, where: o => o.OrderTypeID == OrderType.ONSITE);
  tran.Complete();
}
```

## Dynamic queries
The **PredicateBuilder** extension class adds two methods **And** & **Or** to assist in building dynamic where expressions.
```cs
Expression<Func<Customer, bool>> predicates = c => c.FirstName == c.FirstName;//start with an always true/false predicate to get started

if(!string.IsNullOrEmpty(FirstNameTextBox.Text))
    predicates = predicates.And(c => c.FirstName == FirstNameTextBox.Text);

if(!string.IsNullOrEmpty(LastNameTextBox.Text))
    predicates = predicates.And(c => c.LastName == LastNameTextBox.Text);

if(!string.IsNullOrEmpty(LoginEmailTextBox.Text))
    predicates = predicates.And(c => c.LoginEmail == LoginEmailTextBox.Text);

IEnumerable<Customer> list = db.GetList(predicates);
```

## Utility Functions
| Function  | Description |
| ------------- | ------------- |
| GenerateSequentialGuid  | Generates a sequential COMB GUID. |
| GenerateSequentialUID | Generates a sequential Base36 unique identifier. |
| SHA256 | Creates an SHA256 hash of a string. |
| MD5 | Creates an MD5 hash of a string. |
| MD5ListHash | Creates an MD5 hash of a list of objects. |
| CompressString | Compresses a string using GZip. |
| DecompressString | Decompresses a string using GZip. |
| Base36Decode | Decode Base36 string. |
| Base36Encode | Base36 encode a value. |
| Base64Decode | Decode Base64 string. |
| Base64Encode | Base64 encode a string. |

## Model Generator Tool
The model generator is a simple application included with Cocoon ORM to generate C# model classes.  All you need is a connection string to your database

<img src="https://raw.githubusercontent.com/Guidelinetech/cocoon-orm/master/modelgen.png" width="786" height="593">
