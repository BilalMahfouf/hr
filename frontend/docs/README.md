# HREnap Frontend Documentation

Welcome to the HREnap HR Management Platform frontend documentation.

## 📚 Documentation Index

### 🎨 [Color System Documentation](./COLOR-SYSTEM.md)
Complete guide to HREnap's color palette and design system:
- Primary brand colors
- Neutral slate palette (50-900 shades)
- Semantic status colors (green, amber, red, blue, purple, orange)
- UI component colors
- Accessibility guidelines (WCAG AA compliant)
- Usage examples and best practices
- Required shadcn/ui components with installation instructions

### 🚀 [Setup Guide](./SETUP-GUIDE.md)
Quick start guide for the color system:
- Environment setup verification
- shadcn/ui component installation commands
- Usage examples and code snippets
- Troubleshooting tips
- Quick reference for common patterns

## 🎯 Quick Links

### Installation Commands

```bash
# Install core shadcn/ui components
npx shadcn@latest add button card input label badge table

# Install interaction components
npx shadcn@latest add select dialog dropdown-menu toast alert

# Install all at once
npx shadcn@latest add button card input label badge table select dialog dropdown-menu toast alert tabs
```

### Key Files

- **Color Definitions**: `src/index.css`
- **Utility Helper**: `src/lib/utils.ts`
- **Theme Provider**: `src/components/theme-provider.tsx`
- **shadcn Config**: `components.json`

### Quick Examples

```tsx
// Primary button
<Button>Save Employee</Button>

// Status badges
<Badge className="bg-green-50 text-green-600">Stable</Badge>
<Badge className="bg-amber-50 text-amber-600">Observation</Badge>
<Badge className="bg-red-50 text-red-600">Critical</Badge>

// Text hierarchy
<h1 className="text-slate-900">Dashboard</h1>
<p className="text-slate-600">Body text</p>
```

## 🛠️ Tech Stack

- **Framework**: React 19.2.0
- **Language**: TypeScript 5.9.3
- **Styling**: Tailwind CSS v4.1.18
- **UI Components**: shadcn/ui
- **Build Tool**: Vite 7.2.4
- **Package Manager**: pnpm

## 🎨 Color Philosophy

HREnap uses a **two-layer color system**:
1. **CSS Variables** (HSL format) - For theming and shadcn/ui
2. **Tailwind Utilities** (OKLCH format) - For component styling

All colors are defined in a single source: `src/index.css`

## 📖 Getting Started

1. Read the [Setup Guide](./SETUP-GUIDE.md) to verify your environment
2. Install shadcn/ui components (commands in Setup Guide)
3. Review [Color System Documentation](./COLOR-SYSTEM.md) for detailed usage
4. Start building with semantic color classes!

## 🔗 External Resources

- [shadcn/ui Documentation](https://ui.shadcn.com)
- [Tailwind CSS v4 Documentation](https://tailwindcss.com)
- [React Documentation](https://react.dev)
- [Vite Documentation](https://vitejs.dev)

---

**Project**: HREnap HR Management Platform  
**Version**: 1.0.0  
**Last Updated**: January 2, 2026
