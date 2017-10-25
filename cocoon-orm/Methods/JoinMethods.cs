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
        /// <typeparam name="ForeignTableModelT"></typeparam>
        /// <typeparam name="PrimaryTableModelT"></typeparam>
        /// <param name="foreignKey"></param>
        /// <param name="primaryKey"></param>
        /// <param name="fieldToSelect"></param>
        /// <param name="fieldToRecieve"></param>
        /// <param name="joinType"></param>
        /// <returns></returns>
        public static JoinDef Join<ForeignTableModelT, PrimaryTableModelT>(
            Expression<Func<ForeignTableModelT, object>> foreignKey,
            Expression<Func<PrimaryTableModelT, object>> primaryKey,
            Expression<Func<PrimaryTableModelT, object>> fieldToSelect,
            Expression<Func<ForeignTableModelT, object>> fieldToRecieve,
            JoinType joinType = JoinType.LEFT)
        {

            return new JoinDef()
            {

                RightTable = typeof(PrimaryTableModelT),
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
        /// <typeparam name="ForeignTableModelT"></typeparam>
        /// <typeparam name="PrimaryTableModelT"></typeparam>
        /// <param name="fieldToSelect"></param>
        /// <param name="fieldToRecieve"></param>
        /// <param name="joinType"></param>
        /// <returns></returns>
        public static JoinDef Join<ForeignTableModelT, PrimaryTableModelT>(
            Expression<Func<PrimaryTableModelT, object>> fieldToSelect,
            Expression<Func<ForeignTableModelT, object>> fieldToRecieve,
            JoinType joinType = JoinType.LEFT)
        {

            Type leftType = typeof(ForeignTableModelT);
            Type rightType = typeof(PrimaryTableModelT);

            PropertyInfo leftPrimaryKey = leftType.GetProperties().Where(p => ORMUtilities.HasAttribute<PrimaryKey>(p)).FirstOrDefault();
            PropertyInfo rightPrimaryKey = rightType.GetProperties().Where(p => ORMUtilities.HasAttribute<PrimaryKey>(p)).FirstOrDefault();

            if (leftPrimaryKey == null)
                throw new InvalidMemberException("Left table missing primary key attribute", leftPrimaryKey);

            if (rightPrimaryKey == null)
                throw new InvalidMemberException("Right table missing primary key attribute", rightPrimaryKey);

            return new JoinDef()
            {

                RightTable = typeof(PrimaryTableModelT),
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
        /// <typeparam name="ForeignTableModelT"></typeparam>
        /// <typeparam name="PrimaryTableModelT"></typeparam>
        /// <param name="foreignKey"></param>
        /// <param name="fieldToSelect"></param>
        /// <param name="fieldToRecieve"></param>
        /// <param name="joinType"></param>
        /// <returns></returns>
        public static JoinDef Join<ForeignTableModelT, PrimaryTableModelT>(
            Expression<Func<ForeignTableModelT, object>> foreignKey,
            Expression<Func<PrimaryTableModelT, object>> fieldToSelect,
            Expression<Func<ForeignTableModelT, object>> fieldToRecieve,
            JoinType joinType = JoinType.LEFT
            )
        {

            Type leftType = typeof(ForeignTableModelT);
            Type rightType = typeof(PrimaryTableModelT);

            PropertyInfo rightPrimaryKey = rightType.GetProperties().Where(p => ORMUtilities.HasAttribute<PrimaryKey>(p)).FirstOrDefault();

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
