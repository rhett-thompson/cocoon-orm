using System;

namespace Cocoon.Annotations
{

    /// <summary>
    /// This field is a database column.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class Column : Attribute
    {

        /// <summary>
        /// Overrides the name of this column.
        /// </summary>
        public readonly string OverrideName;

        /// <summary>
        /// The SQL datatype of the column.
        /// </summary>
        public readonly string DataType;

        /// <summary>
        /// The default value of the column.
        /// </summary>
        public readonly string DefaultValue;

        /// <summary>
        /// This column is a multi tenant id.
        /// </summary>
        public readonly bool IsMultiTenantID;

        /// <summary>
        /// This field is a database column.
        /// </summary>
        /// <param name="OverrideName">Overrides the name of this column.</param>
        /// <param name="DataType">The SQL datatype of the column.</param>
        /// <param name="DefaultValue">The default value of the column.</param>
        /// <param name="IsMultiTenantID">This column is a multi tenant id.</param>
        public Column(string OverrideName = null, string DataType = null, string DefaultValue = null, bool IsMultiTenantID = false)
        {
            this.OverrideName = OverrideName;
            this.DataType = DataType;
            this.DefaultValue = DefaultValue;
            this.IsMultiTenantID = IsMultiTenantID;
        }

    }

    /// <summary>
    /// This field is an identity column
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class Identity : Attribute
    {

        /// <summary>
        /// Is the value that is used for the very first row loaded into the table.
        /// </summary>
        public readonly int Seed;

        /// <summary>
        /// Is the incremental value that is added to the identity value of the previous row that was loaded.
        /// </summary>
        public readonly int Increment;

        /// <summary>
        /// This field is an identity column
        /// </summary>
        /// <param name="Seed">Is the value that is used for the very first row loaded into the table.</param>
        /// <param name="Increment">Is the incremental value that is added to the identity value of the previous row that was loaded.</param>
        public Identity(int Seed, int Increment)
        {

            this.Seed = Seed;
            this.Increment = Increment;

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

        /// <summary>
        /// The table where the primary key resides
        /// </summary>
        public readonly Type ReferencesTable;

        /// <summary>
        /// Specifiy this if the name of the column in the primary table is different than the foreign key.
        /// </summary>
        public readonly string ReferenceTablePrimaryKeyOverride;

        /// <summary>
        /// This field is a foreign key in the database
        /// </summary>
        /// <param name="ReferencesTable">The table where the primary key resides</param>
        /// <param name="ReferenceTablePrimaryKeyOverride">Specifiy this if the name of the column in the primary table is different than the foreign key.</param>
        public ForeignKey(Type ReferencesTable = null, string ReferenceTablePrimaryKeyOverride = null)
        {

            this.ReferencesTable = ReferencesTable;
            this.ReferenceTablePrimaryKeyOverride = ReferenceTablePrimaryKeyOverride;

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

        /// <summary>
        /// The foreign key in this class to use
        /// </summary>
        public readonly string ForeignKey;

        /// <summary>
        /// Overrides the name of this column.
        /// </summary>
        public readonly string OverrideName;

        /// <summary>
        /// The name of the primary key in the primary table. If null, then the primary key is the same as the foreign key.
        /// </summary>
        public readonly string PrimaryKey;

        internal string tableName;
        internal Type objectModel;
        internal JoinType joinType;

        /// <summary>
        /// A foreign column linked by a foreign key in this class
        /// </summary>
        /// <param name="ForeignKey">The foreign key in this class to use</param>
        /// <param name="TableName">The table to join to.</param>
        /// <param name="OverrideName">Overrides the name of this column.</param>
        /// <param name="PrimaryKey">The name of the primary key in the primary table. If null, then the primary key is the same as the foreign key.</param>
        public ForeignColumn(string ForeignKey, string TableName, string OverrideName = null, string PrimaryKey = null, JoinType JoinType = JoinType.INNER)
        {

            if (string.IsNullOrEmpty(ForeignKey))
                throw new Exception("No foreign key provided.");

            if (string.IsNullOrEmpty(TableName))
                throw new Exception("No table provided.");

            this.ForeignKey = ForeignKey;
            this.OverrideName = OverrideName;
            this.PrimaryKey = PrimaryKey == null ? ForeignKey : PrimaryKey;

            tableName = TableName;
            joinType = JoinType;

        }

        /// <summary>
        /// A foreign column linked by a foreign key in this class
        /// </summary>
        /// <param name="ForeignKey">The foreign key in this class to use</param>
        /// <param name="ObjectModel">The table to join to.</param>
        /// <param name="OverrideName">Overrides the name of this column.</param>
        /// <param name="PrimaryKey">The name of the primary key in the primary table. If null, then the primary key is the same as the foreign key.</param>
        public ForeignColumn(string ForeignKey, Type ObjectModel, string OverrideName = null, string PrimaryKey = null, JoinType JoinType = JoinType.INNER)
        {

            if (string.IsNullOrEmpty(ForeignKey))
                throw new Exception("No foreign key provided.");

            this.ForeignKey = ForeignKey;
            this.OverrideName = OverrideName;
            this.PrimaryKey = PrimaryKey == null ? ForeignKey : PrimaryKey;

            objectModel = ObjectModel;
            joinType = JoinType;

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

    /// <summary>
    /// 
    /// </summary>
    public enum JoinType
    {

        LEFT,
        INNER,
        RIGHT,
        FULL_OUTER

    }

}
