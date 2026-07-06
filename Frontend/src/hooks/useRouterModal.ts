import { useLocation, useNavigate, useParams } from "react-router-dom";
import { useCallback, useMemo } from "react";

export interface RouterModalConfig {
  basePath: string;
  createPath: string;
  editPath: string;
  idParam: string;
}

export function useRouterModal(config: RouterModalConfig) {
  const navigate = useNavigate();
  const params = useParams();
  const location = useLocation();

  // More precise path matching to avoid false positives
  const isModalOpen = useMemo(() => {
    const currentPath = location.pathname;

    // Handle root path specially
    if (config.basePath === "/") {
      const createFullPath = `/${config.createPath}`;

      // For skill/task configs, check specific patterns
      if (config.createPath === "skill/create") {
        const isCreatePath = currentPath === "/skill/create";
        const isEditPath = !!currentPath.match(/^\/skill\/[^/]+\/edit$/);
        const isOpen = isCreatePath || isEditPath;
        return isOpen;
      }

      if (config.createPath === "task/create") {
        const isCreatePath = currentPath === "/task/create";
        const isEditPath = !!currentPath.match(/^\/task\/[^/]+\/edit$/);
        const isOpen = isCreatePath || isEditPath;
        return isOpen;
      }

      if (config.createPath === "router/create") {
        const isCreatePath = currentPath === "/router/create";
        const isEditPath = !!currentPath.match(/^\/router\/[^/]+\/edit$/);
        const isOpen = isCreatePath || isEditPath;
        return isOpen;
      }

      // Fallback for other root-based modals
      const isOpen =
        currentPath === createFullPath ||
        !!currentPath.match(/^\/[^/]+\/[^/]+\/edit$/);
      return isOpen;
    }

    // Handle other paths normally
    const createFullPath = `${config.basePath}/${config.createPath}`;
    const editPathPattern = `${config.basePath}/`;

    return (
      currentPath === createFullPath ||
      (currentPath.startsWith(editPathPattern) && currentPath.endsWith("/edit"))
    );
  }, [location.pathname, config.basePath, config.createPath]);

  const isEditing = useMemo(() => {
    return location.pathname.endsWith("/edit");
  }, [location.pathname]);

  const entityId = useMemo(() => {
    return params[config.idParam] || null;
  }, [params, config.idParam]);

  // Modal actions
  const openCreateModal = useCallback(() => {
    const path =
      config.basePath === "/"
        ? `/${config.createPath}`
        : `${config.basePath}/${config.createPath}`;
    navigate(path);
  }, [navigate, config.basePath, config.createPath]);

  const openEditModal = useCallback(
    (id: string) => {
      const path =
        config.basePath === "/"
          ? `/${id}/edit`
          : `${config.basePath}/${id}/edit`;
      navigate(path);
    },
    [navigate, config.basePath],
  );

  const closeModal = useCallback(() => {
    navigate(config.basePath);
  }, [navigate, config.basePath]);

  return {
    isModalOpen,
    isEditing,
    entityId,
    openCreateModal,
    openEditModal,
    closeModal,
  };
}

// Specific configurations for different modal types
export const MODAL_CONFIGS = {
  AGENT: {
    basePath: "/management/agents",
    createPath: "create",
    editPath: "edit",
    idParam: "agentId",
  },
  SKILL: {
    basePath: "/management/skills",
    createPath: "create",
    editPath: "edit",
    idParam: "skillId",
  },
  POSITION_TAG: {
    basePath: "/management/position-tags",
    createPath: "create",
    editPath: "edit",
    idParam: "positionTagId",
  },
  SCENE_OBJECT: {
    basePath: "/management/scene-objects",
    createPath: "create",
    editPath: "edit",
    idParam: "sceneObjectId",
  },
  SKILL_CONFIG: {
    basePath: "/",
    createPath: "skill/create",
    editPath: "skill/edit",
    idParam: "nodeId",
  },
  TASK_CONFIG: {
    basePath: "/",
    createPath: "task/create",
    editPath: "task/edit",
    idParam: "nodeId",
  },
  ROUTER_CONFIG: {
    basePath: "/",
    createPath: "router/create",
    editPath: "router/edit",
    idParam: "nodeId",
  },
} as const;
