import axios from 'axios'

const BASE = import.meta.env.VITE_API_URL || '/api'

export const api = axios.create({ baseURL: BASE })

api.interceptors.request.use(cfg => {
  const token = localStorage.getItem('accessToken')
  if (token) cfg.headers.Authorization = `Bearer ${token}`
  return cfg
})

let refreshing = false
api.interceptors.response.use(
  r => r,
  async err => {
    if (err.response?.status === 401 && !refreshing) {
      refreshing = true
      try {
        const rt = localStorage.getItem('refreshToken')
        if (rt) {
          const { data } = await axios.post(`/auth/refresh`, { refreshToken: rt })
          localStorage.setItem('accessToken',  data.data.accessToken)
          localStorage.setItem('refreshToken', data.data.refreshToken)
          err.config.headers.Authorization = `Bearer ${data.data.accessToken}`
          return api(err.config)
        }
      } catch {
        localStorage.clear()
        window.location.href = '/login'
      } finally { refreshing = false }
    }
    return Promise.reject(err)
  }
)

// ── AUTH ──────────────────────────────────────────────────────────────────────
export const authApi = {
  step1: email          => api.post('/auth/login/step1', { email }),
  step2: body           => api.post('/auth/login/step2', body),
  refresh: token        => api.post('/auth/refresh',    { refreshToken: token }),
  logout: token         => api.post('/auth/logout',     { refreshToken: token }),
  me:    ()             => api.get('/auth/me'),
  changePassword: body  => api.put('/auth/change-password', body),

  // OAuth / External Login
  externalLogin:   body => api.post('/auth/external-login', body),
  getLinkedProviders: () => api.get('/auth/external-logins'),
  linkProvider:    body => api.post('/auth/external-logins/link', body),
  unlinkProvider:  prov => api.delete(`/auth/external-logins/${prov}`),
}

// ── CREDITS & PRICING (Admin) ─────────────────────────────────────────────
export const creditsApi = {
  getPricing:      ()           => api.get('/admin/pricing'),
  updatePricing:   (id, body)   => api.put(`/admin/pricing/${id}`, body),
  grantCredits:    body         => api.post('/admin/pricing/grant-credits', body),
  confirmPayment:  body         => api.post('/credits/confirm-payment', body),
}

// ── CONSULTANT SERVICE CONFIG (⚙️ per-consultant settings) ────────────────
export const consultantConfigApi = {
  get:  userId       => api.get(`/admin/consultant-config/${userId}`),
  save: (userId, body) => api.put(`/admin/consultant-config/${userId}`, body),
}

// ── ADMIN INVOICES & PURCHASES ────────────────────────────────────────────
export const adminInvoiceApi = {
  list:       (p, ps, s) => api.get('/admin/invoices', { params: { page: p, pageSize: ps, search: s } }),
  stats:      ()         => api.get('/admin/invoices/stats'),
  download:   id         => `${BASE}/admin/invoices/${id}/download`,
  purchases:  (p, ps, status, s) => api.get('/admin/invoices/purchases', { params: { page: p, pageSize: ps, status, search: s } }),
}

// ── ADMIN — SETTINGS ──────────────────────────────────────────────────────────
export const settingsApi = {
  get:             ()   => api.get('/admin/settings'),
  updateWebsite:   body => api.put('/admin/settings/website',  body),
  updateBusiness:  body => api.put('/admin/settings/business', body),
  updateRoles:     body => api.put('/admin/settings/roles',    body),
}

// ── ADMIN — ORGANIZATION ──────────────────────────────────────────────────────
export const orgApi = {
  get:    ()      => api.get('/admin/setup/organization'),
  update: body    => api.put('/admin/setup/organization', body),
  uploadLogo: fd  => api.post('/admin/setup/organization/logo', fd),
}

// ── ADMIN — LOCATIONS ─────────────────────────────────────────────────────────
export const locationApi = {
  getAll:  (p, ps, s) => api.get('/admin/setup/locations', { params: { page: p, pageSize: ps, search: s } }),
  create:  body       => api.post('/admin/setup/locations', body),
  update:  (id, body) => api.put(`/admin/setup/locations/${id}`, body),
  delete:  id         => api.delete(`/admin/setup/locations/${id}`),
}

// ── ADMIN — ROLES ─────────────────────────────────────────────────────────────
export const roleApi = {
  getAll:  (p, ps, s) => api.get('/admin/users/roles', { params: { page: p, pageSize: ps, search: s } }),
  create:  roleName   => api.post('/admin/users/roles', { roleName }),
  update:  (id, name) => api.put(`/admin/users/roles/${id}`, { roleName: name }),
  delete:  id         => api.delete(`/admin/users/roles/${id}`),
}

