CREATE EXTENSION IF NOT EXISTS "pgcrypto";

CREATE TABLE IF NOT EXISTS processes (
    "Id" uuid PRIMARY KEY,
    "ExternalKey" varchar(200) NOT NULL,
    "Name" varchar(300) NOT NULL,
    "CreatedAt" timestamptz NOT NULL,
    "UpdatedAt" timestamptz NOT NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS "IX_processes_ExternalKey" ON processes ("ExternalKey");

CREATE TABLE IF NOT EXISTS bpmn_elements (
    "Id" uuid PRIMARY KEY,
    "ProcessId" uuid NOT NULL REFERENCES processes ("Id") ON DELETE CASCADE,
    "ExternalElementId" varchar(200) NOT NULL,
    "ElementType" varchar(200),
    "Name" varchar(300)
);
CREATE UNIQUE INDEX IF NOT EXISTS "IX_bpmn_elements_ProcessId_ExternalElementId" ON bpmn_elements ("ProcessId", "ExternalElementId");

CREATE TABLE IF NOT EXISTS property_definitions (
    "Id" uuid PRIMARY KEY,
    "ProcessId" uuid REFERENCES processes ("Id") ON DELETE CASCADE,
    "Key" varchar(100) NOT NULL,
    "DisplayName" varchar(200) NOT NULL,
    "Description" varchar(1000),
    "DataType" varchar(40) NOT NULL,
    "IsRequired" boolean NOT NULL,
    "IsActive" boolean NOT NULL,
    "DisplayOrder" integer NOT NULL,
    "DefaultValueJson" jsonb
);
CREATE UNIQUE INDEX IF NOT EXISTS "IX_property_definitions_ProcessId_Key" ON property_definitions ("ProcessId", "Key");

CREATE TABLE IF NOT EXISTS property_options (
    "Id" uuid PRIMARY KEY,
    "PropertyDefinitionId" uuid NOT NULL REFERENCES property_definitions ("Id") ON DELETE CASCADE,
    "Value" varchar(100) NOT NULL,
    "Label" varchar(200) NOT NULL,
    "DisplayOrder" integer NOT NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS "IX_property_options_PropertyDefinitionId_Value" ON property_options ("PropertyDefinitionId", "Value");

CREATE TABLE IF NOT EXISTS property_values (
    "Id" uuid PRIMARY KEY,
    "ElementId" uuid NOT NULL REFERENCES bpmn_elements ("Id") ON DELETE CASCADE,
    "PropertyDefinitionId" uuid NOT NULL REFERENCES property_definitions ("Id") ON DELETE RESTRICT,
    "ValueJson" jsonb NOT NULL,
    "UpdatedAt" timestamptz NOT NULL,
    "Version" bytea NOT NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS "IX_property_values_ElementId_PropertyDefinitionId" ON property_values ("ElementId", "PropertyDefinitionId");

ALTER TABLE processes ADD COLUMN IF NOT EXISTS "DiagramObjectKey" varchar(500);
ALTER TABLE processes ADD COLUMN IF NOT EXISTS "FileName" varchar(300);

CREATE TABLE IF NOT EXISTS element_attachments (
    "Id" uuid PRIMARY KEY,
    "ElementId" uuid NOT NULL REFERENCES bpmn_elements ("Id") ON DELETE CASCADE,
    "ObjectKey" varchar(500) NOT NULL,
    "FileName" varchar(300) NOT NULL,
    "ContentType" varchar(200) NOT NULL,
    "Size" bigint NOT NULL,
    "CreatedAt" timestamptz NOT NULL
);
CREATE INDEX IF NOT EXISTS "IX_element_attachments_ElementId" ON element_attachments ("ElementId");

INSERT INTO property_definitions
    ("Id", "ProcessId", "Key", "DisplayName", "Description", "DataType", "IsRequired", "IsActive", "DisplayOrder")
SELECT gen_random_uuid(), NULL, 'input', 'Input', 'Task input', 'MultilineText', false, true, 1000
WHERE NOT EXISTS (
    SELECT 1 FROM property_definitions WHERE "ProcessId" IS NULL AND "Key" = 'input'
);

INSERT INTO property_definitions
    ("Id", "ProcessId", "Key", "DisplayName", "Description", "DataType", "IsRequired", "IsActive", "DisplayOrder")
SELECT gen_random_uuid(), NULL, 'output', 'Output', 'Task output', 'MultilineText', false, true, 1001
WHERE NOT EXISTS (
    SELECT 1 FROM property_definitions WHERE "ProcessId" IS NULL AND "Key" = 'output'
);

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
