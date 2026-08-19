import { createContext, useContext } from 'react';

interface AuthContextType {
  isAuthenticated: boolean;
  isLoading: boolean;
}

export const AuthContext = createContext<AuthContextType>({
  isAuthenticated: false,
  isLoading: true,
});

export const useAuthContext = () => useContext(AuthContext);
