import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import "./index.css";
import "./i18n"; // Initialize i18n
import App from "./App.tsx";
import { ReactFlowProvider } from "@xyflow/react";
import { DevSupport } from "@react-buddy/ide-toolbox";
import { ComponentPreviews, useInitial } from "./dev";
import { WebSocketProvider } from "./contexts/WebSocketContext";
import { ApolloProvider } from "./providers/ApolloProvider";

createRoot(document.getElementById("root")!).render(
  <StrictMode>
    <WebSocketProvider>
      <ApolloProvider>
        <ReactFlowProvider>
          <DevSupport
            ComponentPreviews={ComponentPreviews}
            useInitialHook={useInitial}
          >
            <App />
          </DevSupport>
        </ReactFlowProvider>
      </ApolloProvider>
    </WebSocketProvider>
  </StrictMode>,
);
