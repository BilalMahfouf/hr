import { createContext } from "react";
import type { HubConnection } from "@microsoft/signalr";

/**
 * Context for accessing the SignalR HubConnection instance.
 */
export const SignalRContext = createContext<HubConnection | null>(null);
