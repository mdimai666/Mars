using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using AppShared.Dto;
using AppShared.Models;
using Mars.Host.Data;
using Mars.Host.Mappers;
using Mars.Host.Services;
using Mars.Core.Extensions;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Remote.Linq;
using Remote.Linq.Expressions;
using Remote.Linq.Newtonsoft.Json;
using Remote.Linq.Text.Json;

namespace Mars.Host.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GeoLocationController : StandartController<GeoLocationService, GeoLocation>
{
    public GeoLocationController(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    [HttpPost(nameof(SearchLocation))]
    public async Task<TotalResponse<GeoLocationDto>> SearchLocation([FromQuery] Guid regionId, QueryFilter queryFilter)
    {
        return (await modelService.SearchLocation(regionId, queryFilter)).ToReponse();
    }

    #region EXPERIMENTS
    [HttpGet(nameof(ListTable2))]
    public Task<ActionResult<TotalResponse<GeoLocation>>> ListTable2(
        [FromQuery, NotNull] QueryFilter filter,
        [FromQuery] string remote1,
        [FromQuery(Name = "q")] string q2,
        [FromQuery(Name = "x")] string x,
        [FromServices] MarsDbContextLegacy ef
        )
    {
        //var expressionSerializer = new Serialize.Linq.Serializers.ExpressionSerializer(new Serialize.Linq.Serializers.JsonSerializer());

        string d = RemoteLinq.Decompress(x);

        //System.Linq.Expressions.Expression<Func<GeoLocation, bool>> expression
        //    = (System.Linq.Expressions.Expression<Func<GeoLocation, bool>>)expressionSerializer.DeserializeText(x);


        ////ExpressionNode query = x as ExpressionNode;
        ////var expression = query.ToBooleanExpression<GeoLocation>();
        ////return _persons.Where(expression.Compile()).ToList();

        //var geos = ef.GeoLocations.Where(expression).ToList();


        JsonSerializerOptions serializerSettings = new JsonSerializerOptions().ConfigureRemoteLinq();

        Expression q = DeserialiseRemoteExpression<Expression>(remote1);

        //Short string variant
        {
            JsonSerializerSettings _serializerSettings = new JsonSerializerSettings().ConfigureRemoteLinq();

            //ExpressionTranslator.
            //Remote.Linq.Expressions.

            //Remote.Linq.Expressions.LambdaExpression
            //Remote.Linq.Expressions.Expression
            var result = JsonConvert.DeserializeObject<LambdaExpression>(q2, _serializerSettings);

        }

        System.Linq.Expressions.Expression sysExpr = q.ToLinqExpression();

        var w = (q as LambdaExpression).ToLinqExpression<GeoLocation, bool>();

        var exp = q.ToLinqExpression();


        var geos = ef.GeoLocations.Where(w).ToList();


        return base.ListTable(filter);
    }

    /// <summary>
    /// Deserialise a LINQ expression tree
    /// </summary>
    public Expression DeserialiseRemoteExpression<TExpression>(string json) where TExpression : Expression
    {
        JsonSerializerSettings serializerSettings = new JsonSerializerSettings().ConfigureRemoteLinq();
        Expression result = JsonConvert.DeserializeObject<TExpression>(json, serializerSettings);
        return result;
    }
    /// <summary>
    /// Serialise a remote LINQ expression tree
    /// </summary>
    public string SerialiseRemoteExpression<TExpression>(TExpression expression) where TExpression : Expression
    {
        JsonSerializerSettings serializerSettings = new JsonSerializerSettings().ConfigureRemoteLinq();
        string json = JsonConvert.SerializeObject(expression, serializerSettings);
        return json;
    }
    /// <summary>
    /// Convert the specified Remote.Linq Expression to a .NET Expression
    /// </summary>
    public System.Linq.Expressions.Expression<Func<T, TResult>> ToLinqExpression<T, TResult>(LambdaExpression expression)
    {
        var exp = expression.ToLinqExpression();
        var lambdaExpression = System.Linq.Expressions.Expression.Lambda<Func<T, TResult>>(exp.Body, exp.Parameters);
        return lambdaExpression;
    }
    #endregion
}
