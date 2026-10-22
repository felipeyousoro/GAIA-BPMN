import { useCallback, useEffect, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  deleteAttachment,
  downloadAttachment,
  downloadDiagram,
  getDiagram,
  getTasks,
  saveTasks,
  uploadAttachments,
  type Attachment,
  type Task,
  type User
} from "../api";
import { BpmnViewer } from "../components/BpmnViewer";
import { TaskPanel } from "../components/TaskPanel";

export function ProcessView({
  processId,
  user,
  onHome
}: {
  processId: string;
  user: User;
  onHome: () => void;
}) {
  const isReadOnly = user.role !== "Admin";
  const diagram = useQuery({ queryKey: ["diagram", processId], queryFn: () => getDiagram(processId) });
  const tasksQuery = useQuery({ queryKey: ["tasks", processId], queryFn: () => getTasks(processId) });
  const queryClient = useQueryClient();
  const [selectedId, setSelectedId] = useState<string>();
  const [tasks, setTasks] = useState<Task[]>();
  const currentTasks = tasks ?? tasksQuery.data ?? [];
  const selected = currentTasks.find(task => task.externalElementId === selectedId);
  const select = useCallback((id: string) => setSelectedId(id), []);
  const [dirty, setDirty] = useState(false);

  const save = useMutation({
    mutationFn: () => saveTasks(processId, currentTasks),
    onSuccess: () => {
      setTasks(undefined);
      setDirty(false);
      void queryClient.invalidateQueries({ queryKey: ["tasks", processId] });
      void queryClient.invalidateQueries({ queryKey: ["diagram", processId] });
    }
  });

  const upload = useMutation({
    mutationFn: (files: File[]) =>
      selected ? uploadAttachments(selected.elementId, files) : Promise.resolve([] as Attachment[]),
    onSuccess: () => void queryClient.invalidateQueries({ queryKey: ["tasks", processId] })
  });

  const remove = useMutation({
    mutationFn: deleteAttachment,
    onSuccess: () => void queryClient.invalidateQueries({ queryKey: ["tasks", processId] })
  });

  useEffect(() => {
    const handler = (event: BeforeUnloadEvent) => {
      if (dirty && !isReadOnly) {
        event.preventDefault();
        event.returnValue = "";
      }
    };
    window.addEventListener("beforeunload", handler);
    return () => window.removeEventListener("beforeunload", handler);
  }, [dirty, isReadOnly]);

  const leave = () => {
    if (isReadOnly || !dirty || window.confirm("Há alterações não salvas. Sair mesmo assim?")) {
      onHome();
    }
  };

  const download = async (result: Promise<{ blob: Blob; fileName?: string }>, fallback: string) => {
    const file = await result;
    const link = document.createElement("a");
    link.href = URL.createObjectURL(file.blob);
    link.download = file.fileName ?? fallback;
    link.click();
    URL.revokeObjectURL(link.href);
  };

  if (diagram.isLoading || tasksQuery.isLoading) {
    return <p className="status-message">Carregando processo...</p>;
  }

  if (diagram.error || tasksQuery.error || !diagram.data) {
    return (
      <div className="process-view">
        <p className="error">{(diagram.error ?? tasksQuery.error)?.message ?? "Falha ao carregar processo."}</p>
        <button type="button" className="btn-secondary" onClick={onHome}>
          Voltar para Processos
        </button>
      </div>
    );
  }

  return (
    <div className="process-view">
      <div className="process-toolbar">
        <button type="button" className="btn-secondary" onClick={leave}>
          ← Voltar para Processos
        </button>

        <div className="process-toolbar-right">
          {isReadOnly && (
            <span className="readonly-banner-tag">
              Modo Convidado: Visualização e download liberados
            </span>
          )}
          {!isReadOnly && dirty && <span className="unsaved-badge">Alterações não salvas</span>}
          {!isReadOnly && (
            <button
              type="button"
              className="btn-primary"
              disabled={!dirty || save.isPending}
              onClick={() => save.mutate()}
            >
              {save.isPending ? "Salvando..." : "Salvar"}
            </button>
          )}
          <button
            type="button"
            className="btn-secondary"
            onClick={() => void download(downloadDiagram(processId), "diagram.bpmn")}
          >
            Baixar BPMN
          </button>
        </div>
      </div>

      {(save.error || upload.error || remove.error) && (
        <p className="error">{(save.error ?? upload.error ?? remove.error)?.message}</p>
      )}

      <section className="viewer-layout">
        <BpmnViewer xml={diagram.data} tasks={currentTasks} selectedId={selectedId} onSelect={select} />
        <TaskPanel
          task={selected}
          readOnly={isReadOnly}
          onChange={(field, value) => {
            if (isReadOnly) return;
            setTasks(
              currentTasks.map(item =>
                item.externalElementId === selectedId ? { ...item, [field]: value } : item
              )
            );
            setDirty(true);
          }}
          onUpload={files => {
            if (!isReadOnly) upload.mutate(files);
          }}
          onDownload={attachment => void download(downloadAttachment(attachment.id), attachment.fileName)}
          onDelete={attachment => {
            if (!isReadOnly) remove.mutate(attachment.id);
          }}
        />
      </section>
    </div>
  );
}
