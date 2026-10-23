import { z } from "zod";

const configuredApiUrl = (import.meta as ImportMeta & { env?: { VITE_API_URL?: string } }).env?.VITE_API_URL;
const defaultApiUrl = configuredApiUrl === undefined ? "http://localhost:8080" : configuredApiUrl.replace(/\/+$/, "");
const authTokenStorageKey = "gaia.authToken";

const processSchema = z.object({
  id: z.string().uuid(),
  externalKey: z.string(),
  name: z.string(),
  fileName: z.string().nullable(),
  updatedAt: z.string(),
  taskCount: z.number(),
  suborganizationId: z.string().uuid(),
  organizationId: z.string().uuid(),
  organizationName: z.string(),
  suborganizationName: z.string()
});

const attachmentSchema = z.object({
  id: z.string().uuid(),
  fileName: z.string(),
  contentType: z.string(),
  size: z.number(),
  createdAt: z.string()
});

const taskSchema = z.object({
  elementId: z.string().uuid(),
  externalElementId: z.string(),
  name: z.string().nullable(),
  input: z.string().nullable(),
  output: z.string().nullable(),
  attachments: z.array(attachmentSchema)
});

const userSchema = z.object({
  id: z.string().uuid(),
  username: z.string(),
  displayName: z.string(),
  role: z.enum(["Admin", "Guest"]),
  hasAvatar: z.boolean(),
  createdAt: z.string()
});

const loginResponseSchema = z.object({
  token: z.string(),
  user: userSchema
});

export type Process = z.infer<typeof processSchema>;
export type Attachment = z.infer<typeof attachmentSchema>;
export type Task = z.infer<typeof taskSchema>;
export type User = z.infer<typeof userSchema>;
export type UserRole = User["role"];
export type LoginResult = z.infer<typeof loginResponseSchema>;

type UnauthorizedListener = () => void;
const unauthorizedListeners = new Set<UnauthorizedListener>();

export function onUnauthorized(listener: UnauthorizedListener): () => void {
  unauthorizedListeners.add(listener);
  return () => unauthorizedListeners.delete(listener);
}

export function getApiUrl(): string {
  return localStorage.getItem("gaia.apiUrl") ?? defaultApiUrl;
}

export function setApiUrl(value: string): void {
  localStorage.setItem("gaia.apiUrl", value.trim().replace(/\/+$/, ""));
}

export function getAuthToken(): string | null {
  return localStorage.getItem(authTokenStorageKey);
}

export function setAuthToken(token: string | null): void {
  if (token) {
    localStorage.setItem(authTokenStorageKey, token);
  } else {
    localStorage.removeItem(authTokenStorageKey);
  }
}

export function clearSession(): void {
  setAuthToken(null);
}

async function parseErrorMessage(response: Response): Promise<string> {
  const text = await response.text();
  if (!text) return `Erro na requisição (${response.status}).`;
  try {
    const parsed = JSON.parse(text) as { detail?: string; title?: string; message?: string };
    return parsed.detail || parsed.title || parsed.message || text;
  } catch {
    return text;
  }
}

async function request(path: string, init?: RequestInit, skipAuthRedirect = false): Promise<Response> {
  const headers = new Headers(init?.headers);
  const token = getAuthToken();
  if (token && !headers.has("Authorization")) {
    headers.set("Authorization", `Bearer ${token}`);
  }
  const response = await fetch(`${getApiUrl()}${path}`, {
    ...init,
    headers
  });

  if (response.status === 401 && !skipAuthRedirect) {
    clearSession();
    unauthorizedListeners.forEach(listener => listener());
    throw new Error("Sessão expirada. Faça login novamente.");
  }

  if (!response.ok) {
    throw new Error(await parseErrorMessage(response));
  }
  return response;
}

export async function checkApiHealth(customUrl?: string): Promise<boolean> {
  const baseUrl = (customUrl ?? getApiUrl()).trim().replace(/\/+$/, "");
  const response = await fetch(`${baseUrl}/health`);
  return response.ok;
}

