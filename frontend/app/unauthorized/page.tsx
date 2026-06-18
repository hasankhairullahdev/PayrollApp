'use client';

import { useAuth } from '@/lib/auth-context';
import { Button } from '@/components/ui/button';
import { Card } from '@/components/ui/card';
import { useRouter } from 'next/navigation';

export default function UnauthorizedPage() {
  const { user, logout } = useAuth();
  const router = useRouter();

  return (
    <div className="min-h-screen flex items-center justify-center bg-gradient-to-br from-red-50 via-orange-50 to-yellow-50 p-4">
      <Card className="w-full max-w-md p-8 shadow-2xl border-red-100">
        <div className="text-center">
          <div className="inline-flex items-center justify-center w-16 h-16 rounded-full bg-gradient-to-br from-red-500 to-orange-600 mb-4">
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
                d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z"
              />
            </svg>
          </div>

          <h1 className="text-3xl font-bold text-gray-900 mb-2">Access Denied</h1>
          <p className="text-gray-600 mb-6">
            You don't have permission to access this resource.
          </p>

          {user && (
            <div className="mb-6 p-4 bg-gray-50 rounded-lg border border-gray-200">
              <p className="text-sm text-gray-600 mb-1">Logged in as:</p>
              <p className="font-semibold text-gray-900">{user.fullName}</p>
              <p className="text-sm text-gray-500">{user.email}</p>
              <div className="mt-2">
                <span className="inline-flex items-center px-3 py-1 rounded-full text-xs font-medium bg-blue-100 text-blue-800">
                  {user.role}
                </span>
              </div>
            </div>
          )}

          <div className="space-y-3">
            <Button
              onClick={() => router.push('/')}
              className="w-full bg-gradient-to-r from-teal-600 to-emerald-600 hover:from-teal-700 hover:to-emerald-700"
            >
              Go to Dashboard
            </Button>
            <Button
              onClick={() => router.back()}
              variant="outline"
              className="w-full border-gray-300"
            >
              Go Back
            </Button>
            <Button
              onClick={logout}
              variant="outline"
              className="w-full border-red-300 text-red-600 hover:bg-red-50"
            >
              Sign Out
            </Button>
          </div>

          <div className="mt-6 text-center">
            <p className="text-xs text-gray-500">
              If you believe this is an error, please contact your administrator.
            </p>
          </div>
        </div>
      </Card>
    </div>
  );
}

// Made with Bob
