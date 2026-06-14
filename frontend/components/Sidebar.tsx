'use client';

import Link from "next/link";
import { usePathname } from "next/navigation";
import { useState } from "react";

function NavLink({ href, icon, children, isCollapsed }: { href: string; icon: React.ReactNode; children: React.ReactNode; isCollapsed: boolean }) {
  const pathname = usePathname();
  const isActive = pathname === href || (href !== '/' && pathname.startsWith(href));
  
  return (
    <Link
      href={href}
      className={`
        group flex items-center gap-3 px-4 py-3 rounded-lg transition-all duration-200 relative overflow-hidden
        ${isActive
          ? 'text-white bg-gradient-to-r from-[#0D9488]/20 to-transparent border-l-2 border-[#0D9488] shadow-lg shadow-[#0D9488]/20'
          : 'text-slate-300 hover:text-white hover:bg-white/10'
        }
      `}
      title={isCollapsed ? children as string : undefined}
    >
      <div className={`w-5 h-5 flex-shrink-0 transition-transform ${isActive ? 'text-[#0D9488] scale-110' : 'group-hover:scale-110'}`}>
        {icon}
      </div>
      {!isCollapsed && (
        <span className={`font-medium whitespace-nowrap ${isActive ? 'font-semibold' : ''}`}>{children}</span>
      )}
      {!isActive && (
        <div className="absolute left-0 w-1 h-full bg-[#0D9488] scale-y-0 group-hover:scale-y-100 transition-transform origin-center rounded-r" />
      )}
    </Link>
  );
}

export function Sidebar() {
  const [isCollapsed, setIsCollapsed] = useState(false);

  return (
    <>
      {/* Sidebar */}
      <aside
        className={`
          fixed left-0 top-0 bottom-0 z-40
          ${isCollapsed ? 'w-20' : 'w-72'}
          bg-gradient-to-b from-[#1E3A5F] via-[#1a3352] to-[#152B47]
          text-white shadow-2xl transition-all duration-300 ease-in-out
        `}
        style={{
          backgroundImage: 'linear-gradient(rgba(30, 58, 95, 0.85), rgba(21, 43, 71, 0.88)), url(/Building.jpg)',
          backgroundSize: 'cover',
          backgroundPosition: 'center'
        }}
      >
        {/* Logo Section */}
        <div className={`p-6 border-b border-white/10 ${isCollapsed ? 'px-4' : ''}`}>
          <div className="flex items-center justify-between">
            {!isCollapsed ? (
              <div className="flex items-center gap-3">
                <div className="w-10 h-10 bg-gradient-to-br from-[#0D9488] to-[#14B8A6] rounded-xl flex items-center justify-center shadow-lg">
                  <svg className="w-6 h-6 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 8c-1.657 0-3 .895-3 2s1.343 2 3 2 3 .895 3 2-1.343 2-3 2m0-8c1.11 0 2.08.402 2.599 1M12 8V7m0 1v8m0 0v1m0-1c-1.11 0-2.08-.402-2.599-1M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
                  </svg>
                </div>
                <div>
                  <h1 className="text-lg font-bold tracking-tight">Payroll Pro</h1>
                  <p className="text-xs text-slate-400">Enterprise</p>
                </div>
              </div>
            ) : (
              <div className="w-10 h-10 bg-gradient-to-br from-[#0D9488] to-[#14B8A6] rounded-xl flex items-center justify-center shadow-lg mx-auto">
                <svg className="w-6 h-6 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 8c-1.657 0-3 .895-3 2s1.343 2 3 2 3 .895 3 2-1.343 2-3 2m0-8c1.11 0 2.08.402 2.599 1M12 8V7m0 1v8m0 0v1m0-1c-1.11 0-2.08-.402-2.599-1M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
                </svg>
              </div>
            )}
          </div>
        </div>
        
        {/* Navigation */}
        <nav className="mt-6 px-3 space-y-2">
          <NavLink 
            href="/"
            isCollapsed={isCollapsed}
            icon={
              <svg fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M3 12l2-2m0 0l7-7 7 7M5 10v10a1 1 0 001 1h3m10-11l2 2m-2-2v10a1 1 0 01-1 1h-3m-6 0a1 1 0 001-1v-4a1 1 0 011-1h2a1 1 0 011 1v4a1 1 0 001 1m-6 0h6" />
              </svg>
            }
          >
            Dashboard
          </NavLink>
          
          <NavLink 
            href="/payroll"
            isCollapsed={isCollapsed}
            icon={
              <svg fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 8c-1.657 0-3 .895-3 2s1.343 2 3 2 3 .895 3 2-1.343 2-3 2m0-8c1.11 0 2.08.402 2.599 1M12 8V7m0 1v8m0 0v1m0-1c-1.11 0-2.08-.402-2.599-1M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
              </svg>
            }
          >
            Payroll Runs
          </NavLink>
          
          <NavLink 
            href="/employees"
            isCollapsed={isCollapsed}
            icon={
              <svg fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0zm6 3a2 2 0 11-4 0 2 2 0 014 0zM7 10a2 2 0 11-4 0 2 2 0 014 0z" />
              </svg>
            }
          >
            Employees
          </NavLink>
          
          <NavLink 
            href="/reports"
            isCollapsed={isCollapsed}
            icon={
              <svg fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 17v-2m3 2v-4m3 4v-6m2 10H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
              </svg>
            }
          >
            Reports
          </NavLink>
        </nav>

        {/* Collapse Button */}
        <button
          onClick={() => setIsCollapsed(!isCollapsed)}
          className="fixed -right-3 top-20 w-6 h-6 bg-white rounded-full shadow-lg flex items-center justify-center text-[#1E3A5F] hover:bg-[#0D9488] hover:text-white transition-all duration-200 z-50"
          style={{ left: isCollapsed ? '68px' : '276px' }}
          title={isCollapsed ? "Expand sidebar" : "Collapse sidebar"}
        >
          <svg 
            className={`w-4 h-4 transition-transform duration-300 ${isCollapsed ? 'rotate-180' : ''}`} 
            fill="none" 
            stroke="currentColor" 
            viewBox="0 0 24 24"
          >
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 19l-7-7 7-7" />
          </svg>
        </button>

        {/* Bottom Section */}
        <div className={`absolute bottom-0 left-0 right-0 p-4 border-t border-white/10 ${isCollapsed ? 'px-2' : ''}`}>
          {!isCollapsed ? (
            <div className="text-xs text-slate-400 space-y-2">
              <div className="flex items-center gap-2">
                <div className="w-2 h-2 bg-[#0D9488] rounded-full animate-pulse-glow" />
                <span>System Online</span>
              </div>
              <div className="opacity-60">Version 1.0.0</div>
            </div>
          ) : (
            <div className="flex justify-center">
              <div className="w-2 h-2 bg-[#0D9488] rounded-full animate-pulse-glow" />
            </div>
          )}
        </div>
      </aside>

      {/* Spacer to push content */}
      <div className={`flex-shrink-0 transition-all duration-300 ${isCollapsed ? 'w-20' : 'w-72'}`} />
    </>
  );
}

// Made with Bob
