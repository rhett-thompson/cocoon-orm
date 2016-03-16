using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cocoon.ORM;

class Program
{
    static void Main(string[] args)
    {

        CocoonORM db = new CocoonORM("Data Source=172.99.97.188,4120;Initial Catalog=424828_ovgsystem;User ID=424828_ovgsystemuser;Password=Asdf54345tasdf33");

        var a = db.GetList<WorkUnit>(w => w.DistrictID == "1 Portland" || w.DistrictID.Contains("2"));
        //var b = db.GetScalarList<WorkUnit, string>(f => f.DistrictID);
        //var c = db.GetScalar<WorkUnit, string>(f => f.WorkUnitAutoID, w => w.WorkUnitID == 1148);
        //var d = db.Insert(new PO() { Notes = "asdasd", PODate = DateTime.Now, PONum = CocoonORM.getGuid() });
        
    }
}

