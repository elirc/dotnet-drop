import { useEffect, useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Download, File, Folder, Grid2X2, Link, List, LogOut, RefreshCw, Search, Share2, ShieldAlert, Trash2, UploadCloud } from 'lucide-react';
import { useForm } from 'react-hook-form';
import { api, ApiError, FileNode, FileVersion, formatBytes, getToken, setToken, sha256 } from './api';

const CHUNK_SIZE = 4 * 1024 * 1024;
const MAX_PARALLEL_CHUNKS = 4;

type View = 'my-files' | 'shared' | 'trash';

export function App() {
  const [token, setTokenState] = useState(getToken());

  if (!token) {
    return <Login onLogin={(next) => { setToken(next); setTokenState(next); }} />;
  }

  return <Drive onLogout={() => { setToken(null); setTokenState(null); }} />;
}

function Login({ onLogin }: { onLogin: (token: string) => void }) {
  const { register, handleSubmit, formState } = useForm({ defaultValues: { email: 'owner@netdropbox.local', password: 'Passw0rd!' } });
  const [error, setError] = useState<string | null>(null);
  const mutation = useMutation({
    mutationFn: (values: { email: string; password: string }) => api.login(values.email, values.password),
    onSuccess: (result) => onLogin(result.accessToken),
    onError: (err) => setError(err instanceof Error ? err.message : 'Login failed')
  });

  return (
    <main className="login-shell">
      <form className="login-panel" onSubmit={handleSubmit((values) => mutation.mutate(values))}>
        <div>
          <p className="eyebrow">NetDropbox</p>
          <h1>File storage and sharing</h1>
        </div>
        <label>Email<input {...register('email')} autoComplete="email" /></label>
        <label>Password<input {...register('password')} type="password" autoComplete="current-password" /></label>
        {error && <p className="error">{error}</p>}
        <button disabled={mutation.isPending || formState.isSubmitting}>{mutation.isPending ? 'Signing in...' : 'Sign in'}</button>
      </form>
    </main>
  );
}

function Drive({ onLogout }: { onLogout: () => void }) {
  const queryClient = useQueryClient();
  const [view, setView] = useState<View>('my-files');
  const [parentId, setParentId] = useState<string | null>(null);
  const [layout, setLayout] = useState<'list' | 'grid'>('list');
  const [query, setQuery] = useState('');
  const [selected, setSelected] = useState<FileNode | null>(null);
  const [uploading, setUploading] = useState<Record<string, number>>({});

  const files = useQuery({
    queryKey: ['files', view, parentId, query],
    queryFn: () => query ? api.search(query) : view === 'shared' ? api.sharedWithMe() : api.files(parentId, view === 'trash')
  });
  const usage = useQuery({ queryKey: ['usage'], queryFn: api.usage });

  const refresh = () => {
    queryClient.invalidateQueries({ queryKey: ['files'] });
    queryClient.invalidateQueries({ queryKey: ['usage'] });
  };

  async function uploadFiles(fileList: FileList | File[]) {
    const items = Array.from(fileList);
    await Promise.all(items.map((file) => uploadOne(file, parentId, (pct) => setUploading((old) => ({ ...old, [file.name]: pct })))));
    setUploading({});
    refresh();
  }

  return (
    <main className="app-shell">
      <aside className="sidebar">
        <div>
          <p className="eyebrow">NetDropbox</p>
          <h1>Drive</h1>
        </div>
        <nav>
          <button className={view === 'my-files' ? 'active' : ''} onClick={() => { setView('my-files'); setParentId(null); }}>My files</button>
          <button className={view === 'shared' ? 'active' : ''} onClick={() => { setView('shared'); setParentId(null); }}>Shared with me</button>
          <button className={view === 'trash' ? 'active' : ''} onClick={() => { setView('trash'); setParentId(null); }}>Trash</button>
        </nav>
        <UsageMeter usage={usage.data} />
        <button className="ghost" onClick={onLogout}><LogOut size={16} /> Sign out</button>
      </aside>

      <section className="workspace">
        <header className="toolbar">
          <div className="search">
            <Search size={18} />
            <input placeholder="Search names and extracted text" value={query} onChange={(event) => setQuery(event.target.value)} />
          </div>
          <div className="toolbar-actions">
            <CreateFolder parentId={parentId} onDone={refresh} />
            <label className="upload-button">
              <UploadCloud size={16} /> Upload
              <input type="file" multiple onChange={(event) => event.target.files && uploadFiles(event.target.files)} />
            </label>
            <button title="List view" className={layout === 'list' ? 'icon active' : 'icon'} onClick={() => setLayout('list')}><List size={16} /></button>
            <button title="Grid view" className={layout === 'grid' ? 'icon active' : 'icon'} onClick={() => setLayout('grid')}><Grid2X2 size={16} /></button>
            <button title="Refresh" className="icon" onClick={refresh}><RefreshCw size={16} /></button>
          </div>
        </header>

        <div className="crumbs">
          <button onClick={() => setParentId(null)}>Root</button>
          {parentId && <span>{parentId.slice(0, 8)}</span>}
        </div>

        <DropZone onFiles={uploadFiles}>
          {Object.keys(uploading).length > 0 && <UploadQueue progress={uploading} />}
          {files.isLoading && <p className="state">Loading files...</p>}
          {files.error && <p className="error">{files.error instanceof Error ? files.error.message : 'Failed to load files'}</p>}
          {!files.isLoading && files.data?.length === 0 && <p className="state">No files here yet.</p>}
          <div className={layout === 'grid' ? 'grid-view' : 'list-view'}>
            {files.data?.map((node) => (
              <FileRow
                key={node.id}
                node={node}
                layout={layout}
                trash={view === 'trash'}
                onOpen={() => node.type === 'Folder' || node.type === 0 ? setParentId(node.id) : setSelected(node)}
                onChanged={refresh}
              />
            ))}
          </div>
        </DropZone>
      </section>

      <PreviewPanel node={selected} onClose={() => setSelected(null)} onChanged={refresh} />
    </main>
  );
}

