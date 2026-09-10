using System.Drawing;
using FluentAssertions;
using HtmlAgilityPack;
using Mars.Admin.Framework.Components;
using static Mars.Media.Contracts.Options.ImagePreviewSizeConfig;

namespace Mars.Admin.Framework.Tests.Components;

public class WysiwygEditorHelperTests
{
    string html = """
        <h1>Title</h1>
        <img src="/upload/Media/0bbc848a-2a9a-41f9-a0af-7bee42e381d52_xs.webp" alt="/upload/Media/0bbc848a-2a9a-41f9-a0af-7bee42e381d52_xs.webp" width="139" height="139" style="display: block; margin: auto;" data-align="center">
        <p>lorem imposum text</p>
        <img src="/upload/Media/35d72e47-d21d-4d00-a2f2-e2351903f1a6.webp" alt="/upload/Media/35d72e47-d21d-4d00-a2f2-e2351903f1a6.webp" width="400" height="300" >
        <p>123</p>
        <div>
            <p>inner text</p>
            <img src="/upload/Media/35d72e47-d21d-4d00-a2f2-e2351903f1a6.webp" alt="/upload/Media/35d72e47-d21d-4d00-a2f2-e2351903f1a6.webp" width="400" height="76" >
        </div>
        <img src="/upload/Media/35d72e47-d21d-4d00-a2f2-e2351903f1a6.webp" alt="/upload/Media/35d72e47-d21d-4d00-a2f2-e2351903f1a6.webp" width="100%" >
        <p>end.</p>
    """;

    [Fact]
    public void NodeToImageInfo_HtmlWithVariousImages_ReturnsCountAndMinMaxSizes()
    {
        int expectImgCount = 4;
        int expectMaxWidth = 400;
        int expectMaxHeight = 300;
        int expectMinWidth = 139;
        int expectMinHeight = 76;

        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        var imgs = doc.DocumentNode.Descendants("img").ToList();
        var infos = imgs.Select(WysiwygEditorHelper.NodeToImageInfo).Where(s => s.WidthPx != null || s.HeightPx != null).ToList();

        imgs.Count.Should().Be(expectImgCount);
        infos.Max(s => s.WidthPx).Should().Be(expectMaxWidth);
        infos.Max(s => s.HeightPx).Should().Be(expectMaxHeight);
        infos.Min(s => s.WidthPx).Should().Be(expectMinWidth);
        infos.Min(s => s.HeightPx).Should().Be(expectMinHeight);
    }

    [Fact]
    public void ModifyImages_ByFirstImage_AllImagesGetFirstWidth()
    {
        int expectImgCount = 4;

        var modifiedHtml = WysiwygEditorHelper.ModifyImages(html, ImageCollectionModify.ByFirst)!;
        var doc = new HtmlDocument();
        doc.LoadHtml(modifiedHtml);
        var imgs = doc.DocumentNode.Descendants("img").ToList();

        imgs.Count.Should().Be(expectImgCount);
        var firstWidth = imgs.First().GetAttributeValue("width", (string)null!);
        imgs.Select(node => node.GetAttributeValue("width", (string)null!)).Should().AllBe(firstWidth);
    }

    [Fact]
    public void NewSize_ContainMode_FitsTargetKeepingRatio()
    {
        var source = new SizeF(1920, 1080);
        var sourceRatio = source.Width / source.Height;
        var lessSize = new Size(1080, 1080);

        var miniSize = WysiwygEditorHelper.NewSize(source, lessSize, CropScaleMode.Contain);

        miniSize.Should().BeEquivalentTo(new SizeF(lessSize.Width, source.Height / sourceRatio));
    }

}
