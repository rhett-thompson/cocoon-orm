using Cocoon.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cocoon.Tests
{

    [Table]
    internal class Payment
    {

        [Column, PrimaryKey, IgnoreOnInsert, IgnoreOnUpdate, Identity(1, 1)]
        public int PaymentID { get; set; }

        [Column(DataType:"decimal(10,2)"), NotNull]
        public decimal PaymentAmount { get; set; }

        [Column, ForeignKey(typeof(Order)), NotNull]
        public int OrderID { get; set; }

        [Column, IgnoreOnUpdate, NotNull]
        public DateTime CreateDate { get; set; }

    }
}
