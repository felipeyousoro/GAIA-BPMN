CREATE TABLE IF NOT EXISTS users (
    "Id" uuid PRIMARY KEY,
    "Username" varchar(100) NOT NULL,
    "DisplayName" varchar(200) NOT NULL,
    "PasswordHash" varchar(500) NOT NULL,
    "Role" varchar(40) NOT NULL,
    "AvatarObjectKey" varchar(500),
    "AvatarContentType" varchar(200),
    "CreatedAt" timestamptz NOT NULL,
    "UpdatedAt" timestamptz NOT NULL
);

CREATE UNIQUE INDEX IF NOT EXISTS "IX_users_Username" ON users ("Username");
