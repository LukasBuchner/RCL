import { Outlet, useLocation, useSearchParams } from "react-router-dom";
import { useEffect } from "react";
import FlowNavbar from "../components/FlowNavbar";
import Timeline from "../components/Timeline";

export default function RootLayout() {
  const location = useLocation();
  const [searchParams] = useSearchParams();
  const isFlowView = location.pathname === "/";
  const showTimeline =
    isFlowView && searchParams.get("view") !== "hide-timeline";

  // Hide tooltips on route changes
  useEffect(() => {
    document
      .querySelectorAll('[data-bs-toggle="tooltip"]')
      .forEach((element) => {
        const tooltip = (
          window as typeof window & {
            bootstrap?: {
              Tooltip?: {
                getInstance: (element: Element) => { hide: () => void } | null;
              };
            };
          }
        ).bootstrap?.Tooltip?.getInstance(element);
        tooltip?.hide();
      });
  }, [location.pathname]);

  return (
    <div className="app-container">
      <FlowNavbar />
      {showTimeline && <Timeline />}

      <main className="app-main">
        <Outlet />
      </main>
    </div>
  );
}
