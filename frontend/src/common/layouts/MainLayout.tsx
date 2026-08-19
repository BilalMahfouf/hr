import { Outlet } from "react-router-dom";
import Layout from "./Layout";
import { SignalRProvider } from "@/lib/signalr/signalr-context";

export default function MainLayout() {
    
  return (
    <SignalRProvider >
    <Layout>
      <Outlet /> {/* This is where child routes render */}
    </Layout>
    </SignalRProvider>
  );
}