using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using System.Reflection;
using Mars.Core.Attributes;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace AppFront.Shared.Components;

public partial class DocViewer<TValue>
{

    [Parameter, Required] public Expression<Func<TValue>> For { get; set; } = default!;

    FunctionApiDocumentAttribute? attr;

    protected Expression<Func<TValue>>? _previousFieldAccessor;
    protected FieldIdentifier? _fieldIdentifier;

    FluentMarkdownSection markdownEl = default!;

    string? _docUrl;
    Exception? _error;

    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        Type? forType = null;
        Action? forAction = null;

        if (For == null) // Not possible except if you manually specify T
        {
            throw new InvalidOperationException($"{GetType()} requires a value for the " +
                $"{nameof(For)} parameter.");
        }
        else if (For != _previousFieldAccessor)
        {
            Expression expression = For.Body;
            if (expression is UnaryExpression unaryExpression && unaryExpression.NodeType == ExpressionType.Convert && unaryExpression.Type == typeof(object))
            {
                expression = unaryExpression.Operand;
            }

            if (expression is MethodCallExpression methodCallExpression)
            {
                if (methodCallExpression.Method.Name == nameof(Type.GetType))
                {
                    // forType = (Type)methodCallExpression.Method.Invoke(methodCallExpression.Object, [])!;
                    // forType = methodCallExpression.Object.GetType();

                    // Action<Type> compiledDelegate = Expression.Lambda<Action<Type>>(methodCallExpression).Compile();
                    // var t = compiledDelegate.Invoke();

                    var methodCallObjectLambda = Expression.Lambda(typeof(Func<object?>), methodCallExpression!);
                    var methodCallObjectLambdaCompiled = (Func<object?>)methodCallObjectLambda.Compile();
                    forType = (Type)methodCallObjectLambdaCompiled()!;
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
            else if (expression is ConstantExpression constantExpression && constantExpression.GetType().Name == "TypedConstantExpression")
            {
                //var name = expression.GetType().Name;
                var type = (Type)constantExpression.Value!;
                forType = type;
            }
            else if (expression is UnaryExpression unaryExpression2 && unaryExpression2.Operand.Type == typeof(Delegate))
            {
                var delegateInstance = Expression.Lambda(unaryExpression2.Operand).Compile().DynamicInvoke();

                if (delegateInstance is Action action)
                {
                    forAction = action;
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
            else
            {
                _fieldIdentifier = FieldIdentifier.Create(For);
                _previousFieldAccessor = For;
            }
            attr = null;
            _docUrl = null;
            _error = null;
            StateHasChanged();
        }

        if (attr is null)
        {
            if (forType is not null)
            {
                attr = forType?.GetCustomAttribute<FunctionApiDocumentAttribute>();
                if (attr == null)
                    _error = new ArgumentException($"[FunctionApiDocumentAttribute] not found for type'{forType.Name}'");
            }
            else if (forAction is not null)
            {
                attr = forAction.Method.GetCustomAttribute<FunctionApiDocumentAttribute>();
                if (attr == null)
                    _error = new ArgumentException($"[FunctionApiDocumentAttribute] not found for method'{forAction.Method.Name}'");
            }
            else
            {
                var prop = _fieldIdentifier?.Model.GetType().GetProperty(_fieldIdentifier?.FieldName!);
                attr = prop?.GetCustomAttribute<FunctionApiDocumentAttribute>();
                if (attr == null)
                    _error = new ArgumentException($"[FunctionApiDocumentAttribute] not found for '{_fieldIdentifier?.FieldName}'");
            }

            if (attr is not null)
            {
                var lang = "ru";
                var docUrl = FunctionApiDocumentAttribute.ReplaceLang(attr.Url, lang);
                _docUrl = docUrl;
            }
        }
    }
}
