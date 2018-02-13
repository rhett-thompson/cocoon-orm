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

        internal List<PropertyInfo> columns = new List<PropertyInfo>();
        internal List<PropertyInfo> primaryKeys = new List<PropertyInfo>();
        internal List<PropertyInfo> customColumns = new List<PropertyInfo>();
        internal List<Join> joins = new List<Join>();

        internal string objectName;
        internal Type type;

    }
}
