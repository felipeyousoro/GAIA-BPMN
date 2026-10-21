import { useCallback, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { getDiagram, getTasks, type Task } from "../api";
import { BpmnViewer } from "../components/BpmnViewer";
import { TaskPanel } from "../components/TaskPanel";

export function ProcessView({ processId, onHome }: { processId: string; onHome: () => void }) {
  const diagram = useQuery({ queryKey: ["diagram", processId], queryFn: () => getDiagram(processId) });
  const tasksQuery = useQuery({ queryKey: ["tasks", processId], queryFn: () => getTasks(processId) });
  const [selectedId, setSelectedId] = useState<string>();
  const [tasks, setTasks] = useState<Task[]>();
  const currentTasks = tasks ?? tasksQuery.data ?? [];
  const selected = currentTasks.find(task => task.externalElementId === selectedId);
  const select = useCallback((id: string) => setSelectedId(id), []);
  if (diagram.isLoading || tasksQuery.isLoading) return <main className="app-shell"><p>Carregando processo...</p></main>;
  if (diagram.error || tasksQuery.error || !diagram.data) return <main className="app-shell"><p className="error">{(diagram.error ?? tasksQuery.error)?.message ?? "Falha ao carregar processo."}</p></main>;
  return <main className="app-shell process-view"><header className="topbar"><button onClick={onHome}>Voltar para Home</button><h1>Visualizador BPMN</h1></header>
    <section className="viewer-layout"><BpmnViewer xml={diagram.data} tasks={currentTasks} selectedId={selectedId} onSelect={select} /><TaskPanel task={selected} onChange={(field, value) => setTasks(currentTasks.map(item => item.externalElementId === selectedId ? { ...item, [field]: value } : item))} onUpload={() => undefined} onDownload={() => undefined} onDelete={() => undefined} /></section>
  </main>;
}
