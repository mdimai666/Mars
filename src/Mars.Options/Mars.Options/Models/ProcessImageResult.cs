using Mars.Options.Interfaces;

namespace Mars.Options.Models;

public class ProcessImageResult : IProcessImageResult
{
    public int Width { get; set; }
    public int Height { get; set; }
    //public bool HasAlpha { get; set; }
    public long FileSize { get; set; }
    public double ProcessingTime { get; set; }
}
