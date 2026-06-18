'use client';

import { useState } from 'react';
import { useAuth } from '@/lib/auth-context';
import { Button } from '@/components/ui/button';
import { Card } from '@/components/ui/card';
import { useRouter } from 'next/navigation';

export default function LoginPage() {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  const { login } = useAuth();
  const router = useRouter();

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');
    setIsLoading(true);

    const result = await login(email, password);
    
    if (!result.success) {
      setError(result.message || 'Login failed. Please check your credentials.');
    }
    // If success, redirect is handled by auth context
    
    setIsLoading(false);
  };

  // Quick login buttons for demo
  const quickLogin = async (role: 'admin' | 'hr' | 'finance') => {
    const credentials = {
      admin: { email: 'admin@payroll.com', password: 'Admin123!' },
      hr: { email: 'hr@payroll.com', password: 'HR123!' },
      finance: { email: 'finance@payroll.com', password: 'Finance123!' },
    };

    setEmail(credentials[role].email);
    setPassword(credentials[role].password);
    
    setError('');
    setIsLoading(true);

    const result = await login(credentials[role].email, credentials[role].password);
    
    if (!result.success) {
      setError(result.message || 'Login failed');
    }
    // If success, redirect is handled by auth context
    
    setIsLoading(false);
  };

  return (
    <div className="min-h-screen flex items-center justify-center bg-gradient-to-br from-teal-50 via-emerald-50 to-cyan-50 p-4">
      <Card className="w-full max-w-md p-8 shadow-2xl border-teal-100">
        <div className="text-center mb-8">
          <div className="inline-flex items-center justify-center w-16 h-16 rounded-full bg-gradient-to-br from-teal-500 to-emerald-600 mb-4">
            <svg
              className="w-8 h-8 text-white"
              fill="none"
              stroke="currentColor"
              viewBox="0 0 24 24"
            >
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth={2}
                d="M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z"
              />
            </svg>
          </div>
          <h1 className="text-3xl font-bold text-gray-900 mb-2">Payroll System</h1>
          <p className="text-gray-600">Sign in to your account</p>
        </div>

        <form onSubmit={handleSubmit} className="space-y-6">
          {error && (
            <div className="p-4 rounded-lg bg-red-50 border border-red-200">
              <p className="text-sm text-red-800">{error}</p>
            </div>
          )}

          <div>
            <label htmlFor="email" className="block text-sm font-medium text-gray-700 mb-2">
              Email Address
            </label>
            <input
              id="email"
              type="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              required
              className="w-full px-4 py-3 rounded-lg border border-gray-300 focus:ring-2 focus:ring-teal-500 focus:border-transparent transition-all"
              placeholder="you@example.com"
              disabled={isLoading}
            />
          </div>

          <div>
            <label htmlFor="password" className="block text-sm font-medium text-gray-700 mb-2">
              Password
            </label>
            <input
              id="password"
              type="password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              required
              className="w-full px-4 py-3 rounded-lg border border-gray-300 focus:ring-2 focus:ring-teal-500 focus:border-transparent transition-all"
              placeholder="••••••••"
              disabled={isLoading}
            />
          </div>

          <Button
            type="submit"
            disabled={isLoading}
            className="w-full bg-gradient-to-r from-teal-600 to-emerald-600 hover:from-teal-700 hover:to-emerald-700 text-white py-3 rounded-lg font-medium transition-all shadow-lg hover:shadow-xl"
          >
            {isLoading ? (
              <span className="flex items-center justify-center">
                <svg className="animate-spin -ml-1 mr-3 h-5 w-5 text-white" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
                  <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle>
                  <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                </svg>
                Signing in...
              </span>
            ) : (
              'Sign In'
            )}
          </Button>
        </form>

        <div className="mt-8 pt-6 border-t border-gray-200">
          <p className="text-sm text-gray-600 text-center mb-4">Quick Login (Demo)</p>
          <div className="grid grid-cols-3 gap-2">
            <Button
              type="button"
              onClick={() => quickLogin('admin')}
              disabled={isLoading}
              variant="outline"
              className="text-xs py-2 border-teal-200 hover:bg-teal-50 hover:border-teal-300"
            >
              Admin
            </Button>
            <Button
              type="button"
              onClick={() => quickLogin('hr')}
              disabled={isLoading}
              variant="outline"
              className="text-xs py-2 border-emerald-200 hover:bg-emerald-50 hover:border-emerald-300"
            >
              HR
            </Button>
            <Button
              type="button"
              onClick={() => quickLogin('finance')}
              disabled={isLoading}
              variant="outline"
              className="text-xs py-2 border-cyan-200 hover:bg-cyan-50 hover:border-cyan-300"
            >
              Finance
            </Button>
          </div>
        </div>

        <div className="mt-6 text-center">
          <p className="text-xs text-gray-500">
            Protected by enterprise-grade security
          </p>
        </div>
      </Card>
    </div>
  );
}

// Made with Bob
