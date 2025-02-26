namespace Catglobe.ResXFileCodeGenerator.Tests;

public class GroupResxFilesTests
{
    [Fact]
    public void CompareResxGroup_SameRoot_SameSubFiles_DifferentOrder()
    {
        var v1 = new ResxGroup([
	        ResxFile.From(new AdditionalTextStub(@"D:\src\xhg\y\Areas\CaModule\Pages\IdfgControlCenter.resx"))!,
	        ResxFile.From(new AdditionalTextStub(@"D:\src\xhg\y\Areas\CaModule\Pages\IdfgControlCenter.da.resx"))!,
            ResxFile.From(new AdditionalTextStub(@"D:\src\xhg\y\Areas\CaModule\Pages\IdfgControlCenter.vi.resx"))!,
        ]);

        var v2 = new ResxGroup([
	        ResxFile.From(new AdditionalTextStub(@"D:\src\xhg\y\Areas\CaModule\Pages\IdfgControlCenter.resx"))!,
	        ResxFile.From(new AdditionalTextStub(@"D:\src\xhg\y\Areas\CaModule\Pages\IdfgControlCenter.vi.resx"))!,
	        ResxFile.From(new AdditionalTextStub(@"D:\src\xhg\y\Areas\CaModule\Pages\IdfgControlCenter.da.resx"))!,
        ]);

        v1.ShouldBe(v2);
    }

    [Fact]
    public void CompareResxGroup_SameRoot_DiffSubFilesNames()
    {
	    var v1 = new ResxGroup([
		    ResxFile.From(new AdditionalTextStub(@"D:\src\xhg\y\Areas\CaModule\Pages\IdfgControlCenter.resx"))!,
		    ResxFile.From(new AdditionalTextStub(@"D:\src\xhg\y\Areas\CaModule\Pages\IdfgControlCenter.en.resx"))!,
		    ResxFile.From(new AdditionalTextStub(@"D:\src\xhg\y\Areas\CaModule\Pages\IdfgControlCenter.fr.resx"))!,
	    ]);

	    var v2 = new ResxGroup([
		    ResxFile.From(new AdditionalTextStub(@"D:\src\xhg\y\Areas\CaModule\Pages\IdfgControlCenter.resx"))!,
		    ResxFile.From(new AdditionalTextStub(@"D:\src\xhg\y\Areas\CaModule\Pages\IdfgControlCenter.de.resx"))!,
		    ResxFile.From(new AdditionalTextStub(@"D:\src\xhg\y\Areas\CaModule\Pages\IdfgControlCenter.ro.resx"))!,
	    ]);

        v1.ShouldNotBe(v2);
    }

