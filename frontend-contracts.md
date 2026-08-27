# Frontend API Contract — Employee Groups, Work Schedules & Rotations

**Version:** 1.0  
**Base URL:** `https://api.yourdomain.com/api/v1`  
**Auth:** Bearer JWT (`Authorization: Bearer <token>`)  
**Content-Type:** `application/json`  
**Error Format:** RFC 7807 ProblemDetails

---

## Response DTOs (Flat — No Nested Records)

### EmployeeGroupResponse
```typescript
interface EmployeeGroupResponse {
  id: string;                           // Guid
  name: string;
  isSecurity: boolean;
  description: string | null;
  rotationStartDate: string;            // ISO date (yyyy-MM-dd)
  numberOfRotations: number;
  workSchedules: WorkScheduleResponse[];
  rotationEntries: RotationEntryResponse[];
  createdOnUtc: string;                 // ISO datetime
}
```

### WorkScheduleResponse
```typescript
interface WorkScheduleResponse {
  id: string;                           // Guid
  employeeGroupId: string;              // Guid
  shiftStartTime: string;               // HH:mm:ss
  shiftEndTime: string;                 // HH:mm:ss
  breakStartTime: string;               // HH:mm:ss
  breakEndTime: string;                 // HH:mm:ss
  endDayOffset: number;                 // 0 = same day, 1 = next day
  allowedCheckInLatenessMinutes: number;
  allowedCheckOutEarlinessMinutes: number;
  isActive: boolean;
  createdOnUtc: string;                 // ISO datetime
}
```

### RotationEntryResponse
```typescript
interface RotationEntryResponse {
  id: string;                           // Guid
  employeeGroupId: string;              // Guid
  position: number;                     // 1-based (1, 2, 3...)
  workScheduleId: string | null;        // Guid or null (null = Rest)
  status: "Work" | "Rest";              // Computed: WorkScheduleId ? "Work" : "Rest"
}
```

---

## Request DTOs

### CreateEmployeeGroupRequest
```typescript
interface CreateEmployeeGroupRequest {
  name: string;                         // Required, max 100 chars
  isSecurity: boolean;                  // Required
  description: string | null;           // Optional
  rotationStartDate: string;            // Required, ISO date (yyyy-MM-dd)
  workSchedules: CreateWorkScheduleRequest[];
  rotationEntries: CreateRotationEntryRequest[];
}

interface CreateWorkScheduleRequest {
  shiftStartTime: string;               // Required, HH:mm:ss
  shiftEndTime: string;                 // Required, HH:mm:ss
  breakStartTime: string;               // Required, HH:mm:ss
  breakEndTime: string;                 // Required, HH:mm:ss
  endDayOffset: number;                 // Required, >= 0
  allowedCheckInLatenessMinutes: number; // Required, >= 0
  allowedCheckOutEarlinessMinutes: number; // Required, >= 0
}

interface CreateRotationEntryRequest {
  position: number;                     // Required, >= 1, unique within group
  workScheduleIndex: number | null;     // 0-based index into workSchedules[], or null for Rest
}
```

**IMPORTANT — index-based references:** Schedules get their GUIDs generated server-side, so when creating a group (or replacing schedules+rotations) rotation entries reference schedules by **index** in the `workSchedules` array (0-based). The API resolves each index to the created schedule's real ID. A `null` index means a Rest day.

**Validation Rules (enforced by API):**
- `shiftStartTime < shiftEndTime` (unless `endDayOffset > 0`)
- `breakStartTime < breakEndTime` (unless `endDayOffset > 0`)
- `breakStartTime >= shiftStartTime` && `breakEndTime <= shiftEndTime`
- `rotationEntries[position]` must be unique
- `rotationEntries[].workScheduleIndex` must be a valid index into `workSchedules[]` (or null)
- At least one rotation entry is required

---

### UpdateEmployeeGroupRequest (PATCH — Metadata Only)
```typescript
interface UpdateEmployeeGroupRequest {
  name?: string;                        // Optional, max 100 chars
  isSecurity?: boolean;                 // Optional
  description?: string | null;          // Optional
}
```

