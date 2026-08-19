import { useEffect, useState, type PropsWithChildren } from 'react';
import { tokenManager } from '@/lib/api/tokenManager';
import { AuthContext } from './AuthContext';

/**
 * AuthProvider restores the session on app startup via the httpOnly
 * refresh cookie, then keeps isAuthenticated in sync with tokenManager
 * through a lightweight subscription (no monkey-patching).
 */
export const AuthProvider = ({ children }: PropsWithChildren) => {
  const [isLoading, setIsLoading] = useState(true);
  const [isAuthenticated, setIsAuthenticated] = useState(false);

  useEffect(() => {
    // 1. Subscribe to token changes so login / logout / refresh keep the UI in sync
    const unsubscribe = tokenManager.subscribe((token) => {
      setIsAuthenticated(!!token);
    });

    // 2. Restore session on mount
    const initSession = async () => {
      // If a token already exists in memory (e.g. HMR), we're done
      if (tokenManager.getAccessToken()) {
        setIsLoading(false);
        return;
      }

      try {
        await tokenManager.refreshAccessToken();
        // setIsAuthenticated fires automatically via the subscription
      } catch {
        // Refresh cookie missing / expired — user must log in
      } finally {
        setIsLoading(false);
      }
    };

    initSession();

    return unsubscribe;
  }, []);

  return (
    <AuthContext.Provider value={{ isAuthenticated, isLoading }}>
      {children}
    </AuthContext.Provider>
  );
};
