import { ReactNode, useEffect, useState } from "react";
import { fetchAvatarBlobUrl, User } from "../api";

export type NavSection = "processes" | "settings";

export function AppLayout({
  title,
  activeSection,
  user,
  avatarVersion = 0,
  onNavigate,
  onLogout,
  headerActions,
  children
}: {
  title: string;
  activeSection: NavSection;
  user: User;
  avatarVersion?: number;
  onNavigate: (section: NavSection) => void;
  onLogout: () => void;
  headerActions?: ReactNode;
  children: ReactNode;
}) {
  const [avatarUrl, setAvatarUrl] = useState<string | null>(null);

  useEffect(() => {
    let currentUrl: string | null = null;
    let active = true;

    if (!user.hasAvatar) {
      setAvatarUrl(null);
      return;
    }

    fetchAvatarBlobUrl(user.id).then(url => {
      if (!active) {
        if (url) URL.revokeObjectURL(url);
        return;
      }
      currentUrl = url;
      setAvatarUrl(url);
    });

    return () => {
      active = false;
      if (currentUrl) URL.revokeObjectURL(currentUrl);
    };
  }, [user.id, user.hasAvatar, avatarVersion]);

  const initials = user.displayName
    .split(" ")
    .filter(Boolean)
    .slice(0, 2)
    .map(part => part[0]?.toUpperCase() ?? "")
    .join("") || "U";

  const navItems: Array<{ id: NavSection; label: string }> = [
    { id: "processes", label: "Processos (Home)" },
    { id: "settings", label: "Configurações (Settings)" }
  ];

  return (
    <div className="apucarana-shell">
      <aside className="apucarana-sidebar">
        <div className="apucarana-brand">
          <div className="apucarana-bars" aria-hidden="true">
            <div className="apucarana-bar-left" />
            <div className="apucarana-bar-right" />
          </div>
          <div className="apucarana-brand-title">GAIA BPMN</div>
        </div>

        <nav className="apucarana-nav">
          {navItems.map(item => {
            const active = activeSection === item.id;
            return (
              <button
                key={item.id}
                type="button"
                onClick={() => onNavigate(item.id)}
                className={`apucarana-nav-pill ${active ? "active" : ""}`}
              >
                <span className="apucarana-nav-label">{item.label}</span>
                <span className="apucarana-nav-indicator">{active ? "●" : "→"}</span>
              </button>
            );
          })}
        </nav>

        <div className="apucarana-sidebar-footer">
          <div className="apucarana-footer-badge">
            <span className="apucarana-footer-title">GAIA BPMN</span>
            <small>Identidade Apucarana TI</small>
          </div>
        </div>
      </aside>

      <div className="apucarana-main-area">
        <header className="apucarana-header">
          <div className="apucarana-header-left">
            <h1 className="apucarana-page-title">{title}</h1>
            {headerActions && <div className="apucarana-header-actions">{headerActions}</div>}
          </div>

          <div className="apucarana-user-box">
            <span className={`role-badge ${user.role === "Admin" ? "role-admin" : "role-guest"}`}>
              {user.role === "Admin" ? "Mestre" : "Convidado"}
            </span>

            <div className="user-avatar" title={user.displayName}>
              {avatarUrl ? <img src={avatarUrl} alt={user.displayName} /> : <span>{initials}</span>}
            </div>

            <div className="user-meta">
              <strong>{user.displayName}</strong>
              <small>@{user.username}</small>
            </div>

            <button type="button" className="btn-logout" onClick={onLogout}>
              Sair
            </button>
          </div>
        </header>

        <main className="apucarana-content">{children}</main>
      </div>
    </div>
  );
}