// ── ADMIN — USERS ─────────────────────────────────────────────────────────────
export const userApi = {
  getAll:        (p, ps, s) => api.get('/admin/users/registrations', { params: { page: p, pageSize: ps, search: s } }),
  getById:       id         => api.get(`/admin/users/registrations/${id}`),
  create:        body       => api.post('/admin/users/registrations', body),
  update:        (id, body) => api.put(`/admin/users/registrations/${id}`, body),
  delete:        id         => api.delete(`/admin/users/registrations/${id}`),
  resetPassword: (id, pwd)  => api.put(`/admin/users/registrations/${id}/reset-password`, { newPassword: pwd }),
  uploadAvatar:  (id, fd)   => api.post(`/admin/users/registrations/${id}/avatar`, fd),
}

// ── ADMIN — ERROR LOGS ────────────────────────────────────────────────────────
export const errorLogApi = {
  getAll:  (p, ps, s) => api.get('/admin/system/error-logs', { params: { page: p, pageSize: ps, search: s } }),
  getById: id         => api.get(`/admin/system/error-logs/${id}`),
  delete:  id         => api.delete(`/admin/system/error-logs/${id}`),
}

// ── ADMIN — DATA CONSTANTS ────────────────────────────────────────────────────
export const dataApi = {
  controlTypes:    { getAll: s => api.get('/admin/data/control-types',    { params: { search: s } }), create: b => api.post('/admin/data/control-types', b),    update: (id,b) => api.put(`/admin/data/control-types/${id}`, b),    delete: id => api.delete(`/admin/data/control-types/${id}`)    },
  clientAreas:     { getAll: s => api.get('/admin/data/client-areas',     { params: { search: s } }), create: b => api.post('/admin/data/client-areas', b),     update: (id,b) => api.put(`/admin/data/client-areas/${id}`, b),     delete: id => api.delete(`/admin/data/client-areas/${id}`)     },
  currencies:      { getAll: s => api.get('/admin/data/currencies',       { params: { search: s } }), create: b => api.post('/admin/data/currencies', b),       update: (id,b) => api.put(`/admin/data/currencies/${id}`, b),       delete: id => api.delete(`/admin/data/currencies/${id}`)       },
  countries:       { getAll: s => api.get('/admin/data/countries',        { params: { search: s } }), create: b => api.post('/admin/data/countries', b),        update: (id,b) => api.put(`/admin/data/countries/${id}`, b),        delete: id => api.delete(`/admin/data/countries/${id}`)        },
  provinces:       { getAll: (cId,s) => api.get('/admin/data/provinces',  { params: { countryId: cId, search: s } }), create: b => api.post('/admin/data/provinces', b), update: (id,b) => api.put(`/admin/data/provinces/${id}`, b), delete: id => api.delete(`/admin/data/provinces/${id}`) },
  cities:          { getAll: (pId,s) => api.get('/admin/data/cities',     { params: { provinceId: pId, search: s } }), create: b => api.post('/admin/data/cities', b),    update: (id,b) => api.put(`/admin/data/cities/${id}`, b),    delete: id => api.delete(`/admin/data/cities/${id}`)    },
  locationTypes:   { getAll: s => api.get('/admin/data/location-types',   { params: { search: s } }), create: b => api.post('/admin/data/location-types', b),   update: (id,b) => api.put(`/admin/data/location-types/${id}`, b),   delete: id => api.delete(`/admin/data/location-types/${id}`)   },
}

// ── FISCAL YEAR ───────────────────────────────────────────────────────────────
export const fiscalYearApi = {
  getAll:     ()        => api.get('/admin/setup/fiscal-years'),
  create:     body      => api.post('/admin/setup/fiscal-years', body),
  update:     (id,body) => api.put(`/admin/setup/fiscal-years/${id}`, body),
  delete:     id        => api.delete(`/admin/setup/fiscal-years/${id}`),
  setCurrent: id        => api.put(`/admin/setup/fiscal-years/${id}/set-current`),
  toggle:     id        => api.patch(`/admin/setup/fiscal-years/${id}/toggle`),
}

