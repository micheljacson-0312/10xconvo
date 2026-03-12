# 10X Convo — Frontend (3 Portals)

## Structure
```
tenx-frontend/
├── admin-portal/       → localhost:3000  (Admin Role)
├── consultant-portal/  → localhost:3002  (Consultant Role)
└── user-portal/        → localhost:3004  (Public + Client Role)
```

## Setup & Run

### 1. Start Backend first
```bash
cd TenXConvo_v2/src/TenXConvo.API
dotnet run
# API runs on http://localhost:5000
# Swagger UI at http://localhost:5000
```

### 2. Install & run each portal
```bash
# Admin Portal
cd admin-portal && npm install && npm run dev
# → http://localhost:3000

# Consultant Portal
cd consultant-portal && npm install && npm run dev
# → http://localhost:3002

# User Portal
cd user-portal && npm install && npm run dev
# → http://localhost:3004
```

## Test Accounts

| Portal | Email | Password | Role |
|--------|-------|----------|------|
| Admin | admin@htag.mhm | Admin@123 | Admin Role |
| Consultant | ali@htag.mhm | Test@123 | Consultant Role |
| User/Client | sara@htag.mhm | Test@123 | Client Role |

## Features per Portal

### Admin Portal (localhost:3000)
- 2-step login with location + fiscal year dropdowns
- Dashboard with live stats
- Users — full CRUD, avatar upload, password reset
- Roles — create/edit/delete
- Locations — with location types
- Settings — Website / Business / Roles tabs
- Data Constants — Control Types, Client Areas, Currencies, Geography (Country→Province→City)
- Error Logs — read + delete

### Consultant Portal (localhost:3002)
- 2-step login
- Dashboard with client/request/unread counts
- Profile — bio, specialization, hourly rate, public toggle + live preview
- Online / Offline toggle (updates in real-time)
- My Clients — accepted connections
- Requests — pending connect requests with Accept/Reject
- Messages — real-time chat via SignalR
  - Typing indicators
  - Read receipts
  - Auto-joins conversation rooms on connect

### User Portal (localhost:3004)
- Public consultant directory (no login needed)
- Search by name/specialization
- Consultant detail modal with Connect button
- Login with 2-step flow
- Real-time messaging with SignalR
- My Profile (bio, company, city)
- ConnectionAccepted notification via SignalR

## Tech Stack
- React 18 + Vite
- React Router v6
- Zustand (state management)
- Axios + auto JWT refresh interceptor
- @microsoft/signalr (real-time chat)
- react-hot-toast (notifications)
- lucide-react (icons)
- Google Fonts: Syne (headings) + DM Sans (body)
