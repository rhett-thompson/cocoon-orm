using System.Collections.Generic;
using System.Reflection;

namespace Cocoon
{
    internal class ObjectDefinition
    {

        public string TableName;

        public List<PropertyInfo> primaryKeys = new List<PropertyInfo>();
        public List<PropertyInfo> foreignKeys = new List<PropertyInfo>();
        public List<PropertyInfo> allColumns = new List<PropertyInfo>();
        public List<PropertyInfo> linkedColumns = new List<PropertyInfo>();
        public List<FieldInfo> fields = new List<FieldInfo>();

        public PropertyInfo getForeginKey(string fieldName)
       {

            foreach (PropertyInfo prop in foreignKeys)
                if (DBConnection.getColumnName(prop) == fieldName)
                    return prop;

            return null;

        }

        public bool hasColumn(string columnName)
        {

            foreach (PropertyInfo prop in allColumns)
                if (DBConnection.getColumnName(prop) == columnName)
                    return true;

            return false;

        }

    }

}
