# AuthFlowConfigNode

A configuration node for automating authentication flows when making HTTP requests. Allows you to configure the acquisition and use of authentication tokens/cookies/keys before the main request to a protected API.

---

#### 🔑 Authentication Modes (`AuthFlowNodeMode`)

| Mode | Description | Used Properties |
|-------|----------|----------------------|
| `BearerToken` | OAuth 2.0 Client Credentials Flow — obtaining a JWT token via the `/token` endpoint | `TokenUrl`, `ClientId`, `ClientSecret`, `Scope`, `TimeoutSeconds` |
| `CookieForm` | Authentication via an HTML form (login/password) with cookie retrieval | `LoginPageUrl`, `Username`, `Password`, `FollowRedirects`, `TimeoutSeconds` |
| `CookieEndpoint` | Direct API endpoint call to retrieve cookies (without HTML parsing) | `CookieEndpointConfig`, `TimeoutSeconds` |
| `ApiKey` | Passing a static API key in the header | `ApiKey`, `ApiKeyHeaderName` |
| `Custom` | Custom authentication implementation | `CustomType`, other parameters optional |

---

#### ⚙️ Node Properties

##### General Parameters
| Property | Type | Default Value | Description |
|----------|----------------------|----------|
| `Mode` | `AuthFlowNodeMode` | `BearerToken` | Authentication Mode |
| `CustomType` | `string?` | `null` | Full type name for custom implementation (if `Mode = Custom`) |
| `Username` | `string?` | `null` | Login for `CookieForm` / `Basic` modes |
| `Password` | `string?` | `null` | Password for `CookieForm` / `Basic` modes |
| `TimeoutSeconds` | `int` | `30` | Authentication request timeout (in seconds) |

##### Bearer Token (OAuth 2.0)
| Property | Type | Default Value | Description |
|----------|----------------------|----------|
| `TokenUrl` | `string?` | `null` | URL of the token issuing endpoint (e.g., `https://auth.example.com/oauth/token`) |
| `ClientId` | `string?` | `null` | Client ID |
| `ClientSecret` | `string?` | `null` | Client secret |
| `Scope` | `string` | `"openid email"` | Requested access rights (separated by spaces) |

> **Request format:** `POST /token` with `application/x-www-form-urlencoded`:
> ```
> grant_type=client_credentials&client_id=...&client_secret=...&scope=...
> ```

##### Cookie Form (HTML form)
| Property | Type | Default Value | Description |
|----------|------|----------------------|----------|
| `LoginPageUrl` | `string?` | `null` | Login page URL (e.g., `https://app.example.com/login`) |
| `FollowRedirects` | `bool` | `true` | Follow redirects after form submission to retrieve final cookies |

> **Feature:** Noda automatically parses the HTML form on the page, finds the `username`/`password` fields, submits the form, and extracts `Set-Cookie` from the response.

##### Cookie Endpoint (API)
| Property | Type | Description |
|----------|----------|
| `CookieEndpointConfig` | `AuthFlowCookieEndpointConfig` | Configuration for a direct API call to retrieve cookies |

Configuration for directly calling the authentication endpoint without HTML parsing.

```csharp
public class AuthFlowCookieEndpointConfig
```

| Property | Type | Default value | Description |
|----------|------|----------------------|----------|
| `LoginEndpointUrl` | `string` | `""` | Authentication endpoint URL (required) |
| `UsernameFieldName` | `string` | `"username"` | Name of the login field in the request body |
| `PasswordFieldName` | `string` | `"password"` | Name of the password field in the request body |
| `AdditionalFields` | `Dictionary<string, string>` | `[]` | Additional fields to send |
| `ContentType` | `AuthFlowLoginEndpointContentType` | `FormData` | Request body content type |
| `LoginHeaders` | `Dictionary<string, string>` | `[]` | Additional request headers |
| `HealthCheckUrl` | `string?` | `null` | URL for session validation |
| `AuthCookieName` | `string?` | `null` | Authentication cookie name (if known) |
| `FollowRedirects` | `bool` | `true` | Follow redirects after login |

###### 📋 `AuthFlowLoginEndpointContentType`

Content type for the authentication request body.

```csharp
public enum AuthFlowLoginEndpointContentType
```

| Value | Description | Content type | Body format |
|----------|------------|--------------|--------------|
| `FormData` | URL-encoded form | `application/x-www-form-urlencoded` | `username=admin&password=123` |
| `Json` | JSON object | `application/json` | `{"username":"admin","password":"123"}` |
| `Multipart` | Multipart form | `multipart/form-data` | Edge format with files |

---

##### API Key
| Property | Type | Default Value | Description |
|----------|------|----------------------|----------|
| `ApiKey` | `string?` | `null` | Static access key |
| `ApiKeyHeaderName` | `string` | `"X-API-Key"` | HTTP header name for key transfer |

---

#### 🔁 Execution Behavior

1. Before the main request, the node executes an authentication flow according to `Mode`
2. Received credentials (token/cookie/key) are cached for the lifetime of the session
3. When the token expires (401 Unauthorized), an automatic refresh occurs
4. All authentication requests respect `TimeoutSeconds`
5. Authentication errors abort the main request with detailed logging

---
