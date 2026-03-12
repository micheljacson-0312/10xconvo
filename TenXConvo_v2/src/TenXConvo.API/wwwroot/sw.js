// ── 10X Convo Service Worker ────────────────────────────────────────────────
// Place this file at: /public/sw.js in each frontend portal
// Register in main.jsx:
//   navigator.serviceWorker.register('/sw.js')
//     .then(reg => subscribeToPush(reg))

self.addEventListener('install',  () => self.skipWaiting());
self.addEventListener('activate', e => e.waitUntil(clients.claim()));

// ── Receive push notification ─────────────────────────────────────────────
self.addEventListener('push', event => {
  if (!event.data) return;

  const data = event.data.json();
  const notif = data.notification || {};

  event.waitUntil(
    self.registration.showNotification(notif.title || '10X Convo', {
      body:    notif.body   || '',
      icon:    notif.icon   || '/icon-192.png',
      badge:   notif.badge  || '/badge-96.png',
      data:    notif.data   || {},
      actions: notif.actions || [],
      vibrate: [200, 100, 200],
    })
  );
});

// ── Click notification → open URL ─────────────────────────────────────────
self.addEventListener('notificationclick', event => {
  event.notification.close();
  const url = event.notification.data?.url || '/';
  event.waitUntil(
    clients.matchAll({ type: 'window', includeUncontrolled: true }).then(list => {
      const existing = list.find(w => w.url.includes(url));
      if (existing) return existing.focus();
      return clients.openWindow(url);
    })
  );
});
