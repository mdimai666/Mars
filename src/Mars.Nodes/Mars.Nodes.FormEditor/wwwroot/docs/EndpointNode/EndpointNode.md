# EndpointNode

EndpointNode is designed for building APIs in a visual or configuration-based manner and can be used as a foundation for runtime generation of endpoints, Swagger descriptions, and request validation logic.

At the end, you need to respond by adding HttpResponseNode

## URL Patterns
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

## JsonSchema

[https://json-schema.org/understanding-json-schema/about](https://json-schema.org/understanding-json-schema/about)

```json
{
    "type": "object",
    "properties": {
    "first_name": { "type": "string" },
    "last_name": { "type": "string" },
    "birthday": { "type": "string", "format": "date" },
    "address": {
        "type": "object",
        "properties": {
            "street_address": { "type": "string" },
            "city": { "type": "string" },
            "state": { "type": "string" },
            "country": { "type" : "string" }
        }
    }
    },
    "required": [ "first_name", "last_name" ],
}
```
