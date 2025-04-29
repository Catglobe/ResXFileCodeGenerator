namespace Catglobe.ResXFileCodeGenerator;

internal sealed record ResxGroup
{
	private static readonly DiagnosticDescriptor s_duplicateBasename = new(
		id: "CatglobeResXFileCodeGenerator004",
		title: "Invalid culture",
		messageFormat: "Set of resx contains invalid culture - ignored set",
		category: "ResXFileCodeGenerator",
		defaultSeverity: DiagnosticSeverity.Warning,
		isEnabledByDefault: true
	);

	private static readonly DiagnosticDescriptor s_noDefault = new(
		id: "CatglobeResXFileCodeGenerator005",
		title: "No base resx",
		messageFormat: "Set missing default file - ignored set",
		category: "ResXFileCodeGenerator",
		defaultSeverity: DiagnosticSeverity.Warning,
		isEnabledByDefault: true
	);

	private readonly ResxFile? _mainFile;

	/// <summary>
	/// Basename is the name of the file without culture and filetype
	/// </summary>
	public string Basename => MainFile?.Basename!;

	/// <summary>
	/// MainFile is the file without culture
	/// </summary>
	public ResxFile MainFile => _mainFile!;

	/// <summary>
	/// SubFiles are ordered by culture LCID
	/// </summary>
	public ImmutableEquatableArray<ResxFile> SubFiles { get; }

	/// <summary>
	/// Error is set if the group is invalid
	/// </summary>
	public Diagnostic? Error { get; }

	public ResxGroup(IReadOnlyList<ResxFile> resx)
	{
		_mainFile = null;
		SubFiles = ImmutableEquatableArray<ResxFile>.Empty;
		try
		{
			var mainFile = resx.SingleOrDefault(x => x.Culture is null);
			if (mainFile is null)
			{
				Error = Diagnostic.Create(
					descriptor: s_noDefault,
					location: Location.Create(resx.First().File.Path, new(), new()),
					additionalLocations: resx.Skip(1).Select(x => Location.Create(x.File.Path, new(), new())).ToList()
				);
				return;
			}

			_mainFile = mainFile;
		}
		catch
		{
			Error = Diagnostic.Create(
				descriptor: s_duplicateBasename,
				location: Location.Create(resx.First().File.Path, new(), new()),
				additionalLocations: resx.Skip(1).Select(x => Location.Create(x.File.Path, new(), new())).ToList()
			);
			return;
		}
		//order by culture length, so that the more specific are first, e.g. da-DK is more specific than da, then by LCID to make it stable
		SubFiles = new([.. resx.Where(x => x.Culture is not null).OrderByDescending(x => x.CultureIso!.Length).ThenBy(x=>x.Culture!.LCID)]);
	}

	public bool Equals(ResxGroup? other)
	{
		if (other is null)
		{
			return false;
		}

		if (ReferenceEquals(this, other))
		{
			return true;
		}

		return Basename == other.Basename && _mainFile == other._mainFile && SubFiles.Equals(other.SubFiles);
	}

	public override int GetHashCode()
	{
		unchecked
		{
			var hashCode = Basename.GetHashCode();
			hashCode = (hashCode * 397) ^ (_mainFile?.GetHashCode() ?? 0);
			hashCode = (hashCode * 397) ^ SubFiles.GetHashCode();
			return hashCode;
		}
	}

	public override string ToString()
	{
		return $"{nameof(MainFile)}: {_mainFile}, {nameof(SubFiles)}: {string.Join("; ", SubFiles)}";
	}
}