function UsageMeter({ usage }: { usage?: { usedBytes: number; quotaBytes: number } }) {
  const percent = usage ? Math.min(100, (usage.usedBytes / usage.quotaBytes) * 100) : 0;
  return (
    <section className="usage">
      <div><span>Storage</span><strong>{usage ? formatBytes(usage.usedBytes) : '-'}</strong></div>
      <div className="meter"><span style={{ width: `${percent}%` }} /></div>
      <small>{usage ? `${formatBytes(usage.quotaBytes)} quota` : 'Loading quota'}</small>
    </section>
  );
}

function CreateFolder({ parentId, onDone }: { parentId: string | null; onDone: () => void }) {
  const [name, setName] = useState('');
  const mutation = useMutation({ mutationFn: () => api.createFolder(parentId, name), onSuccess: () => { setName(''); onDone(); } });
  return (
    <form className="new-folder" onSubmit={(event) => { event.preventDefault(); if (name.trim()) mutation.mutate(); }}>
      <input placeholder="New folder" value={name} onChange={(event) => setName(event.target.value)} />
      <button disabled={mutation.isPending}>Create</button>
    </form>
  );
}

function DropZone({ children, onFiles }: { children: React.ReactNode; onFiles: (files: FileList) => void }) {
  const [active, setActive] = useState(false);
  return (
    <div
      className={active ? 'dropzone active' : 'dropzone'}
      onDragOver={(event) => { event.preventDefault(); setActive(true); }}
      onDragLeave={() => setActive(false)}
      onDrop={(event) => { event.preventDefault(); setActive(false); onFiles(event.dataTransfer.files); }}
    >
      {children}
    </div>
  );
}

function UploadQueue({ progress }: { progress: Record<string, number> }) {
  return (
    <div className="upload-queue">
      {Object.entries(progress).map(([name, pct]) => (
        <div key={name}>
          <span>{name}</span>
          <progress value={pct} max={100} />
          <strong>{Math.round(pct)}%</strong>
        </div>
      ))}
    </div>
  );
}

function FileRow({ node, layout, trash, onOpen, onChanged }: { node: FileNode; layout: 'list' | 'grid'; trash: boolean; onOpen: () => void; onChanged: () => void }) {
  const [share, setShare] = useState<string | null>(null);
  const icon = node.type === 'Folder' || node.type === 0 ? <Folder size={20} /> : statusIcon(node);
  const deleteMutation = useMutation({ mutationFn: () => trash ? api.restore(node.id) : api.delete(node.id), onSuccess: onChanged });
  const shareMutation = useMutation({
    mutationFn: () => api.createLink(node.id, 'Download', null),
    onSuccess: (link) => setShare(`${location.origin}/api/v1/public/shares/${link.token}/download`)
  });

  return (
    <article className={layout === 'grid' ? 'file-card' : 'file-row'}>
      <button className="file-main" onClick={onOpen}>
        {icon}
        <span>{node.name}</span>
      </button>
      <span className={`pill ${String(node.scanStatus).toLowerCase()}`}>{scanLabel(node.scanStatus)}</span>
      <span>{node.type === 'File' || node.type === 1 ? formatBytes(node.size) : 'Folder'}</span>
      <span>{new Date(node.updatedAt).toLocaleString()}</span>
      <div className="row-actions">
        {!trash && <button title="Share link" className="icon" onClick={() => shareMutation.mutate()}><Link size={15} /></button>}
        {!trash && <button title="Download" className="icon" onClick={() => download(node)}><Download size={15} /></button>}
        <button title={trash ? 'Restore' : 'Trash'} className="icon" onClick={() => deleteMutation.mutate()}>{trash ? <RefreshCw size={15} /> : <Trash2 size={15} />}</button>
      </div>
      {share && <input className="share-output" readOnly value={share} onFocus={(event) => event.currentTarget.select()} />}
    </article>
  );
}

