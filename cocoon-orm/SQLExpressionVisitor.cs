using System;
using System.Data.SqlClient;
using System.Linq;
using System.Linq.Expressions;
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

            Visit(node.Right);

            whereBuilder.Append(")");

            return node;
        }

        protected override Expression VisitMember(MemberExpression node)
        {

            if (node.Expression != null && Utilities.HasAttribute<Column>(node.Member))
            {

                TableDefinition def = orm.getTable(node.Member.DeclaringType);
                whereBuilder.Append(string.Format("{0}.{1}", def.objectName, CocoonORM.getObjectName(node.Member)));
                
            }
            else
                throw new NotSupportedException(string.Format("Member '{0}' not supported", node.Member.Name));

            return node;

        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {

            if(node.Method.Name == "StartsWith")
                addLike(node, ((ConstantExpression)node.Arguments[0]).Value + "%");
            else if(node.Method.Name == "EndsWith")
                addLike(node, "%" + ((ConstantExpression)node.Arguments[0]).Value);
            else if(node.Method.Name == "Contains")
                addLike(node, "%" + ((ConstantExpression)node.Arguments[0]).Value + "%");
            else
                throw new NotSupportedException("Methods are not supported.");

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

            IQueryable val = node.Value as IQueryable;

            if (val == null && node.Value == null)
                whereBuilder.Append("null");
            else if (val == null)
                whereBuilder.Append(CocoonORM.addWhereParam(cmd, node.Value).ParameterName);
            else
                throw new NotSupportedException("The constant not supported");

            return node;
        }

        private bool isConstantNull(Expression exp)
        {
            return exp.NodeType == ExpressionType.Constant && ((ConstantExpression)exp).Value == null;
        }

        private void addLike(MethodCallExpression node, string like)
        {
            var member = (MemberExpression)node.Object;
            var def = orm.getTable(member.Member.DeclaringType);
            whereBuilder.Append(string.Format("{0}.{1} like {2}",
                def.objectName,
                CocoonORM.getObjectName(member.Member),
                CocoonORM.addWhereParam(cmd, like)));

        }

    }
}
