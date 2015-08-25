using System.Collections.Generic;
using System.Reflection;

namespace Cocoon
{

    public class TableDefinition
    {

        public DBConnection connection;

        public string tableName;
        public string objectName;

        public List<PropertyInfo> primaryKeys = new List<PropertyInfo>();
        public List<PropertyInfo> foreignKeys = new List<PropertyInfo>();
        public List<PropertyInfo> allColumns = new List<PropertyInfo>();
        public List<PropertyInfo> linkedColumns = new List<PropertyInfo>();
        public List<PropertyInfo> multiTenantIDColumns = new List<PropertyInfo>();

        public List<FieldInfo> fields = new List<FieldInfo>();
        

        public TableDefinition(DBConnection connection)
        {

            this.connection = connection;

        }

        public PropertyInfo getForeginKey(string fieldName)
       {

            foreach (PropertyInfo prop in foreignKeys)
                if (connection.getColumnName(prop) == fieldName)
                    return prop;

            return null;

        }

        public bool hasColumn(string columnName)
        {

            foreach (PropertyInfo prop in allColumns)
                if (connection.getColumnName(prop) == columnName)
                    return true;

            return false;

        }

    }

}
