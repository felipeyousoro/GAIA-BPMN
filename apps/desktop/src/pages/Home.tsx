import { useRef } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { importProcess, listProcesses, User } from "../api";

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

  const processes = useQuery({ queryKey: ["processes"], queryFn: listProcesses });
  const importer = useMutation({
    mutationFn: importProcess,
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
            <button
              type="button"
              className="btn-primary"
              disabled={importer.isPending}
              onClick={() => input.current?.click()}
            >
              {importer.isPending ? "Importando..." : "+ Importar BPMN"}
            </button>
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
