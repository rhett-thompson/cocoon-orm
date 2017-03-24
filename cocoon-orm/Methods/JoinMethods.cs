using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Cocoon.ORM
{

    public partial class CocoonORM
    {

        /// <summary>
        /// Defines a JOIN operation.
        /// </summary>
        /// <typeparam name="LeftTableModelT"></typeparam>
        /// <typeparam name="RightTableModelT"></typeparam>
        /// <param name="foreignKey"></param>
        /// <param name="primaryKey"></param>
        /// <param name="fieldToSelect"></param>
        /// <param name="fieldToRecieve"></param>
        /// <param name="joinType"></param>
        /// <returns></returns>
        public static JoinDef Join<LeftTableModelT, RightTableModelT>(
            Expression<Func<LeftTableModelT, object>> foreignKey,
            Expression<Func<RightTableModelT, object>> primaryKey,
            Expression<Func<RightTableModelT, object>> fieldToSelect,
            Expression<Func<LeftTableModelT, object>> fieldToRecieve,
            JoinType joinType = JoinType.LEFT)
        {

            return new JoinDef()
            {

                RightTable = typeof(RightTableModelT),
                LeftKey = GetExpressionProp(foreignKey),
                RightKey = GetExpressionProp(primaryKey),
                FieldToSelect = GetExpressionProp(fieldToSelect),
                FieldToRecieve = GetExpressionProp(fieldToRecieve),
                JoinType = joinType

            };

        }

        /// <summary>
        /// Defines a JOIN operation.
        /// </summary>
        /// <typeparam name="LeftTableModelT"></typeparam>
        /// <typeparam name="RightTableModelT"></typeparam>
        /// <param name="fieldToSelect"></param>
        /// <param name="fieldToRecieve"></param>
        /// <param name="joinType"></param>
        /// <returns></returns>
        public static JoinDef Join<LeftTableModelT, RightTableModelT>(
            Expression<Func<RightTableModelT, object>> fieldToSelect,
            Expression<Func<LeftTableModelT, object>> fieldToRecieve,
            JoinType joinType = JoinType.LEFT)
        {

            Type leftType = typeof(LeftTableModelT);
            Type rightType = typeof(RightTableModelT);

            PropertyInfo leftPrimaryKey = leftType.GetProperties().Where(p => Utilities.HasAttribute<PrimaryKey>(p)).FirstOrDefault();
            PropertyInfo rightPrimaryKey = rightType.GetProperties().Where(p => Utilities.HasAttribute<PrimaryKey>(p)).FirstOrDefault();

            if (leftPrimaryKey == null)
                throw new InvalidMemberException("Left table missing primary key attribute", leftPrimaryKey);

            if (rightPrimaryKey == null)
                throw new InvalidMemberException("Right table missing primary key attribute", rightPrimaryKey);

            return new JoinDef()
            {

                RightTable = typeof(RightTableModelT),
                LeftKey = leftPrimaryKey,
                RightKey = rightPrimaryKey,
                FieldToSelect = GetExpressionProp(fieldToSelect),
                FieldToRecieve = GetExpressionProp(fieldToRecieve),
                JoinType = joinType

            };

        }

        /// <summary>
        /// Defines a JOIN operation.
        /// </summary>
        /// <typeparam name="LeftTableModelT"></typeparam>
        /// <typeparam name="RightTableModelT"></typeparam>
        /// <param name="foreignKey"></param>
        /// <param name="fieldToSelect"></param>
        /// <param name="fieldToRecieve"></param>
        /// <param name="joinType"></param>
        /// <returns></returns>
        public static JoinDef Join<LeftTableModelT, RightTableModelT>(
            Expression<Func<LeftTableModelT, object>> foreignKey,
            Expression<Func<RightTableModelT, object>> fieldToSelect,
            Expression<Func<LeftTableModelT, object>> fieldToRecieve,
            JoinType joinType = JoinType.LEFT
            )
        {

            Type leftType = typeof(LeftTableModelT);
            Type rightType = typeof(RightTableModelT);

            PropertyInfo rightPrimaryKey = rightType.GetProperties().Where(p => Utilities.HasAttribute<PrimaryKey>(p)).FirstOrDefault();

            if (rightPrimaryKey == null)
                throw new InvalidMemberException("Right table missing primary key attribute.", rightPrimaryKey);

            return new JoinDef()
            {

                RightTable = rightType,
                LeftKey = GetExpressionProp(foreignKey),
                RightKey = rightPrimaryKey,
                FieldToSelect = GetExpressionProp(fieldToSelect),
                FieldToRecieve = GetExpressionProp(fieldToRecieve),
                JoinType = joinType

            };

        }
        
    }

    /// <summary>
    /// 
    /// </summary>
    public class JoinDef
    {

        /// <summary>
        /// 
        /// </summary>
        public Type RightTable;
        
        /// <summary>
        /// 
        /// </summary>
        public PropertyInfo LeftKey;

        /// <summary>
        /// 
        /// </summary>
        public PropertyInfo RightKey;
        
        /// <summary>
        /// 
        /// </summary>
        public PropertyInfo FieldToSelect;

        /// <summary>
        /// 
        /// </summary>
        public MemberInfo FieldToRecieve;

        /// <summary>
        /// 
        /// </summary>
        public JoinType JoinType;

    }

    /// <summary>
    /// Defines a type of join
    /// </summary>
    public enum JoinType
    {

        /// <summary>
        /// Left join
        /// </summary>
        LEFT,

        /// <summary>
        /// Inner join
        /// </summary>
        INNER,

        /// <summary>
        /// Right join
        /// </summary>
        RIGHT,

        /// <summary>
        /// Full outer join
        /// </summary>
        FULL_OUTER

    }

}
