namespace Catglobe.ResXFileCodeGenerator.Tests.IntegrationTests;

public interface IX
{

}

public partial class Z<T1> : IX
{
	internal partial class Y<T2>
	{
		[ResxSettings()]
		private partial class Z<T3>
		{

		}
	}
}