    static readonly string[] s_data =
    [
	    @"D:\src\xhg\y\Areas\CaModule\Pages\IdfgControlCenter.da.resx",
        @"D:\src\xhg\y\Areas\CaModule\Pages\IdfgControlCenter.resx",
        @"D:\src\xhg\y\Areas\CaModule\Pages\IdfgControlCenter.vi.resx",
        @"D:\src\xhg\y\Areas\CaModule\Pages\IdfgLive.da.resx",
        @"D:\src\xhg\y\Areas\CaModule\Pages\IdfgLive.resx",
        @"D:\src\xhg\y\Areas\CaModule\Pages\IdfgLive.vi.resx",
        @"D:\src\xhg\y\Areas\Identity\Pages\Login.da.resx",
        @"D:\src\xhg\y\Areas\Identity\Pages\Login.resx",
        @"D:\src\xhg\y\Areas\Identity\Pages\Login.vi.resx",
        @"D:\src\xhg\y\Areas\QxModule\Pages\QasdLogon.da.resx",
        @"D:\src\xhg\y\Areas\QxModule\Pages\QasdLogon.resx",
        @"D:\src\xhg\y\Areas\QxModule\Pages\QasdLogon.vi.resx",
        @"D:\src\xhg\y\Areas\QxModule\QtrController.cs-cz.resx",
        @"D:\src\xhg\y\Areas\QxModule\QtrController.da.resx",
        @"D:\src\xhg\y\Areas\QxModule\QtrController.de.resx",
        @"D:\src\xhg\y\Areas\QxModule\QtrController.es.resx",
        @"D:\src\xhg\y\Areas\QxModule\QtrController.fi.resx",
        @"D:\src\xhg\y\Areas\QxModule\QtrController.fr.resx",
        @"D:\src\xhg\y\Areas\QxModule\QtrController.it.resx",
        @"D:\src\xhg\y\Areas\QxModule\QtrController.lt.resx",
        @"D:\src\xhg\y\Areas\QxModule\QtrController.lv.resx",
        @"D:\src\xhg\y\Areas\QxModule\QtrController.nb-no.resx",
        @"D:\src\xhg\y\Areas\QxModule\QtrController.nl.resx",
        @"D:\src\xhg\y\Areas\QxModule\QtrController.nn-no.resx",
        @"D:\src\xhg\y\Areas\QxModule\QtrController.pl.resx",
        @"D:\src\xhg\y\Areas\QxModule\QtrController.resx",
        @"D:\src\xhg\y\Areas\QxModule\QtrController.ru.resx",
        @"D:\src\xhg\y\Areas\QxModule\QtrController.sv.resx",
        @"D:\src\xhg\y\Areas\QxModule\QtrController.tr.resx",
        @"D:\src\xhg\y\Areas\QxModule\QtrController.vi.resx",
        @"D:\src\xhg\y\Areas\QxModule\QtrController.zh-cn.resx",
        @"D:\src\xhg\y\DataAnnotations\DataAnnotation.da.resx",
        @"D:\src\xhg\y\DataAnnotations\DataAnnotation.resx",
        @"D:\src\xhg\y\DataAnnotations\DataAnnotation2.resx",
    ];

