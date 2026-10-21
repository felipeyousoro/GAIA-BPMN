import { z } from "zod";

const defaultApiUrl = "http://localhost:8080";
const processSchema = z.object({
  id: z.string().uuid(),
  externalKey: z.string(),
  name: z.string(),
  fileName: z.string().nullable(),
  updatedAt: z.string(),
  taskCount: z.number()
});
const attachmentSchema = z.object({ id: z.string().uuid(), fileName: z.string(), contentType: z.string(), size: z.number(), createdAt: z.string() });
const taskSchema = z.object({
  elementId: z.string().uuid(),
  externalElementId: z.string(),
  name: z.string().nullable(),
  input: z.string().nullable(),
  output: z.string().nullable(),
  attachments: z.array(attachmentSchema)
});

export type Process = z.infer<typeof processSchema>;
export type Attachment = z.infer<typeof attachmentSchema>;
export type Task = z.infer<typeof taskSchema>;

export function getApiUrl(): string {
  return localStorage.getItem("gaia.apiUrl") ?? defaultApiUrl;
}

export function setApiUrl(value: string): void {
  localStorage.setItem("gaia.apiUrl", value.replace(/\/+$/, ""));
}

async function request(path: string, init?: RequestInit): Promise<Response> {
  const response = await fetch(`${getApiUrl()}${path}`, init);
  if (!response.ok) throw new Error(await response.text() || `API request failed (${response.status}).`);
  return response;
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
  await request(`/api/v1/processes/${id}/tasks`, { method: "PUT", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ tasks }) });
}

export async function uploadAttachments(elementId: string, files: FileList | File[]): Promise<Attachment[]> {
  const form = new FormData();
  Array.from(files).forEach(file => form.append("files", file));
  return attachmentSchema.array().parse(await (await request(`/api/v1/elements/${elementId}/attachments`, { method: "POST", body: form })).json());
}

export async function deleteAttachment(id: string): Promise<void> {
  await request(`/api/v1/attachments/${id}`, { method: "DELETE" });
}

export async function downloadDiagram(id: string): Promise<{ blob: Blob; fileName?: string }> {
  const response = await request(`/api/v1/processes/${id}/diagram?download=true`);
  return { blob: await response.blob(), fileName: response.headers.get("Content-Disposition")?.match(/filename="?([^"]+)"?/)?.[1] };
}

export async function downloadAttachment(id: string): Promise<{ blob: Blob; fileName?: string }> {
  const response = await request(`/api/v1/attachments/${id}/content`);
  return { blob: await response.blob(), fileName: response.headers.get("Content-Disposition")?.match(/filename="?([^"]+)"?/)?.[1] };
}
