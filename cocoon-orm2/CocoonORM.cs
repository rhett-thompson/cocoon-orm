using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace cocoon_orm2
{
    public class CocoonORM
    {

        public List<ModelT> Select<ModelT>(IEnumerable<Expression<Func<ModelT, bool>>> fields, Expression<Func<ModelT, bool>> where = null)
        {
            return null;
        }

    }

}
