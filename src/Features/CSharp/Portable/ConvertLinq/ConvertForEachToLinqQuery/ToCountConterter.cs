﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.ConvertLinq.ConvertForEachToLinqQuery;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.ConvertLinq.ConvertForEachToLinqQuery
{
    internal sealed class ToCountConterter : AbstractToMethodConverter
    {
        public ToCountConterter(
            ForEachInfo<ForEachStatementSyntax, StatementSyntax> forEachInfo,
            ExpressionSyntax selectExpression,
            ExpressionSyntax modifyingExpression)
            : base(forEachInfo, selectExpression, modifyingExpression) { }

        protected override string MethodName => nameof(Enumerable.Count);

        // Checks that the expression is "0".
        protected override bool CanReplaceInitialization(ExpressionSyntax expression, SemanticModel semanticModel, CancellationToken cancellationToken)
            => expression is LiteralExpressionSyntax literalExpression && literalExpression.Token.ValueText == "0";

        /// Input:
        /// foreach(...)
        /// {
        ///     ...
        ///     ...
        ///     counter++;
        ///  }
        ///  
        ///  Output:
        ///  counter += queryGenerated.Count();
        // TODO comments?
        protected override StatementSyntax CreateDefaultStatement(QueryExpressionSyntax queryExpression, ExpressionSyntax expression)
            => SyntaxFactory.ExpressionStatement(
                SyntaxFactory.AssignmentExpression(
                    SyntaxKind.AddAssignmentExpression,
                    expression,
                    CreateInvocationExpression(queryExpression)));
    }
}
