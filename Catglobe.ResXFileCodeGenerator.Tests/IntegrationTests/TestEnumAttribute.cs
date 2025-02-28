namespace Catglobe.ResXFileCodeGenerator.Tests.IntegrationTests;

public partial class Y
{
	internal partial class X
	{
		[ResxSettings(ForEnum = typeof(XEnum), MembersVisibility = Visibility.Public)]
		private partial class Z<T3>
		{

		}

		public enum XEnum
		{
			Enum1 = 0,
			Enum2 = 1
		}
	}

}

