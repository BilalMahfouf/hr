import { useContext } from "react";
import { SignalRContext } from "./SignalRContext";

/**
 * Hook to access the SignalR connection instance.
 * Returns null if not connected.
 */
export const useSignalR = () => useContext(SignalRContext);
