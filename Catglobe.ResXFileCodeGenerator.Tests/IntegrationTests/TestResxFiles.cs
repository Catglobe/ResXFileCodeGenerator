namespace Catglobe.ResXFileCodeGenerator.Tests.IntegrationTests;

public class TestResxFiles
{
	[Fact]
	public void TestNormalResourceGen()
	{
		Thread.CurrentThread.CurrentUICulture = new CultureInfo("da");
		Test1.CreateDate.ShouldBe("OldestDa");
		Thread.CurrentThread.CurrentUICulture = new CultureInfo("en");
		Test1.CreateDate.ShouldBe("Oldest");
		Thread.CurrentThread.CurrentUICulture = new CultureInfo("ch");
		Test1.CreateDate.ShouldBe("Oldest");
		Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-us");
		Test1.CreateDate.ShouldBe("OldestEnUs");
		Thread.CurrentThread.CurrentUICulture = new CultureInfo("da-DK");
		Test1.CreateDate.ShouldBe("OldestDaDK");
	}
	[Fact]
	public void TestCodeGenResourceGen()
	{
		Thread.CurrentThread.CurrentUICulture = new CultureInfo("da");
		Test2.CreateDate.ShouldBe("OldestDa");
		Thread.CurrentThread.CurrentUICulture = new CultureInfo("en");
		Test2.CreateDate.ShouldBe("Oldest");
		Thread.CurrentThread.CurrentUICulture = new CultureInfo("ch");
		Test2.CreateDate.ShouldBe("Oldest");
		Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-us");
		Test2.CreateDate.ShouldBe("OldestEnUs");
		Thread.CurrentThread.CurrentUICulture = new CultureInfo("da-DK");
		Test2.CreateDate.ShouldBe("OldestDaDK");
	}

}