---

### ReplaceSchedulesAndRotationsRequest (PUT — Full Replacement)
```typescript
interface ReplaceSchedulesAndRotationsRequest {
  workSchedules: CreateWorkScheduleRequest[];
  rotationEntries: CreateRotationEntryRequest[];
}
```
**Behavior:** Atomically replaces ALL schedules and rotations for the group. Old schedules/rotations are deleted. Validation same as create.

---

### WorkSchedule Update (PUT — Individual)
```typescript
interface UpdateWorkScheduleRequest {
  shiftStartTime: string;
  shiftEndTime: string;
  breakStartTime: string;
  breakEndTime: string;
  endDayOffset: number;
  allowedCheckInLatenessMinutes: number;
  allowedCheckOutEarlinessMinutes: number;
}
```

---

### Rotation Requests
```typescript
// POST /rotations/work
interface CreateWorkRotationRequest {
  position: number;                     // >= 1, unique
  workScheduleId: string;               // Required Guid, must exist in group
}

// POST /rotations/rest
interface CreateRestRotationRequest {
  position: number;                     // >= 1, unique
}

// PUT /rotations/{position} — full replacement semantics
interface UpdateRotationRequest {
  newPosition?: number;                 // Optional, >= 1; omitted = keep current position
  workScheduleId?: string | null;       // Guid = work day; null/omitted = rest day
}
```

> **PUT semantics:** `workScheduleId` is a full-replacement field. Send a Guid to make the entry a work day (referencing an existing schedule in the group), or `null`/omit it to make it a rest day. `newPosition` is optional and defaults to the current position.

---

## Endpoint Reference

### Employee Groups

| Method | Endpoint | Request | Response |
|--------|----------|---------|----------|
| `POST` | `/employee-groups` | `CreateEmployeeGroupRequest` | `201 EmployeeGroupResponse` |
| `GET` | `/employee-groups` | — | `200 EmployeeGroupResponse[]` |
| `GET` | `/employee-groups/{id}` | — | `200 EmployeeGroupResponse` |
| `PATCH` | `/employee-groups/{id}` | `UpdateEmployeeGroupRequest` | `200 EmployeeGroupResponse` |
| `PUT` | `/employee-groups/{id}/schedules-and-rotations` | `ReplaceSchedulesAndRotationsRequest` | `200 EmployeeGroupResponse` |
| `DELETE` | `/employee-groups/{id}` | — | `204 No Content` |

> **Location header** on 201: `/api/v1/employee-groups/{newId}`

---

### Work Schedules (scoped to group)

| Method | Endpoint | Request | Response |
|--------|----------|---------|----------|
| `POST` | `/employee-groups/{groupId}/work-schedules` | `CreateWorkScheduleRequest` | `201 WorkScheduleResponse` |
| `GET` | `/employee-groups/{groupId}/work-schedules/{scheduleId}` | — | `200 WorkScheduleResponse` |
| `PUT` | `/employee-groups/{groupId}/work-schedules/{scheduleId}` | `UpdateWorkScheduleRequest` | `200 WorkScheduleResponse` |
| `DELETE` | `/employee-groups/{groupId}/work-schedules/{scheduleId}` | — | `204` or `409` if referenced by rotation |
| `POST` | `/employee-groups/{groupId}/work-schedules/{scheduleId}/activate` | — | `200 WorkScheduleResponse` |
| `POST` | `/employee-groups/{groupId}/work-schedules/{scheduleId}/deactivate` | — | `200 WorkScheduleResponse` |

> **409 Conflict** on DELETE: `{ "title": "Work schedule is referenced by rotation entries", "status": 409 }`

---

### Rotations (scoped to group)

