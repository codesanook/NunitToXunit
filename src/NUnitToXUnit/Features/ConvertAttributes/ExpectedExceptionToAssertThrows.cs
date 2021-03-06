﻿using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnitToXUnit.Extensions;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace NUnitToXUnit.Features.ConvertAttributes
{
    static class ExpectedExceptionToAssertThrows
    {
        public static MethodDeclarationSyntax Convert(MethodDeclarationSyntax method)
        {
            if(!method.HasExpectedExceptionAttribute())
            {
                return method;
            }
            var newBody = ReplaceMethodBody(method);
            return ReplaceMethodAttributes(newBody);
        }

        private static MethodDeclarationSyntax ReplaceMethodBody(MethodDeclarationSyntax method)
        {
            var expectedExceptionAttribute = method.GetExpectedExceptionAttribute();
            var attributeArgument = expectedExceptionAttribute.GetExpectedExceptionAttributeArgument();

            if (attributeArgument == null)
            {
                return CreateMethodFromExceptionType(attributeArgument, method, nameof(System.Exception));
            }
            else if (attributeArgument.IsExceptionMessage())
            {
                return CreateMethodFromExceptionMessage(attributeArgument, method);
            }
            else if (attributeArgument.IsTypeOfException())
            {
                return CreateMethodFromExceptionType(attributeArgument, method);
            }
            return method;
        }

        private static MethodDeclarationSyntax ReplaceMethodAttributes(MethodDeclarationSyntax node)
        {
            var expectedExceptionAttribute = node.GetExpectedExceptionAttribute();
            var expectedExceptionList = node.AttributeLists.Single(x => x.Attributes.Any(a => a.IsExpectedExceptionAttribute()));
            return expectedExceptionList.Attributes.Count == 1 ? node.RemoveNode(expectedExceptionList, SyntaxRemoveOptions.KeepNoTrivia) :
                                                                 node.RemoveNode(expectedExceptionAttribute, SyntaxRemoveOptions.KeepNoTrivia);
        }

        private static MethodDeclarationSyntax CreateMethodFromExceptionMessage(AttributeArgumentSyntax expectedException, MethodDeclarationSyntax node)
        {
            var exceptionMessage = expectedException.Expression.ToString();
            var rawStatement = CreateAssertFromExceptionMessage(node);
            var statementToAdd = CreateExtraAssert(exceptionMessage).WithTriviaFrom(node.Body.Statements.First());
            return ReplaceMethodBody(rawStatement, node).AddBodyStatements(statementToAdd);
        }

        private static string CreateAssertFromExceptionMessage(MethodDeclarationSyntax node)
        {
            var rawStatement = CreateAssert("Exception", node);
            return $"var exception = { rawStatement }";
        }

        private static StatementSyntax CreateExtraAssert(string exceptionMessage)
        {
            return ParseStatement($"Assert.Equal({ exceptionMessage }, exception.Message);").NormalizeWhitespace();
        }

        private static MethodDeclarationSyntax CreateMethodFromExceptionType(AttributeArgumentSyntax attributeArgument, MethodDeclarationSyntax node, string overrideExceptionType = null)
        {
            var exceptionType = overrideExceptionType ?? attributeArgument.GetExceptionType();
            var rawStatement = CreateAssert(exceptionType, node);
            return ReplaceMethodBody(rawStatement, node);
        }

        private static string CreateAssert(string exceptionType, MethodDeclarationSyntax node)
        {
            string indentedBody = node.Body.ToString().Replace("\n", "\n    ");

            return $@"Assert.Throws<{exceptionType}>(() =>
            { indentedBody });";
        }

        private static MethodDeclarationSyntax ReplaceMethodBody(string rawStatement, MethodDeclarationSyntax node)
        {
            var xUnitStatement = ParseStatement(rawStatement).WithTriviaFrom(node.Body.Statements.First());
            var methodBody = node.Body.WithStatements(new SyntaxList<StatementSyntax>().Add(xUnitStatement));
            return node.WithBody(methodBody);
        }
    }
}
