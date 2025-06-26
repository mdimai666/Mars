# Handlebars

## Используемая библиотека
[https://github.com/Handlebars-Net/Handlebars.Net](https://github.com/Handlebars-Net/Handlebars.Net)

Доступная базовая документация [https://handlebarsjs.com/guide/#what-is-handlebars](https://handlebarsjs.com/guide/#what-is-handlebars)


## Структура морды сайта
Структура строго должна иметь только 3 части.
- _root.hbs - файл точка входа
- wwwroot - публичная папка, которая будет доступна в вебе
- index файл где угодно, просто должна содержать атрибут @page /

```
TemplateName/
    _root.hbs - файл точка входа, от него рисуется все
    wwwroot/ - если папка есть, она будет расшарена в вебе
        img/
            image1.png
    pages/ - не строгое именование, просто рекомендация
        index.hbs
        404.hbs
    blocks/ - подключаемые shared блоки
        block1.hbs
```

### Page File 

page content example
```hbs
@page "/url"
@extraAttribute = "some value"

<div class="">
    Content
</div>
```

Так же используются предустановленные ссылки
- @page / Домашнаяя страницы
- @page /404 для остутствующих страниц
- @page /500 для отображения страницы ошибки сервера

Если отстутсвуют 404 или 500 будет использоваться страница /

### Root file

root файл должен быть именован как _root.@ext. Пример: _root.hbs.

Это файл точка входа, все остальное отрисовывается через него. У каждой папки может быть свой _root файл.
```hbs
<!doctype html>
<html lang="ru">
<head>
    ...
</head>
<body class="{{bodyClass}}" {{bodyAttrs}}>
    @Body
</body>
</html>
```

> @Body - тут будет отрисовано содержимое страницы

### Layout file
LayoutComponentBase обязательная для определения иначе он будет просто блоком.

```hbs
@inherits LayoutComponentBase

{{>blocks/header1}}
<div class="app-body d-flex flex-column flex-fill justify-content-center">
    <wrapper class="container">
        {{>@partial-block}}
    </wrapper>
</div>

```

### Block file
```hbs
<div>some content</div>
```

И блок можно отрисовать вызовом: папка/имя_файла(без расширения)
```hbs
{{>blocks/header1}}
```
Если такого блока нет по адресу будет ошибка, можно использовать не строгий вызов как 
```hbs
{{#>blocks/header1}}
```

#### Extra attributes 
Attribute can contain custom value

| команда   | пример        | описание  |
|---|---|---|
| page  | @page "/url"  | url may be template like "/post/{Slug}" also can contain filter "/user/{id:int:max(50)}
| layout | @layout 'layouts/blank_page'; @layout null | Шаблон страницы



## Базовые выражения шаблонизатора
```hbs
<!-- Simple expressions -->
<p>{{firstname}} {{lastname}}</p>

<!-- Loop -->
<ul class="people_list">
  {{#each people}}
    <li>{{.}} | {{firstname}}</li>
  {{/each}}
</ul>
```



### Контекст по умолчанию при отрисовке
По умолчанию доступны некоторые переменные, которые заданы Марсом [исходник](https://github.com/mdimai666/Mars/blob/master/src/Mars.Modules/Mars.WebSiteProcessor.Handlebars/TemplateData/HandlebarsTmpCtxBasicDataContext.cs)

| команда       | описание  |
|---|---|
|| **HandlebarsTmpCtxBasicDataContext**
|_user      | Данные текущего авторизованного пользователя [RenderContextUser](https://github.com/mdimai666/Mars/blob/master/src/Mars.Host.Shared/WebSite/Models/RenderContextUser.cs)
|_req       | Данные о запросе, url, headers и т.д. [WebClientRequest](https://github.com/mdimai666/Mars/blob/master/src/Mars.Host.Shared/Models/WebClientRequest.cs)
|SysOptions | Системные настройки сайта Имя, домен и т.д. [SysOptions](https://github.com/mdimai666/Mars/blob/master/src/Mars.Shared/Options/SysOptions.cs)
|| **HandlebarsTmpCtxLanguageDataFiller**
| _lang     | "ru"
|| **HandlebarsTmpCtxAppThemeFiller**
| appTheme  | Текущая тема