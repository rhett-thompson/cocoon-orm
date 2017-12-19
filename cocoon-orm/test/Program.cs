using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cocoon.ORM;

namespace test
{
    class Program
    {
        static void Main(string[] args)
        {

            var db = new CocoonORM("Server=(localdb)\\sourcery;Database=sourcery");

            var x = new Test() { PKString = Guid.NewGuid().ToString("n"), PKDateTime = DateTime.UtcNow, Data = DateTime.UtcNow.ToString() };
            db.Insert(x);

            var y = db.GetSingle<Test>(t => t.PKString == x.PKString && t.PKDateTime == x.PKDateTime);

        }

        class Test
        {
            [PrimaryKey]
            public string PKString { get; set; }

            [PrimaryKey]
            public DateTime PKDateTime { get; set; }

            public string Data { get; set; }

        }

    }



}
