# Release checklist for the improvement

Before a production rollout:

1. Back up PostgreSQL and the storage volume together.
2. Apply migrations `0001`–`0005` in a homologation environment and verify
   that legacy processes retain their IDs, diagrams, elements, and attachments.
3. Verify that the default Órgão/Subórgão receives every legacy process and
   that a BPMN cannot be assigned to two Subórgãos.
4. Test Admin and Guest access to catalog, element, attachment, download, and
   authenticated PDF printing routes.
5. Test task metadata save, reload, reimport without GAIA extensions, and
   upload without clicking **Salvar**.
6. Run `dotnet test backend/BpmnProperties.sln` and
   `Set-Location apps/desktop; npm run build`.
7. Reconcile storage objects against attachment metadata and inspect lifecycle
   logs after a pilot organization import.

The initial rollout should use one pilot organization. Expand only after the
backup, migration, authorization, and orphan-object checks succeed.
