---
name: mars-settings
description: Настройки сайта: базовые (get_site_settings / update_site_settings) и любые опции (list_site_options / get_site_option / update_site_option). Используй, когда задача про настройки сайта, SEO, режим обслуживания, медиа, маршрутизацию фронта.
tags: настройки, опции, seo, сайт
---

# Настройки сайта

- базовые (имя сайта, описание, email админа) — инструменты get_site_settings / update_site_settings;
- любые другие (SEO, режим обслуживания, медиа, маршрутизация фронта и т.д.) —
  list_site_options → get_site_option → update_site_option;
- перед изменением обязательно прочитай текущее значение; изменяй только нужные поля и передавай
  ПОЛНЫЙ JSON настройки, сохраняя точный регистр имён полей;
- если настройка защищена (readable/writable = false), честно скажи об этом и предложи поменять её вручную в Настройках.
