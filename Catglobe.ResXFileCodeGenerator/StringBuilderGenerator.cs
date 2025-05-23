﻿using System.Diagnostics;
using System.Web;
using System.Xml.Linq;

namespace Catglobe.ResXFileCodeGenerator;

internal sealed partial class StringBuilderGenerator : IGenerator
{
    private static readonly Regex s_validMemberNamePattern = new(
        pattern: @"^[\p{L}\p{Nl}_][\p{Cf}\p{L}\p{Mc}\p{Mn}\p{Nd}\p{Nl}\p{Pc}]*$",
        options: RegexOptions.Compiled | RegexOptions.CultureInvariant
    );

    private static readonly Regex s_invalidMemberNameSymbols = new(
        pattern: @"[^\p{Cf}\p{L}\p{Mc}\p{Mn}\p{Nd}\p{Nl}\p{Pc}]",
        options: RegexOptions.Compiled | RegexOptions.CultureInvariant
    );

    private static readonly DiagnosticDescriptor s_duplicateWarning = new(
        id: "CatglobeResXFileCodeGenerator001",
        title: "Duplicate member",
        messageFormat: "Ignored added member '{0}'",
        category: "ResXFileCodeGenerator",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true
    );

    private static readonly DiagnosticDescriptor s_memberSameAsClassWarning = new(
        id: "CatglobeResXFileCodeGenerator002",
        title: "Member same name as class",
        messageFormat: "Ignored member '{0}' has same name as class",
        category: "ResXFileCodeGenerator",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true
    );

