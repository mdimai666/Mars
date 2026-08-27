using Microsoft.AspNetCore.Mvc;

namespace Mars.Excel.Abstractions;

public interface IExcelService
{
    public void BuildExcelReport(string templateFileName, object viewModel, MemoryStream outStream);
    public FileContentResult ExcelResponse(ControllerBase controller, MemoryStream stream, string downloadFilename);
}
