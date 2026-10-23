CREATE TABLE organizations (
    "Id" uuid PRIMARY KEY,
    "Name" varchar(200) NOT NULL,
    "Code" varchar(100),
    "IsActive" boolean NOT NULL,
    "CreatedAt" timestamptz NOT NULL,
    "UpdatedAt" timestamptz NOT NULL
);
CREATE UNIQUE INDEX "IX_organizations_Name" ON organizations ("Name");
CREATE UNIQUE INDEX "IX_organizations_Code" ON organizations ("Code") WHERE "Code" IS NOT NULL;

CREATE TABLE suborganizations (
    "Id" uuid PRIMARY KEY,
    "OrganizationId" uuid NOT NULL REFERENCES organizations ("Id") ON DELETE RESTRICT,
    "Name" varchar(200) NOT NULL,
    "Code" varchar(100),
    "IsActive" boolean NOT NULL,
    "CreatedAt" timestamptz NOT NULL,
    "UpdatedAt" timestamptz NOT NULL
);
CREATE UNIQUE INDEX "IX_suborganizations_OrganizationId_Name"
    ON suborganizations ("OrganizationId", "Name");
CREATE UNIQUE INDEX "IX_suborganizations_OrganizationId_Code"
    ON suborganizations ("OrganizationId", "Code")
    WHERE "Code" IS NOT NULL;

INSERT INTO organizations ("Id", "Name", "Code", "IsActive", "CreatedAt", "UpdatedAt")
VALUES ('00000000-0000-0000-0000-000000000001', 'Órgão padrão', 'DEFAULT', true, now(), now());
INSERT INTO suborganizations ("Id", "OrganizationId", "Name", "Code", "IsActive", "CreatedAt", "UpdatedAt")
VALUES ('00000000-0000-0000-0000-000000000002', '00000000-0000-0000-0000-000000000001', 'Subórgão padrão', 'DEFAULT', true, now(), now());

ALTER TABLE processes ADD COLUMN "SuborganizationId" uuid;
UPDATE processes
SET "SuborganizationId" = '00000000-0000-0000-0000-000000000002'
WHERE "SuborganizationId" IS NULL;
ALTER TABLE processes ALTER COLUMN "SuborganizationId" SET NOT NULL;
ALTER TABLE processes
    ADD CONSTRAINT "FK_processes_suborganizations_SuborganizationId"
    FOREIGN KEY ("SuborganizationId") REFERENCES suborganizations ("Id") ON DELETE RESTRICT;
CREATE INDEX "IX_processes_SuborganizationId" ON processes ("SuborganizationId");
