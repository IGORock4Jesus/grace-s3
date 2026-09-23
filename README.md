# GraceS3

## Авторизация Swagger через Keycloak

Swagger доступен в окружении `Development`: `http://localhost:5000/swagger`.
Кнопка **Authorize** открывает вход в Keycloak. После входа Swagger получает
access token через Authorization Code Flow с PKCE и передаёт его в API
в заголовке `Authorization: Bearer …`.

В realm `graces3-dev` клиент `swagger` должен иметь настройки:

- Client type: `OpenID Connect`.
- Client authentication: `Off` (публичный браузерный клиент, без client secret).
- Standard flow: `On`.
- Valid redirect URIs: `http://localhost:5000/swagger/oauth2-redirect.html`.
- Web origins: `http://localhost:5000`.
- Proof Key for Code Exchange Code Challenge Method: `S256`.
- Client scopes: `openid` и `profile` должны быть доступны при авторизации.

При другом адресе приложения укажите соответствующие callback URL и origin
в Keycloak. Callback ведёт на Swagger, а не на сервер Keycloak; Swagger UI
определяет его по текущему адресу в браузере.

Раздел `Swagger` конфигурации задаёт `OAuthUri`, `OAuthRealm` и `OAuthClientID`.
Клиент API из раздела `Auth` настраивается отдельно.

Проверка: запустите `dotnet run --project GraceS3`, откройте Swagger, нажмите
**Authorize**, выполните вход и вызовите защищённый метод через **Try it out**.
В запросе к Keycloak должны быть `response_type=code`, `client_id=swagger`
и `code_challenge_method=S256`; после возврата запрос к API должен содержать
Bearer token.

Документация: [Swagger UI OAuth 2.0](https://swagger.io/docs/open-source-tools/swagger-ui/usage/oauth2/),
[Keycloak: браузерные клиенты](https://www.keycloak.org/securing-apps/javascript-adapter).

## File storage

Files are stored in `/var/lib/graces3` by default. Set `GRACES3_WORKING_DIRECTORY`
to override it (for example `/var/graces3` or an absolute development directory).
The service account needs read/write access to this directory. Relative paths are
resolved against the service working directory, never the OS temporary directory.
For Docker, mount a persistent volume at the configured storage path.

When upgrading, stop the service and copy the contents of the old
`<OS temp>/grace-s3/<previous GRACES3_WORKING_DIRECTORY>` directory to the new
storage directory, preserving client UUID directories, file UUID names and permissions.
Set the new path before restarting; existing files are not moved automatically.

The admin Files page groups files into folders named by access key; physical
directories continue to use client UUIDs. Both folders and files are sorted by name.
Admin downloads require the admin role. Clients can be deleted only when empty.

Administrators can delete individual files from the Files page after confirmation.
Deletion removes the stored file and its database record; it cannot be undone.
If database cleanup fails after disk deletion, retry the request to remove the remaining record.
