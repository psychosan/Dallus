﻿/* License: http://www.apache.org/licenses/LICENSE-2.0 */

using System;
using System.Linq.Expressions;
using SqlLambda.Resolver.ExpressionTree;
using SqlLambda.ValueObjects;

namespace SqlLambda.Resolver
{
    partial class LambdaResolver
    {
        void BuildSql(Node node)
        {
            BuildSql((dynamic)node);
        }

        void BuildSql(LikeNode node)
        {
            if(node.Method == LikeMethod.Equals)
            {
                _builder.QueryByField(node.MemberNode.TableName, node.MemberNode.FieldName,
                    _opDictionary[ExpressionType.Equal], node.Value);
            }
            else
            {
                string value = node.Value;
                switch (node.Method)
                {
                    case LikeMethod.StartsWith:
                        value = node.Value + "%";
                        break;
                    case LikeMethod.EndsWith:
                        value = "%" + node.Value;
                        break;
                    case LikeMethod.Contains:
                        value = "%" + node.Value + "%";
                        break;
                }
                _builder.QueryByFieldLike(node.MemberNode.TableName, node.MemberNode.FieldName, value);
            }
        }

        void BuildSql(OperationNode node)
        {
            BuildSql((dynamic)node.Left, (dynamic)node.Right, node.Operator);
        }

        void BuildSql(MemberNode memberNode)
        {
            _builder.QueryByField(memberNode.TableName, memberNode.FieldName, _opDictionary[ExpressionType.Equal], true);
        }

        void BuildSql(SingleOperationNode node)
        {
            if(node.Operator == ExpressionType.Not)
                _builder.Not();
            BuildSql(node.Child);
        }

        void BuildSql(MemberNode memberNode, ValueNode valueNode, ExpressionType op)
        {
            if(valueNode?.Value == null)
            {
                ResolveNullValue(memberNode, op);
            }
            else
            {
                _builder.QueryByField(memberNode.TableName, memberNode.FieldName, _opDictionary[op], valueNode.Value);
            }
        }

        void BuildSql(ValueNode valueNode, MemberNode memberNode, ExpressionType op)
        {
            BuildSql(memberNode, valueNode, op);
        }

        void BuildSql(MemberNode leftMember, MemberNode rightMember, ExpressionType op)
        {
            _builder.QueryByFieldComparison(leftMember.TableName, leftMember.FieldName, _opDictionary[op], rightMember.TableName, rightMember.FieldName);
        }

        void BuildSql(SingleOperationNode leftMember, Node rightMember, ExpressionType op)
        {
            if (leftMember.Operator == ExpressionType.Not)              
                BuildSql(leftMember as Node, rightMember, op);
            else
                BuildSql((dynamic)leftMember.Child, (dynamic)rightMember, op);
        }

        void BuildSql(Node leftMember, SingleOperationNode rightMember, ExpressionType op)
        {
            BuildSql(rightMember, leftMember, op);
        }

        void BuildSql(Node leftNode, Node rightNode, ExpressionType op)
        {
            _builder.BeginExpression();
            BuildSql((dynamic)leftNode);
            ResolveOperation(op);
            BuildSql((dynamic)rightNode);
            _builder.EndExpression();
        }

        void ResolveNullValue(MemberNode memberNode, ExpressionType op)
        {
            switch (op)
            {
                case ExpressionType.Equal:
                    _builder.QueryByFieldNull(memberNode.TableName, memberNode.FieldName);
                    break;
                case ExpressionType.NotEqual:
                    _builder.QueryByFieldNotNull(memberNode.TableName, memberNode.FieldName);
                    break;
            }
        }

        void ResolveSingleOperation(ExpressionType op)
        {
            switch (op)
            {
                case ExpressionType.Not:
                    _builder.Not();
                    break;
            }
        }
        
        void ResolveOperation(ExpressionType op)
        {
            switch (op)
            {
                case ExpressionType.And:
                case ExpressionType.AndAlso:
                    _builder.And();
                    break;
                case ExpressionType.Or:
                case ExpressionType.OrElse:
                    _builder.Or();
                    break;
                default:
                    throw new ArgumentException($"Unrecognized binary expression operation '{op.ToString()}'");
            }
        }
    }
}
