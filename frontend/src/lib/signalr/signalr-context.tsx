import {
  useEffect,
  useState,
  type PropsWithChildren,
} from "react";
import {
  HubConnection,
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel,
} from "@microsoft/signalr";
import { useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { tokenManager } from "../api/tokenManager";
import { SignalRContext } from "./SignalRContext";

// ==================== Helper ====================

function buildConnection(): HubConnection | null {
    // Token must already exist – AuthProvider restores it before protected routes render
    const currentToken = tokenManager.getAccessToken();
    if (!currentToken) {
        return null;
    }

    const baseUrl = import.meta.env.VITE_API_URL || window.location.origin;
    const hubUrl = new URL("/hubs/notification", baseUrl).toString();

    const connection = new HubConnectionBuilder()
        .withUrl(hubUrl, {
            // Always read the *current* token so reconnections use a fresh value
            accessTokenFactory: () => tokenManager.getAccessToken() || "",
        })
        .withAutomaticReconnect([2000, 10000, 30000])
        .configureLogging(LogLevel.Warning)
        .build();

    return connection;
}

// ==================== Provider ====================

export const SignalRProvider = ({ children }: PropsWithChildren) => {
  const queryClient = useQueryClient();
  // Build connection instance once using lazy initializer
  const [connection] = useState<HubConnection | null>(() => {
      return buildConnection();
  });

  useEffect(() => {
    // 1. Event Handlers
    const foo = ()=>{
        if(!connection) return;
    connection.on("ReceiveNotification", (data: { notification?: { title?: string; body?: string } }) => {
      queryClient.invalidateQueries({ queryKey: ["notifications","unread"] });

      // Show toast notification
      if (data?.notification) {
        toast(data.notification.title || "New Notification", {
          description: data.notification.body,
        });
      } else {
        toast("New notification received");
      }
    });

    // 2. Connection state handlers
    connection.onreconnecting((error) => {
      console.warn("SignalR reconnecting...", error);
    });

    connection.onreconnected(() => {
    });

    connection.onclose((error) => {
      console.error("SignalR connection closed:", error);
    });

    // 3. Start connection
    connection.start()
      .then(() => {})
      .catch(() => {});

    // 4. Cleanup on unmount
    return () => {
      connection.off("ReceiveNotification");
      if (connection.state !== HubConnectionState.Disconnected) {
        connection.stop().catch((err) => {
          console.error("SignalR stop error:", err);
        });
      }
    };
 
    }
    foo();
 }, [connection, queryClient]);

  return (
    <SignalRContext.Provider value={connection}>
      {children}
    </SignalRContext.Provider>
  );
};