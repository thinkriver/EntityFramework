﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using JetBrains.Annotations;
using Microsoft.Data.Entity.Utilities;
using Remotion.Linq.Clauses.Expressions;

namespace Microsoft.Data.Entity.Query
{
#if DEBUG
    public class ExpressionPrinter
    {
        public static string Print([NotNull]Expression expression)
        {
            Check.NotNull(expression, "expression");

            var visitor = new ExpressionPrintingVisitor();
            var nodePrinter = new TreeNodePrinter();
            var node = visitor.VisitExpression(expression);

            return nodePrinter.Print(node);
        }

        public class TreeNode
        {
            private readonly List<TreeNode> _children = new List<TreeNode>();

            public TreeNode()
            {
                Text = new StringBuilder();
            }

            public TreeNode([CanBeNull]string text, [NotNull]params TreeNode[] children)
            {
                Text = string.IsNullOrEmpty(text) ? new StringBuilder() : new StringBuilder(text);

                if (children != null)
                {
                    _children.AddRange(children);
                }
            }

            public TreeNode([CanBeNull]string text, [CanBeNull]List<TreeNode> children)
                : this(text)
            {
                if (children != null)
                {
                    _children.AddRange(children);
                }
            }

            public virtual StringBuilder Text { get; }

            public virtual IList<TreeNode> Children
            {
                get { return _children; }
            }

            public virtual int Position { get; set; }
        }

        internal class TreeNodePrinter
        {
            private readonly List<TreeNode> _scopes = new List<TreeNode>();
            private const char Horizontals = '_';
            private const char Verticals = '|';

            public virtual string Print(TreeNode node)
            {
                var text = new StringBuilder();
                PrintNode(text, node);

                return text.ToString();
            }

            public virtual void PrintNode(StringBuilder text, TreeNode node)
            {
                IndentLine(text);
                text.Append(node.Text);
                PrintChildren(text, node);
            }

            public virtual void PrintChildren(StringBuilder text, TreeNode node)
            {
                _scopes.Add(node);
                node.Position = 0;
                foreach (var childNode in node.Children)
                {
                    text.AppendLine();
                    node.Position++;
                    PrintNode(text, childNode);
                }

                _scopes.RemoveAt(_scopes.Count - 1);
            }

            public void IndentLine(StringBuilder text)
            {
                var idx = 0;
                for (var scopeIdx = 0; scopeIdx < _scopes.Count; scopeIdx++)
                {
                    var parentScope = _scopes[scopeIdx];
                    if (parentScope.Position == parentScope.Children.Count && scopeIdx != _scopes.Count - 1)
                    {
                        text.Append(' ');
                    }
                    else
                    {
                        text.Append(Verticals);
                    }

                    idx++;
                    if (_scopes.Count == idx)
                    {
                        text.Append(Horizontals);
                    }
                    else
                    {
                        text.Append(' ');
                    }
                }
            }
        }

