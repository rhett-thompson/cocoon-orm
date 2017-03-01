using System;

namespace Cocoon.ORM
{

    /// <summary>
    /// Overwrites the name of an object
    /// </summary>
    public class OverrideName : Attribute
    {

        internal string name;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="Name"></param>
        public OverrideName(string Name)
        {

            name = Name;

        }

    }

    /// <summary>
    /// Defines a table model
    /// </summary>
    public class Table : Attribute
    {

    }

    /// <summary>
    /// Defines a column in a table model
    /// </summary>
    public class Column : Attribute
    {

    }

    /// <summary>
    /// Defines an aggregator column
    /// </summary>
    public class AggSQLColumn : Attribute
    {

        internal string sql;

        /// <summary>
        /// For example: "select count(*) from ForeignTable where ForeignTable.Key = ThisTable.ForeignKey"
        /// </summary>
        /// <param name="SQL"></param>
        public AggSQLColumn(string SQL)
        {

            sql = SQL;

        }

    }
    
    /// <summary>
    /// Defines a column to be a primary key
    /// </summary>
    public class PrimaryKey : Attribute
    {


    }

    /// <summary>
    /// Tells Cocoon to ignore this column during updates
    /// </summary>
    public class IgnoreOnUpdate : Attribute
    {


    }

    /// <summary>
    /// Tells Cocoon to ignore this column during inserts
    /// </summary>
    public class IgnoreOnInsert : Attribute
    {


    }

    /// <summary>
    /// Tells to ignore this column on selects
    /// </summary>
    public class IgnoreOnSelect : Attribute
    {


    }
    
}
