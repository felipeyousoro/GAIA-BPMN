export function ProcessView({ processId, onHome }: { processId: string; onHome: () => void }) {
  return <main className="app-shell"><header className="topbar"><button onClick={onHome}>Voltar para Home</button><h1>Processo</h1></header><p>Processo selecionado: {processId}</p></main>;
}