        internal class ExpressionPrintingVisitor
        {
            public virtual TreeNode VisitExpression(Expression expression)
            {
                if (expression == null)
                    return null;

                var extensionExpression = expression as ExtensionExpression;
                if (extensionExpression != null)
                {
                    // TODO: Handle
                    throw new NotSupportedException();
                }

                switch (expression.NodeType)
                {
                    case ExpressionType.ArrayLength:
                    case ExpressionType.Convert:
                    case ExpressionType.ConvertChecked:
                    case ExpressionType.Negate:
                    case ExpressionType.NegateChecked:
                    case ExpressionType.Not:
                    case ExpressionType.Quote:
                    case ExpressionType.TypeAs:
                    case ExpressionType.UnaryPlus:
                        return VisitUnaryExpression((UnaryExpression)expression);

                    case ExpressionType.Add:
                    case ExpressionType.AddChecked:
                    case ExpressionType.Divide:
                    case ExpressionType.Modulo:
                    case ExpressionType.Multiply:
                    case ExpressionType.MultiplyChecked:
                    case ExpressionType.Power:
                    case ExpressionType.Subtract:
                    case ExpressionType.SubtractChecked:
                    case ExpressionType.And:
                    case ExpressionType.Or:
                    case ExpressionType.ExclusiveOr:
                    case ExpressionType.LeftShift:
                    case ExpressionType.RightShift:
                    case ExpressionType.AndAlso:
                    case ExpressionType.OrElse:
                    case ExpressionType.Equal:
                    case ExpressionType.NotEqual:
                    case ExpressionType.GreaterThanOrEqual:
                    case ExpressionType.GreaterThan:
                    case ExpressionType.LessThan:
                    case ExpressionType.LessThanOrEqual:
                    case ExpressionType.Coalesce:
                    case ExpressionType.ArrayIndex:
                        return VisitBinaryExpression((BinaryExpression)expression);

                    case ExpressionType.Constant:
                        return VisitConstantExpression((ConstantExpression)expression);

                    case ExpressionType.Lambda:
                        return VisitLambdaExpression((LambdaExpression)expression);

                    case ExpressionType.MemberAccess:
                        return VisitMemberExpression((MemberExpression)expression);

                    case ExpressionType.Call:
                        return VisitMethodCallExpression((MethodCallExpression)expression);

                    case ExpressionType.Parameter:
                        return VisitParameterExpression((ParameterExpression)expression);

                    case QuerySourceReferenceExpression.ExpressionType:
                        return VisitQuerySourceReferenceExpression((QuerySourceReferenceExpression)expression);

                    default:
                        throw new NotSupportedException();
                }
            }

            public virtual TreeNode VisitUnaryExpression(UnaryExpression expression)
            {
                var operand = VisitExpression(expression.Operand);

                return new TreeNode(expression.Method.Name, operand);
            }

            public virtual TreeNode VisitBinaryExpression(BinaryExpression expression)
            {
                var left = VisitExpression(expression.Left);
                var right = VisitExpression(expression.Right);

                return new TreeNode(expression.Method.Name, left, right);
            }

            public virtual TreeNode VisitConstantExpression(ConstantExpression expression)
            {
                return new TreeNode(expression.ToString());
            }

            public virtual TreeNode VisitLambdaExpression(LambdaExpression expression)
            {
                var body = VisitExpression(expression.Body);
                // TODO: handle parameters

                return new TreeNode("Lambda", body);
            }

            public virtual TreeNode VisitMemberExpression(MemberExpression expression)
            {
                var parent = VisitExpression(expression.Expression);

                return new TreeNode("." + expression.Member.Name, parent);
            }

            public virtual TreeNode VisitMethodCallExpression(MethodCallExpression expression)
            {
                var arguments = expression.Arguments.Select(VisitExpression).ToList();
                var parameters = string.Join(", ", expression.Method.GetParameters().Select(p => PrintType(p.ParameterType)));
                var methodSignature = PrintType(expression.Method.ReturnType) + " " + expression.Method.Name + "(" + parameters + ")";

                return new TreeNode("MethodCall: " + methodSignature, arguments);
            }

            public virtual TreeNode VisitParameterExpression(ParameterExpression expression)
            {
                return new TreeNode("Parameter(Name='" + expression.Name + "' Type='" + expression.Type + "')");
            }

            public virtual TreeNode VisitQuerySourceReferenceExpression(QuerySourceReferenceExpression expression)
            {
                return new TreeNode("QuerySource(" + expression.ReferencedQuerySource.ToString() + ")");
            }

            private string PrintType(Type type)
            {
                if (type.IsConstructedGenericType)
                {
                    return type.Name.Substring(0, type.Name.IndexOf('`')) + "<" + string.Join(", ", type.GenericTypeArguments.Select(PrintType)) + ">";
                }

                return type.Name;
            }
        }
    }
#endif
}
