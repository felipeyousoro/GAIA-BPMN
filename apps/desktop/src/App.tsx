import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { useState } from "react";
import { Home } from "./pages/Home";
import { ProcessView } from "./pages/ProcessView";

const queryClient = new QueryClient();

function Shell() {
  const [processId, setProcessId] = useState<string>();
  return processId
    ? <ProcessView processId={processId} onHome={() => setProcessId(undefined)} />
    : <Home onOpen={setProcessId} />;
}

export function App() {
  return <QueryClientProvider client={queryClient}><Shell /></QueryClientProvider>;
}
