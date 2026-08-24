# NexusForever on Coolify

This deployment runs the production services directly with Docker Compose. The
.NET Aspire AppHost remains useful for local development but is not run in
production.

## 1. Prepare the host data

Copy the generated WildStar data to the Coolify server. The resulting layout
must be:

```text
/data/wildstar/server-data/tbl/*.tbl
/data/wildstar/server-data/map/**/*.nfmap
```

The directories are mounted read-only. Do not commit these files to Git or bake
them into the application image.

## 2. Create the Coolify application

1. Create a Docker Compose application from this Git repository.
2. Select the `wildstar-16042-development` branch.
3. Set the Compose file to `/compose.coolify.yaml`.
4. Add the variables from `deploy/.env.example` in Coolify. Replace every
   placeholder password. Use long alphanumeric values for the MySQL and
   RabbitMQ passwords because they are embedded in connection strings.
5. Deploy the application.

The world-database repository and ref are Docker build arguments. Pin
`WORLD_DATABASE_REF` to a commit SHA for reproducible production builds.

## 3. Network and firewall

Open these TCP ports on the Coolify host and at the hosting provider:

| Port | Purpose |
| ---: | --- |
| 23115 | Authentication server |
| 6600 | STS server |
| 24000 | World server |

The account API, character API, MySQL, RabbitMQ and the world web console are
internal only. Do not assign public Coolify domains to them unless access is
protected separately.

## 4. Configure the public realm address

The initial auth migration seeds the realm with `127.0.0.1`. After the first
successful deployment, open a shell in the MySQL container and inspect the
realm row:

```sql
USE nexus_forever_auth;
SELECT * FROM server;
```

Then set `host` to the public IPv4 address or DNS name reachable by WildStar
clients and keep `port` at `24000`:

```sql
UPDATE server SET host = 'game.example.com', port = 24000 WHERE id = 1;
```

## 5. Verify and operate

Check that `database-migrations` exits successfully and that all long-running
services remain healthy. A stopped migration container is expected. Back up the
`mysql-data` volume regularly through Coolify. RabbitMQ data is persisted in
`rabbitmq-data`.

After changing database migrations, redeploy the entire stack. To rotate a
database or RabbitMQ password, update both the Coolify variable and the actual
database/broker credential; changing only the variable does not modify an
existing persistent service automatically.
