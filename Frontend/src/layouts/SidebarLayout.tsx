import React from "react";
import { NavLink, Outlet } from "react-router-dom";
import { Nav } from "react-bootstrap";

export interface SidebarLink {
  path: string;
  label: string;
  icon: string;
}

interface SidebarLayoutProps {
  title: string;
  subtitle: string;
  headerIcon: string;
  sidebarLinks: SidebarLink[];
}

const SidebarLayout: React.FC<SidebarLayoutProps> = ({
  title,
  subtitle,
  headerIcon,
  sidebarLinks,
}) => {
  return (
    <div className="management-page">
      <div className="management-header mb-4 p-4 bg-gradient rounded-3 border">
        <div className="d-flex align-items-center">
          <div className="header-icon me-3">
            <i className={`${headerIcon} display-6 text-primary`}></i>
          </div>
          <div>
            <h2 className="mb-1 fw-bold">{title}</h2>
            <p className="mb-0 text-muted">{subtitle}</p>
          </div>
        </div>
      </div>

      <div className="management-content">
        <div className="navigation-sidebar">
          <Nav variant="pills" className="flex-column nav-sidebar">
            {sidebarLinks.map((link) => (
              <Nav.Item key={link.path} className="mb-1">
                <Nav.Link
                  as={NavLink}
                  to={link.path}
                  className="nav-sidebar-link"
                  end={link.path.endsWith("/")}
                >
                  <i className={`${link.icon} me-2`}></i>
                  <span>{link.label}</span>
                  <i className="bi bi-chevron-right ms-auto"></i>
                </Nav.Link>
              </Nav.Item>
            ))}
          </Nav>
        </div>

        <div className="content-area">
          <Outlet />
        </div>
      </div>

      <style>{`
        .management-header {
          background: var(--app-navbar-bg) !important;
          border: 1px solid var(--app-border) !important;
        }
        
        .header-icon {
          width: 60px;
          height: 60px;
          display: flex;
          align-items: center;
          justify-content: center;
          background: var(--app-card-bg);
          border-radius: 12px;
          box-shadow: 0 2px 8px var(--app-shadow);
        }
        
        .navigation-sidebar {
          background: var(--app-surface);
          border-radius: 12px;
          padding: 1rem;
          border: 1px solid var(--app-border);
        }
        
        .nav-sidebar .nav-link {
          border-radius: 8px;
          padding: 0.75rem 1rem;
          color: var(--app-text);
          font-weight: 500;
          border: 1px solid transparent;
          transition: all 0.2s ease;
          display: flex;
          align-items: center;
        }
        
        .nav-sidebar .nav-link:hover {
          background: var(--app-surface-variant);
          color: var(--app-primary);
          transform: translateX(4px);
          border-color: var(--app-surface-variant);
        }
        
        .nav-sidebar .nav-link.active {
          background: var(--app-primary);
          color: var(--app-white);
          box-shadow: 0 2px 8px var(--app-focus-shadow-primary);
          border-color: var(--app-primary);
        }
        
        .nav-sidebar .nav-link.active:hover {
          background: var(--app-primary);
          transform: translateX(4px);
        }
        
        .nav-sidebar .nav-link i:first-child {
          width: 20px;
          text-align: center;
        }
        
        .nav-sidebar .nav-link .bi-chevron-right {
          font-size: 0.8rem;
          opacity: 0.6;
          transition: transform 0.2s ease;
        }
        
        .nav-sidebar .nav-link:hover .bi-chevron-right,
        .nav-sidebar .nav-link.active .bi-chevron-right {
          transform: translateX(2px);
        }
        
        .content-area {
          background: var(--app-card-bg);
          border-radius: 12px;
          flex: 1;
          min-height: 0;
          overflow: hidden;
          display: flex;
          flex-direction: column;
          padding: 0;
          position: relative;
        }
        
        .management-page {
          padding: 1rem;
          height: 100%;
          overflow: hidden;
        }
        
        .management-content {
          display: flex;
          height: calc(100vh - 200px);
          gap: 1rem;
        }
        
        .navigation-sidebar {
          width: 250px;
          flex-shrink: 0;
        }
        
        .content-area > * {
          height: calc(100vh - 250px);
          overflow: auto;
          display: flex;
          flex-direction: column;
          flex: 1;
          border-radius: 12px;
        }
      `}</style>
    </div>
  );
};

export default SidebarLayout;
