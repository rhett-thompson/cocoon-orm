using System;
using System.Collections.Generic;

namespace Cocoon.ORM
{
    public partial class CocoonORM
    {

        /// <summary>
        /// Inserts a single row into a table
        /// </summary>
        /// <typeparam name="T">Table model to use in the where clause and return</typeparam>
        /// <param name="objectToInsert">Object to insert into the table The table model is inferred from the Type of this object.</param>
        /// <param name="timeout">Timeout in milliseconds of query</param>
        /// <returns>The newly inserted object of type T</returns>
        public T Insert<T>(object objectToInsert, int timeout = -1)
        {

            return Platform.insert<T>(objectToInsert.GetType(), objectToInsert, timeout);

        }

        /// <summary>
        /// Inserts a single object
        /// </summary>
        /// <typeparam name="T">Table model to use in the where clause and return</typeparam>
        /// <param name="objectToInsert">Object to insert into the table The table model is inferred from the Type of this object.</param>
        /// <param name="timeout">Timeout in milliseconds of query</param>
        /// <returns>The newly inserted object of type T</returns>
        public T Insert<T>(T objectToInsert, int timeout = -1)
        {

            return Platform.insert<T>(typeof(T), objectToInsert, timeout);

        }

        public T InsertBulk<T>(object[] objectsToInsert, int timeout = -1)
        {

            return Platform.insertBulk<T>(typeof(T), objectsToInsert, timeout);

        }

        /// <summary>
        /// Inserts a single object
        /// </summary>
        /// <typeparam name="T">Table model to use in the where clause and return</typeparam>
        /// <param name="objectToInsert">Object to insert into the table The table model is inferred from the Type of this object.</param>
        /// <param name="model">Type of model</param>
        /// <param name="timeout">Timeout in milliseconds of query</param>
        /// <returns>The newly inserted object of type T</returns>
        public T Insert<T>(T objectToInsert, Type model, int timeout = -1)
        {

            return Platform.insert<T>(model, objectToInsert, timeout);

        }

        /// <summary>
        /// Inserts a list of objects
        /// </summary>
        /// <typeparam name="T">Table model to use in the where clause and return</typeparam>
        /// <param name="objectsToInsert">Objects to insert into the database</param>
        /// <param name="timeout">Timeout in milliseconds of query</param>
        /// <returns>The newly inserted objects of type T</returns>
        public IEnumerable<T> InsertList<T>(IEnumerable<T> objectsToInsert, int timeout = -1)
        {

            List<T> list = new List<T>();

            foreach (T obj in objectsToInsert)
                list.Add(Insert(obj, timeout));

            return list;

        }

    }
}
