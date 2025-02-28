using System.Diagnostics;
using System.Reflection;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Catglobe.ResXFileCodeGenerator;

internal static class Parser
{
	public const string EnumAttribute = "Catglobe.ResXFileCodeGenerator.ResxEnumSettingsAttribute";
	public const string ClassAttribute = "Catglobe.ResXFileCodeGenerator.ResxSettingsAttribute";

	public static (EnumSpec? Spec, (DiagnosticDescriptor Diag, Location Location, string Member)? Error) ParseEnum(GeneratorAttributeSyntaxContext context, CancellationToken ct)
    {
	    if (context.TargetSymbol is not INamedTypeSymbol symbol ||
	        context.TargetNode is not StructDeclarationSyntax decl)
		    throw new NotSupportedException();

        var enumSettings = context.Attributes
	        .Select(x => (GetLocation(x), TypeHelper.MapData<EnumSettingsData>(x.NamedArguments))).FirstOrDefault();
		Debug.Assert(enumSettings.Item2 != null);

        var members = ImmutableArray.CreateBuilder<EnumMemberSpec>();
		members.AddRange(symbol.GetMembers().OfType<IFieldSymbol>().Select(x => new EnumMemberSpec(x.Name)));
		var (symbolReference, error) = SymbolReference.From(symbol, context.SemanticModel, decl, ct);
		if (error is not null)
			return (null, (error, enumSettings.Item1, symbol.Name));

		return (new(enumSettings.Item2!, enumSettings.Item1!, members, symbolReference!), null);
    }

	public static (ClassSpec? Spec, (DiagnosticDescriptor Diag, Location Location, string Member)? Error) ParseClass(GeneratorAttributeSyntaxContext context, CancellationToken ct)
    {
	    if (context.TargetSymbol is not INamedTypeSymbol symbol ||
	        context.TargetNode is not ClassDeclarationSyntax decl)
		    throw new NotSupportedException();

	    var classSettings = context.Attributes
		    .Select(x => (GetLocation(x), TypeHelper.MapData<SettingsData>(x.NamedArguments))).FirstOrDefault();
	    Debug.Assert(classSettings.Item2 != null);

	    var (symbolReference, error) = SymbolReference.From(symbol, context.SemanticModel, decl, ct);
	    if (error is not null)
			return (null, (error, classSettings.Item1, symbol.Name));

		return (new(classSettings.Item2!, classSettings.Item1!, symbolReference!), null);
    }



	private static Location GetLocation(AttributeData attribute)
	{
		var reference = attribute.ApplicationSyntaxReference;
		if (reference is null) return Location.None;
		var syntax = reference.SyntaxTree;
		var textSpan = reference.Span;
		return Location.Create(syntax, textSpan);
	}

	private static class TypeHelper
	{
		public static T MapData<T>(ImmutableArray<KeyValuePair<string, TypedConstant>> data) where T : class, new()
		{
			var instance = new T();

			var props = typeof(T).GetProperties();

			var indexed = new Dictionary<string, PropertyInfo>(props.Length, StringComparer.Ordinal);

			foreach (var info in props)
			{
				indexed.Add(info.Name, info);
			}

			foreach (var pair in data)
			{
				if (pair.Value.Value == null)
					continue;

				var prop = indexed[pair.Key];
				prop.SetValue(instance, pair.Value.Value);
			}

			return instance;
		}
	}
}
