using System.Drawing;
using System.Globalization;
using HtmlAgilityPack;
using static Mars.Options.Models.ImagePreviewSizeConfig;

namespace AppFront.Shared.Components;

public class WysiwygEditorHelper
{

    public static ImageNodeInfo NodeToImageInfo(HtmlNode node) => new ImageNodeInfo(node.GetAttributeValue("width", (string)null!), node.GetAttributeValue("height", (string)null!));


    /// <summary>
    /// Не хватает информации с рендера картинок (ширина/высота картинок)
    /// </summary>
    /// <param name="html"></param>
    /// <param name="mode"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public static string? ModifyImages(string html, ImageCollectionModify mode)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var infos = doc.DocumentNode.Descendants("img")
                                    .Select(node => (info: NodeToImageInfo(node), node: node))
                                    //.Where(s => s.info.WidthPx != null || s.info.HeightPx != null)
                                    .ToList();

        if (infos.Count == 0) return null;


        //float templateSize = infos.Where(s => s.info.WidthPx is not null).Select(s => s.info.WidthPx!.Value).Max();
        ImageNodeInfo templateInfo = mode switch
        {
            ImageCollectionModify.ByFirst => infos.First().info,
            ImageCollectionModify.ByMax => infos.MaxBy(s => s.info.WidthPx).info,
            ImageCollectionModify.ByMin => infos.MinBy(s => s.info.WidthPx).info,
            _ => throw new NotImplementedException()
        };

        var templateSize = new SizeF(templateInfo.WidthPx ?? 0, templateInfo.HeightPx ?? 0);

        //выходим если не знаем размеров
        if (templateSize.Width == 0 || templateSize.Height == 0) return null;

        foreach (var s in infos.Skip(mode == ImageCollectionModify.ByFirst ? 1 : 0))
        {
            SizeF newSize = NewSize(new SizeF(s.info.WidthPx ?? templateSize.Width, s.info.HeightPx ?? templateSize.Height), templateSize, CropScaleMode.Contain);
            s.node.SetAttributeValue("width", newSize.Width.ToString("0", CultureInfo.InvariantCulture));
            s.node.SetAttributeValue("height", newSize.Height.ToString("0", CultureInfo.InvariantCulture));
        }

        return doc.DocumentNode.OuterHtml;
    }

    public static SizeF NewSize(SizeF size, SizeF templateSize, CropScaleMode mode)
    {
        var sizeRatio = size.Width / size.Height;
        var templateRatio = templateSize.Width / templateSize.Height;

        var sourceIsBiggest = size.Width >= templateSize.Width;

        //var multipler = sourceIsBiggest ? 

        return mode switch
        {
            CropScaleMode.Stretch => templateSize,
            CropScaleMode.Contain => new SizeF(templateSize.Width, templateSize.Width / sizeRatio),
            _ => throw new NotImplementedException()
        };
    }
}

public class ImageNodeInfo
{
    public float? WidthPx { get; set; }
    public float? HeightPx { get; set; }

    public ImageNodeInfo()
    {

    }

    public ImageNodeInfo(string width, string height)
    {
        WidthPx = float.TryParse(width, CultureInfo.InvariantCulture, out var _w) ? _w : null;
        HeightPx = float.TryParse(height, CultureInfo.InvariantCulture, out var _h) ? _h : null;
    }
}

public enum ImageCollectionModify
{
    ByFirst,
    ByMin,
    ByMax,
}
