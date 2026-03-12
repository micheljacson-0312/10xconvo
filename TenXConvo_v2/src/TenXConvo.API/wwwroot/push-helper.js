// ── Push Notification Helper ─────────────────────────────────────────────────
// Copy to frontend: src/utils/pushHelper.js

const VAPID_PUBLIC_KEY = 'YOUR_VAPID_PUBLIC_KEY_HERE'; // from appsettings
const API_BASE = 'http://localhost:5000';

function urlBase64ToUint8Array(base64String) {
  const padding = '='.repeat((4 - base64String.length % 4) % 4);
  const base64  = (base64String + padding).replace(/-/g, '+').replace(/_/g, '/');
  const raw     = window.atob(base64);
  return Uint8Array.from([...raw].map(c => c.charCodeAt(0)));
}

export async function registerPushNotifications(loginId) {
  if (!('serviceWorker' in navigator) || !('PushManager' in window)) {
    console.warn('Push notifications not supported');
    return false;
  }

  try {
    // 1. Register service worker
    const reg = await navigator.serviceWorker.register('/sw.js');
    await navigator.serviceWorker.ready;

    // 2. Request permission
    const permission = await Notification.requestPermission();
    if (permission !== 'granted') return false;

    // 3. Subscribe to push
    const subscription = await reg.pushManager.subscribe({
      userVisibleOnly:      true,
      applicationServerKey: urlBase64ToUint8Array(VAPID_PUBLIC_KEY),
    });

    // 4. Register with backend
    const deviceId = `${navigator.userAgent.slice(0, 30)}-${Date.now()}`;
    await fetch(`${API_BASE}/api/admin/notifications/webpush/register`, {
      method:  'POST',
      headers: {
        'Content-Type':  'application/json',
        'Authorization': `Bearer ${localStorage.getItem('accessToken')}`,
      },
      body: JSON.stringify({
        deviceId,
        token:    JSON.stringify(subscription),
        loginId:  loginId || 'user',
        platform: 'WEB',
      }),
    });

    console.log('✅ Push notifications registered');
    return true;
  } catch (err) {
    console.error('Push registration failed:', err);
    return false;
  }
}