| Method | Endpoint | Request | Response |
|--------|----------|---------|----------|
| `GET` | `/employee-groups/{groupId}/rotations` | — | `200 RotationEntryResponse[]` (ordered by position) |
| `POST` | `/employee-groups/{groupId}/rotations/work` | `CreateWorkRotationRequest` | `201 RotationEntryResponse` |
| `POST` | `/employee-groups/{groupId}/rotations/rest` | `CreateRestRotationRequest` | `201 RotationEntryResponse` |
| `PUT` | `/employee-groups/{groupId}/rotations/{position}` | `UpdateRotationRequest` | `200 RotationEntryResponse` |
| `DELETE` | `/employee-groups/{groupId}/rotations/{position}` | — | `204` |

> Positions are **1-based** (1, 2, 3...). Gaps allowed but frontend should present contiguous.

---

## Example: Create Group with Schedules & Rotations

### Request
```http
POST /api/v1/employee-groups
Content-Type: application/json
Authorization: Bearer <token>

{
  "name": "Nurses - Shift A",
  "isSecurity": false,
  "description": "Morning shift nurses",
  "rotationStartDate": "2026-01-01",
  "workSchedules": [
    {
      "shiftStartTime": "07:00:00",
      "shiftEndTime": "15:00:00",
      "breakStartTime": "11:00:00",
      "breakEndTime": "11:30:00",
      "endDayOffset": 0,
      "allowedCheckInLatenessMinutes": 15,
      "allowedCheckOutEarlinessMinutes": 10
    },
    {
      "shiftStartTime": "15:00:00",
      "shiftEndTime": "23:00:00",
      "breakStartTime": "19:00:00",
      "breakEndTime": "19:30:00",
      "endDayOffset": 0,
      "allowedCheckInLatenessMinutes": 15,
      "allowedCheckOutEarlinessMinutes": 10
    },
    {
      "shiftStartTime": "23:00:00",
      "shiftEndTime": "07:00:00",
      "breakStartTime": "03:00:00",
      "breakEndTime": "03:30:00",
      "endDayOffset": 1,
      "allowedCheckInLatenessMinutes": 15,
      "allowedCheckOutEarlinessMinutes": 10
    }
  ],
  "rotationEntries": [
    { "position": 1, "workScheduleIndex": 0 },
    { "position": 2, "workScheduleIndex": 1 },
    { "position": 3, "workScheduleIndex": 2 },
    { "position": 4, "workScheduleIndex": null },
    { "position": 5, "workScheduleIndex": null }
  ]
}
```

### Response (201)
```json
{
  "id": "0192f3c0-...",
  "name": "Nurses - Shift A",
  "isSecurity": false,
  "description": "Morning shift nurses",
  "rotationStartDate": "2026-01-01",
  "numberOfRotations": 5,
  "workSchedules": [
    { "id": "0192f3c1-...", "employeeGroupId": "0192f3c0-...", "shiftStartTime": "07:00:00", ... },
    { "id": "0192f3c2-...", "employeeGroupId": "0192f3c0-...", "shiftStartTime": "15:00:00", ... },
    { "id": "0192f3c3-...", "employeeGroupId": "0192f3c0-...", "shiftStartTime": "23:00:00", ... }
  ],
  "rotationEntries": [
    { "id": "0192f3c4-...", "employeeGroupId": "0192f3c0-...", "position": 1, "workScheduleId": "0192f3c1-...", "status": "Work" },
    { "id": "0192f3c5-...", "employeeGroupId": "0192f3c0-...", "position": 2, "workScheduleId": "0192f3c2-...", "status": "Work" },
    { "id": "0192f3c6-...", "employeeGroupId": "0192f3c0-...", "position": 3, "workScheduleId": "0192f3c3-...", "status": "Work" },
    { "id": "0192f3c7-...", "employeeGroupId": "0192f3c0-...", "position": 4, "workScheduleId": null, "status": "Rest" },
    { "id": "0192f3c8-...", "employeeGroupId": "0192f3c0-...", "position": 5, "workScheduleId": null, "status": "Rest" }
  ],
  "createdOnUtc": "2026-08-26T10:30:00Z"
}
```

---

## Frontend UX Recommendations

