using Cocoon.Annotations;
using System;

namespace Cocoon.Tests
{

    [Table]
    internal class Order
    {

        [Column(IsMultiTenantID: true), IgnoreOnUpdate, NotNull]
        public int WebsiteID { get; set; }

        [Column, PrimaryKey, IgnoreOnInsert, IgnoreOnUpdate, Identity(1, 1)]
        public int OrderID { get; set; }

        [Column, ForeignKey(typeof(Customer)), NotNull]
        public int CustomerID { get; set; }

        [Column(DataType: "varchar(50)"), ForeignKey(typeof(OrderType)), NotNull]
        public string OrderTypeID { get; set; }

        [Column, IgnoreOnUpdate, NotNull]
        public DateTime CreateDate { get; set; }

        [ForeignColumn(ForeignKey: "CustomerID", ObjectModel:typeof(Customer), OverrideName: "FirstName")]
        public string CustomerFirstName { get; set; }

    }
}
