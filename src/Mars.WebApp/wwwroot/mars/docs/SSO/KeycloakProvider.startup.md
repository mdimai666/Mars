## 🚀 1. Войти в админку Keycloak

Перейди в:

```
https://your-keycloak-domain/auth/admin/
```

(или `http://localhost:8080` если у тебя локально)
Авторизуйся под администратором.

---

## 🧭 2. Выбери или создай Realm

* Если у тебя ещё нет Realm — создай:

  1. В левом верхнем углу нажми **“Create realm”**
  2. Укажи имя, например `myrealm`
  3. Нажми **Create**

Все пользователи и клиенты хранятся внутри Realm — это как “изолированный мир”.

---

## 🧩 3. Создай клиента (OAuth приложение)

1. Перейди в меню:
   `Clients → Create client`
2. Введи:

   * **Client ID**: `myapp`
   * **Client type**: `OpenID Connect`
   * Нажми **Next**
3. На вкладке **Capability config**:

   * Включи: ✅ *Client authentication*
   * Включи: ✅ *Authorization* (если нужно)
   * Включи: ✅ *Standard Flow* (Authorization Code Flow)
   * Нажми **Save**

---

## ⚙️ 4. Настрой редиректы и URL-ы

Открой вкладку **Settings** клиента:

* **Valid redirect URIs** — адреса, куда Keycloak может вернуть код авторизации:

  ```
  https://localhost:5001/signin-oidc
  ```

  или

  ```
  https://example.com/signin-oidc
  ```

* **Home URL:** (опционально)

  ```
  https://localhost:5001/
  ```

* **Web origins:**

  ```
  +
  ```

  или конкретные URL (например, если у тебя SPA на другом домене).

Нажми **Save**.

---

## 🔑 5. Найди свои OAuth данные

Перейди на вкладку **Credentials**:
Там ты увидишь:

* `Client Secret`
* `Client ID` (тот, что ты задал)

Эти значения ты потом вставишь в приложение.

---

## ⚙️ 6. Настроить Mars

[OpenID Connect](/dev/Settings/Option/Mars+SSO+Contracts+Options+OpenIDClientOption)

| Поле | Значение |
| -- |
| oauth2_auth_endpoint      | http://localhost:6767/realms/myrealm/protocol/openid-connect/auth |
| oauth2_token_endpoint     | http://localhost:6767/realms/myrealm/protocol/openid-connect/token |
| Issuer                    | http://localhost:6767/realms/myrealm |
| Scopes                    | openid email profile |

---

## ✅ 7. Проверь

1. Запусти приложение.
2. Перейди на `/dev/login`.
3. После входа через Keycloak ты должен попасть обратно на `/`, где будет видно имя пользователя.

---
## 🌐 Проверь OpenID Connect метаданные

Keycloak автоматически публикует конфигурацию:

```
https://your-keycloak-domain/realms/{realm-name}/.well-known/openid-configuration
```

Например:

```
http://localhost:8080/realms/myrealm/.well-known/openid-configuration
```

Там можно увидеть все нужные эндпоинты:

| Назначение             | URL-пример                                         |
| ---------------------- | -------------------------------------------------- |
| Authorization endpoint | `/realms/myrealm/protocol/openid-connect/auth`     |
| Token endpoint         | `/realms/myrealm/protocol/openid-connect/token`    |
| UserInfo endpoint      | `/realms/myrealm/protocol/openid-connect/userinfo` |
| JWKS endpoint          | `/realms/myrealm/protocol/openid-connect/certs`    |

---

## 🔐 Полезные ссылки

| Назначение        | Пример URL                                         |
| ----------------- | -------------------------------------------------- |
| OpenID конфиг     | `/realms/myrealm/.well-known/openid-configuration` |
| Token endpoint    | `/realms/myrealm/protocol/openid-connect/token`    |
| UserInfo endpoint | `/realms/myrealm/protocol/openid-connect/userinfo` |
| Logout endpoint   | `/realms/myrealm/protocol/openid-connect/logout`   |
