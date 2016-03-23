using System;
using System.Data.SqlClient;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Cocoon.ORM;

namespace Cocoon.ORM
{
    internal class SQLExpressionTranslator : ExpressionVisitor
    {

        private StringBuilder whereBuilder;
        private SqlCommand cmd;
        private CocoonORM orm;

        public SQLExpressionTranslator()
        {

        }

        public string GenerateSQLExpression(CocoonORM orm, SqlCommand cmd, Expression node)
        {

            this.cmd = cmd;
            this.orm = orm;

            whereBuilder = new StringBuilder();

            Visit(node);

            return whereBuilder.ToString();

        }

        protected override Expression VisitBinary(BinaryExpression node)
        {
            whereBuilder.Append("(");

            Visit(node.Left);

            if (node.NodeType == ExpressionType.And || node.NodeType == ExpressionType.AndAlso)
                whereBuilder.Append(" and ");
            else if (node.NodeType == ExpressionType.Or || node.NodeType == ExpressionType.OrElse)
                whereBuilder.Append(" or ");
            else if (node.NodeType == ExpressionType.LessThan)
                whereBuilder.Append(" < ");
            else if (node.NodeType == ExpressionType.LessThanOrEqual)
                whereBuilder.Append(" <= ");
            else if (node.NodeType == ExpressionType.GreaterThan)
                whereBuilder.Append(" > ");
            else if (node.NodeType == ExpressionType.GreaterThanOrEqual)
                whereBuilder.Append(" >= ");
            else if (node.NodeType == ExpressionType.Equal)
                if (isConstantNull(node.Right))
                    whereBuilder.Append(" is ");
                else
                    whereBuilder.Append(" = ");
            else if (node.NodeType == ExpressionType.NotEqual)
                if (isConstantNull(node.Right))
                    whereBuilder.Append(" is not ");
                else
                    whereBuilder.Append(" <> ");
            else
                throw new NotSupportedException(string.Format("Binary operator '{0}' not supported", node.NodeType));

            //var r = (MemberExpression)node.Right;
            //whereBuilder.Append(CocoonORM.addWhereParam(cmd, getValue(r)).ParameterName);
            Visit(node.Right);

            whereBuilder.Append(")");

            return node;
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            if (node.Expression != null && node.Expression.NodeType == ExpressionType.Parameter)
                whereBuilder.Append(node.Member.Name);
            else
                whereBuilder.Append(CocoonORM.addWhereParam(cmd, getExpressionValue(node)).ParameterName);

            return node;
            //throw new NotSupportedException(string.Format("The member '{0}' is not supported", node.Member.Name));

        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {

            if (node.Method.Name == "StartsWith")
                addLikeParam(node, getExpressionValue(node.Arguments[0]) + "%");
            else if (node.Method.Name == "EndsWith")
                addLikeParam(node, "%" + getExpressionValue(node.Arguments[0]));
            else if (node.Method.Name == "Contains")
                addLikeParam(node, "%" + getExpressionValue(node.Arguments[0]) + "%");
            else
                throw new NotSupportedException(string.Format("Method '{0}' not supported", node.Method.Name));

            return node;

        }

        protected override Expression VisitUnary(UnaryExpression node)
        {

            if (node.NodeType == ExpressionType.Not)
            {
                whereBuilder.Append(" not ");
                Visit(node.Operand);
            }
            else if (node.NodeType == ExpressionType.Convert)
                Visit(node.Operand);
            else
                throw new NotSupportedException(string.Format("Unary operator '{0}' not supported", node.NodeType));

            return node;

        }

        protected override Expression VisitConstant(ConstantExpression node)
        {

            whereBuilder.Append(CocoonORM.addWhereParam(cmd, node.Value).ParameterName);
            return node;

        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            return base.VisitParameter(node);
        }

        private static bool isConstantNull(Expression exp)
        {
            return exp.NodeType == ExpressionType.Constant && ((ConstantExpression)exp).Value == null;
        }

        private void addLikeParam(MethodCallExpression node, string like)
        {
            MemberExpression member = (MemberExpression)node.Object;
            TableDefinition def = orm.getTable(member.Member.DeclaringType);

            whereBuilder.Append(string.Format("{0}.{1} like {2}",
                def.objectName,
                CocoonORM.getObjectName(member.Member),
                CocoonORM.addWhereParam(cmd, like)));

        }

        private static object getExpressionValue(Expression member)
        {
            UnaryExpression objectMember = Expression.Convert(member, typeof(object));
            Expression<Func<object>> getterLambda = Expression.Lambda<Func<object>>(objectMember);
            Func<object> getter = getterLambda.Compile();

            return getter();
        }

    }
}
