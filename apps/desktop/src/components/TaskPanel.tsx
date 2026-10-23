import { useRef } from "react";
import type { Attachment, Task } from "../api";

export function TaskPanel({
  task,
  readOnly = false,
  onChange,
  onUpload,
  onDownload,
  onPrint,
  onDelete
}: {
  task?: Task;
  readOnly?: boolean;
  onChange: (field: "input" | "output" | "description" | "resources" | "instructions", value: string) => void;
  onUpload: (files: File[]) => void;
  onDownload: (attachment: Attachment) => void;
  onPrint: (attachment: Attachment) => void;
  onDelete: (attachment: Attachment) => void;
}) {
  const input = useRef<HTMLInputElement>(null);

  if (!task) {
    return (
      <aside className="task-panel empty-task-panel">
        <div className="task-panel-header">
          <h2>Detalhes do elemento</h2>
        </div>
        <p className="muted-text">Selecione um elemento no diagrama para visualizar suas propriedades e anexos.</p>
      </aside>
    );
  }

  const isTask = task.elementType?.endsWith("Task")
    || ["task", "subProcess", "callActivity"].includes(task.elementType ?? "");

  return (
    <aside className="task-panel">
      <div className="task-panel-header">
        <div>
          <h2>{task.name || task.externalElementId}</h2>
          <small className="task-id-tag">{task.externalElementId}</small>
        </div>
        {readOnly && <span className="readonly-pill">Somente Leitura</span>}
      </div>

      <label className="form-label">
        Descrição
        <textarea
          readOnly={readOnly}
          className={readOnly ? "textarea-readonly" : ""}
          placeholder={readOnly ? "Sem descrição." : "Descreva este elemento BPMN..."}
          value={task.description ?? ""}
          onChange={event => {
            if (!readOnly) onChange("description", event.target.value);
          }}
        />
      </label>

      {isTask && <label className="form-label">
        Input
        <textarea
          readOnly={readOnly}
          className={readOnly ? "textarea-readonly" : ""}
          placeholder={readOnly ? "Sem conteúdo de entrada." : "Descreva os insumos/entradas desta task..."}
          value={task.input ?? ""}
          onChange={event => {
            if (!readOnly) onChange("input", event.target.value);
          }}
        />
      </label>}

      {isTask && <label className="form-label">
        Recursos necessários
        <textarea
          readOnly={readOnly}
          className={readOnly ? "textarea-readonly" : ""}
          placeholder={readOnly ? "Sem recursos informados." : "Liste os recursos necessários..."}
          value={task.resources ?? ""}
          onChange={event => {
            if (!readOnly) onChange("resources", event.target.value);
          }}
        />
      </label>}

      {isTask && <label className="form-label">
        Instruções
        <textarea
          readOnly={readOnly}
          className={readOnly ? "textarea-readonly" : ""}
          placeholder={readOnly ? "Sem instruções." : "Descreva como executar esta task..."}
          value={task.instructions ?? ""}
          onChange={event => {
            if (!readOnly) onChange("instructions", event.target.value);
          }}
        />
      </label>}

      {isTask && <label className="form-label">
        Output
        <textarea
          readOnly={readOnly}
          className={readOnly ? "textarea-readonly" : ""}
          placeholder={readOnly ? "Sem conteúdo de saída." : "Descreva os resultados/saídas desta task..."}
          value={task.output ?? ""}
          onChange={event => {
            if (!readOnly) onChange("output", event.target.value);
          }}
        />
      </label>}

      <div className="attachments-section">
        <div className="attachments-header">
          <h3>Anexos ({task.attachments.length})</h3>
          {!readOnly && (
            <>
              <button type="button" className="btn-secondary btn-sm" onClick={() => input.current?.click()}>
                + Adicionar anexos
              </button>
              <input
                ref={input}
                hidden
                type="file"
                multiple
                onChange={event => {
                  if (event.target.files) onUpload(Array.from(event.target.files));
                  event.target.value = "";
                }}
              />
            </>
          )}
        </div>

        {task.attachments.length === 0 ? (
          <p className="muted-text">Nenhum anexo vinculado a esta task.</p>
        ) : (
          <ul className="attachment-list">
            {task.attachments.map(attachment => (
              <li key={attachment.id} className="attachment-item">
                <span title={attachment.fileName}>{attachment.fileName}</span>
                <div className="attachment-actions">
                  {attachment.contentType.toLowerCase() === "application/pdf" && (
                    <button type="button" className="btn-secondary btn-xs" onClick={() => onPrint(attachment)}>
                      Imprimir
                    </button>
                  )}
                  <button type="button" className="btn-secondary btn-xs" onClick={() => onDownload(attachment)}>
                    Baixar
                  </button>
                  {!readOnly && (
                    <button type="button" className="btn-danger btn-xs" onClick={() => onDelete(attachment)}>
                      Remover
                    </button>
                  )}
                </div>
              </li>
            ))}
          </ul>
        )}
      </div>
    </aside>
  );
}
