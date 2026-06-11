# Next.js Rules (Code Mode)

## Project Setup
- Next.js 14 dengan App Router — TIDAK BOLEH Pages Router
- TypeScript strict mode: `"strict": true` di tsconfig.json
- Tailwind CSS untuk styling
- Shadcn/ui untuk komponen dasar
- TanStack Query untuk server state
- React Hook Form untuk form handling
- Zod untuk schema validation

## App Router Conventions
- `page.tsx` untuk route pages
- `layout.tsx` untuk shared layout
- `loading.tsx` untuk loading UI
- `error.tsx` untuk error boundary
- `route.ts` untuk API routes (proxy ke .NET backend)
- Gunakan Server Components by default — tambah `"use client"` hanya kalau perlu interaktivitas

## Component Structure
- Satu komponen per file
- Nama file = nama komponen: `PayrollSummaryCard.tsx`
- Props interface di atas komponen, suffix `Props`: `PayrollSummaryCardProps`
- Gunakan `const` arrow function untuk komponen:
  ```tsx
  const PayrollSummaryCard = ({ payrollRun }: PayrollSummaryCardProps) => { ... }
  export default PayrollSummaryCard
  ```

## Data Fetching
- Server Components: fetch langsung di komponen (no TanStack Query)
- Client Components yang butuh realtime/refetch: gunakan TanStack Query
- API base URL dari `process.env.NEXT_PUBLIC_API_URL`
- Selalu handle loading state dan error state

## Forms
- React Hook Form + Zod schema validation
- Jangan gunakan `<form>` action langsung — handle via `onSubmit`
- Tampilkan error per field dari Zod validation
- Disable submit button saat `isSubmitting`

## Styling
- Tailwind CSS utility classes — jangan inline style
- Gunakan Shadcn/ui untuk: Button, Input, Select, Dialog, Table, Badge, Card
- Warna konsisten: gunakan CSS variables dari Shadcn theme
- Responsive: mobile-first, gunakan `sm:`, `md:`, `lg:` breakpoints

## State Management
- Server state: TanStack Query
- UI state (modal open, tab active): `useState`
- Form state: React Hook Form
- TIDAK PERLU Zustand/Redux untuk project ini

## TypeScript
- Semua props harus typed — tidak boleh `any`
- Interface untuk object shapes, type alias untuk union/primitive
- Gunakan `readonly` untuk props yang tidak seharusnya diubah
- Return type explicit untuk utility functions

## API Integration
- Buat typed API client di `lib/api.ts`
- Semua response harus typed — buat interface/type per endpoint
- Handle error response secara konsisten: tampilkan toast notification
- Gunakan `axios` atau native `fetch` — konsisten satu pilihan saja (`fetch`)
