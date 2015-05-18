using Cocoon.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cocoon.Tests
{

    [Table]
    class Product
    {

        [Column, PrimaryKey, IgnoreOnUpdate]
        public Guid ProductID { get; set; }

        [Column, NotNull]
        public string ProductName { get; set; }

        [Column(DefaultValue: "getutcdate()"), IgnoreOnInsert, IgnoreOnUpdate, NotNull]
        public DateTime CreateDate { get; set; }

    }
}
