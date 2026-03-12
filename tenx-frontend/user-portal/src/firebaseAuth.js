// ═══════════════════════════════════════════════════════════════════════════
// FIREBASE AUTH — Multi Provider Social Login
// Google, Facebook, Microsoft, LinkedIn, Apple, Yahoo
// ═══════════════════════════════════════════════════════════════════════════

const firebaseConfig = {
  apiKey:     import.meta.env.VITE_FIREBASE_API_KEY     || 'AIzaSyAfTiyT-D_ZaJFQ9U9zTrc2ForpT6bSiCw',
  authDomain: import.meta.env.VITE_FIREBASE_AUTH_DOMAIN || 'x-convo-b09bc.firebaseapp.com',
  projectId:  import.meta.env.VITE_FIREBASE_PROJECT_ID  || 'x-convo-b09bc',
}

let firebaseApp = null
let firebaseAuth = null

async function getFirebase() {
  if (firebaseAuth) return firebaseAuth

  const { initializeApp } = await import(
    'https://www.gstatic.com/firebasejs/11.6.0/firebase-app.js'
  )

  const {
    getAuth,
    signInWithPopup,
    GoogleAuthProvider,
    FacebookAuthProvider,
    OAuthProvider
  } = await import(
    'https://www.gstatic.com/firebasejs/11.6.0/firebase-auth.js'
  )

  firebaseApp = initializeApp(firebaseConfig)
  firebaseAuth = getAuth(firebaseApp)

  // Providers
  firebaseAuth._GoogleProvider = new GoogleAuthProvider()
  firebaseAuth._FacebookProvider = new FacebookAuthProvider()

  firebaseAuth._MicrosoftProvider = new OAuthProvider('microsoft.com')
  firebaseAuth._LinkedInProvider = new OAuthProvider('linkedin.com')
  firebaseAuth._AppleProvider = new OAuthProvider('apple.com')
  firebaseAuth._YahooProvider = new OAuthProvider('yahoo.com')

  firebaseAuth._signInWithPopup = signInWithPopup

  return firebaseAuth
}

// ─────────────────────────────────────────
// GOOGLE LOGIN
// ─────────────────────────────────────────

export async function signInWithGoogle() {
  const auth = await getFirebase()
  const result = await auth._signInWithPopup(auth, auth._GoogleProvider)
  const user = result.user

  return {
    provider: 'Google',
    providerKey: user.uid,
    email: user.email,
    displayName: user.displayName,
    avatarUrl: user.photoURL,
  }
}

// ─────────────────────────────────────────
// FACEBOOK LOGIN
// ─────────────────────────────────────────

export async function signInWithFacebook() {
  const auth = await getFirebase()
  const result = await auth._signInWithPopup(auth, auth._FacebookProvider)
  const user = result.user

  return {
    provider: 'Facebook',
    providerKey: user.uid,
    email: user.email,
    displayName: user.displayName,
    avatarUrl: user.photoURL,
  }
}

// ─────────────────────────────────────────
// MICROSOFT LOGIN
// ─────────────────────────────────────────

export async function signInWithMicrosoft() {
  const auth = await getFirebase()
  const result = await auth._signInWithPopup(auth, auth._MicrosoftProvider)
  const user = result.user

  return {
    provider: 'Microsoft',
    providerKey: user.uid,
    email: user.email,
    displayName: user.displayName,
    avatarUrl: user.photoURL,
  }
}

// ─────────────────────────────────────────
// LINKEDIN LOGIN
// ─────────────────────────────────────────

export async function signInWithLinkedIn() {
  const auth = await getFirebase()
  const result = await auth._signInWithPopup(auth, auth._LinkedInProvider)
  const user = result.user

  return {
    provider: 'LinkedIn',
    providerKey: user.uid,
    email: user.email,
    displayName: user.displayName,
    avatarUrl: user.photoURL,
  }
}

// ─────────────────────────────────────────
// APPLE LOGIN
// ─────────────────────────────────────────

export async function signInWithApple() {
  const auth = await getFirebase()
  const result = await auth._signInWithPopup(auth, auth._AppleProvider)
  const user = result.user

  return {
    provider: 'Apple',
    providerKey: user.uid,
    email: user.email,
    displayName: user.displayName,
    avatarUrl: user.photoURL,
  }
}

// ─────────────────────────────────────────
// YAHOO LOGIN
// ─────────────────────────────────────────

export async function signInWithYahoo() {
  const auth = await getFirebase()
  const result = await auth._signInWithPopup(auth, auth._YahooProvider)
  const user = result.user

  return {
    provider: 'Yahoo',
    providerKey: user.uid,
    email: user.email,
    displayName: user.displayName,
    avatarUrl: user.photoURL,
  }
}