function PreviewPanel({ node, onClose, onChanged }: { node: FileNode | null; onClose: () => void; onChanged: () => void }) {
  const [preview, setPreview] = useState<{ url: string; kind: string; text?: string } | null>(null);
  const versions = useQuery({ queryKey: ['versions', node?.id], enabled: !!node, queryFn: () => api.versions(node!.id) });
  const restore = useMutation({ mutationFn: (versionId: string) => api.restoreVersion(node!.id, versionId), onSuccess: onChanged });

  useEffect(() => {
    if (!node) return;
    setPreview(null);
    api.downloadBlob(node.id)
      .then(async (blob) => {
        const url = URL.createObjectURL(blob);
        if ((node.mimeType ?? '').startsWith('text/')) {
          setPreview({ url, kind: 'text', text: await blob.text() });
        } else if ((node.mimeType ?? '').includes('pdf')) {
          setPreview({ url, kind: 'pdf' });
        } else if ((node.mimeType ?? '').startsWith('image/')) {
          setPreview({ url, kind: 'image' });
        } else {
          setPreview({ url, kind: 'download' });
        }
      })
      .catch(() => setPreview({ url: '', kind: 'blocked' }));
  }, [node]);

  if (!node) return null;

  return (
    <aside className="preview">
      <header>
        <div><p className="eyebrow">Preview</p><h2>{node.name}</h2></div>
        <button className="icon" onClick={onClose}>x</button>
      </header>
      <div className="preview-body">
        {preview?.kind === 'image' && <img src={preview.url} alt={node.name} />}
        {preview?.kind === 'pdf' && <iframe src={preview.url} title={node.name} />}
        {preview?.kind === 'text' && <pre>{preview.text}</pre>}
        {preview?.kind === 'download' && <button onClick={() => download(node)}>Download file</button>}
        {preview?.kind === 'blocked' && <p className="state">Preview is blocked until scan passes.</p>}
        {!preview && <p className="state">Preparing preview...</p>}
      </div>
      <section className="versions">
        <h3>Versions</h3>
        {versions.data?.map((version: FileVersion) => (
          <div key={version.id}>
            <span>v{version.versionNumber}</span>
            <span>{formatBytes(version.size)}</span>
            <button onClick={() => restore.mutate(version.id)}>Restore</button>
          </div>
        ))}
      </section>
    </aside>
  );
}

async function uploadOne(file: File, parentId: string | null, onProgress: (pct: number) => void) {
  const finalContentHash = await sha256(file);
  const session = await api.startUpload({ parentId, fileName: file.name, totalSize: file.size, mimeType: file.type || 'application/octet-stream', finalContentHash, chunkSize: CHUNK_SIZE });
  const uploaded = new Set(session.uploadedChunks);
  let completed = uploaded.size;
  const chunks = Array.from({ length: session.totalChunks }, (_, index) => index).filter((index) => !uploaded.has(index));

  async function send(index: number) {
    const start = index * session.chunkSize;
    const chunk = file.slice(start, Math.min(file.size, start + session.chunkSize));
    await api.uploadChunk(session.id, index, await sha256(chunk), chunk);
    completed += 1;
    onProgress((completed / session.totalChunks) * 100);
  }

  const workers = Array.from({ length: Math.min(MAX_PARALLEL_CHUNKS, chunks.length) }, async (_, worker) => {
    for (let i = worker; i < chunks.length; i += MAX_PARALLEL_CHUNKS) await send(chunks[i]);
  });
  await Promise.all(workers);
  await api.completeUpload(session.id);
  onProgress(100);
}

async function download(node: FileNode) {
  const blob = await api.downloadBlob(node.id);
  const url = URL.createObjectURL(blob);
  const anchor = document.createElement('a');
  anchor.href = url;
  anchor.download = node.name;
  anchor.click();
  URL.revokeObjectURL(url);
}

function statusIcon(node: FileNode) {
  if (scanLabel(node.scanStatus) === 'Infected') return <ShieldAlert size={20} />;
  return <File size={20} />;
}

function scanLabel(status: FileNode['scanStatus']) {
  if (status === 0 || status === 'PendingScan') return 'Pending';
  if (status === 1 || status === 'Clean') return 'Clean';
  if (status === 2 || status === 'Infected') return 'Infected';
  return 'Failed';
}
