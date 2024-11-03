using System.Web;

namespace Catglobe.ResXFileCodeGenerator;

public sealed partial class StringBuilderGenerator : IGenerator
{
	private static void CreateMemberKey(
		string indent,
		StringBuilder builder,
		FileOptions options,
		string name,
		string value,
		IXmlLineInfo line,
		HashSet<string> alreadyAddedMembers,
		List<Diagnostic> errorsAndWarnings,
		string containerclassname
	)
	{
		if (!GenerateMemberKey(indent, builder, options, name, value, line, alreadyAddedMembers, errorsAndWarnings, containerclassname, out var resourceAccessByName))
		{
			return;
		}

		if (resourceAccessByName)
		{
			// => name
            builder.Append(" = nameof(");
			builder.Append(name);
			builder.Append(")");
		}
		else
		{
			// => "name"
			// replace " with \"
			builder.Append(" = \"");
			builder.Append(name.Replace(@"""", @"\"""));
			builder.Append("\"");
		}

		builder.AppendLineLF(";");
	}

	private static bool GenerateMemberKey(
	  string indent,
	  StringBuilder builder,
	  FileOptions options,
	  string name,
	  string neutralValue,
	  IXmlLineInfo line,
	  HashSet<string> alreadyAddedMembers,
	  List<Diagnostic> errorsAndWarnings,
	  string containerclassname,
	  out bool resourceAccessByName
  )
	{
		string memberName;

		if (s_validMemberNamePattern.IsMatch(name))
		{
			memberName = name;
			resourceAccessByName = true;
		}
		else
		{
			memberName = s_invalidMemberNameSymbols.Replace(name, "_");
			resourceAccessByName = false;
		}

		static Location GetMemberLocation(FileOptions fileOptions, IXmlLineInfo line, string memberName) =>
			Location.Create(
				filePath: fileOptions.GroupedFile.MainFile.File.Path,
				textSpan: new TextSpan(),
				lineSpan: new LinePositionSpan(
					start: new LinePosition(line.LineNumber - 1, line.LinePosition - 1),
					end: new LinePosition(line.LineNumber - 1, line.LinePosition - 1 + memberName.Length)
				)
			);

		if (!alreadyAddedMembers.Add(memberName))
		{
			errorsAndWarnings.Add(Diagnostic.Create(
				descriptor: s_duplicateWarning,
				location: GetMemberLocation(options, line, memberName), memberName
			));
			return false;
		}

		if (memberName == containerclassname)
		{
			errorsAndWarnings.Add(Diagnostic.Create(
				descriptor: s_memberSameAsClassWarning,
				location: GetMemberLocation(options, line, memberName), memberName
			));
			return false;
		}

		builder.AppendLineLF();

		builder.Append(indent);
		builder.AppendLineLF("/// <summary>");

		builder.Append(indent);
		builder.Append("/// Name of resource ");
		builder.Append(name);
		builder.AppendLineLF(".");

		builder.Append(indent);
		builder.AppendLineLF("/// </summary>");

		builder.Append(indent);
		builder.Append("public const string ");
		builder.Append(memberName);
		return true;
	}

	private void KeyGeneration(
		FileOptions options,
		SourceText content,
		string indent,
		string containerClassName,
		StringBuilder builder,
		List<Diagnostic> errorsAndWarnings,
		CancellationToken cancellationToken
	)
	{
		var members = ReadResxFile(content);
		if (members is null)
		{
			return;
		}

		var alreadyAddedMembers = new HashSet<string>() { Constants.CultureInfoVariable };
		foreach (var (key, value, line) in members)
		{
			cancellationToken.ThrowIfCancellationRequested();
			CreateMemberKey(
				indent,
				builder,
				options,
				key,
				value,
				line,
				alreadyAddedMembers,
				errorsAndWarnings,
				containerClassName
			);
		}
	}
}
