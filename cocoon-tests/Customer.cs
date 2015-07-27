using Cocoon.Annotations;
using System;
using System.Collections.Generic;

namespace Cocoon.Tests
{
    [Table]
    internal class Customer
    {

        [Column, PrimaryKey, IgnoreOnInsert, IgnoreOnUpdate, Identity(1, 1)]
        public int CustomerID { get; set; }

        [Column, NotNull]
        public string LoginEmail { get; set; }

        [Column, NotNull]
        public string Password { get; set; }

        [Column]
        public string FirstName { get; set; }

        [Column]
        public string LastName { get; set; }

        [Column]
        public string Address1 { get; set; }

        [Column]
        public string Address2 { get; set; }

        [Column]
        public string City { get; set; }

        [Column]
        public string State { get; set; }

        [Column]
        public string PostalCode { get; set; }

        [Column]
        public string Country { get; set; }

        [Column]
        public string Phone { get; set; }

        [Column, IgnoreOnUpdate, NotNull]
        public DateTime CreateDate { get; set; }


        public List<Order> orders
        {
            get
            {
                return Program.db.GetList<Order>(where: new { CustomerID = CustomerID });
            }
            set { }
        }

    }
}
