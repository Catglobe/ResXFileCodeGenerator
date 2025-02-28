using System.Xml.Linq;

namespace Catglobe.ResXFileCodeGenerator;

internal sealed partial class StringBuilderGenerator : IGenerator
{
	static readonly Dictionary<int, List<int>> s_allChildren = new();

	/// <summary>
	/// Build all cultureinfo children
	/// </summary>
	static StringBuilderGenerator()
	{
		var all = CultureInfo.GetCultures(CultureTypes.AllCultures);

		foreach (var cultureInfo in all)
		{
			if (cultureInfo.LCID == 4096 || cultureInfo.IsNeutralCulture || cultureInfo.Name.IsNullOrEmpty())
			{
				continue;
			}
			var parent = cultureInfo.Parent;
			if (!s_allChildren.TryGetValue(parent.LCID, out var v))
				s_allChildren[parent.LCID] = v = [];
			v.Add(cultureInfo.LCID);
		}
	}

	public (
		string GeneratedFileName,
		string SourceCode,
		ICollection<Diagnostic> ErrorsAndWarnings
		) Generate(
			CultureInfoCombo combo,
			CancellationToken cancellationToken
		)
	{
		var definedLanguages = combo.CultureInfos;
		var builder = GetBuilder("Catglobe.ResXFileCodeGenerator");

		builder.AppendLineLF("internal static partial class Helpers");
		builder.AppendLineLF("{");

		builder.Append("    public static string GetString_");
		var functionNamePostFix = FunctionNamePostFix(definedLanguages);
		builder.Append(functionNamePostFix);
		builder.Append("(string fallback");
		foreach (var resx in definedLanguages)
		{
			builder.Append(", ");
			builder.Append("string ");
			builder.Append(resx.CultureIso!.Replace('-', '_'));
		}

		builder.Append(") => ");
		builder.Append(Constants.SystemGlobalization);
		builder.AppendLineLF(".CultureInfo.CurrentUICulture.LCID switch");
		builder.AppendLineLF("    {");
		var already = new HashSet<int>();
		foreach (var resx in definedLanguages)
		{
			static IEnumerable<int> FindParents(int toFind)
			{
				yield return toFind;
				if (!s_allChildren.TryGetValue(toFind, out var v))
				{
					yield break;
				}

				foreach (var parents in v)
				{
					yield return parents;
				}
			}

			foreach (var parent in FindParents(resx.Culture!.LCID))
			{
				if (!already.Add(parent)) continue;
				builder.Append("        ");
				builder.Append(parent);
				builder.Append(" => ");
				builder.Append(resx.CultureIso!.Replace('-', '_'));
				builder.AppendLineLF(",");
			}
		}

		builder.AppendLineLF("        _ => fallback");
		builder.AppendLineLF("    };");
		builder.AppendLineLF("}");

		return (
			GeneratedFileName: "Catglobe.ResXFileCodeGenerator." + functionNamePostFix + ".g.cs",
			SourceCode: builder.ToString(),
			ErrorsAndWarnings: Array.Empty<Diagnostic>()
		);
	}

	private static string FunctionNamePostFix(IReadOnlyList<ResxFile> definedLanguages)
		=> string.Join("_", definedLanguages.Select(x => x.Culture!.LCID));

	private static void AppendCodeUsings(StringBuilder builder)
	{
		builder.AppendLineLF("using static global::Catglobe.ResXFileCodeGenerator.Helpers;");
		builder.AppendLineLF();
	}

	private void GenerateCode(
		(Dictionary<string, (string, IXmlLineInfo)> main, List<Dictionary<string, (string, IXmlLineInfo)>> subfiles)
			parsed, FileOptions options,
		int indent,
		string containerClassName,
		StringBuilder builder,
		List<Diagnostic> errorsAndWarnings,
		CancellationToken cancellationToken)
	{
		var (fallback, subfiles) = parsed;
		foreach (var kvp in fallback)
		{
			var (key, line, value) = (kvp.Key, kvp.Value.Item2, kvp.Value.Item1);
			cancellationToken.ThrowIfCancellationRequested();
			if (
				!GenerateMember(
					indent,
					builder,
					options,
					key,
					value,
					line,
					errorsAndWarnings,
					containerClassName,
					out _
				)
			)
			{
				continue;
			}

			builder.Append(" => GetString_");
			builder.Append(FunctionNamePostFix(options.GroupedFile.SubFiles));
			builder.Append("(");
			builder.Append(SymbolDisplay.FormatLiteral(value, true));

			foreach (var xml in subfiles)
			{
				builder.Append(", ");
				builder.Append(SymbolDisplay.FormatLiteral(xml.TryGetValue(key, out var langValue) ? langValue.Item1 : value, true));
			}

			builder.AppendLineLF(");");
		}
	}
	
}
