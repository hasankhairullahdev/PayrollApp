import type { Metadata } from "next";

export const metadata: Metadata = {
  title: "Login - Payroll System",
  description: "Sign in to your payroll account",
};

export default function LoginLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return children;
}

// Made with Bob
