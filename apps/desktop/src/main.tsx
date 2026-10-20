import { StrictMode, useState } from "react";
import { createRoot } from "react-dom/client";
import "./styles.css";

type BpmnNode = { id: string; name: string; type: string };

function parseBpmn(xml: string): BpmnNode[] {
  const document = new DOMParser().parseFromString(xml, "application/xml");
  if (document.querySelector("parsererror")) throw new Error("O arquivo BPMN não possui XML válido.");
  return Array.from(document.querySelectorAll("*[id]")).map((node) => ({
    id: node.getAttribute("id") ?? "",
    name: node.getAttribute("name") ?? "(sem nome)",
    type: node.localName
  }));
}

function App() {
  const [apiUrl, setApiUrl] = useState("http://localhost:8080");
  const [nodes, setNodes] = useState<BpmnNode[]>([]);
  const [selected, setSelected] = useState<BpmnNode>();
  const [message, setMessage] = useState("Importe um arquivo .bpmn para começar.");

  const importFile = async (file?: File) => {
    if (!file) return;
    try {
      setNodes(parseBpmn(await file.text()));
      setMessage(`${file.name} importado para visualização. O XML original não é alterado.`);
    } catch (error) {
      setMessage(error instanceof Error ? error.message : "Falha ao importar o arquivo.");
    }
  };

  return <main>
    <header><h1>GAIA BPMN</h1><label>URL da API <input value={apiUrl} onChange={(event) => setApiUrl(event.target.value)} /></label></header>
    <section className="toolbar"><label className="button">Importar BPMN<input type="file" accept=".bpmn,.xml" onChange={(event) => void importFile(event.target.files?.[0])} /></label><span>{message}</span></section>
    <div className="workspace">
      <aside><h2>Elementos ({nodes.length})</h2>{nodes.map((node) => <button className={selected?.id === node.id ? "selected" : ""} key={node.id} onClick={() => setSelected(node)}><strong>{node.name}</strong><small>{node.type} · {node.id}</small></button>)}</aside>
      <article><h2>{selected?.name ?? "Nenhum elemento selecionado"}</h2>{selected ? <form onSubmit={(event) => { event.preventDefault(); setMessage(`Metadados de ${selected.id} prontos para salvar em ${apiUrl}.`); }}><label>Descrição<textarea placeholder="Descrição da atividade" /></label><label>Metodologia<input placeholder="Metodologia" /></label><button type="submit">Salvar metadados</button></form> : <p>Selecione um elemento BPMN na lista.</p>}</article>
    </div>
  </main>;
}

createRoot(document.getElementById("root")!).render(<StrictMode><App /></StrictMode>);
