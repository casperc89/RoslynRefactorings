using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.Operations;

using RoslynRefactorings.Console.ArgumentTranslation;

namespace RoslynRefactorings.Console;

internal class Program
{
    private static readonly IArgumentTranslationStrategy TranslationStrategy =
        new NoArgumentTranslationStrategy();
    // new LimitTranslationsArgumentTranslationStrategy(new OpenAITranslateInputAsync(), 10);
    
    public static async Task Main(string[] args)
    {
        var path = "/Users/caspercramer/Documents/dev/RosylinTranslator/SampleInputApplication/SampleInputApplication.csproj";
        var workspace = MSBuildWorkspace.Create();
        var project = await workspace.OpenProjectAsync(path);
        

        await TranslateLog4NetStatementsAsync(project);
        // await TranslateWrappedLoggerInvocationsAsync(project);
    }

    private static async Task TranslateWrappedLoggerInvocationsAsync(Project project)
    {
        var editedDocuments = new DocumentEdits();
        foreach (var document in project.Documents)
        {
            var syntaxTree = await document.GetSyntaxTreeAsync();
            var root = await syntaxTree!.GetRootAsync();
            var model = await document.GetSemanticModelAsync();
            
            var wrappedLoggerClass = root.DescendantNodes()
                .OfType<ClassDeclarationSyntax>()
                .FirstOrDefault(x => x.Identifier.ValueText == "WrappedLogger");
            
            if (wrappedLoggerClass == null)
                continue;

            var test = ModelExtensions.GetDeclaredSymbol(model, wrappedLoggerClass);
            var classRefs = await SymbolFinder.FindReferencesAsync(test, project.Solution);
            
            var propertyDeclaration = wrappedLoggerClass.DescendantNodes()
                .OfType<PropertyDeclarationSyntax>()
                .FirstOrDefault(x => x.Identifier.ValueText == "Instance");

            if (propertyDeclaration == null)
                throw new InvalidOperationException("Expected an Instance property declaration in this class");

            var symbol = ModelExtensions.GetDeclaredSymbol(model, propertyDeclaration) as IPropertySymbol;
            var symbolReferences = await SymbolFinder.FindReferencesAsync(symbol, project.Solution);
            var locationReferences = symbolReferences.SelectMany(r => r.Locations);

            foreach (var locRef in locationReferences)
            {
                var tree = await locRef.Document.GetSyntaxRootAsync();
                var node = tree.FindNode(locRef.Location.SourceSpan);

                while (node != null && node is not InvocationExpressionSyntax)
                {
                    node = node.Parent;
                }

                var invocation = (InvocationExpressionSyntax)node;
                var editor = await editedDocuments.GetOrAddAsync(locRef.Document);
                await TranslateArgumentAsync(invocation, "message", editor);
            }

        }

        // editedDocuments.SaveAsync(".gen");
    }

    private static async Task TranslateLog4NetStatementsAsync(Project project)
    {
        var editedDocuments = new DocumentEdits();
        foreach (var document in project.Documents)
        {
            var syntaxTree = await document.GetSyntaxTreeAsync();
            var root = await syntaxTree!.GetRootAsync();
            var model = await document.GetSemanticModelAsync();

            var fieldDeclarations = root.DescendantNodes().OfType<FieldDeclarationSyntax>();
            var allVariableDeclarations = fieldDeclarations.SelectMany(x => x.Declaration.Variables);

            // We're searching for field declarations of the log4net.ILog type.
            // This is an indication we're instantiating a logger.
            foreach (var variableDeclaration in allVariableDeclarations)
            {
                var symbol = ModelExtensions.GetDeclaredSymbol(model, variableDeclaration) as IFieldSymbol;
                var typeName = symbol.Type.ToDisplayString();

                if (!"log4net.ILog".Equals(typeName))
                    continue;
                
                // Now we need to find all references of this field
                var symbolReferences = await SymbolFinder.FindReferencesAsync(symbol, project.Solution);
                var locationReferences = symbolReferences.SelectMany(r => r.Locations);

                // Update each reference that is an invocation on the ILog
                foreach (var locRef in locationReferences)
                {
                    var doc = locRef.Document;
                    var loc = locRef.Location;
                    var tree = await doc.GetSyntaxRootAsync();
                    var node = tree.FindNode(loc.SourceSpan);
                    
                    // We are only interested in member access expressions that are being invoked.
                    // For example: Logger.Info(x)
                    // But not: Logger.IsInfoEnabled
                    if (node.Parent is MemberAccessExpressionSyntax { Parent: InvocationExpressionSyntax invocation })
                    {
                        var editor = await editedDocuments.GetOrAddAsync(locRef.Document);

                        await TranslateArgumentAsync(invocation, "message", editor);
                        await TranslateArgumentAsync(invocation, "format", editor);
                    }
                }
            }
        }

        await editedDocuments.SaveAsync();
    }

    private static async Task TranslateArgumentAsync(InvocationExpressionSyntax invocation, string argName,
        DocumentEditor editor)
    {
        var curArgs = invocation.ArgumentList;
        var argToTranslate = FindArgument(curArgs, argName, editor.SemanticModel);

        if (argToTranslate == null)
            return;

        // Based on the argument expression we'll have to apply different logic to
        // create a replacement argument. Right now we'll only support string literals, interpolated strings
        // and add expression strings.
        // Unsupported:
        // * Arguments that use a (local) reference
        bool supportedArgument = argToTranslate.Expression.Kind() switch
        {
            SyntaxKind.StringLiteralExpression => true,
            SyntaxKind.InterpolatedStringExpression => true,
            SyntaxKind.AddExpression => true,
            _ => false
        };

        if (!supportedArgument)
            return;

        var argValue = argToTranslate.ToFullString();
        var translatedArgValue = await TranslationStrategy.TranslateInputAsync(argValue);

        System.Console.WriteLine($"{argValue} >> {translatedArgValue}");

        var translatedArgument = ConvertToArgumentSyntax(translatedArgValue);
        var newArgs = curArgs.ReplaceNode(argToTranslate, translatedArgument);
        editor.ReplaceNode(curArgs, newArgs);
    }

    private static SyntaxNode ConvertToArgumentSyntax(string value)
    {
        var syntaxFromString = CSharpSyntaxTree.ParseText(value);
        var interpolatedString = syntaxFromString.GetRoot()
            .DescendantNodes()
            .OfType<ExpressionStatementSyntax>()
            .First();

        return SyntaxFactory.Argument(interpolatedString.Expression);
    }

    private static ArgumentSyntax? FindArgument(ArgumentListSyntax curArgs, string argName, SemanticModel model)
    {
        foreach (var arg in curArgs.Arguments)
        {
            var operation = (IArgumentOperation)model.GetOperation(arg);
            if (operation?.Parameter.Name == argName) return arg;
        }

        return null;
    }
}