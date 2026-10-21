import { useRef } from "react";
import type { Attachment, Task } from "../api";

export function TaskPanel({ task, onChange, onUpload, onDownload, onDelete }: {
  task?: Task;
  onChange: (field: "input" | "output", value: string) => void;
  onUpload: (files: File[]) => void;
  onDownload: (attachment: Attachment) => void;
  onDelete: (attachment: Attachment) => void;
}) {
  const input = useRef<HTMLInputElement>(null);
  if (!task) return <aside className="task-panel"><p>Selecione uma task no diagrama.</p></aside>;
  return <aside className="task-panel"><h2>{task.name || task.externalElementId}</h2><small>{task.externalElementId}</small>
    <label>Input<textarea value={task.input ?? ""} onChange={event => onChange("input", event.target.value)} /></label>
    <label>Output<textarea value={task.output ?? ""} onChange={event => onChange("output", event.target.value)} /></label>
    <h3>Anexos ({task.attachments.length})</h3>
    <button onClick={() => input.current?.click()}>Adicionar anexos</button><input ref={input} hidden type="file" multiple onChange={event => { if (event.target.files) onUpload(Array.from(event.target.files)); event.target.value = ""; }} />
    <ul>{task.attachments.map(attachment => <li key={attachment.id}><span>{attachment.fileName}</span><button onClick={() => onDownload(attachment)}>Baixar</button><button onClick={() => onDelete(attachment)}>Remover</button></li>)}</ul>
  </aside>;
}
