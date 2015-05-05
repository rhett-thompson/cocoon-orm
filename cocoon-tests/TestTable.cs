using Cocoon.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cocoon.Tests
{

    [Table]
    class TestTable
    {

        [Column(DefaultValue:"newid()"), PrimaryKey, IgnoreOnInsert, IgnoreOnUpdate]
        public Guid Prim1 { get; set; }
        [Column(DataType:"varchar(50)", DefaultValue: "getdate()"), PrimaryKey, IgnoreOnInsert, IgnoreOnUpdate]
        public string Prim2 { get; set; }
        [Column, PrimaryKey, IgnoreOnInsert, IgnoreOnUpdate, Identity(1,1)]
        public int Prim3 { get; set; }
        [Column]
        public string Name { get; set; }
    }
}
