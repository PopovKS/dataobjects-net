// Copyright (C) 2009 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Alexey Gamzov
// Created:    2009.04.24

using System;
using System.Linq.Expressions;
using Xtensive.Core.Linq;
using Xtensive.Core.Parameters;
using Xtensive.Core.Tuples;
using Xtensive.Storage.Linq.Expressions;
using Xtensive.Storage.Linq.Expressions.Visitors;
using Xtensive.Storage.Rse;
using Xtensive.Storage.Rse.Providers;

namespace Xtensive.Storage.Linq.Rewriters
{
  [Serializable]
  internal class ApplyParameterToTupleParameterRewriter : ExtendedExpressionVisitor
  {
    private readonly Expression parameterOfTupleExpression;
    private readonly ApplyParameter applyParameter;
    private readonly Parameter<Tuple> parameterOfTuple;

    public static CompilableProvider Rewrite(CompilableProvider provider,
      Parameter<Tuple> parameterOfTuple, ApplyParameter applyParameter)
    {
      var expressionRewriter = new ApplyParameterToTupleParameterRewriter(parameterOfTuple, applyParameter);
      var providerRewriter = new CompilableProviderVisitor(expressionRewriter.RewriteExpression);
      return providerRewriter.VisitCompilable(provider);
    }

    public static Expression Rewrite(Expression expression,
      Parameter<Tuple> parameterOfTuple, ApplyParameter applyParameter)
    {
      var expressionRewriter = new ApplyParameterToTupleParameterRewriter(parameterOfTuple, applyParameter);
      return expressionRewriter.Visit(expression);
    }

    protected override Expression VisitMemberAccess(MemberExpression m)
    {
      if (m.Member!=WellKnownMembers.ApplyParameterValue)
        return base.VisitMemberAccess(m);
      if (m.Expression.NodeType!=ExpressionType.Constant)
        return base.VisitMemberAccess(m);
      var parameter = ((ConstantExpression) m.Expression).Value;
      if (parameter!=applyParameter)
        return base.VisitMemberAccess(m);
      return parameterOfTupleExpression;
    }

    protected override Expression VisitGroupingExpression(GroupingExpression expression)
    {
      var newProvider = Rewrite(expression.ProjectionExpression.ItemProjector.DataSource.Provider, parameterOfTuple, applyParameter);
      var newItemProjectorBody = Visit(expression.ProjectionExpression.ItemProjector.Item);
      var newKeyExpression = Visit(expression.KeyExpression);
      if (newProvider != expression.ProjectionExpression.ItemProjector.DataSource.Provider 
        || newItemProjectorBody!=expression.ProjectionExpression.ItemProjector.Item
        || newKeyExpression!=expression.KeyExpression
        ) {
        var newItemProjector = new ItemProjectorExpression(newItemProjectorBody, newProvider.Result, expression.ProjectionExpression.ItemProjector.Context);
        var newProjectionExpression = new ProjectionExpression(expression.ProjectionExpression.Type, newItemProjector, expression.ProjectionExpression.TupleParameterBindings, expression.ProjectionExpression.ResultType);
        return new GroupingExpression(expression.Type, expression.OuterParameter, expression.DefaultIfEmpty, newProjectionExpression, expression.ApplyParameter, expression.KeyExpression, expression.Mapping);
      }
      return expression;
    }

    protected override Expression VisitSubQueryExpression(SubQueryExpression expression)
    {
      var newProvider = Rewrite(expression.ProjectionExpression.ItemProjector.DataSource.Provider, parameterOfTuple, applyParameter);
      var newItemProjectorBody = Visit(expression.ProjectionExpression.ItemProjector.Item);
      if (newProvider != expression.ProjectionExpression.ItemProjector.DataSource.Provider || newItemProjectorBody!=expression.ProjectionExpression.ItemProjector.Item) {
        var newItemProjector = new ItemProjectorExpression(newItemProjectorBody, newProvider.Result, expression.ProjectionExpression.ItemProjector.Context);
        var newProjectionExpression = new ProjectionExpression(expression.ProjectionExpression.Type, newItemProjector, expression.ProjectionExpression.TupleParameterBindings, expression.ProjectionExpression.ResultType);
        return new SubQueryExpression(expression.Type, expression.OuterParameter, expression.DefaultIfEmpty, newProjectionExpression, expression.ApplyParameter, expression.ExtendedType);
      }
      return expression;
    }

    private Expression RewriteExpression(Provider provider, Expression expression)
    {
      return Visit(expression);
    }

    // Constructors

    private ApplyParameterToTupleParameterRewriter(Parameter<Tuple> parameterOfTuple, ApplyParameter applyParameter)
    {
      this.parameterOfTuple = parameterOfTuple;
      parameterOfTupleExpression = Expression.Property(Expression.Constant(parameterOfTuple), WellKnownMembers.ParameterOfTupleValue);
      this.applyParameter = applyParameter;
    }
  }
}