// ── NOTIFICATION TEMPLATES ────────────────────────────────────────────────────
export const templateApi = {
  wa:    { getAll: (p,ps,s) => api.get('/admin/notifications/templates/wa',    {params:{page:p,pageSize:ps,search:s}}), getById: id => api.get(`/admin/notifications/templates/wa/${id}`),    create: b => api.post('/admin/notifications/templates/wa', b),    update: (id,b) => api.put(`/admin/notifications/templates/wa/${id}`, b),    setStatus: (id,s) => api.patch(`/admin/notifications/templates/wa/${id}/status`,{status:s}),    delete: id => api.delete(`/admin/notifications/templates/wa/${id}`),    preview: (id,vars) => api.post(`/admin/notifications/templates/wa/${id}/preview`, vars) },
  sms:   { getAll: (p,ps,s) => api.get('/admin/notifications/templates/sms',   {params:{page:p,pageSize:ps,search:s}}), getById: id => api.get(`/admin/notifications/templates/sms/${id}`),   create: b => api.post('/admin/notifications/templates/sms', b),   update: (id,b) => api.put(`/admin/notifications/templates/sms/${id}`, b),   setStatus: (id,s) => api.patch(`/admin/notifications/templates/sms/${id}/status`,{status:s}),   delete: id => api.delete(`/admin/notifications/templates/sms/${id}`),   charCount: msg => api.post('/admin/notifications/templates/sms/char-count',{message:msg}) },
  email: { getAll: (p,ps,s) => api.get('/admin/notifications/templates/email', {params:{page:p,pageSize:ps,search:s}}), getById: id => api.get(`/admin/notifications/templates/email/${id}`), create: b => api.post('/admin/notifications/templates/email', b), update: (id,b) => api.put(`/admin/notifications/templates/email/${id}`, b), setStatus: (id,s) => api.patch(`/admin/notifications/templates/email/${id}/status`,{status:s}), delete: id => api.delete(`/admin/notifications/templates/email/${id}`), preview: (id,vars) => api.post(`/admin/notifications/templates/email/${id}/preview`, vars), sendTest: (id,b) => api.post(`/admin/notifications/templates/email/${id}/send-test`, b) },
  web:   { getAll: (p,ps,s) => api.get('/admin/notifications/templates/web',   {params:{page:p,pageSize:ps,search:s}}), getById: id => api.get(`/admin/notifications/templates/web/${id}`),   create: b => api.post('/admin/notifications/templates/web', b),   update: (id,b) => api.put(`/admin/notifications/templates/web/${id}`, b),   setStatus: (id,s) => api.patch(`/admin/notifications/templates/web/${id}/status`,{status:s}),   delete: id => api.delete(`/admin/notifications/templates/web/${id}`) },
}

// ── NOTIFICATIONS SEND / HISTORY ──────────────────────────────────────────────
export const notifyApi = {
  wa:      { getAll: (p,ps,s) => api.get('/admin/notifications/wa',    {params:{page:p,pageSize:ps,search:s}}), send: b => api.post('/admin/notifications/wa/send', b),    bulk: b => api.post('/admin/notifications/wa/bulk', b) },
  sms:     { getAll: (p,ps,s) => api.get('/admin/notifications/sms',   {params:{page:p,pageSize:ps,search:s}}), send: b => api.post('/admin/notifications/sms/send', b) },
  email:   { getAll: (p,ps,s) => api.get('/admin/notifications/email', {params:{page:p,pageSize:ps,search:s}}), send: b => api.post('/admin/notifications/email/send', b), bulk: b => api.post('/admin/notifications/email/bulk', b) },
  webpush: { getAll: (p,ps,s) => api.get('/admin/notifications/webpush',{params:{page:p,pageSize:ps,search:s}}), sendOne: (id,b) => api.post(`/admin/notifications/webpush/${id}/send`, b), broadcast: b => api.post('/admin/notifications/webpush/broadcast', b), sendToUser: (uid,b) => api.post(`/admin/notifications/webpush/send-to-user/${uid}`, b), delete: id => api.delete(`/admin/notifications/webpush/${id}`) },
  app:     { getAll: (p,ps,t) => api.get('/admin/notifications/app',   {params:{page:p,pageSize:ps,type:t}}),   create: b => api.post('/admin/notifications/app', b), getById: id => api.get(`/admin/notifications/app/${id}`), delete: id => api.delete(`/admin/notifications/app/${id}`) },
}

// ── ROLE MODULES & MENUS ──────────────────────────────────────────────────────
export const roleModuleApi = {
  getModules:    roleId          => api.get(`/admin/users/roles/${roleId}/modules`),
  bulkModules:   (roleId,body)   => api.post(`/admin/users/roles/${roleId}/modules/bulk`, body),
  deleteModule:  (roleId,id)     => api.delete(`/admin/users/roles/${roleId}/modules/${id}`),
  getMenus:      roleId          => api.get(`/admin/users/roles/${roleId}/menus`),
  bulkMenus:     (roleId,body)   => api.post(`/admin/users/roles/${roleId}/menus/bulk`, body),
  deleteMenu:    (roleId,id)     => api.delete(`/admin/users/roles/${roleId}/menus/${id}`),
}

