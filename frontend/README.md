# HREnap — Frontend

A full-featured HR Management System built with **React 19**, **TypeScript**, and **Vite**. This document is the primary reference for any developer joining the project.

---

## Table of Contents

1. [Overview](#overview)
2. [Tech Stack & Key Dependencies](#tech-stack--key-dependencies)
3. [Getting Started](#getting-started)
4. [Environment Variables](#environment-variables)
5. [Project Structure](#project-structure)
6. [Architecture & Design Decisions](#architecture--design-decisions)
7. [Routing](#routing)
8. [Authentication & Token Management](#authentication--token-management)
9. [API Layer](#api-layer)
10. [State Management](#state-management)
11. [Feature Modules](#feature-modules)
12. [Shared Components](#shared-components)
13. [Data Tables](#data-tables)
14. [Real-Time Notifications (SignalR)](#real-time-notifications-signalr)
15. [Internationalization (i18n)](#internationalization-i18n)
16. [Styling](#styling)
17. [Form Validation](#form-validation)
18. [Error Handling](#error-handling)
19. [Build & Deployment](#build--deployment)

---

## Overview

HREnap is a single-page application (SPA) for managing user accounts, subscriptions, and notifications:

- **Authentication** — secure login, registration, and password management
- **User Management** — admin controls for staff roles and permissions
- **Subscriptions** — plan selection, payment processing, and subscription lifecycle
- **Notifications** — real-time in-app notifications via SignalR
- **Settings** — user profile, preferences, and language
- **Dashboard** — workspace overview and key metrics

---

## Tech Stack & Key Dependencies

| Category | Library | Version | Purpose |
|---|---|---|---|
| UI Framework | `react` + `react-dom` | 19.x | Component-based UI |
| Language | `typescript` | ~5.9 | Static typing |
| Build Tool | `vite` | 7.x | Dev server + bundler |
| Routing | `react-router-dom` | 7.x | Client-side routing |
| Server State | `@tanstack/react-query` | 5.x | Data fetching, caching, invalidation |
| Tables | `@tanstack/react-table` | 8.x | Server-side paginated tables |
| HTTP Client | `axios` | 1.x | API requests with interceptors |
| Forms | `react-hook-form` | 7.x | Performant form state management |
| Validation | `zod` | 4.x | Schema-based form validation |
| Styling | `tailwindcss` | 4.x | Utility-first CSS |
| UI Components | `@radix-ui/*` (shadcn/ui pattern) | various | Accessible, unstyled primitives |
| Icons | `lucide-react` | 0.56x | Icon set |
| Toasts | `sonner` | 2.x | Toast notifications |
| Real-time | `@microsoft/signalr` | 10.x | WebSocket connection to backend |
| i18n | `i18next` + `react-i18next` | 25.x / 16.x | Multi-language support |
| CSS Utilities | `clsx` + `tailwind-merge` | latest | Conditional class merging |

---

## Getting Started

> **Package manager:** This project uses [pnpm](https://pnpm.io/). Install it globally first if you don't have it: `npm install -g pnpm`

```bash
# 1. Install dependencies
pnpm install

# 2. Create your local environment file
cp .env .env.local
# Edit .env.local and point VITE_API_URL at your backend

# 3. Start the dev server (hot-reload)
pnpm dev

# 4. Build for production
pnpm build

# 5. Preview the production build locally
pnpm preview

# 6. Run linter
pnpm lint
```

The dev server runs on `http://localhost:5173` by default.

---

## Environment Variables

All Vite environment variables must be prefixed with `VITE_` to be exposed to the client bundle.

| Variable | Default | Description |
|---|---|---|
| `VITE_API_URL` | `http://localhost:5088/api/v1` | Base URL for all backend API calls |

Create a `.env.local` file (it is git-ignored) to override the defaults for your local environment.

---

## Project Structure

```
frontend/
├── public/                     # Static assets (logo, favicon, etc.)
├── src/
│   ├── main.tsx                # Entry point — mounts React, boots i18n
│   ├── App.tsx                 # Router definition + QueryClient provider
│   ├── index.css               # Global CSS / Tailwind base imports
│   │
│   ├── assets/                 # Images and static assets used in components
│   │
│   ├── common/
│   │   ├── layouts/
│   │   │   ├── Layout.tsx          # Shell: positions Sidebar + TopNavigation + Outlet
│   │   │   ├── MainLayout.tsx      # Wraps Layout with the SignalR context provider
│   │   │   ├── SideBar.tsx         # Collapsible sidebar with navigation links + logout
│   │   │   ├── SideBarLink.tsx     # A single sidebar nav item (active state aware)
│   │   │   └── TopNavigation.tsx   # Top bar: mobile menu toggle + notifications dropdown
│   │   └── results/
│   │       └── resutl.ts           # (Reserved) shared result type placeholder
│   │
│   ├── components/
│   │   ├── theme-provider.tsx      # Dark/light theme context wrapper
│   │   ├── ui/                     # All shadcn/ui component wrappers
│   │   └── tables/                 # Generic server-side data table system
│   │
│   ├── features/                   # Vertical-slice feature modules (one folder per domain)
│   │   ├── auth/
│   │   ├── dashboard/
│   │   ├── notifications/
│   │   ├── settings/
│   │   ├── subscriptions/
│   │   └── users/
│   │
│   ├── hooks/
│   │   └── use-toast.ts            # Wrapper around sonner for consistent toast API
│   │
│   └── lib/
│       ├── api/
│       │   ├── api.ts              # Axios instance (baseURL, cookies, timeout)
│       │   ├── auth.ts             # Auth API calls (login, logout, forgot/reset password)
│       │   ├── tokenManager.ts     # In-memory token store + axios interceptors
│       │   ├── error-types.ts      # Backend error codes and ProblemDetails types
│       │   └── axios.d.ts          # TypeScript declaration augmenting AxiosRequestConfig
│       ├── i18n/
│       │   ├── index.ts            # i18next initialization (EN/FR/AR)
│       │   ├── keyContainer.ts     # Centralized translation key constants
│       │   └── locales/
│       │       ├── en/en.json      # English translations
│       │       ├── fr/fr.json      # French translations
│       │       └── ar/ar.json      # Arabic translations (RTL)
│       ├── signalr/
│       │   ├── SignalRContext.ts   # React context object for the HubConnection
│       │   ├── signalr-context.tsx # SignalRProvider component (connection lifecycle)
│       │   └── use-signalr.ts      # Hook to consume the SignalR context
│       └── utils.ts                # cn() class merger + getTableRequestParams() helper
│
├── .env                        # Default environment variables (committed)
├── vite.config.ts              # Vite config: React plugin, Tailwind plugin, @ path alias
├── tsconfig.json               # TypeScript project references config
├── tsconfig.app.json           # App TypeScript config (strict mode)
├── components.json             # shadcn/ui CLI configuration
├── vercel.json                 # Vercel deployment config (SPA rewrites)
└── pnpm-workspace.yaml         # pnpm workspace root declaration
```

---

## Architecture & Design Decisions

### Vertical Slice (Feature-Based) Architecture

Each business domain lives as a self-contained folder under `src/features/`. A typical feature folder looks like:

```
features/auth/
├── auth-api.ts             # All API functions + TypeScript types for auth
├── AuthProvider.tsx        # Auth context and provider
├── useCurrentUser.ts       # Hook to access the current authenticated user
└── pages/
    ├── Login.tsx
    ├── RegisterPage.tsx
    ├── ForgotPasswordPage.tsx
    └── ResetPasswordPage.tsx
```

This keeps all concerns for a domain (types, API calls, UI, mutations, feedback) co-located and easy to find.

### Separation of Concerns

| Layer | Responsibility |
|---|---|
| `*-api.ts` | Raw API calls, request/response types, enum definitions |
| `use-*.ts` hooks | TanStack Query `useMutation` wrappers — own side-effects (toasts, invalidations) |
| Page components | Compose the table, forms, and dialogs; manage open/close state |
| `components/ui/` | Pure presentational primitives with zero business logic |

---

## Routing

Routing is declared in `src/App.tsx` using `createBrowserRouter` from React Router v7.

### Public Routes (no auth required)

| Path | Component | Description |
|---|---|---|
| `/` | Redirect | Redirects to `/login` |
| `/login` | `Login` | Email + password login |
| `/register` | `RegisterPage` | New account registration |
| `/forgot-password` | `ForgotPasswordPage` | Request password reset email |
| `/reset-password` | `ResetPasswordPage` | Submit new password with token |

### Protected Routes (under `MainLayout`)

All protected routes are children of the `MainLayout` element. `MainLayout` renders the authenticated shell (Sidebar + TopBar + SignalR connection).

| Path | Component | Description |
|---|---|---|
| `/dashboard` | `DashboardPage` | Workspace overview and key metrics |
| `/users` | `UserPage` | Admin: manage staff |
| `/subscription-plans` | `SubscriptionPlansPage` | Admin: manage subscription plans |
| `/settings` | `SettingPage` | User profile, preferences, language |

> **Note:** There is currently no client-side route guard. Authentication is enforced by the backend returning `401` responses, which the Axios interceptor handles by attempting a token refresh — redirecting to `/login` if the refresh also fails.

---

## Authentication & Token Management

The auth flow uses a **dual-token strategy**:

- **Access Token** — short-lived JWT stored **in memory only** (never in `localStorage`). This prevents XSS attacks from reading the token.
- **Refresh Token** — longer-lived token stored in an **httpOnly cookie** set by the backend. JavaScript cannot read this cookie, protecting it from XSS.

### Flow

```
User logs in
    │
    ▼
POST /auth/login ──► Backend sets httpOnly refresh-token cookie
                 ──► Returns access token in response body
                 ──► tokenManager.setAccessToken(token)
    │
    ▼
Every protected API request
    │
    ├── Axios request interceptor reads token from tokenManager
    ├── Attaches: Authorization: Bearer <token>
    │
    ▼
Response is 401?
    │
    ├── Yes ──► tokenManager.refreshAccessToken()
    │               POST /auth/refresh-token  (cookie sent automatically)
    │               New access token stored in memory
    │               Original request retried with new token
    │
    │           If refresh also fails (401) ──► redirect to /login
    │
    └── No ──► Normal response returned to caller
```

### Concurrent Refresh Queueing

`tokenManager.ts` implements a **subscriber queue** for concurrent 401s. If multiple requests fail at the same time, only one refresh call is made. All queued requests are resolved (or rejected together) once the single refresh completes.

### Key Files

- `src/lib/api/api.ts` — Axios instance creation
- `src/lib/api/tokenManager.ts` — Token memory store + both request/response interceptors
- `src/lib/api/auth.ts` — `authApi` object (login, logout, refresh, forgotPassword, resetPassword)

---

## API Layer

### Axios Instance (`src/lib/api/api.ts`)

```ts
const api = axios.create({
  baseURL: import.meta.env.VITE_API_URL,  // e.g. http://localhost:5088/api/v1
  timeout: 10000,
  withCredentials: true,                   // sends httpOnly cookies on every request
  headers: { 'Content-Type': 'application/json' },
});
```

### Skipping Auth for Public Endpoints

Any request that should not trigger the refresh logic (e.g., login, the refresh call itself) passes a custom Axios config option:

```ts
api.post('/auth/login', data, { skipAuthRefresh: true })
```

This flag is checked in both the request and response interceptors and is declared in `src/lib/api/axios.d.ts` to keep TypeScript happy.

### Feature API Files

Each feature has its own `*-api.ts` file that imports the `api` instance and exports typed async functions:

```ts
// user-api.ts
export const userApi = {
  getAll: async (request: TableRequest): Promise<PagedList<User>> => {
    const params = getTableRequestParams(request);
    const response = await api.get<PagedList<User>>('/users', { params });
    return response.data;
  },
  // create, update, delete ...
};
```

---

## State Management

This project uses **TanStack Query (React Query) v5** as the single source of truth for all **server state**. There is no global client-state store (no Redux, Zustand, etc.).

```
useQuery      — READ  (GET requests, cached, background refetch)
useMutation   — WRITE (POST / PUT / DELETE, with onSuccess / onError callbacks)
```

After a successful mutation the relevant queries are **invalidated** so the UI automatically refetches fresh data:

```ts
const mutation = useMutation({
  mutationFn: userApi.deleteUser,
  onSuccess: () => {
    queryClient.invalidateQueries({ queryKey: ['users'] });
    toast.success('User deleted');
  },
});
```

The single `QueryClient` instance is created in `App.tsx` and provided via `<QueryClientProvider>`.

---

## Feature Modules

### Auth (`src/features/auth/`)

Pages: Login, Register, ForgotPassword, ResetPassword. All are standalone full-screen layouts (no sidebar). Forms use React Hook Form + Zod. On successful login the access token is stored by `tokenManager` and the user navigates to `/dashboard`.

### Dashboard (`src/features/dashboard/`)

Provides a workspace overview with key information and quick-action buttons. The dashboard can be customized to display relevant business metrics based on the authenticated user's role and subscription status.

### Users (`src/features/users/`)

Admin-only feature for managing staff. Display team members, their roles, and permissions with Create, Edit, and Delete operations.

### Subscriptions (`src/features/subscriptions/`)

Manages subscription plans, payment processing (Chargily integration), and subscription lifecycle (activate, renew, cancel).

### Notifications (`src/features/notifications/`)

Real-time notification dropdown in the top navigation. Shows an unread count badge on the bell icon. Clicking a notification marks it as read via `useMutation`. New notifications arrive over the SignalR connection.

### Settings (`src/features/settings/`)

Tabbed settings page with sections:
1. **User Profile** — name, email, language preference
2. **Notifications** — push notification preferences
3. **Subscriptions** — current plan and billing details

Schemas live in the co-located `schemas.ts` file.

---

## Shared Components

All shared UI components live in `src/components/ui/` and follow the **shadcn/ui** pattern — thin wrappers around Radix UI primitives styled with Tailwind. They contain zero business logic and can be used freely across all features.

| Component | Description |
|---|---|
| `Button` | Variants: default, ghost, destructive, outline |
| `Card` | Content card with `CardHeader`, `CardContent`, `CardFooter` |
| `Dialog` / `Sheet` | Modal dialogs and slide-over panels |
| `Form` | React Hook Form-integrated field components |
| `Input` / `Textarea` | Styled text inputs |
| `Select` | Radix-based accessible dropdown |
| `Tabs` | Tab navigation |
| `Badge` | Status / label pill |
| `Avatar` | User avatar with fallback initials |
| `Skeleton` | Loading placeholder |
| `Separator` | Horizontal / vertical divider |
| `Switch` | Toggle switch |
| `Checkbox` | Accessible checkbox |
| `confirm-delete-dialog.tsx` | Reusable "are you sure?" alert dialog used by all delete actions |
| `sonner.tsx` | `<Toaster>` component, mounted once in `App.tsx` |

---

## Data Tables

`src/components/tables/` provides a **generic, reusable server-side data table system** built on TanStack Table v8.

All table operations (pagination, sorting, search) happen **on the server**. The frontend sends a `TableRequest` and receives a `PagedList<T>`.

```ts
// Shape sent to every paginated endpoint
interface TableRequest {
  page: number;
  pageSize: number;
  search?: string;
  sortColumn?: string;
  sortOrder?: 'asc' | 'desc';
}

// Shape returned from every paginated endpoint
interface PagedList<T> {
  item: T[];
  totalCount: number;
  pageSize: number;
  page: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}
```

### Key Files

| File | Purpose |
|---|---|
| `types.ts` | All table-related TypeScript types |
| `data-table.tsx` | Main `<DataTable>` component |
| `data-table-pagination.tsx` | Prev / Next / page-size controls |
| `data-table-search.tsx` | Debounced search input |
| `data-table-column-header.tsx` | Sortable column header |
| `data-table-row-actions.tsx` | Per-row action dropdown (View / Edit / Delete) |
| `data-table-skeleton.tsx` | Loading state placeholder rows |
| `use-table-query.ts` | Hook that manages `TableRequest` state (page, search, sort) |
| `cell-renderers.tsx` | Reusable cell formatter components (date, badge, etc.) |

### Usage Pattern

```tsx
// 1. Define columns with types
const columns: DataTableColumn<User>[] = [
  { accessorKey: 'name', header: 'Name', enableServerSorting: true },
];

// 2. Manage table state
const { tableRequest, setPage, setSearch, setSorting } = useTableQuery();

// 3. Fetch data
const { data, isLoading } = useQuery({
  queryKey: ['users', tableRequest],
  queryFn: () => userApi.getAll(tableRequest),
});

// 4. Render
<DataTable columns={columns} data={data?.item ?? []} isLoading={isLoading} ... />
```

---

## Real-Time Notifications (SignalR)

The app maintains a persistent WebSocket connection to the backend for real-time push notifications.

### Setup

`SignalRProvider` (`src/lib/signalr/signalr-context.tsx`) is mounted inside `MainLayout`, so the connection is active for authenticated pages only.

```
MainLayout
 └── SignalRProvider          ← Establishes HubConnection on mount
      └── Layout
           └── <Outlet />     ← Authenticated page content
```

### Connection Details

| Property | Value |
|---|---|
| Hub URL | `/hubs/notification` (appended to `VITE_API_URL` base) |
| Auth | Access token passed via `accessTokenFactory` |
| Auto-reconnect | 2s → 10s → 30s |
| Log level | `Warning` (keeps console clean) |

### Event: `ReceiveNotification`

When this server event fires:
1. `queryClient.invalidateQueries({ queryKey: ['notifications', 'unread'] })` — the bell badge count refreshes immediately.
2. A `sonner` toast appears with the notification title and body.

### Consuming the Connection in Components

```ts
import { useSignalR } from '@/lib/signalr/use-signalr';

const connection = useSignalR(); // returns HubConnection | null
```

---

## Internationalization (i18n)

Three languages are supported:

| Code | Language | Direction |
|---|---|---|
| `en` | English | LTR |
| `fr` | French | LTR |
| `ar` | Arabic | RTL |

`i18next` is initialized in `src/lib/i18n/index.ts` and imported in `src/main.tsx` before the React tree renders. All translation JSON files are bundled at build time (no runtime fetch needed).

### Key Container

All translation keys are declared as constants in `src/lib/i18n/keyContainer.ts`. Always use the key container — never hardcode translation keys as strings:

```ts
const { t } = useTranslation();

t(i18nKeyContainer.dashboard);  // ✅ type-safe
t('dashboard');                  // ❌ avoid — prone to typos
```

### RTL Support

When Arabic is active, layout-aware components read `i18n.language === 'ar'` and toggle RTL Tailwind classes:

```tsx
const isRtl = i18n.language === 'ar';
<div className={cn('start-0', isRtl && 'end-0 lg:end-auto lg:start-0')} />
```

Language preference is saved to the backend via the Settings page so it persists across sessions.

---

## Styling

- **Tailwind CSS v4** with the `@tailwindcss/vite` plugin. No separate config file is needed — Tailwind v4 auto-scans source files.
- The **`@` path alias** points to `src/`, configured in `vite.config.ts`. Use `@/components/ui/button` instead of relative paths.
- The **`cn()` utility** in `src/lib/utils.ts` combines `clsx` and `tailwind-merge`:

```ts
cn('px-4 py-2', isActive && 'bg-blue-500', 'px-6')
// Result: 'py-2 bg-blue-500 px-6'  (tailwind-merge resolves the px conflict)
```

- Color system documentation: `docs/COLOR-SYSTEM.md`.
- `tw-animate-css` provides Tailwind-compatible CSS animation utilities.

---

## Form Validation

All forms use **React Hook Form + Zod**:

```ts
// 1. Define a schema
const schema = z.object({
  name: z.string().min(1, 'Name is required'),
  email: z.string().email('Invalid email'),
});

// 2. Infer the TypeScript type
type FormData = z.infer<typeof schema>;

// 3. Create the form
const form = useForm<FormData>({
  resolver: zodResolver(schema),
  defaultValues: { name: '', email: '' },
});
```

For complex pages (e.g., Settings) schemas are extracted to a dedicated `schemas.ts` file within the feature folder.

---

## Error Handling

### Backend Error Contract

The backend uses **RFC 7807 ProblemDetails**. All error shapes are typed in `src/lib/api/error-types.ts`:

```ts
interface ProblemDetails {
  type: string;
  title: string;   // error code e.g. "User.UserNotFound"
  status: number;
  errors?: [string, string]; // [errorCode, errorDescription]
}
```

A large `ErrorCodes` constant object maps every known backend error code string to a named constant, making conditional error handling safe and refactor-friendly.

### Feature-Level Toast Mapping

Each feature has a `use-*-toast.ts` file that maps backend error codes to translated user-facing messages:

```ts
// use-user-toast.ts
export function useUserToast() {
  const { t } = useTranslation();
  return {
    onError: (error: unknown) => {
      const parsed = parseApiError(error);
      if (parsed.code === ErrorCodes.USER_NOT_FOUND) {
        toast.error(t(i18nKeyContainer.userNotFound));
      } else {
        toast.error(t(i18nKeyContainer.genericError));
      }
    },
  };
}
```

---

## Build & Deployment

### Production Build

```bash
pnpm build
# TypeScript compiled first (tsc -b), then Vite bundles to dist/
```

### Vercel Deployment

`vercel.json` includes a catch-all rewrite to support client-side routing:

```json
{ "rewrites": [{ "source": "/(.*)", "destination": "/index.html" }] }
```

Set `VITE_API_URL` in your Vercel project environment settings to point to the production backend.

### Local Backend (Docker)

See `backend/docker-compose.yml` to run the backend locally. Ensure `VITE_API_URL` in `.env.local` targets the correct port (default: `http://localhost:5088/api/v1`).

---

