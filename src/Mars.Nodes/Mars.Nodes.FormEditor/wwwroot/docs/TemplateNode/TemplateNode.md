# TemplateNode

Templated output

---

## Handlebars
[Handlebars.Net](https://github.com/Handlebars-Net/Handlebars.Net)
```html
<!-- Переменная -->
Привет, {{ name }}!

<!-- Условие -->
{{#if isAdmin}}
    Вы администратор.
{{else}}
    Вы пользователь.
{{/if}}

<!-- Цикл -->
<ul>
{{#each items}}
    <li>{{this}}</li>
{{/each}}
</ul>
```

---

## Scriban
[Scriban](https://scriban.github.io/)
```html
<!-- Переменная -->
Привет, {{ name }}!

<!-- Условие -->
{{ if is_admin }}
    Вы администратор.
{{ else }}
    Вы пользователь.
{{ end }}

<!-- Цикл -->
<ul>
{{ for item in items }}
    <li>{{ item }}</li>
{{ end }}
</ul>
```

---

## ScribanRazorStyleTemplateEngine
```html
<!-- Переменная -->
Привет, @Name!

<!-- Условие -->
@if(IsAdmin) {
    Вы администратор.
} else {
    Вы пользователь.
}

<!-- Цикл -->
<ul>
@for(item in Items) {
    <li>@item</li>
}
</ul>
```

---

## TextReplaceTemplateEngine
```html
<!-- Переменная -->
Привет, {Name}!
```
