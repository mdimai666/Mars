## 🚀 1. Зарегистрировать приложение в Microsoft Entra ID (бывший Azure AD)

1. Перейди на [https://entra.microsoft.com/](https://entra.microsoft.com/)
   (или [https://portal.azure.com/](https://portal.azure.com/) → Microsoft Entra ID).
2. В левом меню выбери:
   **Microsoft Entra ID → App registrations → New registration**
3. Заполни:

   * **Name:** `My Web App`
   * **Supported account types:**
     Обычно — *Accounts in any organizational directory and personal Microsoft accounts (e.g., Outlook.com, Xbox Live, etc.)*
   * **Redirect URI:**

     ```
     https://localhost:5001/signin-oidc
     ```
   * Нажми **Register**

---

## 🧩 2. Получи параметры приложения

После регистрации ты попадёшь на страницу приложения.
Там тебе понадобятся:

* **Application (client) ID** — `client_id`
* **Directory (tenant) ID** — `tenant_id`

Теперь открой раздел **Certificates & secrets** → **New client secret**
и скопируй его — это твой `client_secret`.

---

## ⚙️ 2. Настроить Mars

[OpenID Connect](/dev/Settings/Option/Mars+SSO+Contracts+Options+OpenIDClientOption)

| Поле | Значение |
| -- |
| oauth2_auth_endpoint      | https://login.microsoftonline.com/<tenant_id>/oauth2/v2.0/authorize |
| oauth2_token_endpoint     | https://login.microsoftonline.com/<tenant_id>/oauth2/v2.0/token |
| Issuer                    | https://login.microsoftonline.com/<tenant_id>/v2.0 |
| Scopes                    | openid email profile |

---

## ✅ 3. Проверь

1. Запусти приложение.
2. Перейди на `/dev/login`.
3. После входа через Microsoft ты должен попасть обратно на `/`, где будет видно имя пользователя.

---
## ⚙️ 3. OAuth/OpenID Connect эндпоинты Microsoft

Ты можешь увидеть их здесь:

```
https://login.microsoftonline.com/{tenant}/v2.0/.well-known/openid-configuration
```

или, например:

```
https://login.microsoftonline.com/common/v2.0/.well-known/openid-configuration
```

Основные эндпоинты:

| Назначение                 | URL                                                                |
| -------------------------- | ------------------------------------------------------------------ |
| **Authorization endpoint** | `https://login.microsoftonline.com/{tenant}/oauth2/v2.0/authorize` |
| **Token endpoint**         | `https://login.microsoftonline.com/{tenant}/oauth2/v2.0/token`     |
| **UserInfo endpoint**      | `https://graph.microsoft.com/oidc/userinfo`                        |
| **Logout endpoint**        | `https://login.microsoftonline.com/{tenant}/oauth2/v2.0/logout`    |
