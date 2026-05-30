# HttpInNode

This node creates an HTTP endpoint and listens for incoming requests. Use it as an entry point to your scenario.

> **Important:** After processing the request, you must use `HttpResponseNode` to send a response back to the client.

## Settings

| Parameter | Description |
|-----------|-------------|
| `Method` | HTTP method the node will handle (GET, POST, PUT, DELETE, HEAD) |
| `UrlPattern` | URL pattern where the node will accept requests |
| `IsRequireAuthorize` | Whether authentication is required to access the endpoint |
| `AllowedRoles` | List of roles allowed to access (only works if `IsRequireAuthorize = true`) |
| `AllowMultipart` | Allow file uploads via `multipart/form-data` |
| `MaxFileSize` | Maximum allowed file size (e.g., `10mb`, `5gb`, `1024kb`) |

## URL Patterns

You can use parameterized paths:

| Example | Description |
|---------|-------------|
| `/static` | Fixed path |
| `/{name}` | Any text |
| `/{id:int}` | Integer only |
| `/sub/{age:int:min(20)}` | Integer not less than 20 |
| `/{code:maxlength(10)}` | String no longer than 10 characters |

### Available Constraints

- `minlength(N)` — minimum string length
- `maxlength(N)` — maximum string length
- `length(N)` — exact string length
- `min(N)` — minimum numeric value
- `max(N)` — maximum numeric value
- `range(A,B)` — numeric value between A and B
- `regex(expression)` — match regular expression

## Accessing URL Parameters

Inside your scenario, you can access route parameters through the request context:

```
HttpInNodeHttpRequestContext.Request.RouteValues["parameter_name"]
```

For example, to check if parameter `id` equals 777:

```
HttpInNodeHttpRequestContext.Request.RouteValues["id"]?.ToString() == "777"
```

## What Goes Into `input.Payload`

Depending on the request type, `Payload` is automatically populated:

| Content-Type | What the scenario receives |
|--------------|----------------------------|
| `multipart/form-data` | Form object with files |
| `application/x-www-form-urlencoded` | Form object |
| `application/json` | Deserialized JSON (object, array, or value) |
| Others | Request body as a string |

## Error Handling

The node automatically returns errors in the following cases:

| Situation | Response Code |
|-----------|---------------|
| Authentication failed | `401 Unauthorized` |
| Access denied (role not allowed) | `403 Forbidden` |
| `AllowMultipart = false` but multipart request received | `415 Unsupported Media Type` |
| File exceeds `MaxFileSize` | `413 Payload Too Large` |

---
