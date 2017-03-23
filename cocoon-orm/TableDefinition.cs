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

        internal List<MemberInfo> columns = new List<MemberInfo>();
        internal List<MemberInfo> primaryKeys = new List<MemberInfo>();
        internal List<MemberInfo> customColumns = new List<MemberInfo>();
        internal List<JoinDef> joins = new List<JoinDef>();

        internal string objectName;
        internal Type type;

    }
}
