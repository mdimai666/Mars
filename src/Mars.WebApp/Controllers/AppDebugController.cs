using System.Net.Mime;
using System.Text.RegularExpressions;
using Mars.Host.Shared.ExceptionFilters;
using Mars.Shared.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mars.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
[Produces(MediaTypeNames.Application.Json)]
[UserActionResultExceptionFilter]
[AllExceptionCatchToUserActionResultFilter]
public class AppDebugController : ControllerBase
{
    private readonly IHostEnvironment _env;

    public AppDebugController(IHostEnvironment env)
    {
        _env = env;
    }

    private string _logPath => Path.Combine(_env.ContentRootPath, "data", "logs");

    [HttpGet("GetLogs")]
    [ProducesErrorResponseType(typeof(void))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ValidationProblemDetails))]
    public UserActionResult<string> GetLogs(string filename, int lines = 1000)
    {
        try
        {
            string logFileName;

            if (string.IsNullOrEmpty(filename))
            {
                logFileName = string.Format("app_{0:yyyy}-{0:MM}-{0:dd}.log", DateTime.Now);
            }
            else
            {
                logFileName = Path.GetFileName(filename) + ".log";
            }

            string logspath = Path.Combine(_logPath, logFileName);

            //using TextReader reader = new StreamReader(System.IO.File.OpenRead(logspath));


            using var fs = new FileStream(logspath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var sr = new StreamReader(fs);
            using TextReader reader = sr;//System.IO.File.OpenText(logspath);

            var text = reader.Tail(Math.Min(lines, 1000));

            return new UserActionResult<string>
            {
                Ok = true,
                Data = string.Join(Environment.NewLine, text)
            };
        }
        catch (Exception ex)
        {
            return new UserActionResult<string>
            {
                Message = ex.Message,
            };
        }
    }

    [HttpGet("LogFiles")]
    public IEnumerable<string> LogFiles()
    {
        var files = Directory.EnumerateFiles(_logPath, "*.log")
                        .Select(file => Path.GetFileNameWithoutExtension(file))
                        .Where(file => file.StartsWith("app"));

        return [.. OrderByAlphaNumeric(files, s => s).Reverse().Take(10)];
    }

    static IOrderedEnumerable<T> OrderByAlphaNumeric<T>(IEnumerable<T> source, Func<T, string> selector)
    {
        int max = source
            .SelectMany(i => Regex.Matches(selector(i), @"\d+").Cast<Match>().Select(m => (int?)m.Value.Length))
            .Max() ?? 0;

        return source.OrderBy(i => Regex.Replace(selector(i), @"\d+", m => m.Value.PadLeft(max, '0')));
    }

}

public static class FileReaderExtensions
{

    ///<summary>Returns the end of a text reader.</summary>
    ///<param name="reader">The reader to read from.</param>
    ///<param name="lineCount">The number of lines to return.</param>
    ///<returns>The last lneCount lines from the reader.</returns>
    public static string[] Tail(this TextReader reader, int lineCount)
    {
        var buffer = new List<string>(lineCount);
        string? line;
        for (int i = 0; i < lineCount; i++)
        {
            line = reader.ReadLine();
            if (line == null) return buffer.ToArray();
            buffer.Add(line);
        }

        int lastLine = lineCount - 1;           //The index of the last line read from the buffer.  Everything > this index was read earlier than everything <= this indes

        while (null != (line = reader.ReadLine()))
        {
            lastLine++;
            if (lastLine == lineCount) lastLine = 0;
            buffer[lastLine] = line;
        }

        if (lastLine == lineCount - 1) return buffer.ToArray();
        var retVal = new string[lineCount];
        buffer.CopyTo(lastLine + 1, retVal, 0, lineCount - lastLine - 1);
        buffer.CopyTo(0, retVal, lineCount - lastLine - 1, lastLine + 1);
        return retVal;
    }
}