### Create Page (`/employee-groups/new`)
```
┌─────────────────────────────────────────────────────────────┐
│ Create Employee Group                                        │
├─────────────────────────────────────────────────────────────┤
│ Group Info                                                  │
│  Name: [____________________]  Is Security: [☐]             │
│  Description: [____________________]                         │
│  Rotation Start Date: [📅 2026-01-01]                        │
├─────────────────────────────────────────────────────────────┤
│ Work Schedules (dynamic list)                               │
│  ┌─ Schedule 1 ──────────────────────────────────────────┐  │
│  │ Shift: [07:00] – [15:00]  Break: [11:00] – [11:30]    │  │
│  │ End Day Offset: [0]  Late: [15]  Early: [10]  [🗑]    │  │
│  └───────────────────────────────────────────────────────┘  │
│  [+ Add Schedule]                                           │
├─────────────────────────────────────────────────────────────┤
│ Rotations (dynamic list, positions auto-assigned)           │
│  Position 1: [Work ▼] ▼ Schedule: [Schedule 1 ▼]  [🗑]     │
│  Position 2: [Work ▼] ▼ Schedule: [Schedule 2 ▼]  [🗑]     │
│  Position 3: [Rest ▼]                                       │
│  [+ Add Rotation]                                           │
├─────────────────────────────────────────────────────────────┤
│                          [Cancel]  [Create Group]           │
└─────────────────────────────────────────────────────────────┘
```
- **Single submit** → `POST /employee-groups` with full payload
- Validate client-side: times, unique positions, workScheduleIndex bounds
- Show inline errors from 400 response

---

### Edit Page (`/employee-groups/{id}`)
```
┌─────────────────────────────────────────────────────────────┐
│ Edit: Nurses - Shift A                                       │
├─────────────────────────────────────────────────────────────┤
│ Group Info (READONLY per requirement)                       │
│  Name: Nurses - Shift A          Is Security: ☐             │
│  Description: Morning shift nurses                           │
│  Rotation Start Date: 2026-01-01                             │
├─────────────────────────────────────────────────────────────┤
│ Work Schedules & Rotations (single form)                    │
│  [Tab: Schedules]  [Tab: Rotations]                         │
│                                                              │
│  Schedules Tab:                                             │
│  ┌─ Schedule 1 ──────────────────────────────────────────┐  │
│  │ Shift: [07:00] – [15:00]  Break: [11:00] – [11:30]    │  │
│  │ End Day Offset: [0]  Late: [15]  Early: [10]          │  │
│  │ [Activate] [Deactivate] [Delete]  (disable if in use) │  │
│  └───────────────────────────────────────────────────────┘  │
│  [+ Add Schedule]                                           │
│                                                              │
│  Rotations Tab:                                             │
│  Position 1: [Work] Schedule: [Schedule 1] [🗑]             │
│  Position 2: [Work] Schedule: [Schedule 2] [🗑]             │
│  Position 3: [Rest]                                         │
│  [+ Add Work] [+ Add Rest]                                  │
├─────────────────────────────────────────────────────────────┤
│                    [Cancel]  [Save All Changes]             │
└─────────────────────────────────────────────────────────────┘
```

**Save Flow:**
1. User clicks **Save All Changes**
2. Frontend collects all schedules + rotations from both tabs
3. Sends `PUT /employee-groups/{id}/schedules-and-rotations` with full payload
4. On success → refresh GET `/employee-groups/{id}` to show updated state
5. Individual actions (Activate/Deactivate/Delete schedule, Add/Delete rotation) call their specific endpoints immediately for instant feedback

---

### List Page (`/employee-groups`)
- `GET /employee-groups` → table with: Name, IsSecurity, RotationStartDate, #Schedules, #Rotations, Created
- Row click → navigate to Edit page
- [New Group] button → Create page
- Delete button per row → confirmation → `DELETE /employee-groups/{id}`

---

## Error Handling Cheat Sheet