    private static readonly DiagnosticDescriptor s_memberWithStaticError = new(
        id: "CatglobeResXFileCodeGenerator003",
        title: "Incompatible settings",
        messageFormat: "Cannot have static members/class with an class instance",
        category: "ResXFileCodeGenerator",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );
    private static readonly DiagnosticDescriptor s_cannotParseFile = new(
	    id: "CatglobeResXFileCodeGenerator006",
	    title: "Cannot read file",
	    messageFormat: "Problem reading the resx file",
	    category: "ResXFileCodeGenerator",
	    defaultSeverity: DiagnosticSeverity.Error,
	    isEnabledByDefault: true
    );
    private static readonly DiagnosticDescriptor s_spuriousKey = new(
	    id: "CatglobeResXFileCodeGenerator007",
	    title: "Spurious key",
	    messageFormat: "Key '{0}' does not match a neutral culture key",
	    category: "ResXFileCodeGenerator",
	    defaultSeverity: DiagnosticSeverity.Warning,
	    isEnabledByDefault: true
    );
    public (
        string GeneratedFileName,
        string SourceCode,
        ICollection<Diagnostic> ErrorsAndWarnings
    ) Generate(
        FileOptions options,
		ClassSpec? classSpec = null,
		CancellationToken cancellationToken = default
    )
    {
        var errorsAndWarnings = new List<Diagnostic>();

        StringBuilder builder;
        var indent = 0;
        //TODO better generatedFileName for classSpec
        var generatedFileName = $"{options.LocalNamespace}.{options.ClassName}.g.cs";

        if (classSpec is not null)
        {
	        var sr = classSpec.SymbolReference;
	        builder = new();

	        builder.AppendLineLF(Constants.AutoGeneratedHeader);
	        builder.AppendLineLF("#nullable enable");

	        if (sr.Namespace is {} ns)
	        {
		        builder.Append("namespace ");
		        builder.Append(ns);
		        builder.AppendLineLF(";");
	        }

	        if (classSpec.ClassSettings.ForEnum is { } enumRef)
	        {
		        builder.Append("using __ENUM = ");
		        builder.Append(enumRef.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
		        builder.AppendLineLF(";");
	        }
	        if (options.UseResManager)
		        AppendCodeUsings(builder);
	        else
		        AppendResourceManagerUsings(builder);

	        foreach (var symbol in sr.ClassDeclChain.Reverse())
	        {
				builder.Append(' ', indent);
				builder.Append(symbol);
				builder.AppendLineLF(" {");
				indent += 4;
			}

	        var settings = classSpec.ClassSettings;
			options = options with
			{
				//never generate inner class when it is declared on a type... Then that type is the target
				InnerClassVisibility = Visibility.NotGenerated,
				MemberVisibility = settings.MembersVisibility.ToString().ToLowerInvariant(),
				StaticMembers = settings.StaticMembers || classSpec.SymbolReference.TheType.TypeSymbol.IsStatic,
			};
        }
        else {
	        builder = GetBuilder(options.CustomToolNamespace ?? options.LocalNamespace);

	        if (options.UseResManager)
		        AppendCodeUsings(builder);
	        else
		        AppendResourceManagerUsings(builder);

	        builder.Append(options.PublicClass ? "public" : "internal");
	        builder.Append(options.StaticClass ? " static" : string.Empty);
	        builder.Append(options.PartialClass ? " partial class " : " class ");
	        builder.AppendLineLF(options.ClassName);
	        builder.AppendLineLF("{");
	        indent += 4;
        }

        string containerClassName = options.ClassName;

        if (options.InnerClassVisibility != Visibility.NotGenerated)
        {
	        containerClassName = string.IsNullOrEmpty(options.InnerClassName) ? "Resources" : options.InnerClassName;
	        if (!string.IsNullOrEmpty(options.InnerClassInstanceName))
            {
	            if (options.StaticClass || options.StaticMembers)
                {
                    errorsAndWarnings.Add(Diagnostic.Create(
                        descriptor: s_memberWithStaticError,
                        location: Location.Create(
                            filePath: options.GroupedFile.MainFile.File.Path,
                            textSpan: new(),
                            lineSpan: new()
                        )
                    ));
                }

                builder.Append(' ', indent);
				//cant decide if this should use the MemberVisibility or just always public
				builder.Append(options.MemberVisibility);
                builder.Append(' ');
                builder.Append(containerClassName);
                builder.Append(' ');
                builder.Append(options.InnerClassInstanceName);
                builder.AppendLineLF(" { get; } = new();");
                builder.AppendLineLF();
            }

            builder.Append(' ', indent);
            builder.Append(options.InnerClassVisibility == Visibility.SameAsOuter
                ? options.PublicClass ? "public" : "internal"
                : options.InnerClassVisibility.ToString().ToLowerInvariant());
            builder.Append(options.StaticClass ? " static" : string.Empty);
            builder.Append(options.PartialClass ? " partial class " : " class ");

            builder.AppendLineLF(containerClassName);
            builder.Append(' ', indent);
            builder.AppendLineLF("{");

            indent += 4;
        }

        var parsed = ParseResxFiles(options, errorsAndWarnings, cancellationToken);
		if (parsed is null) return (generatedFileName, "", errorsAndWarnings);
		if (options.UseResManager)
            GenerateCode(parsed.Value, options,  indent, containerClassName, builder, errorsAndWarnings, cancellationToken);
        else
            GenerateResourceManager(parsed.Value, options, indent, containerClassName, builder, errorsAndWarnings, cancellationToken);

        if (options.InnerClassVisibility != Visibility.NotGenerated)
        {
	        indent -= 4;
	        builder.Append(' ', indent);
	        builder.AppendLineLF("}");
        }

        if (classSpec is not null)
        {
	        if (classSpec.ClassSettings.ForEnum is { } enumRef)
		        GenerateEnumLookup(parsed.Value, options, enumRef, indent, builder, errorsAndWarnings, cancellationToken);
	        if (classSpec.ClassSettings.GenerateLookup)
		        GenerateLookup(parsed.Value, options, indent, builder, errorsAndWarnings, cancellationToken);


	        var sr = classSpec.SymbolReference;

	        foreach (var _ in sr.ClassDeclChain)
	        {
		        indent -= 4;
		        builder.Append(' ', indent);
		        builder.AppendLineLF("}");
	        }
        }
        else
        {
	        indent -= 4;
	        builder.AppendLineLF("}");
        }
        Debug.Assert(indent == 0);

        return (
            GeneratedFileName: generatedFileName,
            SourceCode: builder.ToString(),
            ErrorsAndWarnings: errorsAndWarnings
        );
    }

    private (Dictionary<string, (string, IXmlLineInfo)> main, List<Dictionary<string, (string, IXmlLineInfo)>> subfiles)? ParseResxFiles(FileOptions options,
	    List<Diagnostic> errorsAndWarnings, CancellationToken cancellationToken)
    {
	    if (!ReadSingleResxFile(options.GroupedFile.MainFile, out var main))
	    {
		    return null;
	    }

	    var subfiles = new List<Dictionary<string, (string, IXmlLineInfo)>>();

	    foreach (var lang in options.GroupedFile.SubFiles)
	    {
		    if (!ReadSingleResxFile(lang, out var dictionary, main))
		    {
			    return null;
		    }

		    subfiles.Add(dictionary);
	    }

	    return (main, subfiles);
	    bool ReadSingleResxFile(ResxFile lang, out Dictionary<string, (string, IXmlLineInfo)> dictionary,
		    Dictionary<string, (string, IXmlLineInfo)>? fallback = null)
	    {
		    dictionary = new();
		    var resxFile = ReadResxFile(lang.Content);
		    if (resxFile == null)
		    {
			    errorsAndWarnings.Add(Diagnostic.Create(s_cannotParseFile,
				    Location.Create(lang.File.Path, new(), new())));
			    return false;
		    }

		    foreach (var entry in resxFile)
		    {
			    if (fallback is not null && !fallback.ContainsKey(entry.key))
				    errorsAndWarnings.Add(Diagnostic.Create(s_spuriousKey,
					    GetMemberLocation(lang, entry.line, entry.key), entry.key));
			    else if (dictionary.ContainsKey(entry.key))
				    errorsAndWarnings.Add(Diagnostic.Create(s_duplicateWarning,
					    GetMemberLocation(lang, entry.line, entry.key), entry.key));
			    else
				    dictionary.Add(entry.key, (entry.value, entry.line));
		    }

		    return true;
	    }

		
	    static IEnumerable<(string key, string value, IXmlLineInfo line)>? ReadResxFile(SourceText? content)
	    {
		    if (content is null) return null;
		    using var reader = new StringReader(content.ToString());
		    try
		    {
			    if (XDocument.Load(reader, LoadOptions.SetLineInfo).Root is { } element)
				    return element
					    .Descendants()
					    .Where(static data => data.Name == "data")
					    .Select(static data => (
						    key: data.Attribute("name")!.Value,
						    value: data.Descendants("value").First().Value,
						    line: (IXmlLineInfo)data.Attribute("name")!
					    ));
		    }
		    catch (Exception)
		    {
			    return null;
		    }

		    return null;
	    }
    }

    private static Location GetMemberLocation(ResxFile fileOptions, IXmlLineInfo line, string memberName) =>
	    Location.Create(
		    filePath: fileOptions.File.Path,
		    textSpan: new(),
		    lineSpan: new(
			    start: new(line.LineNumber - 1, line.LinePosition - 1),
			    end: new(line.LineNumber - 1, line.LinePosition - 1 + memberName.Length)
		    )
	    );


    private static bool GenerateMember(
        int indent,
        StringBuilder builder,
        FileOptions options,
        string name,
        string neutralValue,
        IXmlLineInfo line,
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

        if (memberName == containerclassname)
        {
            errorsAndWarnings.Add(Diagnostic.Create(
                descriptor: s_memberSameAsClassWarning,
                location: GetMemberLocation(options.GroupedFile.MainFile, line, memberName), memberName
            ));
            return false;
        }

        builder.AppendLineLF();

        builder.Append(' ', indent);
        builder.AppendLineLF("/// <summary>");

        builder.Append(' ', indent);
        builder.Append("/// Looks up a localized string similar to ");
        builder.Append(HttpUtility.HtmlEncode(neutralValue.Trim().Replace("\r\n", "\n").Replace("\r", "\n")
            .Replace("\n", $"\n{new string(' ', indent)}/// ")));
        builder.AppendLineLF(".");

        builder.Append(' ', indent);
        builder.AppendLineLF("/// </summary>");

        builder.Append(' ', indent);
        builder.Append(options.MemberVisibility);
        builder.Append(' ');
        builder.Append(options.StaticMembers ? "static " : string.Empty);
        builder.Append("string");
		if (!options.NullForgivingOperators)
			builder.Append('?');
		builder.Append(' ');
        builder.Append(memberName);
        return true;
    }

    private static StringBuilder GetBuilder(string withnamespace)
    {
        var builder = new StringBuilder();
        
        builder.AppendLineLF(Constants.AutoGeneratedHeader);
        builder.AppendLineLF("#nullable enable");

        builder.Append("namespace ");
        builder.Append(withnamespace);
        builder.AppendLineLF(";");

        return builder;
    }

}
