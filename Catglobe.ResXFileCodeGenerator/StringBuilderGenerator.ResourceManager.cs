namespace Catglobe.ResXFileCodeGenerator;

internal sealed partial class StringBuilderGenerator : IGenerator
{
    private void GenerateResourceManager(
	    (Dictionary<string, (string, IXmlLineInfo)> main, List<Dictionary<string, (string, IXmlLineInfo)>> subfiles)
		    parsed, FileOptions options,
	    int indent,
	    string containerClassName,
	    StringBuilder builder,
	    List<Diagnostic> errorsAndWarnings,
	    CancellationToken cancellationToken)
    {
        GenerateResourceManagerMembers(builder, indent, containerClassName, options);

        var (fallback, _) = parsed;
        foreach (var kvp in fallback)
        {
	        var (key, line, value) = (kvp.Key, kvp.Value.Item2, kvp.Value.Item1);
            cancellationToken.ThrowIfCancellationRequested();
            CreateMember(
                indent,
                builder,
                options,
                key,
                value,
                line,
                errorsAndWarnings,
                containerClassName
            );
        }
    }
    
    private static void CreateMember(
        int indent,
        StringBuilder builder,
        FileOptions options,
        string name,
        string value,
        IXmlLineInfo line,
        List<Diagnostic> errorsAndWarnings,
        string containerclassname
    )
    {
        if (!GenerateMember(indent, builder, options, name, value, line, errorsAndWarnings, containerclassname, out var resourceAccessByName))
        {
            return;
        }

        if (resourceAccessByName)
        {
            builder.Append(" => ResourceManager.GetString(nameof(");
            builder.Append(name);
            builder.Append("), ");
        }
        else
        {
            builder.Append(@" => ResourceManager.GetString(""");
            builder.Append(name.Replace(@"""", @"\"""));
            builder.Append(@""", ");
        }

        builder.Append(Constants.CultureInfoVariable);
        builder.Append(")");
        builder.Append(options.NullForgivingOperators ? "!" : null);
        builder.AppendLineLF(";");
    }

    private static void AppendResourceManagerUsings(StringBuilder builder)
    {
        builder.Append("using ");
        builder.Append(Constants.SystemGlobalization);
        builder.AppendLineLF(";");

        builder.Append("using ");
        builder.Append(Constants.SystemResources);
        builder.AppendLineLF(";");

        builder.AppendLineLF();
    }

    private static void GenerateResourceManagerMembers(
        StringBuilder builder,
        int indent,
        string containerClassName,
        FileOptions options
    )
    {
	    builder.Append(' ', indent);
        builder.Append("private static ");
        builder.Append(nameof(ResourceManager));
        builder.Append("? ");
        builder.Append(Constants.s_resourceManagerVariable);
        builder.AppendLineLF(";");

        builder.Append(' ', indent);
        builder.Append("public static ");
        builder.Append(nameof(ResourceManager));
        builder.Append(" ");
        builder.Append(Constants.ResourceManagerVariable);
        builder.Append(" => ");
        builder.Append(Constants.s_resourceManagerVariable);
        builder.Append(" ??= new ");
        builder.Append(nameof(ResourceManager));
        builder.Append("(\"");
        builder.Append(options.EmbeddedFilename);
        builder.Append("\", typeof(");
        builder.Append(containerClassName);
        builder.AppendLineLF(").Assembly);");

        builder.Append(' ', indent);
        builder.Append("public ");
        builder.Append(options.StaticMembers ? "static " : string.Empty);
        builder.Append(nameof(CultureInfo));
        builder.Append("? ");
        builder.Append(Constants.CultureInfoVariable);
        builder.AppendLineLF(" { get; set; }");
    }
}
