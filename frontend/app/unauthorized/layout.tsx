import type { Metadata } from "next";

export const metadata: Metadata = {
  title: "Access Denied - Payroll System",
  description: "You don't have permission to access this resource",
};

export default function UnauthorizedLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return children;
}

// Made with Bob
