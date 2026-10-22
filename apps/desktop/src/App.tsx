import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { useEffect, useState } from "react";
import { clearSession, getAuthToken, getMe, onUnauthorized, User } from "./api";
import { Home } from "./pages/Home";
import { Login } from "./pages/Login";
import { ProcessView } from "./pages/ProcessView";

const queryClient = new QueryClient();

function Shell() {
  const [user, setUser] = useState<User | null>(null);
  const [checkingAuth, setCheckingAuth] = useState<boolean>(() => Boolean(getAuthToken()));
  const [processId, setProcessId] = useState<string>();

  useEffect(() => {
    const unsubscribe = onUnauthorized(() => {
      setUser(null);
      setProcessId(undefined);
      queryClient.clear();
    });
    return unsubscribe;
  }, []);

  useEffect(() => {
    const token = getAuthToken();
    if (!token) {
      setCheckingAuth(false);
      return;
    }
    let active = true;
    getMe()
      .then(profile => {
        if (active) setUser(profile);
      })
      .catch(() => {
        clearSession();
        if (active) setUser(null);
      })
      .finally(() => {
        if (active) setCheckingAuth(false);
      });
    return () => {
      active = false;
    };
  }, []);

  if (checkingAuth) {
    return (
      <div className="login-page">
        <div className="login-card">
          <p className="login-subtitle">Carregando sessão...</p>
        </div>
      </div>
    );
  }

  if (!user) {
    return <Login onSuccess={loggedUser => setUser(loggedUser)} />;
  }

  return processId
    ? <ProcessView processId={processId} onHome={() => setProcessId(undefined)} />
    : <Home onOpen={setProcessId} />;
}

export function App() {
  return <QueryClientProvider client={queryClient}><Shell /></QueryClientProvider>;
}
