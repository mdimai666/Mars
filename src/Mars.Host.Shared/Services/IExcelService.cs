using Microsoft.AspNetCore.Mvc;

namespace Mars.Host.Shared.Services;

public interface IExcelService
{
    public void BuildExcelReport(string templateFileName, object viewModel, MemoryStream outStream);
    public FileContentResult ExcelRespone(ControllerBase controller, MemoryStream stream, string downloadFilename);
}