export async function login(username: string, password: string): Promise<LoginResult> {
  const response = await request(
    "/api/v1/auth/login",
    {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ username, password })
    },
    true
  );
  const data = loginResponseSchema.parse(await response.json());
  setAuthToken(data.token);
  return data;
}

export async function getMe(): Promise<User> {
  return userSchema.parse(await (await request("/api/v1/me")).json());
}

export async function updateProfile(displayName: string): Promise<User> {
  return userSchema.parse(
    await (
      await request("/api/v1/me", {
        method: "PUT",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ displayName })
      })
    ).json()
  );
}

export async function changePassword(currentPassword: string, newPassword: string): Promise<void> {
  await request("/api/v1/me/password", {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ currentPassword, newPassword })
  });
}

export async function uploadAvatar(file: File): Promise<User> {
  const form = new FormData();
  form.append("file", file);
  return userSchema.parse(await (await request("/api/v1/me/avatar", { method: "POST", body: form })).json());
}

export async function fetchAvatarBlobUrl(userId: string): Promise<string | null> {
  try {
    const response = await request(`/api/v1/users/${userId}/avatar`);
    const blob = await response.blob();
    return URL.createObjectURL(blob);
  } catch {
    return null;
  }
}

export async function listUsers(): Promise<User[]> {
  return userSchema.array().parse(await (await request("/api/v1/users")).json());
}

export async function createUser(payload: {
  username: string;
  displayName: string;
  password: string;
  role: UserRole;
}): Promise<User> {
  return userSchema.parse(
    await (
      await request("/api/v1/users", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(payload)
      })
    ).json()
  );
}

export async function resetUserPassword(userId: string, newPassword: string): Promise<void> {
  await request(`/api/v1/users/${userId}/reset-password`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ newPassword })
  });
}

export async function deleteUser(userId: string): Promise<void> {
  await request(`/api/v1/users/${userId}`, { method: "DELETE" });
}

export async function listProcesses(): Promise<Process[]> {
  return processSchema.array().parse(await (await request("/api/v1/processes")).json());
}

export async function importProcess(file: File): Promise<Process> {
  const form = new FormData();
  form.append("file", file);
  return processSchema.parse(await (await request("/api/v1/processes/import", { method: "POST", body: form })).json());
}

export async function getDiagram(id: string): Promise<string> {
  return await (await request(`/api/v1/processes/${id}/diagram`)).text();
}

export async function getTasks(id: string): Promise<Task[]> {
  return taskSchema.array().parse(await (await request(`/api/v1/processes/${id}/tasks`)).json());
}

export async function saveTasks(id: string, tasks: Array<Pick<Task, "externalElementId" | "input" | "output">>): Promise<void> {
  await request(`/api/v1/processes/${id}/tasks`, {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ tasks })
  });
}

export async function uploadAttachments(elementId: string, files: File[]): Promise<Attachment[]> {
  const form = new FormData();
  Array.from(files).forEach(file => form.append("files", file));
  return attachmentSchema.array().parse(
    await (await request(`/api/v1/elements/${elementId}/attachments`, { method: "POST", body: form })).json()
  );
}

export async function deleteAttachment(id: string): Promise<void> {
  await request(`/api/v1/attachments/${id}`, { method: "DELETE" });
}

export async function downloadDiagram(id: string): Promise<{ blob: Blob; fileName?: string }> {
  const response = await request(`/api/v1/processes/${id}/diagram?download=true`);
  return {
    blob: await response.blob(),
    fileName: response.headers.get("Content-Disposition")?.match(/filename="?([^"]+)"?/)?.[1]
  };
}

export async function downloadAttachment(id: string): Promise<{ blob: Blob; fileName?: string }> {
  const response = await request(`/api/v1/attachments/${id}/content`);
  return {
    blob: await response.blob(),
    fileName: response.headers.get("Content-Disposition")?.match(/filename="?([^"]+)"?/)?.[1]
  };
}
