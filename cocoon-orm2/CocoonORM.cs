using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace cocoon_orm2
{
    public class CocoonORM
    {

        public static Expression<Func<ModelT, bool>> Field<ModelT>(Expression<Func<ModelT, bool>> fieldToSelect)
        {
            return fieldToSelect;
        }

        public static IEnumerable<ModelT> Select<ModelT>(IEnumerable<Expression<Func<ModelT, bool>>> fields, Expression<Func<ModelT, bool>> where = null)
        {
            return null;
        }

        public static IEnumerable<ModelT> SelectExcept<ModelT>(IEnumerable<Expression<Func<ModelT, bool>>> fieldsToExclude, Expression<Func<ModelT, bool>> where = null)
        {
            return null;
        }

        public static void fgsdfsdf()
        {

            Select(new[] { Field<asd>(x => x.id == "asd"), Field<asd>(x => x.b == 5) });

        }

    }

    public class asd
    {
        public string id { get; set; }
        public string a { get; set; }
        public int b { get; set; }
    }

}
