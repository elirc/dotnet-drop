export type FileNodeType = 'Folder' | 'File' | 0 | 1;
export type ScanStatus = 'PendingScan' | 'Clean' | 'Infected' | 'Failed' | 0 | 1 | 2 | 3;

export interface FileNode {
  id: string;
  parentId: string | null;
  name: string;
  type: FileNodeType;
  scanStatus: ScanStatus;
  size: number;
  mimeType: string | null;
  updatedAt: string;
  isDeleted: boolean;
  thumbnailPath: string | null;
}

export interface FileVersion {
  id: string;
  versionNumber: number;
  size: number;
  mimeType: string;
  contentHash: string;
  createdAt: string;
}

export interface UploadSession {
  id: string;
  fileName: string;
  chunkSize: number;
  totalChunks: number;
  expiresAt: string;
  uploadedChunks: number[];
}

export interface Usage {
  usedBytes: number;
  quotaBytes: number;
}

export interface ShareLink {
  id: string;
  token: string;
  permission: string;
  expiresAt: string | null;
  accessCount: number;
  lastAccessedAt: string | null;
  revoked: boolean;
}

const API_BASE = import.meta.env.VITE_API_BASE ?? '';

export class ApiError extends Error {
  constructor(message: string, public status: number) {
    super(message);
  }
}

export function getToken() {
  return localStorage.getItem('netdropbox.token');
}

export function setToken(token: string | null) {
  if (token) localStorage.setItem('netdropbox.token', token);
  else localStorage.removeItem('netdropbox.token');
}

async function request<T>(path: string, init: RequestInit = {}): Promise<T> {
  const token = getToken();
  const headers = new Headers(init.headers);
  if (!headers.has('Content-Type') && init.body && !(init.body instanceof Blob)) headers.set('Content-Type', 'application/json');
  if (token) headers.set('Authorization', `Bearer ${token}`);

  const response = await fetch(`${API_BASE}${path}`, { ...init, headers });
  if (!response.ok) {
    const problem = await response.json().catch(() => ({ title: response.statusText }));
    throw new ApiError(problem.detail ?? problem.title ?? 'Request failed', response.status);
  }
  if (response.status === 204) return undefined as T;
  const contentType = response.headers.get('content-type') ?? '';
  if (!contentType.includes('application/json')) return undefined as T;
  return response.json() as Promise<T>;
}

export const api = {
  async login(email: string, password: string) {
    return request<{ accessToken: string; expiresAt: string }>('/api/v1/auth/login', {
      method: 'POST',
      body: JSON.stringify({ email, password })
    });
  },
  files(parentId: string | null, trash = false) {
    const params = new URLSearchParams();
    if (parentId) params.set('parentId', parentId);
    if (trash) params.set('trash', 'true');
    return request<FileNode[]>(`/api/v1/files?${params}`);
  },
  search(q: string) {
    return request<FileNode[]>(`/api/v1/files/search?q=${encodeURIComponent(q)}`);
  },
  sharedWithMe() {
    return request<FileNode[]>('/api/v1/shares/direct/shared-with-me');
  },
  usage() {
    return request<Usage>('/api/v1/files/usage');
  },
  createFolder(parentId: string | null, name: string) {
    return request<FileNode>('/api/v1/files/folders', { method: 'POST', body: JSON.stringify({ parentId, name }) });
  },
  rename(id: string, name: string) {
    return request<void>(`/api/v1/files/${id}/name`, { method: 'PUT', body: JSON.stringify({ name }) });
  },
  move(id: string, parentId: string | null) {
    return request<void>(`/api/v1/files/${id}/move`, { method: 'PUT', body: JSON.stringify({ parentId }) });
  },
  copy(id: string, parentId: string | null, name?: string) {
    return request<FileNode>(`/api/v1/files/${id}/copy`, { method: 'POST', body: JSON.stringify({ parentId, name }) });
  },
  delete(id: string) {
    return request<void>(`/api/v1/files/${id}`, { method: 'DELETE' });
  },
  restore(id: string) {
    return request<void>(`/api/v1/files/${id}/restore`, { method: 'POST' });
  },
  versions(id: string) {
    return request<FileVersion[]>(`/api/v1/files/${id}/versions`);
  },
  restoreVersion(id: string, versionId: string) {
    return request<void>(`/api/v1/files/${id}/versions/${versionId}/restore`, { method: 'POST' });
  },
  startUpload(payload: { parentId: string | null; fileName: string; totalSize: number; mimeType: string; finalContentHash: string; chunkSize: number }) {
    return request<UploadSession>('/api/v1/uploads', { method: 'POST', body: JSON.stringify(payload) });
  },
  uploadChunk(sessionId: string, index: number, checksum: string, chunk: Blob) {
    return request<number[]>(`/api/v1/uploads/${sessionId}/chunks/${index}`, {
      method: 'PUT',
      headers: { 'X-Chunk-SHA256': checksum },
      body: chunk
    });
  },
  completeUpload(sessionId: string) {
    return request<FileNode>(`/api/v1/uploads/${sessionId}/complete`, { method: 'POST' });
  },
  createLink(nodeId: string, permission: 'View' | 'Download' | 'EditUpload', expiresAt: string | null, password?: string) {
    return request<ShareLink>('/api/v1/shares/links', {
      method: 'POST',
      body: JSON.stringify({ nodeId, permission, expiresAt, password: password || null })
    });
  },
  async downloadBlob(id: string, versionId?: string) {
    const token = getToken();
    const params = versionId ? `?versionId=${versionId}` : '';
    const response = await fetch(`${API_BASE}/api/v1/files/${id}/download${params}`, {
      headers: token ? { Authorization: `Bearer ${token}` } : undefined
    });
    if (!response.ok) throw new ApiError(response.statusText, response.status);
    return response.blob();
  }
};

export async function sha256(blob: Blob) {
  const data = await blob.arrayBuffer();
  const hash = await crypto.subtle.digest('SHA-256', data);
  return [...new Uint8Array(hash)].map((b) => b.toString(16).padStart(2, '0')).join('');
}

export function formatBytes(bytes: number) {
  if (!bytes) return '0 B';
  const units = ['B', 'KB', 'MB', 'GB', 'TB'];
  const index = Math.min(Math.floor(Math.log(bytes) / Math.log(1024)), units.length - 1);
  return `${(bytes / 1024 ** index).toFixed(index === 0 ? 0 : 1)} ${units[index]}`;
}
