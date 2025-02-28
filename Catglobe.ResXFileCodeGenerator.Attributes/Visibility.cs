namespace Catglobe.ResXFileCodeGenerator;

/// <summary>
/// Visibility for the generated class/properties
/// </summary>
public enum Visibility
{
	/// <summary>
	/// Used to indicate that the visibility is set elsewhere, or use default
	/// </summary>
	NotSet = -1,
	/// <summary>
	/// For inner classes, indicates that the inner class should not be generated
	/// </summary>
	NotGenerated = 0,
	/// <summary>
	/// Public visibility
	/// </summary>
	Public,
	/// <summary>
	/// Internal visibility
	/// </summary>
	Internal,
	/// <summary>
	/// Private visibility
	/// </summary>
	Private,
	/// <summary>
	/// Protected visibility
	/// </summary>
	Protected,
	/// <summary>
	/// For inner classes, use the same visibility as the outer class
	/// </summary>
	SameAsOuter
}
