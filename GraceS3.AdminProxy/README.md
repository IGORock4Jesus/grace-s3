# AdminProxy sessions

Set `REDIS_CONNECTION_STRING` to a StackExchange.Redis connection string. The
placeholder in `appsettings.Development.json` is intentionally empty. Environment
variables override JSON configuration. The proxy requires this setting at startup.

`REDIS_KEY_PREFIX` defaults to `graces3-adminproxy:` (`graces3-adminproxy-dev:` in
Development). Use the same prefix and Redis database for all replicas of one
deployment, and different prefixes for different environments.

Redis stores protected authentication tickets with the cookie expiration as TTL,
and the shared ASP.NET Core Data Protection key ring without expiration. Configure
Redis persistence to retain sessions and keys across Redis restarts. Existing
in-memory sessions require a new login after this change.

The cookie contains a session reference; access and refresh tokens remain on the
server. Logout removes the shared ticket. Cookie options receive the ticket store
through DI without creating a separate service provider.

The web app's `withAuthLoader` converts HTTP 401 from root and child loaders into
a document redirect to `/auth/login?returnUrl=...`. Login returns to that local
path. HTTP 403, network errors and server failures are not treated as logout.
Wrap new protected `clientLoader` functions with this helper as well.

Checks (no Redis server required):

```sh
dotnet run --project GraceS3.AdminProxy.Tests
node --test admin-web/tests/auth.test.mjs
```

The session checks use a shared fake cache; they do not verify connectivity or
persistence against a real Redis deployment.
