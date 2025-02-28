namespace Catglobe.ResXFileCodeGenerator;

internal sealed record EnumSpec(
	EnumSettingsData EnumSettings,
	LocationWrapper Location,
	ImmutableEquatableArray<EnumMemberSpec> Members,
	SymbolReference SymbolReference);

internal sealed record EnumMemberSpec(string Name);

/// <summary>
/// This must match 100% the ResxEnumSettingsAttribute
/// </summary>
internal sealed record EnumSettingsData
{
	/// <summary>
	/// Set the prefix of the enum resource class.
	/// </summary>
	public string? Prefix { get; set; }
	/// <summary>
	/// Set the postfix of the enum resource class.
	/// </summary>
	public string? Postfix { get; set; }
	/// <summary>
	/// Set if the class should have static members.
	/// </summary>
	public bool StaticMembers { get; set; }
}