| Scenario | HTTP | Frontend Action |
|----------|------|-----------------|
| Validation failed | 400 | Show field-level errors from `errors` object |
| Group not found | 404 | Redirect to list with toast "Group not found" |
| Name duplicate | 409 | Show inline "Name already exists" on name field |
| Delete schedule in use | 409 | Toast "Cannot delete: schedule used in rotation" |
| Unauthorized | 401 | Redirect to login |
| Server error | 500 | Toast "Something went wrong, try again" |

---

## TypeScript Types (Copy-Paste Ready)

```typescript
// Shared
type Guid = string;
type IsoDate = string;        // yyyy-MM-dd
type IsoDateTime = string;    // yyyy-MM-ddTHH:mm:ssZ
type TimeOnly = string;       // HH:mm:ss

// Responses
interface EmployeeGroupResponse {
  id: Guid;
  name: string;
  isSecurity: boolean;
  description: string | null;
  rotationStartDate: IsoDate;
  numberOfRotations: number;
  workSchedules: WorkScheduleResponse[];
  rotationEntries: RotationEntryResponse[];
  createdOnUtc: IsoDateTime;
}

interface WorkScheduleResponse {
  id: Guid;
  employeeGroupId: Guid;
  shiftStartTime: TimeOnly;
  shiftEndTime: TimeOnly;
  breakStartTime: TimeOnly;
  breakEndTime: TimeOnly;
  endDayOffset: number;
  allowedCheckInLatenessMinutes: number;
  allowedCheckOutEarlinessMinutes: number;
  isActive: boolean;
  createdOnUtc: IsoDateTime;
}

interface RotationEntryResponse {
  id: Guid;
  employeeGroupId: Guid;
  position: number;
  workScheduleId: Guid | null;
  status: "Work" | "Rest";
}

// Requests
interface CreateEmployeeGroupRequest {
  name: string;
  isSecurity: boolean;
  description: string | null;
  rotationStartDate: IsoDate;
  workSchedules: CreateWorkScheduleRequest[];
  rotationEntries: CreateRotationEntryRequest[];
}

interface CreateWorkScheduleRequest {
  shiftStartTime: TimeOnly;
  shiftEndTime: TimeOnly;
  breakStartTime: TimeOnly;
  breakEndTime: TimeOnly;
  endDayOffset: number;
  allowedCheckInLatenessMinutes: number;
  allowedCheckOutEarlinessMinutes: number;
}

interface CreateRotationEntryRequest {
  position: number;                     // >= 1, unique
  workScheduleIndex: number | null;     // 0-based index into workSchedules[], null = Rest
}

interface UpdateEmployeeGroupRequest {
  name?: string;
  isSecurity?: boolean;
  description?: string | null;
}

interface ReplaceSchedulesAndRotationsRequest {
  workSchedules: CreateWorkScheduleRequest[];
  rotationEntries: CreateRotationEntryRequest[];
}

interface CreateWorkRotationRequest {
  position: number;
  workScheduleId: Guid;
}

interface CreateRestRotationRequest {
  position: number;
}

interface UpdateRotationRequest {
  newPosition?: number;
  workScheduleId?: Guid | null;
}
```

---

## OpenAPI/Scalar Documentation

Once deployed, interactive docs available at:
- **Development**: `https://localhost:7xxx/scalar`
- **Production**: `https://api.yourdomain.com/scalar`

All endpoints documented with request/response examples.

---

## Summary for Frontend Team

| Page | Primary Endpoints | Notes |
|------|-------------------|-------|
| **List** | `GET /employee-groups` | Full data in one call |
| **Create** | `POST /employee-groups` | Single atomic request with nested arrays |
| **Edit** | `GET /employee-groups/{id}` (initial) → `PUT /employee-groups/{id}/schedules-and-rotations` (save) | Group metadata readonly; schedules+rotations full replace |
| **Individual Actions** | Specific endpoints (activate, delete, etc.) | Call immediately for instant UI feedback |

**Key constraint**: When deleting a schedule, check if it's referenced by any rotation first (API returns 409). Disable delete button or show warning in UI.