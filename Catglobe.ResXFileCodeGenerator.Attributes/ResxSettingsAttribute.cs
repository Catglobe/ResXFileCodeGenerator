using System;

namespace Catglobe.ResXFileCodeGenerator;

/// <summary>
/// Attribute force generation of matching resx files to this class instead of a class matching the filename.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
[System.Diagnostics.Conditional("ResxSettingsAttribute_NEVER")]
public class ResxSettingsAttribute() : Attribute
{
	/// <summary>
	/// Set if the class should have static members. Default is true.
	/// </summary>
	public bool StaticMembers { get; set; } = true;
	/// <summary>
	/// Set the visibility of the members. Default is private.
	/// </summary>
	public Visibility MembersVisibility { get; set; } = Visibility.Private;

	/// <summary>
	/// Set if it should add a helper to get translations for Enum members of the given type
	/// </summary>
	public Type? ForEnum { get; set; }
}

