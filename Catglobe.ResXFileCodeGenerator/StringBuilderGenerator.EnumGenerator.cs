namespace Catglobe.ResXFileCodeGenerator;

internal sealed partial class StringBuilderGenerator : IGenerator
{
	private static readonly DiagnosticDescriptor s_NotEnum = new(
		id: "CatglobeResXFileCodeGenerator011",
		title: "References non-enum",
		messageFormat: "'{0}' is not an enum",
		category: "ResXFileCodeGenerator",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true
	);
	private static readonly DiagnosticDescriptor s_missingEnum = new(
		id: "CatglobeResXFileCodeGenerator012",
		title: "Missing enum member",
		messageFormat: "'{1}' in '{0}' is not translated",
		category: "ResXFileCodeGenerator",
		defaultSeverity: DiagnosticSeverity.Info,
		isEnabledByDefault: true
	);
	private void GenerateEnumLookup(
		(Dictionary<string, (string, IXmlLineInfo)> main, List<Dictionary<string, (string, IXmlLineInfo)>> subfiles)
			parsed, FileOptions options, INamedTypeSymbol enumRef, int indent, StringBuilder builder,
		List<Diagnostic> errorsAndWarnings,
		CancellationToken cancellationToken)
	{
		if (enumRef.TypeKind != TypeKind.Enum)
		{
			errorsAndWarnings.Add(Diagnostic.Create(s_NotEnum, enumRef.Locations.FirstOrDefault()??Location.None, enumRef.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat)));
			return;
		}
		builder.Append(' ', indent);
		builder.AppendLineLF("/// <summary>");
		builder.Append(' ', indent);
		builder.AppendLineLF("/// Looks up a localized string for the given enum.");
		builder.Append(' ', indent);
		builder.AppendLineLF("/// </summary>");

		builder.Append(' ', indent);
		builder.Append(options.MemberVisibility);
		builder.Append(options.StaticMembers ? " static" : string.Empty);
		builder.Append(" string");
		if (!options.NullForgivingOperators)
			builder.Append('?');
		builder.Append(" ToString(");
		builder.Append(enumRef.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
		builder.AppendLineLF(" e) => e switch {");
		indent += 4;
		foreach (var memberName in enumRef.MemberNames)
		{
			builder.Append(' ', indent);
			builder.Append("__ENUM.");
			builder.Append(memberName);
			builder.Append(" => ");
			if (parsed.main.ContainsKey(memberName))
			{
				builder.Append(memberName);
			}
			else
			{
				errorsAndWarnings.Add(Diagnostic.Create(s_missingEnum, enumRef.Locations.FirstOrDefault()??Location.None, enumRef.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat), memberName));
				builder.Append("string.Empty");
			}

			builder.AppendLineLF(",");
		}
		builder.Append(' ', indent);
		builder.AppendLineLF(options.NullForgivingOperators ? "_ => string.Empty," : "_ => null,");
		indent -= 4;
		builder.Append(' ', indent);
		builder.AppendLineLF("};");
		builder.AppendLineLF();
	}
	private void GenerateLookup(
		(Dictionary<string, (string, IXmlLineInfo)> main, List<Dictionary<string, (string, IXmlLineInfo)>> subfiles)
			parsed, FileOptions options, int indent, StringBuilder builder, List<Diagnostic> errorsAndWarnings, CancellationToken cancellationToken)
	{
		builder.Append(' ', indent);
		builder.AppendLineLF("/// <summary>");
		builder.Append(' ', indent);
		builder.AppendLineLF("/// Looks up a localized string for the given key.");
		builder.Append(' ', indent);
		builder.AppendLineLF("/// </summary>");

		builder.Append(' ', indent);
		builder.Append(options.MemberVisibility);
		builder.Append(options.StaticMembers ? " static" : string.Empty);
		builder.Append(" string");
		if (!options.NullForgivingOperators)
			builder.Append('?');
		builder.AppendLineLF(" ToString(string key) => key switch {");
		indent += 4;
		foreach (var memberName in parsed.main)
		{
			builder.Append(' ', indent);
			builder.Append('"');
			builder.Append(memberName.Key);
			builder.Append('"');
			builder.Append(" => ");
			builder.Append(memberName.Key);
			builder.AppendLineLF(",");
		}
		builder.Append(' ', indent);
		builder.AppendLineLF(options.NullForgivingOperators ? "_ => string.Empty," : "_ => null,");
		indent -= 4;
		builder.Append(' ', indent);
		builder.AppendLineLF("};");
		builder.AppendLineLF();
	}
}
