// App.tsx
import { createBrowserRouter, RouterProvider } from "react-router-dom";
import "@xyflow/react/dist/style.css";
import "bootstrap/dist/css/bootstrap.min.css";
import "./styles/motion-tokens.css";
// Bootstrap icons are loaded via CDN in index.html
import { AppProviders } from "./components/AppProviders";
import { routes } from "./router/routes";
import { useTheme } from "./hooks/useTheme";
import { createLogger } from "./utils/logger";

const log = createLogger("App");

log.trace("Routes loaded:", routes);
const router = createBrowserRouter(routes);
log.trace("Router created:", router);

export default function App() {
  // Initialise theme system
  useTheme();

  return (
    <AppProviders>
      <RouterProvider router={router} />
    </AppProviders>
  );
}
