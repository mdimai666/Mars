# Проверка производительности ASP.NET Core приложения с помощью ApacheBench (ab)
```
.\ab.exe -n 100 -c 20 http://localhost:5003/
# или
.\ab.exe -n 1000 -c 50 http://localhost:5003/
```

## Пример 1
Для конфигурации Windows 11, i7-13700.

1. Для пустой страницы, или полностью закешированной.
`Requests per second:    8322.93 [#/sec] (mean)`

> Без разницы шабонный Url ({param}) или нет.

2. Для страницы с запросом к БД
`Requests per second:    2895.86 [#/sec] (mean)`

3. Для Nodes Http In 
`Requests per second:    27.19 [#/sec] (mean)`
