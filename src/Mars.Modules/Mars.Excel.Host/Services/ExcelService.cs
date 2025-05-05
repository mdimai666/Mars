using Mars.Host.Shared.Services;
using ClosedXML.Excel;
using ClosedXML.Report;
using Microsoft.AspNetCore.Mvc;

namespace Mars.Excel.Host.Services;

internal class ExcelService : IExcelService
{
    public void BuildExcelReport(string templateFileName, object viewModel, MemoryStream outStream)
    {
        using IXLWorkbook wb = BuildExcelFile(templateFileName, viewModel);
        wb.SaveAs(outStream);
    }

    public FileContentResult ExcelRespone(ControllerBase controller, MemoryStream stream, string downloadFilename)
    {
        return controller.ExcelRespone(stream, downloadFilename);
    }

    IXLWorkbook BuildExcelFile(string templateFileName, object viewModel)
    {
        ArgumentException.ThrowIfNullOrEmpty(templateFileName, nameof(templateFileName));
        if (!File.Exists(templateFileName))
            throw new FileNotFoundException($"templateFileName: ({templateFileName}) not found", templateFileName);


        XLTemplate template = new XLTemplate(templateFileName);

        template.AddVariable(viewModel);
        template.Generate();

        return template.Workbook;
    }
}

public static class ExcelServiceExtensions
{
    public static FileContentResult ExcelRespone(this ControllerBase controller, MemoryStream stream, string downloadFilename)
    {
        var content = stream.ToArray();

        if (downloadFilename.EndsWith(".xlsx") == false)
            downloadFilename += ".xlsx";


        return controller.File(
            content,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            downloadFilename);
    }
}
