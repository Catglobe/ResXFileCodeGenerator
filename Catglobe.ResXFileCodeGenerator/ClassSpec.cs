namespace Catglobe.ResXFileCodeGenerator;

internal sealed record ClassSpec(SettingsData ClassSettings, LocationWrapper Location, SymbolReference SymbolReference);

/// <summary>
/// This must match 100% the ResxSettingsAttribute
/// </summary>
internal sealed record SettingsData
{
	/// <summary>
	/// Set if the inner class should have static members.
	/// </summary>
	public bool StaticMembers { get; set; } = true;
	/// <summary>
	/// Set the visibility of the inner class. Default is private.
	/// </summary>
	public Visibility MembersVisibility { get; set; } = Visibility.Private;

	/// <summary>
	/// Set if it should add a helper to get translations for Enum members of the given type
	/// </summary>
	public INamedTypeSymbol? ForEnum { get; set; }

	/// <summary>
	/// Similar to ForEnum, but more generic in that all keys are available
	/// </summary>
	public bool GenerateLookup{ get; set; }

}

