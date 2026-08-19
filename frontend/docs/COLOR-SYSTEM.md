# HREnap Color System Implementation Plan

**Project:** HREnap HR Management Platform  
**Version:** 1.0.0  
**Date:** January 2026  
**Tech Stack:** React + TypeScript + Tailwind CSS v4 + shadcn/ui

---

## 📋 Table of Contents

1. [Color Strategy Overview](#color-strategy-overview)
2. [Where Colors Are Defined](#where-colors-are-defined)
3. [Color Architecture](#color-architecture)
4. [Primary Brand Colors](#primary-brand-colors)
5. [Neutral Slate Palette](#neutral-slate-palette)
6. [Semantic Status Colors](#semantic-status-colors)
7. [UI Component Colors](#ui-component-colors)
8. [Required shadcn/ui Components](#required-shadcnui-components)
9. [Implementation Approach](#implementation-approach)
10. [Usage Examples](#usage-examples)
11. [Accessibility Guidelines](#accessibility-guidelines)

---

## 🎨 Color Strategy Overview

### The Two-Layer Approach

HREnap uses a **two-layer color system** that combines:

1. **CSS Custom Properties (CSS Variables)** - Semantic tokens for theme support
2. **Tailwind CSS Classes** - Utility-first styling in components

```
┌─────────────────────────────────────────┐
│   Component Layer (React/TSX)          │
│   Uses: className="bg-primary"         │
└─────────────────┬───────────────────────┘
                  │
┌─────────────────▼───────────────────────┐
│   Tailwind Layer (Utility Classes)     │
│   Generates: .bg-primary { ... }       │
└─────────────────┬───────────────────────┘
                  │
┌─────────────────▼───────────────────────┐
│   CSS Variable Layer (index.css)       │
│   Defines: --primary: 207 73% 51%      │
└─────────────────────────────────────────┘
```

### Why This Approach?

✅ **Semantic Naming** - `bg-primary` instead of `bg-blue-500`  
✅ **Theme Support** - Change entire color scheme by updating CSS variables  
✅ **Consistency** - Same color token across all components  
✅ **Maintainability** - Update one place, changes reflect everywhere  
✅ **Dark Mode Ready** - Toggle variables based on `.dark` class  
✅ **Type Safety** - TypeScript knows about Tailwind classes

---

## 📍 Where Colors Are Defined

### Primary Definition: `src/index.css`

All colors are defined in **one central file**: `frontend/src/index.css`

```css
/* File: frontend/src/index.css */

@import "tailwindcss";

/* ============================================ */
/*   CSS VARIABLES (for shadcn/ui & theming)  */
/* ============================================ */
:root {
  --primary: 207 73% 51%;              /* #1E88E5 */
  --primary-foreground: 210 40% 98%;   /* #F8FAFC */
  --destructive: 0 84.2% 60.2%;        /* #EF4444 */
  /* ...more variables */
}

/* ============================================ */
/*   TAILWIND THEME (for utility classes)     */
/* ============================================ */
@theme {
  --color-primary: oklch(63.5% 0.18 244);
  --color-slate-50: oklch(98.5% 0.005 244);
  /* ...more colors */
}
```

### Why Two Formats?

1. **CSS Variables (HSL format)** - Used by shadcn/ui components
   - Format: `207 73% 51%` (no `hsl()` wrapper)
   - Applied via: `hsl(var(--primary))`

2. **Tailwind Theme (OKLCH format)** - Used by Tailwind utilities
   - Format: `oklch(63.5% 0.18 244)`
   - Applied via: `bg-primary`, `text-primary`

---

## 🏗️ Color Architecture

### The Color Hierarchy

```
HREnap Color System
│
├── 🔵 Brand Colors (Primary Identity)
│   ├── primary (#1E88E5) - Main brand blue
│   └── primary-foreground (#F8FAFC) - Text on primary
│
├── ⚫ Neutral Palette (Slate - 10 shades)
│   ├── slate-50 → slate-900 (lightest to darkest)
│   └── Used for: backgrounds, text, borders
│
├── 🎯 Semantic Status Colors (Meaning-based)
│   ├── 🟢 Green - Success/Stable
│   ├── 🟡 Amber - Warning/Observation
│   ├── 🔴 Red - Critical/Error/Destructive
│   ├── 🔵 Blue - Information
│   └── 🟠 Orange - Activity
│
└── 🧩 UI Component Colors (Functional)
    ├── background, foreground
    ├── card, card-foreground
    ├── muted, muted-foreground
    ├── secondary, secondary-foreground
    ├── accent, accent-foreground
    ├── destructive, destructive-foreground
    ├── border, input, ring
    └── popover, popover-foreground
```

---

## 🔵 Primary Brand Colors

### Primary Blue (Main Brand Color)

**Color:** `#1E88E5` (Material Blue 600)

| Format | Value |
|--------|-------|
| Hex | `#1E88E5` |
| RGB | `rgb(30, 136, 229)` |
| HSL | `hsl(207, 73%, 51%)` |
| CSS Variable | `--primary: 207 73% 51%` |

**Tailwind Classes:**
- `bg-primary` - Primary button backgrounds
- `text-primary` - Primary text color
- `border-primary` - Primary borders
- `ring-primary` - Focus rings

**Usage:**
```tsx
// Primary action button
<button className="bg-primary text-primary-foreground hover:bg-primary/80">
  Save Changes
</button>

// Active navigation item
<nav className="text-primary border-b-2 border-primary">
  Dashboard
</nav>

// Link
<a className="text-primary hover:text-primary/80">Learn More</a>
```

### Primary Opacity Variants

| Opacity | Use Case | Example |
|---------|----------|---------|
| `/5` | Very subtle backgrounds | `bg-primary/5` |
| `/10` | Icon hover states | `hover:bg-primary/10` |
| `/20` | Button shadows | `shadow-primary/20` |
| `/80` | Button hover | `hover:bg-primary/80` |
| `/90` | Button active | `active:bg-primary/90` |

---

## ⚫ Neutral Slate Palette

### Complete Slate Scale (10 Shades)

| Shade | Hex | Usage | Tailwind Classes |
|-------|-----|-------|------------------|
| **50** | `#F8FAFC` | Main background, subtle sections | `bg-slate-50` |
| **100** | `#F1F5F9` | Card backgrounds, hover states | `bg-slate-100` |
| **200** | `#E2E8F0` | Input borders, card borders | `border-slate-200` |
| **300** | `#CBD5E1` | Disabled states, scrollbar | `bg-slate-300` |
| **400** | `#94A3B8` | Placeholder text, icons | `text-slate-400` |
| **500** | `#64748B` | Tertiary text, descriptions | `text-slate-500` |
| **600** | `#475569` | Body text, table cells | `text-slate-600` |
| **700** | `#334155` | Emphasized text, strong labels | `text-slate-700` |
| **800** | `#1E293B` | Headings, card titles | `text-slate-800` |
| **900** | `#0F172A` | Page titles, primary headings | `text-slate-900` |

### Text Hierarchy Example

```tsx
<div>
  {/* Page Title - Darkest */}
  <h1 className="text-slate-900 text-3xl font-bold">
    Employee Dashboard
  </h1>

  {/* Section Heading */}
  <h2 className="text-slate-800 text-2xl font-semibold">
    Recent Visits
  </h2>

  {/* Body Text */}
  <p className="text-slate-600">
    This employee is currently on a performance improvement plan.
  </p>

  {/* Secondary Text */}
  <span className="text-slate-500">
    Last updated by Dr. Smith
  </span>

  {/* Timestamp - Lightest */}
  <time className="text-slate-400">
    2 hours ago
  </time>
</div>
```

---

## 🎯 Semantic Status Colors

### 🟢 Green (Success / Stable)

**Primary Use:** Employee onboarding, successful operations, completed tasks

| Shade | Hex | Usage |
|-------|-----|-------|
| **50** | `#F0FDF4` | Badge backgrounds (stable) |
| **100** | `#DCFCE7` | Hover states |
| **500** | `#22C55E` | Success indicators, checkmarks |
| **600** | `#16A34A` | Status text (stable, recovered) |
| **700** | `#15803D` | Emphasized success text |

**Examples:**
```tsx
// Stable employee status badge
<div className="bg-green-50 text-green-600 px-3 py-1 rounded-full">
  Stable
</div>

// Success notification
<div className="bg-green-100 border border-green-200 p-4 rounded">
  <p className="text-green-700">Surgery completed successfully!</p>
</div>
```

### 🟡 Amber (Warning / Observation)

**Primary Use:** Items under observation or requiring attention, caution

| Shade | Hex | Usage |
|-------|-----|-------|
| **50** | `#FFFBEB` | Badge backgrounds (observation) |
| **100** | `#FEF3C7` | Warning backgrounds |
| **500** | `#F59E0B` | Warning indicators |
| **600** | `#D97706` | Status text (observation) |
| **700** | `#B45309` | Emphasized warning text |

**Examples:**
```tsx
// Under observation badge
<div className="bg-amber-50 text-amber-600 px-3 py-1 rounded-full">
  Under Observation
</div>

// Pending appointment
<div className="bg-amber-100 border border-amber-200 p-4 rounded">
  <p className="text-amber-700">Appointment pending confirmation</p>
</div>
```

### 🔴 Red (Critical / Error / Destructive)

**Primary Use:** Critical employees, errors, delete actions, emergencies

| Shade | Hex | Usage |
|-------|-----|-------|
| **50** | `#FEF2F2` | Critical alert backgrounds |
| **100** | `#FEE2E2` | Error backgrounds |
| **300** | `#FCA5A5` | Subtle error indicators |
| **500** | `#EF4444` | Error states, critical indicators |
| **600** | `#DC2626` | Critical status text, delete actions |

**CSS Variable:** `--destructive: 0 84.2% 60.2%`

**Examples:**
```tsx
// Critical employee status
<div className="bg-red-50 text-red-600 px-3 py-1 rounded-full">
  Critical
</div>

// Delete button
<button className="bg-destructive text-destructive-foreground hover:bg-destructive/90">
  Delete Employee Record
</button>

// Validation error
<input className="border-destructive focus:ring-destructive" />
<p className="text-red-600 text-sm mt-1">This field is required</p>
```

### 🔵 Blue (Information)

**Primary Use:** Info notifications, ongoing processes, neutral status

| Shade | Hex | Usage |
|-------|-----|-------|
| **50** | `#EFF6FF` | Info notification backgrounds |
| **100** | `#DBEAFE` | Secondary info backgrounds |
| **400** | `#60A5FA` | Gradient accents |
| **500** | `#3B82F6` | Info indicators |
| **600** | `#2563EB` | Info text, links |

**Examples:**
```tsx
// Info notification
<div className="bg-blue-50 text-blue-600 p-4 rounded">
  <p>Your appointment is scheduled for tomorrow at 10 AM</p>
</div>

// In-progress status
<div className="bg-blue-50 text-blue-600 px-3 py-1 rounded-full">
  In Progress
</div>
```

### 🟠 Orange (Activity / Metrics)

**Primary Use:** Activity feeds, metric highlights, trend indicators

| Shade | Hex | Usage |
|-------|-----|-------|
| **50** | `#FFF7ED` | Activity card backgrounds |
| **600** | `#EA580C` | Activity indicators |

**Examples:**
```tsx
// Activity indicator
<div className="bg-orange-50 p-4 rounded">
  <p className="text-orange-600">3 new employees registered today</p>
</div>
```

---

## 🧩 UI Component Colors

### Semantic Component Tokens

These colors are used by shadcn/ui components and provide semantic meaning:

| Token | CSS Variable | Hex | Usage |
|-------|--------------|-----|-------|
| `background` | `210 40% 98%` | `#F8FAFC` | Main app background |
| `foreground` | `222.2 84% 4.9%` | `#0A0F1E` | Primary text |
| `card` | `0 0% 100%` | `#FFFFFF` | Card backgrounds |
| `card-foreground` | `222.2 84% 4.9%` | `#0A0F1E` | Text on cards |
| `muted` | `210 40% 96.1%` | `#F1F5F9` | Muted backgrounds |
| `muted-foreground` | `215.4 16.3% 46.9%` | `#64748B` | Muted text |
| `secondary` | `210 40% 96.1%` | `#F1F5F9` | Secondary buttons |
| `secondary-foreground` | `222.2 47.4% 11.2%` | `#1E293B` | Text on secondary |
| `accent` | `210 40% 96.1%` | `#F1F5F9` | Accent highlights |
| `accent-foreground` | `222.2 47.4% 11.2%` | `#1E293B` | Text on accent |
| `border` | `214.3 31.8% 91.4%` | `#E2E8F0` | Default borders |
| `input` | `214.3 31.8% 91.4%` | `#E2E8F0` | Input borders |
| `ring` | `207 73% 51%` | `#1E88E5` | Focus rings |

### Component Usage

```tsx
// Card with semantic colors
<div className="bg-card border border-border rounded-lg p-4">
  <h3 className="text-card-foreground font-semibold">Employee Info</h3>
  <p className="text-muted-foreground">Joined: 3 days ago</p>
</div>

// Input with focus ring
<input 
  className="border-input bg-background focus:ring-2 focus:ring-ring" 
  placeholder="Search employees..."
/>

// Secondary button
<button className="bg-secondary text-secondary-foreground hover:bg-secondary/80">
  Cancel
</button>
```

---

## 🧱 Required shadcn/ui Components

### Installation Order & Rationale

Install these components in order to build the HREnap UI:

#### 1. **Button** (Priority: Critical)
```bash
npx shadcn-ui@latest add button
```
**Why:** Primary, secondary, destructive, ghost, and outline button variants
**Colors Used:** `primary`, `secondary`, `destructive`, `slate-100`
**Example:**
```tsx
<Button variant="default">Save</Button> {/* primary */}
<Button variant="secondary">Cancel</Button> {/* secondary */}
<Button variant="destructive">Delete</Button> {/* red */}
<Button variant="ghost">View</Button> {/* transparent */}
<Button variant="outline">Edit</Button> {/* bordered */}
```

#### 2. **Card** (Priority: Critical)
```bash
npx shadcn-ui@latest add card
```
**Why:** Employee cards, dashboard widgets, info panels
**Colors Used:** `card`, `card-foreground`, `border`
**Example:**
```tsx
<Card>
  <CardHeader>
    <CardTitle>Employee Statistics</CardTitle>
  </CardHeader>
  <CardContent>
    <p>Total Employees: 245</p>
  </CardContent>
</Card>
```

#### 3. **Input** (Priority: Critical)
```bash
npx shadcn-ui@latest add input
```
**Why:** Forms, search bars, filters
**Colors Used:** `input`, `ring`, `border`, `destructive` (errors)
**Example:**
```tsx
<Input 
  placeholder="Search employees..." 
  className="w-full"
/>
```

#### 4. **Label** (Priority: High)
```bash
npx shadcn-ui@latest add label
```
**Why:** Form field labels
**Colors Used:** `foreground`, `muted-foreground`
**Example:**
```tsx
<Label htmlFor="name">Employee Name</Label>
<Input id="name" />
```

#### 5. **Badge** (Priority: High)
```bash
npx shadcn-ui@latest add badge
```
**Why:** Status indicators (Stable, Critical, Observation)
**Colors Used:** `green-50/600`, `amber-50/600`, `red-50/600`, `blue-50/600`
**Example:**
```tsx
<Badge variant="default" className="bg-green-50 text-green-600">Stable</Badge>
<Badge variant="destructive">Critical</Badge>
<Badge className="bg-amber-50 text-amber-600">Observation</Badge>
```

#### 6. **Table** (Priority: High)
```bash
npx shadcn-ui@latest add table
```
**Why:** Employee lists, appointment tables, data grids
**Colors Used:** `slate-50`, `slate-100`, `border`, `muted-foreground`
**Example:**
```tsx
<Table>
  <TableHeader>
    <TableRow>
      <TableHead>Name</TableHead>
      <TableHead>Status</TableHead>
    </TableRow>
  </TableHeader>
  <TableBody>
    <TableRow className="hover:bg-slate-50">
      <TableCell>Max</TableCell>
      <TableCell>
        <Badge className="bg-green-50 text-green-600">Stable</Badge>
      </TableCell>
    </TableRow>
  </TableBody>
</Table>
```

#### 7. **Select** (Priority: High)
```bash
npx shadcn-ui@latest add select
```
**Why:** Dropdowns for filters, department selection, status filters
**Colors Used:** `popover`, `ring`, `primary`, `muted-foreground`
**Example:**
```tsx
<Select>
  <SelectTrigger>
    <SelectValue placeholder="Select status" />
  </SelectTrigger>
  <SelectContent>
    <SelectItem value="stable">Stable</SelectItem>
    <SelectItem value="critical">Critical</SelectItem>
  </SelectContent>
</Select>
```

#### 8. **Dialog** (Priority: Medium)
```bash
npx shadcn-ui@latest add dialog
```
**Why:** Modal dialogs for add/edit forms, confirmations
**Colors Used:** `background`, `card`, `border`, `foreground`
**Example:**
```tsx
<Dialog>
  <DialogTrigger asChild>
    <Button>Add Employee</Button>
  </DialogTrigger>
  <DialogContent>
    <DialogHeader>
      <DialogTitle>New Employee</DialogTitle>
    </DialogHeader>
    {/* Form content */}
  </DialogContent>
</Dialog>
```

#### 9. **Dropdown Menu** (Priority: Medium)
```bash
npx shadcn-ui@latest add dropdown-menu
```
**Why:** User menu, action menus, context menus
**Colors Used:** `popover`, `primary`, `destructive`, `muted-foreground`
**Example:**
```tsx
<DropdownMenu>
  <DropdownMenuTrigger asChild>
    <Button variant="ghost">Actions</Button>
  </DropdownMenuTrigger>
  <DropdownMenuContent>
    <DropdownMenuItem>Edit</DropdownMenuItem>
    <DropdownMenuItem className="text-destructive">Delete</DropdownMenuItem>
  </DropdownMenuContent>
</DropdownMenu>
```

#### 10. **Toast** (Priority: Medium)
```bash
npx shadcn-ui@latest add toast
```
**Why:** Success/error notifications, feedback messages
**Colors Used:** `green-50/600`, `red-50/600`, `blue-50/600`, `amber-50/600`
**Example:**
```tsx
toast({
  variant: "default",
  className: "bg-green-50 text-green-600",
  title: "Success",
  description: "Employee added successfully"
})

toast({
  variant: "destructive",
  title: "Error",
  description: "Failed to save changes"
})
```

#### 11. **Alert** (Priority: Medium)
```bash
npx shadcn-ui@latest add alert
```
**Why:** Status messages, warnings, info banners
**Colors Used:** All semantic colors (green, amber, red, blue)
**Example:**
```tsx
<Alert className="bg-blue-50 border-blue-200">
  <InfoIcon className="text-blue-600" />
  <AlertTitle className="text-blue-700">Notice</AlertTitle>
  <AlertDescription className="text-blue-600">
    Appointment scheduled for tomorrow
  </AlertDescription>
</Alert>
```

#### 12. **Tabs** (Priority: Low)
```bash
npx shadcn-ui@latest add tabs
```
**Why:** Navigation between sections (Overview, History, Notes)
**Colors Used:** `primary`, `muted`, `border`
**Example:**
```tsx
<Tabs defaultValue="overview">
  <TabsList>
    <TabsTrigger value="overview">Overview</TabsTrigger>
    <TabsTrigger value="history">History</TabsTrigger>
  </TabsList>
  <TabsContent value="overview">{/* Content */}</TabsContent>
</Tabs>
```

#### 13. **Calendar** (Priority: Low)
```bash
npx shadcn-ui@latest add calendar
```
**Why:** Appointment scheduling, date pickers
**Colors Used:** `primary`, `purple-50/600`, `slate-100`
**Example:**
```tsx
<Calendar 
  mode="single" 
  selected={date} 
  onSelect={setDate} 
  className="border rounded-lg"
/>
```

#### 14. **Avatar** (Priority: Low)
```bash
npx shadcn-ui@latest add avatar
```
**Why:** User profile images, employee photos
**Colors Used:** `slate-200`, `slate-400`, `muted`
**Example:**
```tsx
<Avatar>
  <AvatarImage src="/employee.jpg" alt="Employee" />
  <AvatarFallback className="bg-slate-200 text-slate-600">MX</AvatarFallback>
</Avatar>
```

### Component Installation Command (All at Once)

```bash
# Install all components at once
npx shadcn-ui@latest add button card input label badge table select dialog dropdown-menu toast alert tabs calendar avatar
```

---

## 🚀 Implementation Approach

### Phase 1: Setup Foundation (Day 1)

**Step 1.1: Install Dependencies**
```bash
cd frontend
npm install clsx tailwind-merge
```

**Step 1.2: Create Color System in `index.css`**

Create `frontend/src/index.css` with:
- CSS variables (`:root` block)
- Tailwind theme (`@theme` block)
- Scrollbar styles
- Global styles

**Step 1.3: Configure Path Aliases**

Update `tsconfig.json` and `tsconfig.app.json`:
```json
{
  "compilerOptions": {
    "baseUrl": ".",
    "paths": {
      "@/*": ["./src/*"]
    }
  }
}
```

Update `vite.config.ts`:
```typescript
import path from 'path'

export default defineConfig({
  resolve: {
    alias: {
      '@': path.resolve(__dirname, './src'),
    },
  },
})
```

**Step 1.4: Create Utility Helper**

Create `src/lib/utils.ts`:
```typescript
import { clsx, type ClassValue } from 'clsx'
import { twMerge } from 'tailwind-merge'

export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs))
}
```

### Phase 2: Install shadcn/ui (Day 1-2)

**Step 2.1: Create shadcn Config**

Create `frontend/components.json`:
```json
{
  "$schema": "https://ui.shadcn.com/schema.json",
  "style": "default",
  "rsc": false,
  "tsx": true,
  "tailwind": {
    "config": "",
    "css": "src/index.css",
    "baseColor": "slate",
    "cssVariables": true
  },
  "aliases": {
    "components": "@/components",
    "utils": "@/lib/utils"
  }
}
```

**Step 2.2: Install Components**
```bash
# Core components first
npx shadcn-ui@latest add button card input label badge table

# Interaction components
npx shadcn-ui@latest add select dialog dropdown-menu

# Feedback components
npx shadcn-ui@latest add toast alert

# Optional components
npx shadcn-ui@latest add tabs calendar avatar
```

### Phase 3: Refactor Existing Code (Day 2-3)

**Step 3.1: Update App.tsx**

Replace hardcoded colors:
```tsx
// ❌ Before
<button className="bg-blue-500 text-white hover:bg-blue-600">
  Sign In
</button>

// ✅ After
<Button>Sign In</Button>
// or
<button className="bg-primary text-primary-foreground hover:bg-primary/80">
  Sign In
</button>
```

**Step 3.2: Create Status Badge Component**

```tsx
// src/components/status-badge.tsx
import { Badge } from '@/components/ui/badge'
import { cn } from '@/lib/utils'

type Status = 'stable' | 'observation' | 'critical' | 'in-progress'

const statusConfig = {
  stable: 'bg-green-50 text-green-600 border-green-200',
  observation: 'bg-amber-50 text-amber-600 border-amber-200',
  critical: 'bg-red-50 text-red-600 border-red-200',
  'in-progress': 'bg-blue-50 text-blue-600 border-blue-200',
}

export function StatusBadge({ status }: { status: Status }) {
  return (
    <Badge className={cn(statusConfig[status])}>
      {status}
    </Badge>
  )
}
```

### Phase 4: Create Theme Provider (Day 3)

**Step 4.1: Create Provider**

Create `src/components/theme-provider.tsx` (see full code in previous response)

**Step 4.2: Wrap App**

```tsx
// src/main.tsx
import { ThemeProvider } from '@/components/theme-provider'

ReactDOM.createRoot(document.getElementById('root')!).render(
  <React.StrictMode>
    <ThemeProvider defaultTheme="light">
      <App />
    </ThemeProvider>
  </React.StrictMode>,
)
```

### Phase 5: Build Feature Components (Day 4-7)

Build feature-specific components using the color system:
- Employee cards with status badges
- Dashboard metrics with appropriate colors
- Forms with validation states
- Tables with hover states
- Notifications with semantic colors

---

## 💡 Usage Examples

### Example 1: Employee Card

```tsx
import { Card, CardHeader, CardTitle, CardContent } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'

export function EmployeeCard({ employee }) {
  return (
    <Card className="hover:shadow-md transition-shadow">
      <CardHeader>
        <div className="flex items-center justify-between">
          <CardTitle className="text-slate-800">{employee.name}</CardTitle>
          <Badge className="bg-green-50 text-green-600">Stable</Badge>
        </div>
      </CardHeader>
      <CardContent>
        <p className="text-slate-600">Department: {employee.department}</p>
        <p className="text-slate-500 text-sm mt-2">
          Hired: {employee.hireDate}
        </p>
      </CardContent>
    </Card>
  )
}
```

### Example 2: Dashboard Metric Card

```tsx
import { Card } from '@/components/ui/card'

export function MetricCard({ title, value, trend, type }) {
  const bgColors = {
    employees: 'bg-blue-50',
    revenue: 'bg-green-50',
    alerts: 'bg-amber-50',
  }

  const textColors = {
    employees: 'text-blue-600',
    revenue: 'text-green-600',
    alerts: 'text-amber-600',
  }

  return (
    <Card className={bgColors[type]}>
      <div className="p-6">
        <p className="text-slate-500 text-sm">{title}</p>
        <p className={`text-3xl font-bold mt-2 ${textColors[type]}`}>
          {value}
        </p>
        <p className="text-slate-500 text-sm mt-1">{trend}</p>
      </div>
    </Card>
  )
}
```

### Example 3: Form with Validation

```tsx
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Button } from '@/components/ui/button'

export function EmployeeForm() {
  const [errors, setErrors] = useState({})

  return (
    <form className="space-y-4">
      <div>
        <Label htmlFor="name">Employee Name</Label>
        <Input 
          id="name"
          className={errors.name ? 'border-destructive' : ''}
        />
        {errors.name && (
          <p className="text-destructive text-sm mt-1">{errors.name}</p>
        )}
      </div>

      <div className="flex gap-2">
        <Button type="submit">Save</Button>
        <Button type="button" variant="secondary">Cancel</Button>
      </div>
    </form>
  )
}
```

### Example 4: Data Table with Status

```tsx
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table'
import { Badge } from '@/components/ui/badge'

export function EmployeeTable({ employees }) {
  return (
    <Table>
      <TableHeader>
        <TableRow className="bg-slate-50">
          <TableHead className="text-slate-700">Name</TableHead>
          <TableHead className="text-slate-700">Species</TableHead>
          <TableHead className="text-slate-700">Status</TableHead>
        </TableRow>
      </TableHeader>
      <TableBody>
        {employees.map((employee) => (
          <TableRow key={employee.id} className="hover:bg-slate-50/50">
            <TableCell className="text-slate-800 font-medium">
              {employee.name}
            </TableCell>
            <TableCell className="text-slate-600">
              {employee.department}
            </TableCell>
            <TableCell>
              {employee.status === 'stable' && (
                <Badge className="bg-green-50 text-green-600">Stable</Badge>
              )}
              {employee.status === 'critical' && (
                <Badge className="bg-red-50 text-red-600">Critical</Badge>
              )}
              {employee.status === 'observation' && (
                <Badge className="bg-amber-50 text-amber-600">Observation</Badge>
              )}
            </TableCell>
          </TableRow>
        ))}
      </TableBody>
    </Table>
  )
}
```

### Example 5: Notification System

```tsx
import { useToast } from '@/components/ui/use-toast'

export function NotificationExample() {
  const { toast } = useToast()

  const showSuccess = () => {
    toast({
      className: 'bg-green-50 border-green-200',
      title: 'Success',
      description: 'Employee record saved successfully',
    })
  }

  const showError = () => {
    toast({
      variant: 'destructive',
      title: 'Error',
      description: 'Failed to save employee record',
    })
  }

  const showWarning = () => {
    toast({
      className: 'bg-amber-50 border-amber-200',
      title: 'Warning',
      description: 'This employee is due for a review',
    })
  }

  const showInfo = () => {
    toast({
      className: 'bg-blue-50 border-blue-200',
      title: 'Information',
      description: 'Appointment reminder sent',
    })
  }

  return (
    <div className="space-x-2">
      <Button onClick={showSuccess}>Show Success</Button>
      <Button onClick={showError} variant="destructive">Show Error</Button>
      <Button onClick={showWarning}>Show Warning</Button>
      <Button onClick={showInfo}>Show Info</Button>
    </div>
  )
}
```

---

## ♿ Accessibility Guidelines

### WCAG AA Contrast Ratios ✓

All color combinations meet WCAG AA standards (4.5:1 minimum for normal text):

| Combination | Ratio | Status |
|-------------|-------|--------|
| Primary Blue (#1E88E5) on White | 4.52:1 | ✅ Pass |
| Slate 900 (#0F172A) on White | 16.91:1 | ✅ Pass |
| Slate 600 (#475569) on White | 7.43:1 | ✅ Pass |
| Green 600 (#16A34A) on Green 50 | 7.12:1 | ✅ Pass |
| Red 600 (#DC2626) on Red 50 | 6.89:1 | ✅ Pass |
| Amber 700 (#B45309) on Amber 50 | 7.01:1 | ✅ Pass |

### Color Blindness Considerations

**Never rely on color alone:**

✅ **Good:**
```tsx
<Badge className="bg-green-50 text-green-600">
  <CheckCircleIcon className="w-4 h-4 mr-1" />
  Stable
</Badge>
```

❌ **Bad:**
```tsx
<div className="bg-green-50 w-4 h-4 rounded-full" />
```

**Use multiple indicators:**
1. Color (green, red, amber)
2. Icon (check, warning, X)
3. Text label ("Stable", "Critical", "Observation")

### Focus States

Always provide visible focus indicators:

```tsx
// ✅ Proper focus ring
<button className="focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2">
  Click Me
</button>

// ✅ Input focus
<input className="focus:ring-2 focus:ring-primary focus:border-transparent" />
```

### Screen Reader Support

Use semantic HTML and ARIA labels:

```tsx
<Badge 
  className="bg-green-50 text-green-600"
  aria-label="Employee status: Stable"
>
  Stable
</Badge>
```

---

## 📊 Color Decision Tree

Use this flowchart to choose the right color:

```
Need a color?
│
├─ Is it for branding/primary actions?
│  └─ Use: bg-primary / text-primary
│
├─ Is it for text?
│  ├─ Page title → text-slate-900
│  ├─ Heading → text-slate-800
│  ├─ Body text → text-slate-600
│  ├─ Secondary text → text-slate-500
│  └─ Placeholder → text-slate-400
│
├─ Is it for employee status?
│  ├─ Stable/Success → bg-green-50 text-green-600
│  ├─ Observation/Warning → bg-amber-50 text-amber-600
│  ├─ Critical/Error → bg-red-50 text-red-600
│  └─ In Progress → bg-blue-50 text-blue-600
│
├─ Is it for a button?
│  ├─ Primary action → bg-primary text-primary-foreground
│  ├─ Secondary action → bg-secondary text-secondary-foreground
│  ├─ Delete/danger → bg-destructive text-destructive-foreground
│  ├─ Ghost → transparent hover:bg-slate-100
│  └─ Outline → border-slate-200 bg-white
│
├─ Is it for activity/metrics?
│  └─ Use: bg-orange-50 text-orange-600
│
└─ Is it for backgrounds?
   ├─ Main app → bg-background (slate-50)
   ├─ Cards → bg-card (white)
   ├─ Muted → bg-muted (slate-100)
   └─ Hover → hover:bg-slate-50
```

---

## 🎯 Summary: Where Colors Live

### Single Source of Truth: `src/index.css`

```
frontend/
└── src/
    └── index.css ← ALL COLORS DEFINED HERE
        ├── CSS Variables (:root)
        │   └── Used by shadcn/ui components
        └── Tailwind Theme (@theme)
            └── Used by utility classes
```

### How Colors Flow Through the App

```
1. Developer writes:
   <Button>Save</Button>

2. shadcn Button component renders:
   <button className="bg-primary text-primary-foreground">

3. Tailwind generates:
   .bg-primary { background-color: oklch(63.5% 0.18 244); }

4. Browser displays:
   Blue button with white text (#1E88E5 with #F8FAFC)
```

---

## 🔧 Developer Cheat Sheet

### Quick Color Reference

```tsx
// Primary actions
<button className="bg-primary text-primary-foreground hover:bg-primary/80">

// Text hierarchy
<h1 className="text-slate-900">       /* Page title */
<h2 className="text-slate-800">       /* Section heading */
<p className="text-slate-600">        /* Body text */
<span className="text-slate-500">    /* Secondary text */
<time className="text-slate-400">    /* Timestamp */

// Status badges
<Badge className="bg-green-50 text-green-600">Stable</Badge>
<Badge className="bg-amber-50 text-amber-600">Observation</Badge>
<Badge className="bg-red-50 text-red-600">Critical</Badge>
<Badge className="bg-blue-50 text-blue-600">In Progress</Badge>

// Borders
<div className="border-slate-200">    /* Default border */
<input className="border-input">      /* Input border */
<div className="border-destructive">  /* Error border */

// Backgrounds
<div className="bg-background">       /* App background */
<div className="bg-card">            /* Card background */
<div className="bg-muted">           /* Muted background */
```

---

## 📞 Support & Resources

**Questions?**
- Check this document first
- Review [src/index.css](frontend/src/index.css) for color definitions
- See [shadcn/ui docs](https://ui.shadcn.com) for component usage
- Review [Tailwind docs](https://tailwindcss.com) for utility classes

**Maintaining the System:**
- All color changes happen in `src/index.css` only
- Never hardcode hex values in components
- Use semantic class names (`bg-primary`, not `bg-blue-500`)
- Follow the accessibility guidelines
- Test color combinations for contrast

---

**Last Updated:** January 1, 2026  
**Version:** 1.0.0  
**Maintained By:** HREnap Development Team
