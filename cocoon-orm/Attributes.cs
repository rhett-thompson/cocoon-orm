﻿using System;

namespace Cocoon.ORM
{

    public class OverrideName : Attribute
    {

        internal string name;
        
        public OverrideName(string Name)
        {

            name = Name;

        }

    }

    public class Table : Attribute
    {

    }

    public class Column : Attribute
    {

    }

    public class ForeignColumn : Attribute
    {

        public readonly string KeyInThisTableModel;
        public readonly string KeyInOtherTableModel;

        internal Type otherTableModel;
        internal JoinType joinType;

        public ForeignColumn(string KeyInThisTableModel, Type OtherTableModel, string KeyInOtherTableModel = null, JoinType JoinType = JoinType.INNER)
        {

            if (string.IsNullOrEmpty(KeyInThisTableModel))
                throw new Exception("No foreign key provided.");

            this.KeyInThisTableModel = CocoonORM.getObjectName(KeyInThisTableModel);
            this.KeyInOtherTableModel = CocoonORM.getObjectName(KeyInOtherTableModel == null ? KeyInThisTableModel : KeyInOtherTableModel);

            otherTableModel = OtherTableModel;
            joinType = JoinType;

        }
        
    }

    public class PrimaryKey : Attribute
    {


    }

    public class IgnoreOnUpdate : Attribute
    {


    }

    public class IgnoreOnInsert : Attribute
    {


    }

    public class IgnoreOnSelect : Attribute
    {


    }

    public enum JoinType
    {

        LEFT,
        INNER,
        RIGHT,
        FULL_OUTER

    }

}