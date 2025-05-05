using AppShared.Models;
using Mars.Host.Data;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace Mars.Host.Controllers;

[ApiController]
[Route("api/[controller]/[action]")]
public class StatisticController : ControllerBase
{

    protected readonly HttpClient _httpClient;

    public StatisticController(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    [HttpGet]
    public async Task<ActionResult<ChartData>> Statistic1()
    {
        return await _httpClient.GetFromJsonAsync<ChartData>("http://localhost:5003/data/fake_chart_data.json");
    }
}
