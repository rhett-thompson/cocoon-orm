using System;

namespace Cocoon.Annotations
{

    /// <summary>
    /// This field is a database column.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class Column : Attribute
    {
        public string overrideName;
        public string dataType;
        public string defaultValue;
        public bool isMultiTenantID;

        /// <summary>
        /// This field is a database column.
        /// </summary>
        /// <param name="OverrideName">Overrides the name of this column.</param>
        /// <param name="DataType">The SQL datatype of the column.</param>
        /// <param name="DefaultValue">The default value of the column.</param>
        /// <param name="IsMultiTenantID">This column is a multi tenant id.</param>
        public Column(string OverrideName = null, string DataType = null, string DefaultValue = null, bool IsMultiTenantID = false)
        {
            this.overrideName = OverrideName;
            this.dataType = DataType;
            this.defaultValue = DefaultValue;
            this.isMultiTenantID = IsMultiTenantID;
        }

    }

    /// <summary>
    /// This field is an identity column
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class Identity : Attribute
    {

        public int seed;
        public int increment;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="Seed">Is the value that is used for the very first row loaded into the table.</param>
        /// <param name="Increment">Is the incremental value that is added to the identity value of the previous row that was loaded.</param>
        public Identity(int Seed, int Increment)
        {

            this.seed = Seed;
            this.increment = Increment;

        }

    }

    /// <summary>
    /// This field may not be null
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class NotNull : Attribute
    {

    }

    /// <summary>
    /// This field is a primary key in the database
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class PrimaryKey : Attribute
    {

    }

    /// <summary>
    /// This field is a foreign key in the database
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class ForeignKey : Attribute
    {

        public Type referencesTable;
        public string referenceTablePrimaryKeyOverride;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ReferencesTable"></param>
        /// <param name="ReferenceTablePrimaryKeyOverride"></param>
        public ForeignKey(Type ReferencesTable = null, string ReferenceTablePrimaryKeyOverride = null)
        {

            this.referencesTable = ReferencesTable;
            this.referenceTablePrimaryKeyOverride = ReferenceTablePrimaryKeyOverride;

        }

    }

    /// <summary>
    /// This field should be ignored on inserts (e.g. it's an identity column, or has a default value)
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class IgnoreOnInsert : Attribute
    {


    }

    /// <summary>
    /// This field should be ignored on updates (e.g. it's a primary key)
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class IgnoreOnUpdate : Attribute
    {

    }

    /// <summary>
    /// This field should be ignored on select (i.e. Get methods etc.)
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class IgnoreOnSelect : Attribute
    {

    }

    /// <summary>
    /// This field exists in another table and should be joined by a foreign key.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class ForeignColumn : Attribute
    {

        public string foreignKey;
        public string tableName;
        public string overrideName;
        public string primaryKey;
        public Type objectModel;
 
        /// <summary>
        /// A foreign column linked by a foreign key in this class
        /// </summary>
        /// <param name="ForeignKey">The foreign key in this class to use</param>
        /// <param name="TableName">The table to join to.</param>
        /// <param name="OverrideName">Overrides the name of this column.</param>
        /// <param name="PrimaryKey">The name of the primary key in the primary table. If null, then the primary key is the same as the foreign key.</param>
        public ForeignColumn(string ForeignKey, string TableName, string OverrideName = null, string PrimaryKey = null)
        {

            if (string.IsNullOrEmpty(ForeignKey))
                throw new Exception("No foreign key provided.");

            if (string.IsNullOrEmpty(TableName))
                throw new Exception("No table provided.");

            this.foreignKey = ForeignKey;
            this.tableName = TableName;
            this.overrideName = OverrideName;
            this.primaryKey = PrimaryKey == null ? ForeignKey : PrimaryKey;

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ForeignKey"></param>
        /// <param name="ObjectModel"></param>
        /// <param name="OverrideName"></param>
        /// <param name="PrimaryKey"></param>
        public ForeignColumn(string ForeignKey, Type ObjectModel, string OverrideName = null, string PrimaryKey = null)
        {

            if (string.IsNullOrEmpty(ForeignKey))
                throw new Exception("No foreign key provided.");

            this.foreignKey = ForeignKey;
            this.objectModel = ObjectModel;
            this.overrideName = OverrideName;
            this.primaryKey = PrimaryKey == null ? ForeignKey : PrimaryKey;

        }

    }

    /// <summary>
    /// Designates this class as existing in the database
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class Table : Attribute
    {

        internal string tableName;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="TableName">The name of the table in the database.  If null the name of the class is used.</param>
        public Table(string TableName = null)
        {

            this.tableName = TableName;

        }

    }


}
