import { Navigate } from 'react-router-dom';
import { Loader2 } from 'lucide-react';
import { useCurrentUser } from './useCurrentUser';
import type { JSX } from 'react';

export default function RoleGuard({
  requiredRole,
  children,
}: {
  requiredRole: string;
  children: JSX.Element;
}) {
  const { data, isLoading } = useCurrentUser();

  if (isLoading) {
    return (
      <div className="flex h-screen w-full items-center justify-center">
        <Loader2 className="h-8 w-8 animate-spin text-primary" />
      </div>
    );
  }

  if (data?.role?.toLowerCase() !== requiredRole.toLowerCase()) {
    return <Navigate to="/dashboard" replace />;
  }

  return children;
}
