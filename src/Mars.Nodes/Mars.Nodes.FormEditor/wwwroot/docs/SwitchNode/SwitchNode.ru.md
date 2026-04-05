# SwitchNode

Условная маршрутизация

## Примеры
- `msg.Payload == 123`
- `msg.Payload > 123`
- `msg.Payload.ToString() > "string"`
- `msg.HttpInNodeHttpRequestContext.Request.RouteValues["myUrlParam"]?.ToString()=="777"`
- `msg.HttpInNodeHttpRequestContext.Request.Query["s"]=="777"`
