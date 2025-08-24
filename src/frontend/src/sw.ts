/// <reference lib="webworker" />

// Service Worker (TypeScript). Built by Vite as a separate entry and emitted as /sw.js
// Scope: root. Registration uses `{ type: 'module' }` for modern browsers.

const APP_CACHE = 'app-shell-v2';
const ASSET_CACHE = 'asset-cache-v2';
const MANIFEST_CACHE = 'manifest-cache-v2';
const DATA_CACHE = 'data-cache-v2';
const INDEX_CACHE = 'index-cache-v2';

self.addEventListener('install', (event: ExtendableEvent) => {
  event.waitUntil((async () => {
    const cache = await caches.open(APP_CACHE);
    await cache.addAll(['/', '/index.html']);
    (self as ServiceWorkerGlobalScope).skipWaiting();
  })());
});

self.addEventListener('activate', (event: ExtendableEvent) => {
  event.waitUntil((async () => {
    await (self as ServiceWorkerGlobalScope).clients.claim();
    const clients = await (self as ServiceWorkerGlobalScope).clients.matchAll({ includeUncontrolled: true, type: 'window' });
    for (const client of clients) (client as WindowClient).postMessage({ type: 'sw-updated' });
  })());
});

function isSameOrigin(url: string) {
  try { const u = new URL(url); return u.origin === (self as ServiceWorkerGlobalScope).location.origin; } catch { return false; }
}

self.addEventListener('fetch', (event: FetchEvent) => {
  const req = event.request;
  const url = new URL(req.url);

  // App shell for navigations – network-first to avoid stale index.html with old asset hashes
  if (req.mode === 'navigate') {
    event.respondWith((async () => {
      const cache = await caches.open(APP_CACHE);
      try {
        const resp = await fetch('/index.html', { cache: 'no-store' });
        if (resp && resp.status === 200) cache.put('/index.html', resp.clone());
        return resp;
      } catch {
        const cached = await cache.match('/index.html');
        if (cached) return cached;
        return fetch(req);
      }
    })());
    return;
  }

  // Static assets: cache-first (Vite hashed files)
  if (isSameOrigin(req.url) && url.pathname.startsWith('/assets/')) {
    event.respondWith(cacheFirst(req, ASSET_CACHE));
    return;
  }

  // Manifest: network-first
  if (url.pathname === '/site-manifest.json') {
    event.respondWith(networkFirst(req, MANIFEST_CACHE));
    return;
  }

  // Immutable data bundles: cache-first
  if (url.pathname.startsWith('/data/') && url.pathname.endsWith('.json')) {
    event.respondWith(cacheFirst(req, DATA_CACHE));
    return;
  }
  if (url.pathname.startsWith('/index/') && url.pathname.endsWith('.json')) {
    event.respondWith(cacheFirst(req, INDEX_CACHE));
    return;
  }
});

async function cacheFirst(request: Request, cacheName: string) {
  const cache = await caches.open(cacheName);
  const cached = await cache.match(request);
  if (cached) return cached;
  const resp = await fetch(request);
  if (resp && resp.status === 200) cache.put(request, resp.clone());
  return resp;
}

async function networkFirst(request: Request, cacheName: string) {
  const cache = await caches.open(cacheName);
  try {
    const resp = await fetch(request);
    if (resp && resp.status === 200) cache.put(request, resp.clone());
    return resp;
  } catch (err) {
    const cached = await cache.match(request);
    if (cached) return cached;
    throw err;
  }
}
