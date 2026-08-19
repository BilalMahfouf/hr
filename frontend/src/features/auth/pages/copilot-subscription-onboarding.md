# Copilot Prompt — Subscription Onboarding Page (Frontend Only)

## Context

HR management app — HREnap.
Stack: React + TypeScript, with i18n (`en.json` / `fr.json` / `ar.json`) and a KeyContainer/useTranslation pattern.
The app currently goes straight to `/login` or `/register`. We are adding one onboarding page after registration.

This task is **FRONTEND ONLY**. Backend endpoints will be provided separately.
Every API call must go through the feature's dedicated `feature-api.ts` file. No inline fetch/axios calls in components.

---

## Onboarding Flow

```
/register → /onboarding/subscribe → /dashboard
```

- After signup succeeds → redirect to `/onboarding/subscribe`
- After "Start Free Trial" is clicked → call the checkout API → redirect user to the returned Chargily checkout URL
- A `SubscriptionGuard` component must wrap all protected app routes. It reads `subscriptionStatus` from the auth context (provided by `/api/auth/me` — endpoint already exists).
  - If `subscriptionStatus !== "Active"` → redirect to `/onboarding/subscribe`

---

## API Calls (all in `feature-api.ts`)

One API function is needed. Ask for the exact endpoint shape if unsure.

**1. `createCheckout()`**
- `POST /api/payments/checkout`
- No request body needed
- Response: `{ checkoutUrl: string }`
- On success: `window.location.href = checkoutUrl`

Put it in the feature's own `feature-api.ts`.
Use the project's existing base API client which handles auth headers and base URL.
The base client already attaches the `Idempotency-Key` header — no need to add it manually.

---

## Page 1 — `/onboarding/subscribe`

**Layout:** centered white card on light gray background (`#f0f2f5`), matching the login page exactly.

**Card contains (top to bottom):**
- Logo + "HREnap" title + tagline
- Card title and subtitle (from i18n keys)
- Single plan box with blue border (`1.5px #2563eb`, `border-radius: 12px`)
  - "Professional Plan" pill badge centered at the top edge of the box
  - Plan name
  - Price: amount from `import.meta.env.VITE_PLAN_PRICE_DZD` + currency "DZD" + period "/ month" — **DO NOT hardcode the price number**
  - Horizontal divider
  - Feature list: 5 items, each with a blue circle check icon
- Light blue info box with free trial note
- Full-width blue primary button "Start Free Trial"
  - On click: call `createCheckout()` from `feature-api.ts`, show loading state, redirect to `checkoutUrl`
- Small footer: "Already have an account? Sign in" — "Sign in" links to `/login`

---

## i18n — STRICT RULES

- **ZERO hardcoded strings in JSX.** Every visible string must use the translation key via the project's existing `t()` / KeyContainer pattern.
- This includes: labels, placeholders, button text, error messages, helper text, aria-labels.
- Add all keys to `en.json`, `fr.json`, and `ar.json`.

### Key structure — add under the `onboarding` namespace

**`en.json`**
```json
{
  "onboarding": {
    "subscribe": {
      "title": "Activate your workspace",
      "subtitle": "One simple plan. Everything your team needs.",
      "planLabel": "Professional Plan",
      "planName": "Full workspace access",
      "currency": "DZD",
      "period": "/ month",
      "feature1": "Employee management",
      "feature2": "Attendance tracking",
      "feature3": "Staff & roles management",
      "feature4": "Payment processing via Chargily",
      "feature5": "Unlimited employees & records",
      "trialNote": "14-day free trial — no payment required to start",
      "cta": "Start Free Trial",
      "ctaLoading": "Redirecting...",
      "signinPrompt": "Already have an account?",
      "signinLink": "Sign in",
      "errorGeneric": "Something went wrong. Please try again."
    }
  }
}
```

**`fr.json`**
```json
{
  "onboarding": {
    "subscribe": {
      "title": "Activez votre espace de travail",
      "subtitle": "Un seul forfait. Tout ce dont votre équipe a besoin.",
      "planLabel": "Forfait Professionnel",
      "planName": "Accès complet à l'espace de travail",
      "currency": "DZD",
      "period": "/ mois",
      "feature1": "Gestion des employés",
      "feature2": "Suivi des présences",
      "feature3": "Gestion du personnel et des rôles",
      "feature4": "Paiement via Chargily",
      "feature5": "Employés et dossiers illimités",
      "trialNote": "Essai gratuit de 14 jours — aucun paiement requis",
      "cta": "Commencer l'essai gratuit",
      "ctaLoading": "Redirection...",
      "signinPrompt": "Vous avez déjà un compte ?",
      "signinLink": "Se connecter",
      "errorGeneric": "Une erreur s'est produite. Veuillez réessayer."
    }
  }
}
```

**`ar.json`**
```json
{
  "onboarding": {
    "subscribe": {
      "title": "تفعيل مساحة العمل",
      "subtitle": "خطة واحدة بسيطة. كل ما يحتاجه فريقك.",
      "planLabel": "الخطة الاحترافية",
      "planName": "وصول كامل لمساحة العمل",
      "currency": "دج",
      "period": "/ شهر",
      "feature1": "إدارة الموظفين",
      "feature2": "تتبع الحضور",
      "feature3": "إدارة الموظفين والأدوار",
      "feature4": "معالجة الدفع عبر Chargily",
      "feature5": "موظفون وسجلات غير محدودة",
      "trialNote": "تجربة مجانية لمدة 14 يومًا — لا يلزم الدفع للبدء",
      "cta": "ابدأ التجربة المجانية",
      "ctaLoading": "جارٍ التحويل...",
      "signinPrompt": "هل لديك حساب بالفعل؟",
      "signinLink": "تسجيل الدخول",
      "errorGeneric": "حدث خطأ ما. يرجى المحاولة مرة أخرى."
    }
  }
}
```

---

## RTL & Arabic Support

- Detect active locale. When locale is `"ar"`, add `dir="rtl"` to the page root `div`.
- Use CSS logical properties throughout:
  - `padding-inline-start` instead of `padding-left`
  - `margin-inline-end` instead of `margin-right`
  - `text-align: start` instead of `text-align: left`
- Icon positions, input icons, and flex rows must all flip correctly under RTL without any hardcoded directional overrides.
- The check icon in the feature list must appear on the correct side in RTL.
- The plan pill badge must remain centered in both LTR and RTL.

---

## Style Constraints

- Match the login page exactly: same `#f0f2f5` background, white card, `border-radius: 16px`, blue-500 (`#2563eb`), same logo area, same footer `© 2026 HREnap. Built for professionals.`
- Use whatever styling system the project already uses (Tailwind or CSS modules) — do not introduce new libraries.
- Plan price must come from `import.meta.env.VITE_PLAN_PRICE_DZD` — never hardcoded in the component.
- No backend code. No .NET. No database. **Frontend only.**