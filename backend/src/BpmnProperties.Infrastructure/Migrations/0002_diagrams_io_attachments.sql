ALTER TABLE processes
    ADD COLUMN "DiagramObjectKey" varchar(500),
    ADD COLUMN "FileName" varchar(300);

CREATE TABLE element_attachments (
    "Id" uuid PRIMARY KEY,
    "ElementId" uuid NOT NULL REFERENCES bpmn_elements ("Id") ON DELETE CASCADE,
    "ObjectKey" varchar(500) NOT NULL,
    "FileName" varchar(300) NOT NULL,
    "ContentType" varchar(200) NOT NULL,
    "Size" bigint NOT NULL,
    "CreatedAt" timestamptz NOT NULL
);
CREATE INDEX "IX_element_attachments_ElementId" ON element_attachments ("ElementId");

INSERT INTO property_definitions
    ("Id", "ProcessId", "Key", "DisplayName", "Description", "DataType", "IsRequired", "IsActive", "DisplayOrder")
VALUES
    (gen_random_uuid(), NULL, 'input', 'Input', 'Task input', 'MultilineText', false, true, 1000),
    (gen_random_uuid(), NULL, 'output', 'Output', 'Task output', 'MultilineText', false, true, 1001)
ON CONFLICT DO NOTHING;
