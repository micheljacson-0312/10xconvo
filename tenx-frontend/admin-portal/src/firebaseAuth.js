// ═══════════════════════════════════════════════════════════════════════════
//  FIREBASE AUTH — Google + Facebook social login
//  Uses Firebase client SDK → gets id_token → sends to our backend
//
//  Setup:
//  1. Create Firebase project at https://console.firebase.google.com
//  2. Enable Google + Facebook sign-in methods under Authentication → Sign-in method
//  3. Set env vars VITE_FIREBASE_API_KEY, VITE_FIREBASE_AUTH_DOMAIN, VITE_FIREBASE_PROJECT_ID
//  4. For Facebook: create app at https://developers.facebook.com, add to Firebase
// ═══════════════════════════════════════════════════════════════════════════

// Firebase SDK loaded from CDN via import maps (no npm install needed)
// Add to index.html:  <script type="module"> ... </script>

const firebaseConfig = {
  apiKey:            import.meta.env.VITE_FIREBASE_API_KEY            || 'YOUR_API_KEY',
  authDomain:        import.meta.env.VITE_FIREBASE_AUTH_DOMAIN        || 'YOUR_PROJECT.firebaseapp.com',
  projectId:         import.meta.env.VITE_FIREBASE_PROJECT_ID         || 'YOUR_PROJECT_ID',
}

let firebaseApp = null
let firebaseAuth = null

async function getFirebase() {
  if (firebaseAuth) return firebaseAuth

  // Dynamic import — Firebase modules loaded only when social login is clicked
  const { initializeApp }              = await import('https://www.gstatic.com/firebasejs/11.6.0/firebase-app.js')
  const { getAuth, signInWithPopup,
          GoogleAuthProvider,
          FacebookAuthProvider }        = await import('https://www.gstatic.com/firebasejs/11.6.0/firebase-auth.js')

  firebaseApp  = initializeApp(firebaseConfig)
  firebaseAuth = getAuth(firebaseApp)

  // Attach providers to the auth instance for external use
  firebaseAuth._GoogleProvider   = new GoogleAuthProvider()
  firebaseAuth._FacebookProvider = new FacebookAuthProvider()
  firebaseAuth._signInWithPopup  = signInWithPopup

  return firebaseAuth
}

/**
 * Sign in with Google via Firebase popup.
 * Returns { provider, providerKey, email, displayName, avatarUrl }
 * which can be sent directly to POST /api/auth/external-login
 */
export async function signInWithGoogle() {
  const auth = await getFirebase()
  const result = await auth._signInWithPopup(auth, auth._GoogleProvider)
  const user = result.user
  return {
    provider:    'Google',
    providerKey: user.uid,
    email:       user.email,
    displayName: user.displayName,
    avatarUrl:   user.photoURL,
  }
}

/**
 * Sign in with Facebook via Firebase popup.
 * Returns { provider, providerKey, email, displayName, avatarUrl }
 */
export async function signInWithFacebook() {
  const auth = await getFirebase()
  const result = await auth._signInWithPopup(auth, auth._FacebookProvider)
  const user = result.user
  return {
    provider:    'Facebook',
    providerKey: user.uid,
    email:       user.email,
    displayName: user.displayName,
    avatarUrl:   user.photoURL,
  }
}
