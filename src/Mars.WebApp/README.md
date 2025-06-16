# Mars.WebApp

low-code CRM

# EF

## Migrations
use it for init db
```
cd Mars.Host.Data
src\Mars.Host.Data> dotnet ef migrations add '<someName>' --startup-project ..\Mars.WebApp\Mars.WebApp.csproj
```

dotnet tool install --global dotnet-ef

```
//add migration
dotnet ef migrations add 'added posts'

//update db
dotnet ef database update

// e.t.c
dotnet ef database update 'Initial'
ef migrations remove
ef database drop
ef database drop -f
```

# for local dev
create appsettings.Local.json

# For inspiration
https://github.com/thangchung/awesome-dotnet-core#sample-projects

## Publish

### Init

```csharp
//for aplly DB migrations
dotnet Mars.dll -migrate
```

### close XML require 
```
apt-get install -y libgdiplus
```
if error 
```
 Connection id "0HMDUHRRHMDFD", Request id "0HMDUHRRHMDFD:00000002": An unhandled exception was thrown by the application.
1|app|       System.TypeInitializationException: The type initializer for 'Gdip' threw an exception.
1|app  |        ---> System.DllNotFoundException: Unable to load shared library 'libgdiplus' or one of its dependencies. In order to help diagnose loading problems, consider setting the LD_DEBUG environment variable: liblibgdiplus: cannot open shared object file: No such file or directory
````

## change postgres entities owner when export
```
https://stackoverflow.com/a/37259655/6723966
.\pg_dump.exe -U postgres --no-privileges --no-owner -f C:\Users\d\Downloads\Mars.sql Mars
.\psql.exe -U postgres -f C:\Users\d\Downloads\someDB.sql Mars
```

##raw sql for filter by metafield
```sql
SELECT *
FROM "posts"
JOIN "PostMetaValues" on "PostMetaValues"."PostId" = "posts"."Id" 
INNER JOIN "MetaValues" on "MetaValues"."Id" = "PostMetaValues"."MetaValueId"
--INNER JOIN "PostTypeMetaFields" on "PostTypeMetaFields"."MetaFieldId" = "MetaValues"."MetaFieldId"
INNER JOIN "MetaFields" on "MetaFields"."Id" = "MetaValues"."MetaFieldId"
WHERE "posts"."Type" = 'vacancy' AND "MetaFields"."Key" = 'salary' AND "MetaValues"."Decimal">=200
```

entity framework query
```csharp
Post post = ef.Posts
                    .Include(s=>s.MetaValues)
                    .ThenInclude(s=>s.MetaField)
                    .FirstOrDefault(s=>s.MetaValues.Any(x=>x.MetaField.Key == "someKey" && x.Decimal>200));
```

check db timezone
```
psql
postgres=# SELECT * FROM pg_timezone_names;
postgres=# Asia/Yakutsk^C
postgres=# \l
postgres=# ALTER DATABASE diary2 SET timezone TO 'Asia/Yakutsk';
ALTER DATABASE
postgres=# client_loop: send disconnect: Connection reset
```

https://learn.microsoft.com/ru-ru/dotnet/api/system.threading.semaphoreslim?view=net-7.0
