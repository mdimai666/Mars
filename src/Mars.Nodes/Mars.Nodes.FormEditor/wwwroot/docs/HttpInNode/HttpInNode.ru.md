# HttpInNode

Создает endpoint и ловит http запрос.

В конце надо ответить добавив HttpResponseNode

## Паттерны URL
 - `/static`
 - `/{pattern}`
 - `/{typedPattern:int}`
 - `/{typedPatternConstraints:int:min(20)}`
 - `/{typedPatternConstraints:maxlength(10)}`

### IRouteConstraint
```
"minlength" => new MinLengthRouteConstraint(_int),
"maxlength" => new MaxLengthRouteConstraint(_int),
"length" => new LengthRouteConstraint(_int),
"min" => new MinRouteConstraint(_int),
"max" => new MaxRouteConstraint(_int),
"range" => new RangeRouteConstraint(_ints[0], _ints[1]),

"regex" => new RegexInlineRouteConstraint(argString!),
```

### Property access
- `HttpInNodeHttpRequestContext.Request.RouteValues["myUrlParam"]?.ToString()=="777"`
