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
    /// Defines a column that exists in another table model that should be joined to ths table model
    /// </summary>
    public class ForeignColumn : Attribute
    {

        /// <summary>
        /// The name of the foreign key column in this table
        /// </summary>
        public readonly string KeyInThisTableModel;

        /// <summary>
        /// The name of the primary key column in the other table
        /// </summary>
        public readonly string KeyInOtherTableModel;

        /// <summary>
        /// Field to select in the other table
        /// </summary>
        public readonly string FieldInOtherTableModel;

        internal Type otherTableModel;
        internal JoinType joinType;

        /// <summary>
        /// Defines a column that exists in another table model that should be joined to ths table model
        /// </summary>
        /// <param name="KeyInThisTableModel">The name of the foreign key column in this table</param>
        /// <param name="OtherTableModel">The type of the other table model</param>
        /// <param name="FieldInOtherTableModel">Field to select in the other table</param>
        /// <param name="KeyInOtherTableModel">The name of the primary key column in the other table</param>
        /// <param name="JoinType">The type of join to perform</param>
        public ForeignColumn(string KeyInThisTableModel, Type OtherTableModel, string FieldInOtherTableModel = null, string KeyInOtherTableModel = null, JoinType JoinType = JoinType.LEFT)
        {

            if (string.IsNullOrEmpty(KeyInThisTableModel))
                throw new Exception("No foreign key provided.");

            this.KeyInThisTableModel = CocoonORM.getObjectName(KeyInThisTableModel);
            this.KeyInOtherTableModel = CocoonORM.getObjectName(KeyInOtherTableModel ?? KeyInThisTableModel);

            if(FieldInOtherTableModel != null)
                this.FieldInOtherTableModel = CocoonORM.getObjectName(FieldInOtherTableModel);

            otherTableModel = OtherTableModel;
            joinType = JoinType;

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

    /// <summary>
    /// Defines a type of join
    /// </summary>
    public enum JoinType
    {

        /// <summary>
        /// Left join
        /// </summary>
        LEFT,

        /// <summary>
        /// Inner join
        /// </summary>
        INNER,

        /// <summary>
        /// Right join
        /// </summary>
        RIGHT,

        /// <summary>
        /// Full outer join
        /// </summary>
        FULL_OUTER

    }

}
