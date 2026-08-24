#!/bin/sh
set -eu

mysql --protocol=socket -uroot -p"${MYSQL_ROOT_PASSWORD}" <<-EOSQL
CREATE DATABASE IF NOT EXISTS nexus_forever_auth CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
CREATE DATABASE IF NOT EXISTS nexus_forever_character CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
CREATE DATABASE IF NOT EXISTS nexus_forever_world CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
CREATE DATABASE IF NOT EXISTS nexus_forever_group CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
CREATE DATABASE IF NOT EXISTS nexus_forever_chat CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
CREATE DATABASE IF NOT EXISTS nexus_forever_friendship CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
CREATE DATABASE IF NOT EXISTS nexus_forever_query CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

CREATE USER IF NOT EXISTS '${MYSQL_USER}'@'%' IDENTIFIED BY '${MYSQL_PASSWORD}';
ALTER USER '${MYSQL_USER}'@'%' IDENTIFIED BY '${MYSQL_PASSWORD}';
GRANT ALL PRIVILEGES ON nexus_forever_auth.* TO '${MYSQL_USER}'@'%';
GRANT ALL PRIVILEGES ON nexus_forever_character.* TO '${MYSQL_USER}'@'%';
GRANT ALL PRIVILEGES ON nexus_forever_world.* TO '${MYSQL_USER}'@'%';
GRANT ALL PRIVILEGES ON nexus_forever_group.* TO '${MYSQL_USER}'@'%';
GRANT ALL PRIVILEGES ON nexus_forever_chat.* TO '${MYSQL_USER}'@'%';
GRANT ALL PRIVILEGES ON nexus_forever_friendship.* TO '${MYSQL_USER}'@'%';
GRANT ALL PRIVILEGES ON nexus_forever_query.* TO '${MYSQL_USER}'@'%';
FLUSH PRIVILEGES;
EOSQL
