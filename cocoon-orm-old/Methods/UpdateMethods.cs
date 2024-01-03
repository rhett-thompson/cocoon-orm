using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Cocoon.ORM
{
    public partial class CocoonORM
    {

        /// <summary>
        /// Updates records in a table
        /// </summary>
        /// <typeparam name="T">Table model to use in the where clause</typeparam>
        /// <param name="objectToUpdate">Object to update in the table. The table model is inferred from the Type of this object.</param>
        /// <param name="where">Where expression to use for the query</param>
        /// <param name="timeout">Timeout in milliseconds of query</param>
        /// <returns>The number of records that were affected</returns>
        public int Update<T>(object objectToUpdate, Expression<Func<T, bool>> where = null, int timeout = -1)
        {

            if (objectToUpdate == null)
                throw new NullReferenceException("objectToUpdate cannot be null.");

            TableDefinition def = GetTable(objectToUpdate.GetType());
            
            return Platform.update(Platform.getObjectName(objectToUpdate.GetType()), def.columns.Select(p => new Tuple<PropertyInfo, object>(p, p.GetValue(objectToUpdate))), timeout, where);

        }

        /// <summary>
        /// Updates records in a table
        /// </summary>
        /// <typeparam name="T">Table model to use in the where clause</typeparam>
        /// <param name="objectToUpdate">Object to update in the table. The table model is inferred from T.</param>
        /// <param name="where">Where expression to use for the query</param>
        /// <param name="timeout">Timeout in milliseconds of query</param>
        /// <returns>The number of records that were affected</returns>
        public int Update<T>(T objectToUpdate, Expression<Func<T, bool>> where = null, int timeout = -1)
        {

            return Update((object)objectToUpdate, where, timeout);

        }

        /// <summary>
        /// Update a single field in a table
        /// </summary>
        /// <typeparam name="T">Table model to use</typeparam>
        /// <param name="fieldToUpdate">Expression pick the field in the model to update</param>
        /// <param name="value">The new value of the field</param>
        /// <param name="where">Where expression to use for the query</param>
        /// <param name="timeout">Timeout in milliseconds of query</param>
        /// <returns></returns>
        public int Update<T>(Expression<Func<T, object>> fieldToUpdate, object value, Expression<Func<T, bool>> where = null, int timeout = -1)
        {

            PropertyInfo prop = GetExpressionProp(fieldToUpdate);
            
            return Platform.update(Platform.getObjectName(typeof(T)), new List<Tuple<PropertyInfo, object>>() { new Tuple<PropertyInfo, object>(prop, value) }, timeout, where);

        }

        /// <summary>
        /// Updates multiple fields in a table
        /// </summary>
        /// <typeparam name="T">Table model to use</typeparam>
        /// <param name="fieldsToUpdate">A collection of fields to update with values</param>
        /// <param name="where">Where expression to use for the query</param>
        /// <param name="timeout">Timeout in milliseconds of query</param>
        /// <returns></returns>
        public int Update<T>(UpdateFields<T> fieldsToUpdate, Expression<Func<T, bool>> where = null, int timeout = -1)
        {

            return Platform.update(Platform.getObjectName(typeof(T)), fieldsToUpdate, timeout, where);

        }
        
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class UpdateFields<T> : IEnumerable<Tuple<PropertyInfo, object>>
    {

        internal List<Tuple<PropertyInfo, object>> fields = new List<Tuple<PropertyInfo, object>>();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fieldToUpdate"></param>
        /// <param name="value"></param>
        public UpdateFields<T> Add(Expression<Func<T, object>> fieldToUpdate, object value)
        {

            PropertyInfo prop = CocoonORM.GetExpressionProp(fieldToUpdate);
            
            fields.Add(new Tuple<PropertyInfo, object>(prop, value));

            return this;

        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public IEnumerator<Tuple<PropertyInfo, object>> GetEnumerator()
        {
            return ((IEnumerable<Tuple<PropertyInfo, object>>)fields).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<Tuple<PropertyInfo, object>>)fields).GetEnumerator();
        }
    }

}
