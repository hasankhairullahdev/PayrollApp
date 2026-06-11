import type { Metadata } from "next";
import { Inter } from "next/font/google";
import "./globals.css";
import { Providers } from "./providers";
import { Sidebar } from "@/components/Sidebar";

const inter = Inter({ subsets: ["latin"], variable: '--font-inter' });

export const metadata: Metadata = {
  title: "Payroll App - Enterprise Payroll Management",
  description: "Manage payroll, calculate taxes, and generate payslips",
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="en">
      <body className={inter.className}>
        <Providers>
          <div className="min-h-screen bg-gradient-to-br from-[#FAFAF9] via-[#F8FAFC] to-[#F1F5F9] flex">
            {/* Sidebar Component (includes spacer) */}
            <Sidebar />

            {/* Main Content */}
            <div className="flex-1 flex flex-col min-w-0">
              {/* Top Bar */}
              <header className="bg-white/80 backdrop-blur-sm shadow-sm border-b border-gray-200/50 h-16 flex items-center justify-between px-4 md:px-8 sticky top-0 z-30">
                <div className="flex items-center gap-4">
                  <div className="text-xs md:text-sm text-[#64748B] font-medium">
                    {new Date().toLocaleDateString('id-ID', {
                      weekday: 'long',
                      year: 'numeric',
                      month: 'long',
                      day: 'numeric'
                    })}
                  </div>
                </div>
                
                <div className="flex items-center gap-2 md:gap-4">
                  <button className="relative text-[#64748B] hover:text-[#1E3A5F] transition-colors p-2 hover:bg-gray-100 rounded-lg">
                    <svg className="w-5 h-5 md:w-6 md:h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 17h5l-1.405-1.405A2.032 2.032 0 0118 14.158V11a6.002 6.002 0 00-4-5.659V5a2 2 0 10-4 0v.341C7.67 6.165 6 8.388 6 11v3.159c0 .538-.214 1.055-.595 1.436L4 17h5m6 0v1a3 3 0 11-6 0v-1m6 0H9" />
                    </svg>
                    <span className="absolute top-1 right-1 w-2 h-2 bg-[#F59E0B] rounded-full animate-pulse" />
                  </button>
                  
                  <div className="flex items-center gap-2 md:gap-3 pl-2 md:pl-4 border-l border-gray-200">
                    <div className="w-8 h-8 md:w-9 md:h-9 bg-gradient-to-br from-[#0D9488] to-[#14B8A6] rounded-full flex items-center justify-center text-white text-xs md:text-sm font-bold shadow-md ring-2 ring-white">
                      HR
                    </div>
                    <div className="hidden md:block text-sm">
                      <div className="font-semibold text-[#1E3A5F]">HR Admin</div>
                      <div className="text-xs text-[#64748B]">Administrator</div>
                    </div>
                  </div>
                </div>
              </header>

              {/* Page Content */}
              <main className="flex-1 overflow-auto">
                {children}
              </main>
            </div>
          </div>
        </Providers>
      </body>
    </html>
  );
}

// Made with Bob
