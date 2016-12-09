using System;
using System.Collections.Generic;
using System.Reflection;

namespace Cocoon.ORM
{
    class TableDefinition
    {

        internal List<MemberInfo> columns = new List<MemberInfo>();
        internal List<MemberInfo> primaryKeys = new List<MemberInfo>();

        internal string objectName;
        internal Type type;

    }
}
