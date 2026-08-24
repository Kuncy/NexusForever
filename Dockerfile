# syntax=docker/dockerfile:1

FROM alpine/git:2.49.1 AS world-database
ARG WORLD_DATABASE_REPOSITORY=https://github.com/Kuncy/NexusForever.WorldDatabase.git
ARG WORLD_DATABASE_REF=master
RUN git init /world-database \
    && git -C /world-database remote add origin "${WORLD_DATABASE_REPOSITORY}" \
    && git -C /world-database fetch --depth 1 origin "${WORLD_DATABASE_REF}" \
    && git -C /world-database checkout --detach FETCH_HEAD \
    && rm -rf /world-database/.git

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY . .
RUN dotnet restore Source/NexusForever.slnx

RUN dotnet publish Source/NexusForever.API.Account/NexusForever.API.Account.csproj \
        --configuration Release --no-restore --output /out/account-api \
    && dotnet publish Source/NexusForever.API.Character/NexusForever.API.Character.csproj \
        --configuration Release --no-restore --output /out/character-api \
    && dotnet publish Source/NexusForever.Aspire.Database.Migrations/NexusForever.Aspire.Database.Migrations.csproj \
        --configuration Release --no-restore --output /out/database-migrations \
    && dotnet publish Source/NexusForever.AuthServer/NexusForever.AuthServer.csproj \
        --configuration Release --no-restore --output /out/auth \
    && dotnet publish Source/NexusForever.Server.Character/NexusForever.Server.Character.csproj \
        --configuration Release --no-restore --output /out/character \
    && dotnet publish Source/NexusForever.Server.ChatServer/NexusForever.Server.ChatServer.csproj \
        --configuration Release --no-restore --output /out/chat \
    && dotnet publish Source/NexusForever.Server.Friendship/NexusForever.Server.Friendship.csproj \
        --configuration Release --no-restore --output /out/friendship \
    && dotnet publish Source/NexusForever.Server.GroupServer/NexusForever.Server.GroupServer.csproj \
        --configuration Release --no-restore --output /out/group \
    && dotnet publish Source/NexusForever.StsServer/NexusForever.StsServer.csproj \
        --configuration Release --no-restore --output /out/sts \
    && dotnet publish Source/NexusForever.WorldServer/NexusForever.WorldServer.csproj \
        --configuration Release --no-restore --output /out/world

# Script assemblies are loaded dynamically by the world server at runtime.
RUN set -eu; \
    for project in \
        NexusForever.Script.Alizar \
        NexusForever.Script.Arcterra \
        NexusForever.Script.Farside \
        NexusForever.Script.Instance \
        NexusForever.Script.Isigrol \
        NexusForever.Script.Main \
        NexusForever.Script.Olyssia; \
    do \
        rm -rf /tmp/script; \
        dotnet publish "Source/${project}/${project}.csproj" \
            --configuration Release --no-restore --output /tmp/script; \
        cp "/tmp/script/${project}.dll" /out/world/; \
    done

# Every process requires its JSON file; runtime values are overridden by environment variables.
RUN cp /out/account-api/AccountAPI.example.json /out/account-api/AccountAPI.json \
    && cp /out/character-api/CharacterAPI.example.json /out/character-api/CharacterAPI.json \
    && cp /out/database-migrations/AspireMigrations.example.json /out/database-migrations/AspireMigrations.json \
    && cp /out/auth/AuthServer.example.json /out/auth/AuthServer.json \
    && cp /out/character/CharacterServer.example.json /out/character/CharacterServer.json \
    && cp /out/chat/ChatServer.example.json /out/chat/ChatServer.json \
    && cp /out/friendship/FriendshipServer.example.json /out/friendship/FriendshipServer.json \
    && cp /out/group/GroupServer.example.json /out/group/GroupServer.json \
    && cp /out/sts/StsServer.example.json /out/sts/StsServer.json \
    && cp /out/world/WorldServer.example.json /out/world/WorldServer.json

FROM mysql:8.4 AS database
COPY deploy/mysql-init.sh /docker-entrypoint-initdb.d/10-nexusforever-databases.sh
RUN chmod 0755 /docker-entrypoint-initdb.d/10-nexusforever-databases.sh

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /out/account-api ./account-api
COPY --from=build /out/character-api ./character-api
COPY --from=build /out/database-migrations ./database-migrations
COPY --from=build /out/auth ./auth
COPY --from=build /out/character ./character
COPY --from=build /out/chat ./chat
COPY --from=build /out/friendship ./friendship
COPY --from=build /out/group ./group
COPY --from=build /out/sts ./sts
COPY --from=build /out/world ./world
COPY --from=world-database /world-database ./database/world

RUN mkdir -p /app/world/tbl /app/world/map /app/chat/tbl /app/friendship/tbl \
    && chown -R app:app /app

USER app
ENTRYPOINT ["dotnet"]
