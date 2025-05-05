using Mars.Host.Shared.Dto.MetaFields;
using Mars.Host.Shared.Models;
using Mars.Host.Shared.Templators;
using Mars.Host.Templators.HandlebarsFunc;
using Mars.Pages;
using BlazorTemplater;
using HandlebarsDotNet;
using Microsoft.AspNetCore.Components;

namespace Mars.Templators;

public class TemplatorFormOutput
{

    public static void RegisterExtraMethods()
    {

        MyHandlebars.extraRegisteredActions.Add((handlebars, data, FillData) =>
        {

            handlebars.RegisterHelper("display1", (output, context, args) =>
            {
                // TODO: IMPLEMENT
                throw new NotImplementedException();
                //data ??= FillData(q, q.post.MetaValues);
                //string html = TemplatorFormOutput.RenderMetaFields(q.post.MetaValues, data, q.req);
                //output.WriteSafeString(html);
            });

            handlebars.RegisterHelper("edit1", (output, context, args) =>
            {
                throw new NotImplementedException();
                //data ??= FillData(q, q.post.MetaValues);
                //string html = TemplatorFormOutput.RenderMetaFields(q.post.MetaValues, data, q.req, edit: true);
                //output.WriteSafeString(html);
            });

        });
    }

    public static string RenderRazor<TRazorComponent>(object model) where TRazorComponent : ComponentBase, IMyRenderHelper, new()
    {
        //https://github.com/conficient/BlazorTemplater
        string html = new ComponentRenderer<TRazorComponent>()
                //.AddService<ITestService>(new TestService())
                .Set(c => c.Model, model)
                //.UseLayout<MyLayout>()
                .Render();

        return html;
    }

    public static string RenderMetaFields(
        IEnumerable<MetaValueDto> metaValues,
        Dictionary<Guid, MetaRelationObjectDict> dataDict, WebClientRequest req,
        bool edit = false)
    {
        //string html;

        //if (!edit)
        //{
        //    html = new ComponentRenderer<DisplayFF1>()
        //       //.AddService<ITestService>(new TestService())
        //       .Set(c => c.MetaValues, metaValues)
        //       .Set(c => c.DataDict, dataDict)
        //       .Set(c => c.Req, req ?? default)
        //       //.UseLayout<MyLayout>()
        //       .Render();

        //}
        //else
        //{
        //    html = new ComponentRenderer<EditFF1>()
        //        //.AddService<ITestService>(new TestService())
        //        .Set(c => c.MetaValues, metaValues)
        //        .Set(c => c.DataDict, dataDict)
        //        .Set(c => c.Req, req ?? default)
        //        //.UseLayout<MyLayout>()
        //        .Render();
        //}

        //return html;
        throw new NotImplementedException();
    }

}

public interface IMyRenderHelper
{
    //public void AddToTree(RenderTreeBuilder builder, object model);
    public object Model { get; set; }

}