    [Fact]
    public void FileGrouping()
    {
        var result = GroupResxFiles.Group(s_data.Select(x => ResxFile.From(new AdditionalTextStub(x))!).OrderBy(_ => NewGuid()).ToArray());

        var testData = new List<ResxGroup>
        {
	        new ResxGroup(
		        [
			        ResxFile.From(new AdditionalTextStub(@"D:\src\xhg\y\Areas\CaModule\Pages\IdfgControlCenter.resx"))!,
			        ResxFile.From(new AdditionalTextStub(@"D:\src\xhg\y\Areas\CaModule\Pages\IdfgControlCenter.da.resx"))!,
			        ResxFile.From(new AdditionalTextStub(@"D:\src\xhg\y\Areas\CaModule\Pages\IdfgControlCenter.vi.resx"))!,
		        ]
	        ),
	        new ResxGroup(
		        [
			        ResxFile.From(new AdditionalTextStub(@"D:\src\xhg\y\Areas\CaModule\Pages\IdfgLive.resx"))!,
			        ResxFile.From(new AdditionalTextStub(@"D:\src\xhg\y\Areas\CaModule\Pages\IdfgLive.da.resx"))!,
			        ResxFile.From(new AdditionalTextStub(@"D:\src\xhg\y\Areas\CaModule\Pages\IdfgLive.vi.resx"))!,
		        ]
	        ),
	        new ResxGroup(
		        [
			        ResxFile.From(new AdditionalTextStub(@"D:\src\xhg\y\Areas\Identity\Pages\Login.resx"))!,
			        ResxFile.From(new AdditionalTextStub(@"D:\src\xhg\y\Areas\Identity\Pages\Login.da.resx"))!,
			        ResxFile.From(new AdditionalTextStub(@"D:\src\xhg\y\Areas\Identity\Pages\Login.vi.resx"))!,
		        ]
	        ),
	        new ResxGroup(
		        [
			        ResxFile.From(new AdditionalTextStub(@"D:\src\xhg\y\Areas\QxModule\Pages\QasdLogon.resx"))!,
			        ResxFile.From(new AdditionalTextStub(@"D:\src\xhg\y\Areas\QxModule\Pages\QasdLogon.da.resx"))!,
			        ResxFile.From(new AdditionalTextStub(@"D:\src\xhg\y\Areas\QxModule\Pages\QasdLogon.vi.resx"))!,
		        ]
	        ),
	        new ResxGroup(
	        [
		        ResxFile.From(new AdditionalTextStub(@"D:\src\xhg\y\Areas\QxModule\QtrController.resx"))!,
		        ResxFile.From(new AdditionalTextStub(@"D:\src\xhg\y\Areas\QxModule\QtrController.cs-cz.resx"))!,
		        ResxFile.From(new AdditionalTextStub(@"D:\src\xhg\y\Areas\QxModule\QtrController.da.resx"))!,
		        ResxFile.From(new AdditionalTextStub(@"D:\src\xhg\y\Areas\QxModule\QtrController.de.resx"))!,
		        ResxFile.From(new AdditionalTextStub(@"D:\src\xhg\y\Areas\QxModule\QtrController.es.resx"))!,
		        ResxFile.From(new AdditionalTextStub(@"D:\src\xhg\y\Areas\QxModule\QtrController.fi.resx"))!,
		        ResxFile.From(new AdditionalTextStub(@"D:\src\xhg\y\Areas\QxModule\QtrController.fr.resx"))!,
		        ResxFile.From(new AdditionalTextStub(@"D:\src\xhg\y\Areas\QxModule\QtrController.it.resx"))!,
		        ResxFile.From(new AdditionalTextStub(@"D:\src\xhg\y\Areas\QxModule\QtrController.lt.resx"))!,
		        ResxFile.From(new AdditionalTextStub(@"D:\src\xhg\y\Areas\QxModule\QtrController.lv.resx"))!,
		        ResxFile.From(new AdditionalTextStub(@"D:\src\xhg\y\Areas\QxModule\QtrController.nb-no.resx"))!,
		        ResxFile.From(new AdditionalTextStub(@"D:\src\xhg\y\Areas\QxModule\QtrController.nl.resx"))!,
		        ResxFile.From(new AdditionalTextStub(@"D:\src\xhg\y\Areas\QxModule\QtrController.nn-no.resx"))!,
		        ResxFile.From(new AdditionalTextStub(@"D:\src\xhg\y\Areas\QxModule\QtrController.pl.resx"))!,
		        ResxFile.From(new AdditionalTextStub(@"D:\src\xhg\y\Areas\QxModule\QtrController.ru.resx"))!,
		        ResxFile.From(new AdditionalTextStub(@"D:\src\xhg\y\Areas\QxModule\QtrController.sv.resx"))!,
		        ResxFile.From(new AdditionalTextStub(@"D:\src\xhg\y\Areas\QxModule\QtrController.tr.resx"))!,
		        ResxFile.From(new AdditionalTextStub(@"D:\src\xhg\y\Areas\QxModule\QtrController.vi.resx"))!,
		        ResxFile.From(new AdditionalTextStub(@"D:\src\xhg\y\Areas\QxModule\QtrController.zh-cn.resx"))!,
	        ]),
	        new ResxGroup(
		        [
			        ResxFile.From(new AdditionalTextStub(@"D:\src\xhg\y\DataAnnotations\DataAnnotation.resx"))!,
			        ResxFile.From(new AdditionalTextStub(@"D:\src\xhg\y\DataAnnotations\DataAnnotation.da.resx"))!,
		        ]
	        ),
	        new ResxGroup(
		        [
			        ResxFile.From(new AdditionalTextStub(@"D:\src\xhg\y\DataAnnotations\DataAnnotation2.resx"))!,
		        ]
	        ),
        };
        var resAsList = result.ToList();
        resAsList.Count.ShouldBe(testData.Count);
        foreach (var grp in testData)
        {
            resAsList.ShouldContain(grp);
        }
    }

}


