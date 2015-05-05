using Cocoon.Annotations;

namespace Cocoon.Tests
{
    [Table]
    internal class TestLookupTable
    {
        [Column("LookupID", "varchar(50)"), PrimaryKey, NotNull]
        public const string INTERNAL = "val1";
        [Column("LookupID")]
        public const string WEBSITE = "val2";
        [Column("LookupID")]
        public const string PHONE = "val3";
        [Column("LookupID")]
        public const string ONSITE = "val4";
        
    }
}
