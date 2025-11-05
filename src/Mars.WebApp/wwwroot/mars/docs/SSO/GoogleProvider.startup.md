## 🚀 1. Создать проект в Google Cloud Console

1. Перейди в [Google Cloud Console](https://console.cloud.google.com/).

2. В верхнем меню выбери или создай **проект**.

3. Перейди в **APIs & Services → Credentials**.

4. Нажми **Create Credentials → OAuth client ID**.

5. Если потребуется — сначала включи **OAuth consent screen** (экран согласия):

   * Укажи тип: *External* (внешний).
   * Введи название приложения.
   * Укажи email.
   * Добавь разрешённые домены (например, `example.com`).
   * Сохрани.

6. Затем выбери тип клиента:
   👉 **Web application**
   и укажи:

   * **Authorized redirect URIs** — например:

     ```
     https://example.com/dev/Login?provider=google1
     ```

     или при локальной разработке:

     ```
     http://localhost:5003/dev/Login?provider=google1
     ```

7. После создания ты получишь:

   * `Client ID`
   * `Client Secret`

---

## ⚙️ 2. Настроить Mars

[OpenID Connect](/dev/Settings/Option/Mars+Options+Models+OpenIDClientOption)

| Поле | Значение |
| -- |
| oauth2_auth_endpoint      | https://accounts.google.com/o/oauth2/v2/auth |
| oauth2_token_endpoint     | https://oauth2.googleapis.com/token |
| Issuer                    | https://accounts.google.com |
| Scopes                    | openid email profile |

---

## ✅ 3. Проверь

1. Запусти приложение.
2. Перейди на `/dev/login`.
3. После входа через Google ты должен попасть обратно на `/`, где будет видно имя пользователя.

---

## 🔐 OAuth 2.0 эндпоинты Google

Вот официальные URL, актуальные для **всех OAuth2/OpenID Connect запросов**:

| Назначение                                                 | URL                                                            |
| ---------------------------------------------------------- | -------------------------------------------------------------- |
| **Authorization endpoint** (вход, редирект пользователя)   | `https://accounts.google.com/o/oauth2/v2/auth`                 |
| **Token endpoint** (получение access_token, refresh_token) | `https://oauth2.googleapis.com/token`                          |
| **User info endpoint** (получение данных профиля)          | `https://www.googleapis.com/oauth2/v3/userinfo`                |
| **OpenID configuration** (JSON со всеми путями)            | `https://accounts.google.com/.well-known/openid-configuration` |

---
