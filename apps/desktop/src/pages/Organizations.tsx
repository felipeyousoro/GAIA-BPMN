import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  createOrganization,
  createSuborganization,
  listOrganizations,
  listSuborganizations,
  updateOrganization,
  updateSuborganization,
  type Organization,
  type Suborganization
} from "../api";

export function Organizations() {
  const queryClient = useQueryClient();
  const [selectedOrganizationId, setSelectedOrganizationId] = useState<string>();
  const [organizationName, setOrganizationName] = useState("");
  const [organizationCode, setOrganizationCode] = useState("");
  const [suborganizationName, setSuborganizationName] = useState("");
  const [suborganizationCode, setSuborganizationCode] = useState("");

  const organizations = useQuery({ queryKey: ["organizations"], queryFn: listOrganizations });
  const suborganizations = useQuery({
    queryKey: ["suborganizations", selectedOrganizationId],
    queryFn: () => listSuborganizations(selectedOrganizationId!),
    enabled: Boolean(selectedOrganizationId)
  });
  const refreshOrganizations = () => {
    void queryClient.invalidateQueries({ queryKey: ["organizations"] });
    void queryClient.invalidateQueries({ queryKey: ["suborganizations"] });
  };
  const createOrganizationMutation = useMutation({
    mutationFn: () => createOrganization(organizationName, organizationCode),
    onSuccess: organization => {
      setOrganizationName("");
      setOrganizationCode("");
      setSelectedOrganizationId(organization.id);
      refreshOrganizations();
    }
  });
  const createSuborganizationMutation = useMutation({
    mutationFn: () => createSuborganization(selectedOrganizationId!, suborganizationName, suborganizationCode),
    onSuccess: () => {
      setSuborganizationName("");
      setSuborganizationCode("");
      refreshOrganizations();
    }
  });
  const error = organizations.error
    ?? suborganizations.error
    ?? createOrganizationMutation.error
    ?? createSuborganizationMutation.error;

  const toggleOrganization = (organization: Organization) => {
    void updateOrganization(organization.id, organization.name, !organization.isActive).then(refreshOrganizations);
  };
  const toggleSuborganization = (suborganization: Suborganization) => {
    void updateSuborganization(
      suborganization.organizationId,
      suborganization.id,
      suborganization.name,
      !suborganization.isActive
    ).then(refreshOrganizations);
  };

  return (
    <div className="settings-container">
      <section className="settings-card">
        <div className="settings-card-header">
          <div>
            <h2>Órgãos</h2>
            <p className="muted-text">Cadastre e ative os contextos organizacionais dos BPMNs.</p>
          </div>
        </div>
        <form
          className="settings-form-grid"
          onSubmit={event => {
            event.preventDefault();
            if (organizationName.trim()) createOrganizationMutation.mutate();
          }}
        >
          <label className="form-label">Nome do órgão<input value={organizationName} onChange={event => setOrganizationName(event.target.value)} required /></label>
          <label className="form-label">Código (opcional)<input value={organizationCode} onChange={event => setOrganizationCode(event.target.value)} /></label>
          <button type="submit" className="btn-primary" disabled={createOrganizationMutation.isPending}>Adicionar órgão</button>
        </form>
        <div className="process-grid">
          {organizations.data?.map(organization => (
            <button
              type="button"
              className={`process-card ${selectedOrganizationId === organization.id ? "selected" : ""}`}
              key={organization.id}
              onClick={() => setSelectedOrganizationId(organization.id)}
            >
              <strong className="process-card-title">{organization.name}</strong>
              <span className="process-card-file">{organization.code ?? "Sem código"} · {organization.isActive ? "Ativo" : "Inativo"}</span>
              <span className="btn-secondary btn-xs" onClick={event => { event.stopPropagation(); toggleOrganization(organization); }}>
                {organization.isActive ? "Inativar" : "Ativar"}
              </span>
            </button>
          ))}
        </div>
      </section>

      <section className="settings-card">
        <div className="settings-card-header">
          <div>
            <h2>Subórgãos</h2>
            <p className="muted-text">{selectedOrganizationId ? "Subórgãos do órgão selecionado." : "Selecione um órgão para administrar seus subórgãos."}</p>
          </div>
        </div>
        {selectedOrganizationId && (
          <>
            <form
              className="settings-form-grid"
              onSubmit={event => {
                event.preventDefault();
                if (suborganizationName.trim()) createSuborganizationMutation.mutate();
              }}
            >
              <label className="form-label">Nome do subórgão<input value={suborganizationName} onChange={event => setSuborganizationName(event.target.value)} required /></label>
              <label className="form-label">Código (opcional)<input value={suborganizationCode} onChange={event => setSuborganizationCode(event.target.value)} /></label>
              <button type="submit" className="btn-primary" disabled={createSuborganizationMutation.isPending}>Adicionar subórgão</button>
            </form>
            <div className="process-grid">
              {suborganizations.data?.map(suborganization => (
                <div className="process-card" key={suborganization.id}>
                  <strong className="process-card-title">{suborganization.name}</strong>
                  <span className="process-card-file">{suborganization.code ?? "Sem código"} · {suborganization.isActive ? "Ativo" : "Inativo"}</span>
                  <button type="button" className="btn-secondary btn-xs" onClick={() => toggleSuborganization(suborganization)}>
                    {suborganization.isActive ? "Inativar" : "Ativar"}
                  </button>
                </div>
              ))}
            </div>
          </>
        )}
      </section>
      {error && <p className="error">{error.message}</p>}
    </div>
  );
}
