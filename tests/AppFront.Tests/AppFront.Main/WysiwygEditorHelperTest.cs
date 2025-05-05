using System.Drawing;
using AppFront.Shared.Components;
using FluentAssertions;
using HtmlAgilityPack;
using static Mars.Options.Models.ImagePreviewSizeConfig;

namespace AppFront.Tests.AppFront.Main;

public class WysiwygEditorHelperTest
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
    public void WysiwygEditorHelper_GetImages()
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        int expectImgCount = 4;

        var imgs = doc.DocumentNode.Descendants("img").ToList();
        var infos = imgs.Select(WysiwygEditorHelper.NodeToImageInfo).Where(s => s.WidthPx != null || s.HeightPx != null).ToList();

        var maxWidth = infos.Max(s => s.WidthPx);
        var maxHeight = infos.Max(s => s.HeightPx);

        var minWidth = infos.Min(s => s.WidthPx);
        var minHeight = infos.Min(s => s.HeightPx);


        int expectMaxWidth = 400;
        int expectMaxHeight = 300;
        int expectMinWidth = 139;
        int expectMinHeight = 76;

        imgs.Count.Should().Be(expectImgCount);

        maxWidth.Should().Be(expectMaxWidth);
        maxHeight.Should().Be(expectMaxHeight);
        minWidth.Should().Be(expectMinWidth);
        minHeight.Should().Be(expectMinHeight);


    }

    [Fact]
    public void WysiwygEditorHelper_ModifyImages()
    {
        var html2 = WysiwygEditorHelper.ModifyImages(html, ImageCollectionModify.ByFirst);

        var doc = new HtmlDocument();
        doc.LoadHtml(html2);
        var imgs = doc.DocumentNode.Descendants("img").ToList();
        var infos = imgs.Select(WysiwygEditorHelper.NodeToImageInfo).Where(s => s.WidthPx != null || s.HeightPx != null).ToList();

        int expectImgCount = 4;
        imgs.Count.Should().Be(expectImgCount);

        var firstWidth = imgs.First().GetAttributeValue("width", (string)null!);

        var widthS = imgs.Select(node => node.GetAttributeValue("width", (string)null!)).ToList();

        widthS.Should().AllBe(firstWidth);

    }

    [Fact]
    public void NewSizeTest()
    {
        var source = new SizeF(1920, 1080);
        var sourceRatio = source.Width / source.Height;
        var lessSize = new Size(1080, 1080);

        var miniSize = WysiwygEditorHelper.NewSize(source, lessSize, CropScaleMode.Contain);

        miniSize.Should().BeEquivalentTo(new SizeF(lessSize.Width, source.Height / sourceRatio));
    }

}
