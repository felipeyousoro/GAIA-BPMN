CREATE EXTENSION IF NOT EXISTS "pgcrypto";

CREATE TABLE processes (
    "Id" uuid PRIMARY KEY,
    "ExternalKey" varchar(200) NOT NULL,
    "Name" varchar(300) NOT NULL,
    "CreatedAt" timestamptz NOT NULL,
    "UpdatedAt" timestamptz NOT NULL
);
CREATE UNIQUE INDEX "IX_processes_ExternalKey" ON processes ("ExternalKey");

CREATE TABLE bpmn_elements (
    "Id" uuid PRIMARY KEY,
    "ProcessId" uuid NOT NULL REFERENCES processes ("Id") ON DELETE CASCADE,
    "ExternalElementId" varchar(200) NOT NULL,
    "ElementType" varchar(200),
    "Name" varchar(300)
);
CREATE UNIQUE INDEX "IX_bpmn_elements_ProcessId_ExternalElementId" ON bpmn_elements ("ProcessId", "ExternalElementId");

CREATE TABLE property_definitions (
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
CREATE UNIQUE INDEX "IX_property_definitions_ProcessId_Key" ON property_definitions ("ProcessId", "Key");

CREATE TABLE property_options (
    "Id" uuid PRIMARY KEY,
    "PropertyDefinitionId" uuid NOT NULL REFERENCES property_definitions ("Id") ON DELETE CASCADE,
    "Value" varchar(100) NOT NULL,
    "Label" varchar(200) NOT NULL,
    "DisplayOrder" integer NOT NULL
);
CREATE UNIQUE INDEX "IX_property_options_PropertyDefinitionId_Value" ON property_options ("PropertyDefinitionId", "Value");

CREATE TABLE property_values (
    "Id" uuid PRIMARY KEY,
    "ElementId" uuid NOT NULL REFERENCES bpmn_elements ("Id") ON DELETE CASCADE,
    "PropertyDefinitionId" uuid NOT NULL REFERENCES property_definitions ("Id") ON DELETE RESTRICT,
    "ValueJson" jsonb NOT NULL,
    "UpdatedAt" timestamptz NOT NULL,
    "Version" bytea NOT NULL
);
CREATE UNIQUE INDEX "IX_property_values_ElementId_PropertyDefinitionId" ON property_values ("ElementId", "PropertyDefinitionId");
