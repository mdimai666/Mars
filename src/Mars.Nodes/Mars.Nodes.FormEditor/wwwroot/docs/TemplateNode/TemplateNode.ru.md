# TemplateNode

Шаблонизированный вывод

Varible
```html
//input (Payload = 123)
{{Payload}}

//output
123
```

Each
```html
//input ([1,2,3])
{{#each Payload}}
    <div>{{.}}</div>
{{/each}}
{{Payload}}

//output
<div>1</div>
<div>2</div>
<div>3</div>
```

See also

[https://github.com/Handlebars-Net/Handlebars.Net](https://github.com/Handlebars-Net/Handlebars.Net)
