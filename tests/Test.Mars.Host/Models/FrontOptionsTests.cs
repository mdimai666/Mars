using Mars.Shared.Options;
using Newtonsoft.Json;

namespace Test.Mars.Host.Models;

[Obsolete]
public class FrontOptionsTests
{
    readonly string v1BlankFrontOptionJson = @"{""IndexHtml"":""<h1>title</h1>\n@Body""}";
    readonly string v2BlankFrontOptionJson = @"{
        ""HostItems"": [{
            ""Url"": """",
            ""HostHtml"": ""<h1>title</h1>\n@Body""
        }]
    }";

    //[Fact]
    //public void FrontOptionsRead()
    //{
    //    var v1 = JsonConvert.DeserializeObject<FrontOptions>(v1BlankFrontOptionJson);
    //    var v2 = JsonConvert.DeserializeObject<FrontOptions>(v2BlankFrontOptionJson);

    //    v1.NormalizeAfterRead();
    //    v2.NormalizeAfterRead();

    //    Assert.Equal(v1.HostItems.Count, v2.HostItems.Count);
    //    Assert.Equal(v1.IndexHtml, v2.IndexHtml);
    //    Assert.Equal(v1.HostItems[0].Url, v2.HostItems[0].Url);
    //    Assert.Equal(v1.HostItems[0].HostHtml, v2.HostItems[0].HostHtml);
    //}
}
