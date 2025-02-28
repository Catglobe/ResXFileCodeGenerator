using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Catglobe.ResXFileCodeGenerator;

internal sealed record SymbolReference(
	TypeRef TheType,
	string? Namespace,
	ImmutableEquatableArray<string> ClassDeclChain,
	ImmutableEquatableArray<string> Basenames)
{
	public override int GetHashCode()
	{
		unchecked
		{
			var hashCode = TheType.GetHashCode();
			hashCode = (hashCode * 397) ^ (Namespace != null ? Namespace.GetHashCode() : 0);
			hashCode = (hashCode * 397) ^ ClassDeclChain.GetHashCode();
			hashCode = (hashCode * 397) ^ Basenames.GetHashCode();
			return hashCode;
		}
	}

	private static readonly DiagnosticDescriptor ContextClassesMustBePartial = new(
		id: "CatglobeResXFileCodeGenerator009",
		title: "Partial missing",
		messageFormat: "Context classes must be partial '{0}'",
		category: "ResXFileCodeGenerator",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true
	);

	private static readonly DiagnosticDescriptor NoDeclaredSymbol = new(
		id: "CatglobeResXFileCodeGenerator010",
		title: "No symbol",
		messageFormat: "Problem parsing '{0}'",
		category: "ResXFileCodeGenerator",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true
	);

	public static (SymbolReference?,DiagnosticDescriptor?) From(INamedTypeSymbol symbol, SemanticModel semanticModel, TypeDeclarationSyntax decl, CancellationToken ct)
	{
		var contextTypeSymbol = semanticModel.GetDeclaredSymbol(decl, ct);
		if (contextTypeSymbol is null)
			return (null, NoDeclaredSymbol);
		//var contextClassLocation = contextTypeSymbol?.Locations.FirstOrDefault() ?? Location.None;
		var nsName = contextTypeSymbol.ContainingNamespace is { IsGlobalNamespace: false } ns ? ns.ToDisplayString(): null;

		if (!TryGetNestedTypeDeclarations(decl, semanticModel, ct, out var classDeclarationList) || classDeclarationList is null)
		{
			return (null, ContextClassesMustBePartial);
		}

		var filePaths = ImmutableArray.CreateBuilder<string>();
		filePaths.AddRange(symbol.DeclaringSyntaxReferences
			.Select(reference => reference.SyntaxTree.FilePath)
			.Select(x => !Utilities.BasenameFromPath(x, out var basename, out _) ? null! : basename)
			.Where(x=>x is not null));

		return (new(new(symbol), nsName, classDeclarationList, filePaths), null);
	}

	private static bool TryGetNestedTypeDeclarations(TypeDeclarationSyntax contextClassSyntax, SemanticModel semanticModel, CancellationToken cancellationToken, [NotNullWhen(true)] out ImmutableArray<string>? typeDeclarations)
	{
		typeDeclarations = null;
		var typeDeclarationsBuilder = ImmutableArray.CreateBuilder<string>();
		StringBuilder stringBuilder = new();

		for (TypeDeclarationSyntax? currentType = contextClassSyntax; currentType != null; currentType = currentType.Parent as TypeDeclarationSyntax)
		{
			stringBuilder.Clear();
			var isPartialType = false;

			foreach (var modifier in currentType.Modifiers)
			{
				stringBuilder.Append(modifier.Text);
				stringBuilder.Append(' ');
				isPartialType |= modifier.IsKind(SyntaxKind.PartialKeyword);
			}

			if (!isPartialType)
			{
				typeDeclarations = null;
				return false;
			}

			stringBuilder.Append(currentType.GetTypeKindKeyword());
			stringBuilder.Append(' ');

			var typeSymbol = semanticModel.GetDeclaredSymbol(currentType, cancellationToken);
			Debug.Assert(typeSymbol != null);

			var typeName = typeSymbol!.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
			stringBuilder.Append(typeName);

			typeDeclarationsBuilder.Add(stringBuilder.ToString());
		}

		Debug.Assert(typeDeclarationsBuilder.Count > 0);
		typeDeclarations = typeDeclarationsBuilder.ToImmutable();
		return true;
	}
}

/// <summary>
/// An equatable value representing type identity.
/// </summary>
[DebuggerDisplay("Name = {Name}")]
internal readonly struct TypeRef(ITypeSymbol type) : IEquatable<TypeRef>
{
	public string Name { get; } = type.Name;

	public ITypeSymbol TypeSymbol { get; } = type;

	/// <summary>
	/// Fully qualified assembly name, prefixed with "global::", e.g. global::System.Numerics.BigInteger.
	/// </summary>
	public string FullyQualifiedName { get; } = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
	public ImmutableArray<SymbolDisplayPart> mini { get; } = type.ToDisplayParts(NullableFlowState.NotNull,SymbolDisplayFormat.FullyQualifiedFormat);

	public bool Equals(TypeRef other) => FullyQualifiedName == other.FullyQualifiedName;
	public override bool Equals(object? obj) => obj is TypeRef other && Equals(other);
	public override int GetHashCode() => FullyQualifiedName.GetHashCode();
}
