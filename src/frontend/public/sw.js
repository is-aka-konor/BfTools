const APP_CACHE = 'app-shell-v1';
const ASSET_CACHE = 'asset-cache-v1';
const MANIFEST_CACHE = 'manifest-cache-v1';
const DATA_CACHE = 'data-cache-v1';
const INDEX_CACHE = 'index-cache-v1';

self.addEventListener('install', (event) => {
  event.waitUntil((async () => {
    const cache = await caches.open(APP_CACHE);
    await cache.addAll(['/','/index.html']);
    self.skipWaiting();
  })());
});

self.addEventListener('activate', (event) => {
  event.waitUntil((async () => {
    // Optional: cleanup old caches in future by listing keys
    await self.clients.claim();
    const clients = await self.clients.matchAll({ includeUncontrolled: true, type: 'window' });
    for (const client of clients) client.postMessage({ type: 'sw-updated' });
  })());
});

function isSameOrigin(url) { try { const u = new URL(url); return u.origin === self.location.origin; } catch { return false; } }

self.addEventListener('fetch', (event) => {
  const req = event.request;
  const url = new URL(req.url);

  // App shell for navigations
  if (req.mode === 'navigate') {
    event.respondWith((async () => {
      const cache = await caches.open(APP_CACHE);
      const cached = await cache.match('/index.html');
      if (cached) return cached;
      const resp = await fetch('/index.html');
      cache.put('/index.html', resp.clone());
      return resp;
    })());
    return;
  }

  // Static assets: cache-first (Vite hashed files)
  if (isSameOrigin(req.url) && url.pathname.startsWith('/assets/')) {
    event.respondWith(cacheFirst(req, ASSET_CACHE));
    return;
  }

  // Manifest: network-first
  if (url.pathname === '/dist-site/site-manifest.json') {
    event.respondWith(networkFirst(req, MANIFEST_CACHE));
    return;
  }

  // Immutable data bundles: cache-first
  if (url.pathname.startsWith('/dist-site/data/') && url.pathname.endsWith('.json')) {
    event.respondWith(cacheFirst(req, DATA_CACHE));
    return;
  }
  if (url.pathname.startsWith('/dist-site/index/') && url.pathname.endsWith('.json')) {
    event.respondWith(cacheFirst(req, INDEX_CACHE));
    return;
  }
});

async function cacheFirst(request, cacheName) {
  const cache = await caches.open(cacheName);
  const cached = await cache.match(request);
  if (cached) return cached;
  const resp = await fetch(request);
  if (resp && resp.status === 200) cache.put(request, resp.clone());
  return resp;
}

async function networkFirst(request, cacheName) {
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

