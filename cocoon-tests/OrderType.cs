using Cocoon.Annotations;

namespace Cocoon.Tests
{
    [Table]
    internal class OrderType
    {
        [Column("OrderTypeID", "varchar(50)"), PrimaryKey, NotNull]
        public const string INTERNAL = "internal";
        [Column("OrderTypeID")]
        public const string WEBSITE = "website";
        [Column("OrderTypeID")]
        public const string PHONE = "phone";
        [Column("OrderTypeID")]
        public const string ONSITE = "onsite";
        
    }
}
