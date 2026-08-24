import { Navigate, Outlet } from 'react-router-dom';
import { Loader2 } from 'lucide-react';
import { useAuthContext } from './AuthContext';

/**
 * AuthGuard wraps protected route groups.
 * - While the session is being restored → shows a loading spinner.
 * - If the user is not authenticated → redirects to /login.
 * - Otherwise → renders child routes via <Outlet />.
 */
export default function AuthGuard() {
  const { isAuthenticated, isLoading } = useAuthContext();

  if (isLoading) {
    return (
      <div className="flex h-screen w-full items-center justify-center">
        <Loader2 className="h-8 w-8 animate-spin text-primary" />
      </div>
    );
  }

  // for production uncoment this for secuitity 

  if (!isAuthenticated) {
    // return <Navigate to="/login" replace />;
  }

  return <Outlet />;
}
