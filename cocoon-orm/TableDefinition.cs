using System;
using System.Collections.Generic;
using System.Reflection;

namespace Cocoon.ORM
{

    /// <summary>
    /// 
    /// </summary>
    public class TableDefinition
    {

        public List<PropertyInfo> columns = new List<PropertyInfo>();
        public List<PropertyInfo> primaryKeys = new List<PropertyInfo>();
        public List<PropertyInfo> customColumns = new List<PropertyInfo>();
        public List<Join> joins = new List<Join>();

        public string objectName;
        public Type type;

    }
}