// ── USER PERMISSIONS ──────────────────────────────────────────────────────────
export const userPermApi = {
  get:    userId       => api.get(`/admin/users/${userId}/permissions`),
  bulk:   (userId,b)   => api.put(`/admin/users/${userId}/permissions`, b),
  delete: (userId,id)  => api.delete(`/admin/users/${userId}/permissions/${id}`),
  menu:   (userId,loc) => api.get(`/admin/users/${userId}/menu`, {params:{locationId:loc}}),
}

// ── DOCUMENT MOVEMENTS ────────────────────────────────────────────────────────
export const docMovApi = {
  getAll:   s           => api.get('/admin/data/document-movements', {params:{search:s}}),
  create:   body        => api.post('/admin/data/document-movements', body),
  update:   (id,body)   => api.put(`/admin/data/document-movements/${id}`, body),
  delete:   id          => api.delete(`/admin/data/document-movements/${id}`),
  getNext:  id          => api.post(`/admin/data/document-movements/${id}/next`),
  generate: prefix      => api.post('/admin/data/document-movements/generate', {prefix}),
}

// ── REVIEWS (admin) ───────────────────────────────────────────────────────────
export const reviewAdminApi = {
  getAll: (p,ps,min,max) => api.get('/admin/data/reviews', {params:{page:p,pageSize:ps,minRating:min,maxRating:max}}),
  delete: id             => api.delete(`/admin/data/reviews/${id}`),
}

// ── EXTENDED DATA CONSTANTS ───────────────────────────────────────────────────
export const dataExtApi = {
  districts:       { getAll: (cId,s) => api.get('/admin/data/districts',       {params:{cityId:cId,search:s}}),    create: b => api.post('/admin/data/districts', b),       update:(id,b) => api.put(`/admin/data/districts/${id}`,b),       delete: id => api.delete(`/admin/data/districts/${id}`) },
  tehsils:         { getAll: (dId,s) => api.get('/admin/data/tehsils',         {params:{districtId:dId,search:s}}), create: b => api.post('/admin/data/tehsils', b),         update:(id,b) => api.put(`/admin/data/tehsils/${id}`,b),         delete: id => api.delete(`/admin/data/tehsils/${id}`) },
  areas:           { getAll: (tId,s) => api.get('/admin/data/areas',           {params:{tehsilId:tId,search:s}}),   create: b => api.post('/admin/data/areas', b),           update:(id,b) => api.put(`/admin/data/areas/${id}`,b),           delete: id => api.delete(`/admin/data/areas/${id}`) },
  documentTypes:   { getAll: s       => api.get('/admin/data/document-types',  {params:{search:s}}),                create: b => api.post('/admin/data/document-types', b),  update:(id,b) => api.put(`/admin/data/document-types/${id}`,b),  delete: id => api.delete(`/admin/data/document-types/${id}`) },
  criteriaTypes:   { getAll: s       => api.get('/admin/data/criteria-types',  {params:{search:s}}),                create: b => api.post('/admin/data/criteria-types', b),  update:(id,b) => api.put(`/admin/data/criteria-types/${id}`,b),  delete: id => api.delete(`/admin/data/criteria-types/${id}`) },
  criteriaSubTypes:{ getAll: (cId,s) => api.get('/admin/data/criteria-subtypes',{params:{criteriaTypeId:cId,search:s}}), create: b => api.post('/admin/data/criteria-subtypes', b), update:(id,b) => api.put(`/admin/data/criteria-subtypes/${id}`,b), delete: id => api.delete(`/admin/data/criteria-subtypes/${id}`) },
  clientCategories:{ getAll: s       => api.get('/admin/data/client-categories',{params:{search:s}}),               create: b => api.post('/admin/data/client-categories', b), update:(id,b) => api.put(`/admin/data/client-categories/${id}`,b), delete: id => api.delete(`/admin/data/client-categories/${id}`) },
  controlCategories:{ getAll:(cId,s) => api.get('/admin/data/control-categories',{params:{controlTypeId:cId,search:s}}), create: b => api.post('/admin/data/control-categories', b), update:(id,b) => api.put(`/admin/data/control-categories/${id}`,b), delete: id => api.delete(`/admin/data/control-categories/${id}`) },
}
