using System;

namespace Cocoon.ORM
{

    /// <summary>
    /// Overwrites the name of an object
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property)]
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
    /// Defines a column in a table model
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class Column : Attribute
    {

    }

    /// <summary>
    /// Defines an aggregator column
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
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
    [AttributeUsage(AttributeTargets.Property)]
    public class PrimaryKey : Attribute
    {


    }

    /// <summary>
    /// Ignore this column during updates
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class IgnoreOnUpdate : Attribute
    {


    }

    /// <summary>
    /// Ignore this column during inserts
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class IgnoreOnInsert : Attribute
    {


    }

    /// <summary>
    /// Ignore this column on selects
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class IgnoreOnSelect : Attribute
    {


    }
    
    /// <summary>
    /// Ignore this field entirely
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class Ignore : Attribute
    {


    }

}
