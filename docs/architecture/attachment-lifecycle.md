# Attachment lifecycle

An attachment upload is independent from task field saving. The API validates
the file, generates a server-owned storage key, writes the object, persists its
metadata, and only then returns the attachment response.

The initial policy is 25 MiB per file and the following extension/MIME pairs:
PDF, PNG, JPEG, SVG, DOCX, XLSX, and BPMN/XML. The original name is sanitized
with `Path.GetFileName`; physical object keys are never returned to the
desktop.

If metadata persistence fails after the object is written, the API attempts to
delete the object and rethrows the failure. This prevents a failed upload from
being presented as successful. Future operational hardening should reconcile
storage keys against `element_attachments` and report cleanup failures.

Removal deletes the object before deleting its metadata. A failure is returned
to the caller and the metadata remains available for a retry.
