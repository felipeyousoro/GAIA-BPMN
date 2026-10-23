import { useRef, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { importProcess, listOrganizations, listProcesses, listSuborganizations, User } from "../api";

export function Home({
  user,
  onOpen
}: {
  user: User;
  onOpen: (id: string) => void;
}) {
  const input = useRef<HTMLInputElement>(null);
  const queryClient = useQueryClient();
  const isAdmin = user.role === "Admin";
  const [organizationId, setOrganizationId] = useState<string>();
  const [suborganizationId, setSuborganizationId] = useState<string>();

  const processes = useQuery({ queryKey: ["processes"], queryFn: listProcesses });
  const organizations = useQuery({ queryKey: ["organizations"], queryFn: listOrganizations });
  const suborganizations = useQuery({
    queryKey: ["suborganizations", organizationId],
    queryFn: () => listSuborganizations(organizationId!),
    enabled: Boolean(organizationId)
  });
  const importer = useMutation({
    mutationFn: (file: File) => importProcess(file, suborganizationId),
    onSuccess: process => {
      void queryClient.invalidateQueries({ queryKey: ["processes"] });
      onOpen(process.id);
    }
  });

  const totalProcesses = processes.data?.length ?? 0;
  const totalTasks = processes.data?.reduce((acc, p) => acc + p.taskCount, 0) ?? 0;

  return (
    <div className="home-container">
      <div className="kpi-banner">
        <div className="kpi-card">
          <span className="kpi-label">Processos Mapeados</span>
          <strong className="kpi-value">{totalProcesses}</strong>
        </div>
        <div className="kpi-card">
          <span className="kpi-label">Tasks Totais</span>
          <strong className="kpi-value">{totalTasks}</strong>
        </div>
        <div className="kpi-card">
          <span className="kpi-label">Modo de Acesso</span>
          <strong className="kpi-value">{isAdmin ? "Edição Completa (Mestre)" : "Somente Leitura (Convidado)"}</strong>
        </div>

        {isAdmin && (
          <div className="kpi-actions">
            <select
              className="form-input"
              value={organizationId ?? ""}
              onChange={event => {
                setOrganizationId(event.target.value || undefined);
                setSuborganizationId(undefined);
              }}
            >
              <option value="">Selecione o órgão</option>
              {organizations.data?.filter(item => item.isActive).map(item => (
                <option key={item.id} value={item.id}>{item.name}</option>
              ))}
            </select>
            <select
              className="form-input"
              value={suborganizationId ?? ""}
              onChange={event => setSuborganizationId(event.target.value || undefined)}
              disabled={!organizationId}
            >
              <option value="">Selecione o subórgão</option>
              {suborganizations.data?.filter(item => item.isActive).map(item => (
                <option key={item.id} value={item.id}>{item.name}</option>
              ))}
            </select>
            <button
              type="button"
              className="btn-primary"
              disabled={importer.isPending || !suborganizationId}
              onClick={() => input.current?.click()}
            >
              {importer.isPending ? "Importando..." : "+ Importar BPMN"}
            </button>
            {!suborganizationId && <small className="muted-text">Selecione o órgão e o subórgão antes de importar.</small>}
            <input
              ref={input}
              hidden
              type="file"
              accept=".bpmn"
              onChange={event => {
                const file = event.target.files?.[0];
                if (file) importer.mutate(file);
                event.target.value = "";
              }}
            />
          </div>
        )}
      </div>

      {importer.error && <p className="error">{importer.error.message}</p>}
      {processes.isLoading && <p className="status-message">Carregando processos...</p>}
      {processes.error && <p className="error">{processes.error.message}</p>}

      {processes.data && processes.data.length === 0 && (
        <div className="empty-state-card">
          <h3>Nenhum processo importado ainda</h3>
          <p>
            {isAdmin
              ? "Utilize o botão '+ Importar BPMN' acima para carregar seu primeiro diagrama BPMN 2.0."
              : "Aguarde até que o usuário Mestre importe diagramas BPMN para visualização."}
          </p>
        </div>
      )}

      <section className="process-grid">
        {processes.data?.map(process => (
          <button
            type="button"
            className="process-card"
            key={process.id}
            onClick={() => onOpen(process.id)}
          >
            <div className="process-card-top">
              <span className="process-key-pill">{process.externalKey}</span>
              <span className="process-task-counter">{process.taskCount} task(s)</span>
            </div>
            <strong className="process-card-title">{process.name}</strong>
            <span className="process-card-file">{process.organizationName} → {process.suborganizationName}</span>
            <span className="process-card-file">{process.fileName ?? "Sem arquivo"}</span>
            <small className="process-card-date">
              Atualizado em {new Date(process.updatedAt).toLocaleString()}
            </small>
          </button>
        ))}
      </section>
    </div>
  );
}
