using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Catglobe.ResXFileCodeGenerator;

internal static class RoslynExtensions
{
	/// <summary>
	/// Returns the kind keyword corresponding to the specified declaration syntax node.
	/// </summary>
	public static string GetTypeKindKeyword(this TypeDeclarationSyntax typeDeclaration)
	{
		switch (typeDeclaration.Kind())
		{
			case SyntaxKind.ClassDeclaration:
				return "class";
			case SyntaxKind.InterfaceDeclaration:
				return "interface";
			case SyntaxKind.StructDeclaration:
				return "struct";
			case SyntaxKind.RecordDeclaration:
				return "record";
			case SyntaxKind.RecordStructDeclaration:
				return "record struct";
			case SyntaxKind.EnumDeclaration:
				return "enum";
			case SyntaxKind.DelegateDeclaration:
				return "delegate";
			default:
				Debug.Fail("unexpected syntax kind");
				return null!;
		}
	}
}
