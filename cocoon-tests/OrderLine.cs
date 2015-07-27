using Cocoon.Annotations;
using System;

namespace Cocoon.Tests
{

    [Table]
    internal class OrderLine
    {

        [Column, PrimaryKey, IgnoreOnInsert, IgnoreOnUpdate, Identity(1, 1)]
        public int OrderLineID { get; set; }

        [Column, ForeignKey(typeof(Order)), NotNull]
        public int OrderID { get; set; }

        [Column, ForeignKey, NotNull]
        public string SKU { get; set; }

        [Column(DataType: "decimal(10,2)"), NotNull]
        public decimal UnitPrice { get; set; }

        [Column, NotNull]
        public int Quantity { get; set; }

        [Column, IgnoreOnUpdate, NotNull]
        public DateTime CreateDate { get; set; }

    }
}
