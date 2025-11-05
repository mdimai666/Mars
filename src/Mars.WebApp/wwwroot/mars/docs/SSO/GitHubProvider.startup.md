## 🚀 1. Создай OAuth App в GitHub

1. Перейди на страницу:
   👉 [https://github.com/settings/developers](https://github.com/settings/developers)
2. Нажми **“New OAuth App”**
3. Заполни поля:

   * **Application name:** любое название
   * **Homepage URL:** `https://localhost:5001/` (или твой сайт)
   * **Authorization callback URL:**

     ```
     http://localhost:5003/dev/Login?provider=github1
     ```
4. Нажми **Register application**

После этого ты получишь:

* **Client ID**
* **Client Secret**

---

## ⚙️ 2. Настроить Mars

[OpenID Connect](/dev/Settings/Option/Mars+Options+Models+OpenIDClientOption)

| Поле | Значение |
| -- |
| oauth2_auth_endpoint      | https://github.com/login/oauth/authorize |
| oauth2_token_endpoint     | https://github.com/login/oauth/access_token |
| Issuer                    | https://github.com |
| Scopes                    | openid email profile |

---

## ✅ 3. Проверь

1. Запусти приложение.
2. Перейди на `/dev/login`.
3. После входа через GitHub ты должен попасть обратно на `/`, где будет видно имя пользователя.

---

## 🔍 Эндпоинты GitHub (если хочешь вручную)

| Назначение                 | URL                                           |
| -------------------------- | --------------------------------------------- |
| **Authorization endpoint** | `https://github.com/login/oauth/authorize`    |
| **Token endpoint**         | `https://github.com/login/oauth/access_token` |
| **User info endpoint**     | `https://api.github.com/user`                 |
| **Email info endpoint**    | `https://api.github.com/user/emails`          |

---
