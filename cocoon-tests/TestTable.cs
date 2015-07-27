using Cocoon.Annotations;
using System;

namespace Cocoon.Tests
{

    [Table]
    class TestTable
    {

        [Column, PrimaryKey, IgnoreOnUpdate]
        public Guid Prim1 { get; set; }
        [Column(DataType:"varchar(50)"), PrimaryKey, IgnoreOnUpdate]
        public string Prim2 { get; set; }
        [Column, PrimaryKey, IgnoreOnUpdate]
        public int Prim3 { get; set; }
        [Column]
        public string Name { get; set; }
    }
}
