using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Libr4.AI.Infrastructure.CodeGraph;

public class CodeExtractor
{
    public async Task<(List<CodeNode> Nodes, List<CodeEdge> Edges)> ExtractFromCSharpFile(string filePath)
    {
        var code = await File.ReadAllTextAsync(filePath);
        var syntaxTree = CSharpSyntaxTree.ParseText(code);
        var root = syntaxTree.GetCompilationUnitRoot();
        
        var nodes = new List<CodeNode>();
        var edges = new List<CodeEdge>();
        
        var fileId = Path.GetFileNameWithoutExtension(filePath).Replace(" ", "_");
        
        ExtractClasses(root, fileId, filePath, nodes, edges);
        ExtractInterfaces(root, fileId, filePath, nodes, edges);
        ExtractMethods(root, fileId, filePath, nodes, edges);
        ExtractProperties(root, fileId, filePath, nodes, edges);
        ExtractUsings(root, fileId, filePath, edges);
        
        return (nodes, edges);
    }

    private void ExtractClasses(CompilationUnitSyntax root, string fileId, string filePath, List<CodeNode> nodes, List<CodeEdge> edges)
    {
        foreach (var classDecl in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
        {
            var classId = $"{fileId}_{classDecl.Identifier.Text}";
            nodes.Add(new CodeNode
            {
                Id = classId,
                Label = classDecl.Identifier.Text,
                NodeType = "class",
                FilePath = filePath,
                LineNumber = classDecl.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                Metadata = new Dictionary<string, object>
                {
                    ["modifiers"] = string.Join(", ", classDecl.Modifiers.Select(m => m.Text)),
                    ["is_abstract"] = classDecl.Modifiers.Any(m => m.Text == "abstract"),
                    ["is_static"] = classDecl.Modifiers.Any(m => m.Text == "static")
                }
            });
            
            edges.Add(new CodeEdge
            {
                Source = fileId,
                Target = classId,
                Relation = "contains",
                FilePath = filePath,
                LineNumber = classDecl.GetLocation().GetLineSpan().StartLinePosition.Line + 1
            });

            // Extract inheritance relationships
            var baseClass = classDecl.BaseList?.Types.FirstOrDefault();
            if (baseClass != null)
            {
                var baseClassId = baseClass.ToString().Replace(".", "_");
                edges.Add(new CodeEdge
                {
                    Source = classId,
                    Target = baseClassId,
                    Relation = "inherits",
                    FilePath = filePath,
                    LineNumber = classDecl.GetLocation().GetLineSpan().StartLinePosition.Line + 1
                });
            }
        }
    }

    private void ExtractInterfaces(CompilationUnitSyntax root, string fileId, string filePath, List<CodeNode> nodes, List<CodeEdge> edges)
    {
        foreach (var interfaceDecl in root.DescendantNodes().OfType<InterfaceDeclarationSyntax>())
        {
            var interfaceId = $"{fileId}_{interfaceDecl.Identifier.Text}";
            nodes.Add(new CodeNode
            {
                Id = interfaceId,
                Label = interfaceDecl.Identifier.Text,
                NodeType = "interface",
                FilePath = filePath,
                LineNumber = interfaceDecl.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                Metadata = new Dictionary<string, object>
                {
                    ["modifiers"] = string.Join(", ", interfaceDecl.Modifiers.Select(m => m.Text))
                }
            });
            
            edges.Add(new CodeEdge
            {
                Source = fileId,
                Target = interfaceId,
                Relation = "contains",
                FilePath = filePath,
                LineNumber = interfaceDecl.GetLocation().GetLineSpan().StartLinePosition.Line + 1
            });
        }
    }

    private void ExtractMethods(CompilationUnitSyntax root, string fileId, string filePath, List<CodeNode> nodes, List<CodeEdge> edges)
    {
        foreach (var methodDecl in root.DescendantNodes().OfType<MethodDeclarationSyntax>())
        {
            var classId = methodDecl.Ancestors().OfType<ClassDeclarationSyntax>().FirstOrDefault();
            var interfaceId = methodDecl.Ancestors().OfType<InterfaceDeclarationSyntax>().FirstOrDefault();
            
            string parentId;
            if (classId != null)
            {
                parentId = $"{fileId}_{classId.Identifier.Text}";
            }
            else if (interfaceId != null)
            {
                parentId = $"{fileId}_{interfaceId.Identifier.Text}";
            }
            else
            {
                parentId = fileId;
            }
            
            var methodId = $"{parentId}_{methodDecl.Identifier.Text}";
            
            nodes.Add(new CodeNode
            {
                Id = methodId,
                Label = $"{methodDecl.Identifier.Text}()",
                NodeType = "method",
                FilePath = filePath,
                LineNumber = methodDecl.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                Metadata = new Dictionary<string, object>
                {
                    ["return_type"] = methodDecl.ReturnType.ToString(),
                    ["parameters"] = string.Join(", ", methodDecl.ParameterList.Parameters.Select(p => $"{p.Type} {p.Identifier}")),
                    ["is_async"] = methodDecl.Modifiers.Any(m => m.Text == "async"),
                    ["is_static"] = methodDecl.Modifiers.Any(m => m.Text == "static")
                }
            });
            
            edges.Add(new CodeEdge
            {
                Source = parentId,
                Target = methodId,
                Relation = "contains",
                FilePath = filePath,
                LineNumber = methodDecl.GetLocation().GetLineSpan().StartLinePosition.Line + 1
            });

            // Extract method calls
            ExtractMethodCalls(methodDecl, methodId, filePath, edges);
        }
    }

    private void ExtractMethodCalls(MethodDeclarationSyntax methodDecl, string methodId, string filePath, List<CodeEdge> edges)
    {
        var invocationExpressions = methodDecl.DescendantNodes().OfType<InvocationExpressionSyntax>();
        
        foreach (var invocation in invocationExpressions)
        {
            if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
            {
                var calledMethodName = memberAccess.Name.Identifier.Text;
                var targetId = $"{methodId}_calls_{calledMethodName}";
                
                edges.Add(new CodeEdge
                {
                    Source = methodId,
                    Target = targetId,
                    Relation = "calls",
                    FilePath = filePath,
                    LineNumber = invocation.GetLocation().GetLineSpan().StartLinePosition.Line + 1
                });
            }
        }
    }

    private void ExtractProperties(CompilationUnitSyntax root, string fileId, string filePath, List<CodeNode> nodes, List<CodeEdge> edges)
    {
        foreach (var propertyDecl in root.DescendantNodes().OfType<PropertyDeclarationSyntax>())
        {
            var classId = propertyDecl.Ancestors().OfType<ClassDeclarationSyntax>().FirstOrDefault();
            var parentId = classId != null ? $"{fileId}_{classId.Identifier.Text}" : fileId;
            
            var propertyId = $"{parentId}_{propertyDecl.Identifier.Text}";
            
            nodes.Add(new CodeNode
            {
                Id = propertyId,
                Label = propertyDecl.Identifier.Text,
                NodeType = "property",
                FilePath = filePath,
                LineNumber = propertyDecl.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                Metadata = new Dictionary<string, object>
                {
                    ["type"] = propertyDecl.Type.ToString(),
                    ["has_getter"] = propertyDecl.AccessorList?.Accessors.Any(a => a.IsKind(SyntaxKind.GetAccessorDeclaration)) ?? false,
                    ["has_setter"] = propertyDecl.AccessorList?.Accessors.Any(a => a.IsKind(SyntaxKind.SetAccessorDeclaration)) ?? false
                }
            });
            
            edges.Add(new CodeEdge
            {
                Source = parentId,
                Target = propertyId,
                Relation = "contains",
                FilePath = filePath,
                LineNumber = propertyDecl.GetLocation().GetLineSpan().StartLinePosition.Line + 1
            });
        }
    }

    private void ExtractUsings(CompilationUnitSyntax root, string fileId, string filePath, List<CodeEdge> edges)
    {
        foreach (var usingDirective in root.Usings)
        {
            var importName = usingDirective.Name.ToString().Replace(".", "_");
            var targetId = importName;
            
            edges.Add(new CodeEdge
            {
                Source = fileId,
                Target = targetId,
                Relation = "imports",
                FilePath = filePath,
                LineNumber = usingDirective.GetLocation().GetLineSpan().StartLinePosition.Line + 1
            });
        }
    }
}
