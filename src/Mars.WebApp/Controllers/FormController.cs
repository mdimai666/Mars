using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Text;
using System.Threading.Tasks;
using AppShared.Models;
using Mars.Host.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace Mars.Host.Controllers;

[Route("api/[controller]/[action]")]
public class FormController : MinimalControllerBase
{
    private readonly FormService formService;

    public FormController(FormService formService, IServiceProvider serviceProvider) : base(serviceProvider)
    {
        this.formService = formService;
    }


    [HttpGet("{modelName}")]
    public async Task<IEnumerable<TitleEntity>> SelectVariants(string modelName)
    {
        return await formService.SelectVariants(modelName);
    }

    //[ValidateAntiForgeryToken]
    [HttpPost("{modelName}")]
    public async Task<UserActionResult<Guid>> Form(string modelName/*, [FromBody] JObject form*/)
    {
        bool isForm = HttpContext.Request.HasFormContentType;

        if (!isForm)
        {
            ReadResult requestBodyInBytes = await Request.BodyReader.ReadAsync();
            Request.BodyReader.AdvanceTo(requestBodyInBytes.Buffer.Start, requestBodyInBytes.Buffer.End);
            string body = Encoding.UTF8.GetString(requestBodyInBytes.Buffer.FirstSpan);

            JObject form = JObject.Parse(body);

            return await formService.FormAdd(modelName, form);
        }
        else
        {
            JObject form = new JObject();
            foreach (var k in Request.Form.Keys)
            {
                form[k] = Request.Form[k].ToString();
            }

            return await formService.FormAdd(modelName, form, Request.Form.Files);
        }

        throw new NotImplementedException();
    }

    [HttpPut("{modelName}/{id:guid}")]
    public async Task<UserActionResult> Form(string modelName, Guid id, [FromBody] JObject form)
    {
        return await formService.FormUpdate(modelName, id, form);
    }


    //[HttpGet()]
    //public async Task<ActionResult> RenderView1()
    //{

    //    //using var ef = GetEFContext();

    //    //Post post = ef.Posts.Include(s => s.MetaValues).ThenInclude(s => s.MetaField).FirstOrDefault(s => s.Type == "vacancy");

    //    var ps = _serviceProvider.GetRequiredService<PostService>();
    //    Post post = await ps.Get(s => s.Type == "vacancy");

    //    WebClientRequest req = new WebClientRequest(HttpContext.Request);

    //    string html = TemplatorFormOutput.RenderMetaFields(post.MetaValues, null, req);

    //    return Ok(html);
    //}

}
