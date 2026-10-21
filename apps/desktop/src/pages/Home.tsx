import { useRef, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { getApiUrl, importProcess, listProcesses, setApiUrl } from "../api";

export function Home({ onOpen }: { onOpen: (id: string) => void }) {
  const input = useRef<HTMLInputElement>(null);
  const queryClient = useQueryClient();
  const processes = useQuery({ queryKey: ["processes"], queryFn: listProcesses });
  const importer = useMutation({
    mutationFn: importProcess,
    onSuccess: process => { void queryClient.invalidateQueries({ queryKey: ["processes"] }); onOpen(process.id); }
  });
  const [apiUrl, setUrl] = useState(getApiUrl());
  return <main className="app-shell">
    <header className="topbar"><h1>GAIA BPMN</h1><label>API <input value={apiUrl} onChange={event => { setUrl(event.target.value); setApiUrl(event.target.value); }} /></label></header>
    <nav className="toolbar"><button onClick={() => input.current?.click()}>Importar BPMN</button><input ref={input} hidden type="file" accept=".bpmn" onChange={event => { const file = event.target.files?.[0]; if (file) importer.mutate(file); event.target.value = ""; }} />{importer.error && <span className="error">{importer.error.message}</span>}</nav>
    {processes.isLoading && <p>Carregando processos...</p>}
    {processes.error && <p className="error">{processes.error.message}</p>}
    <section className="process-grid">{processes.data?.map(process => <button className="process-card" key={process.id} onClick={() => onOpen(process.id)}>
      <strong>{process.name}</strong><span>{process.fileName ?? "Sem arquivo"}</span><span>{process.taskCount} task(s)</span><small>{new Date(process.updatedAt).toLocaleString()}</small>
    </button>)}</section>
  </main>;
}
