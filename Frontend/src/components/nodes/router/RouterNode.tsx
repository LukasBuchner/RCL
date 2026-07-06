import { Node, NodeProps, useEdges, useReactFlow } from "@xyflow/react";
import React, { useCallback, useMemo } from "react";
import { Form } from "react-bootstrap";
import { useTranslation } from "react-i18next";
import RouterNodeToolbar from "./RouterNodeToolbar";
import { RouterBasicData } from "../../../types/nodeTypes";
import BaseNode, { MIN_CONTAINER_WIDTH, nodeBoxWidth } from "../BaseNode";
import NodeLabel from "../NodeLabel";
import { useMutation, useQuery } from "@apollo/client";
import {
  GetNodeByIdDocument,
  GetNodeByIdQuery,
  NodeInput,
  UpdateNodeDocument,
  UpdateNodeMutation,
  UpdateNodeMutationVariables,
} from "../../../__generated__/graphql";
import { createLogger } from "../../../utils/logger";

const log = createLogger("RouterNode");

/**
 * RouterNode component displays a router node with conditional branching capabilities.
 *
 * The component features a branch selector dropdown positioned inside the node,
 * directly below the router name. The backend calculates additional height (RouterDropdownHeight = 26px)
 * to accommodate the dropdown when branches exist. The dropdown allows users to manually select
 * which branch path to take during execution.
 *
 * Key features:
 * - Branch selector dropdown positioned below name (inside node bounds)
 * - Backend adds RouterDropdownHeight (26px) when branches exist
 * - Dropdown uses available node width for optimal display
 * - 4px margin provides spacing between name row and dropdown
 * - No overlap with node content or progress bar
 *
 * @param props - Node properties including router data with branches and selected branch
 * @returns The RouterNode component
 */
function RouterNode(props: NodeProps<Node<RouterBasicData, "routerNode">>) {
  const { id, parentId, data } = props;
  const { t } = useTranslation();
  const { setNodes } = useReactFlow();
  const edges = useEdges();
  const hasRightEdge = useMemo(
    () => edges.some((e) => e.source === id && e.sourceHandle === "right"),
    [edges, id],
  );

  const [updateNode] = useMutation<UpdateNodeMutation>(UpdateNodeDocument);

  const { data: nodeData } = useQuery<GetNodeByIdQuery>(GetNodeByIdDocument, {
    variables: { id },
    skip: false,
    fetchPolicy: "cache-first",
  });

  const branchCount = data.branches?.length || 0;
  // During execution, selectedBranch (from execution) takes priority over manuallySelectedBranch (from design)
  const currentBranch = data.selectedBranch || data.manuallySelectedBranch;
  const branches = data.branches || [];

  log.traceLazy(() => [
    "render",
    {
      nodeId: id,
      nodeName: data.name,
      "data.selectedBranch": data.selectedBranch,
      "data.manuallySelectedBranch": data.manuallySelectedBranch,
      "computed currentBranch": currentBranch,
      branches: branches.map((b) => ({ name: b.name, priority: b.priority })),
    },
  ]);

  const handleBranchSelection = useCallback(
    async (e: React.ChangeEvent<HTMLSelectElement>) => {
      const branchName = e.target.value;

      log.debug("selection start", {
        nodeId: id,
        selectedBranchName: branchName,
        isEmpty: branchName === "",
      });

      // Update local state immediately for responsive UI
      setNodes((nodes) =>
        nodes.map((node) => {
          if (node.id === id) {
            log.debug("local state update", {
              nodeId: id,
              oldSelectedBranch: node.data.selectedBranch,
              oldManuallySelectedBranch: node.data.manuallySelectedBranch,
              newValue: branchName || undefined,
            });
            return {
              ...node,
              data: {
                ...node.data,
                selectedBranch: branchName || undefined,
                manuallySelectedBranch: branchName || undefined,
              },
            };
          }
          return node;
        }),
      );

      // Persist to backend
      try {
        const routerNode = nodeData?.nodeById;

        if (!routerNode || routerNode.__typename !== "RouterNode") {
          log.error("Node data not available or not a router node");
          return;
        }

        const routerTask = routerNode.routerTask;

        log.debug("backend data", {
          nodeId: id,
          routerTask: {
            name: routerTask.name,
            manuallySelectedBranch: routerTask.manuallySelectedBranch,
            branches: routerTask.branches.map((b) => b.name),
          },
        });

        const nodeInput: NodeInput = {
          routerNode: {
            id,
            position: {
              x: routerNode.position.x,
              y: routerNode.position.y,
            },
            parentId: routerNode.parentId ?? null,
            width: routerNode.width,
            height: routerNode.height,
            routerTaskInput: {
              name: routerTask.name,
              description: routerTask.description ?? "",
              startTime: routerTask.startTime,
              duration: routerTask.duration,
              selector: {
                simpleVariableSelector: {
                  expression: routerTask.selector.expression,
                },
              },
              branches: routerTask.branches.map((b) => ({
                name: b.name,
                condition: b.condition ?? null,
                priority: b.priority,
                targetNodeId: b.targetNodeId ?? null,
              })),
              manuallySelectedBranch: branchName || null,
            },
          },
        };

        log.debug("mutation input", {
          nodeId: id,
          manuallySelectedBranch:
            nodeInput.routerNode.routerTaskInput.manuallySelectedBranch,
          branches: nodeInput.routerNode.routerTaskInput.branches.map(
            (b) => b.name,
          ),
        });

        const result = await updateNode({
          variables: {
            input: { nodeInput },
          } as UpdateNodeMutationVariables,
        });

        log.debug("mutation success", {
          nodeId: id,
          selectedBranchName: branchName,
          result,
        });
      } catch (error) {
        log.error("mutation error", { nodeId: id, error });
      }
    },
    [id, setNodes, updateNode, nodeData],
  );

  const formatBranchOptionLabel = (branch: {
    name: string;
    condition?: string;
    priority: number;
  }) => {
    if (branch.priority === 999) {
      return `${branch.name} ${t("router.defaultLabel")}`;
    }
    return branch.condition
      ? `${branch.name} (${branch.condition})`
      : branch.name;
  };

  return (
    <BaseNode
      {...props}
      toolbarContent={<RouterNodeToolbar nodeId={id} parentId={parentId} />}
      backgroundColor="var(--bs-warning-bg-subtle)"
      minWidth={MIN_CONTAINER_WIDTH}
    >
      <div
        className="d-flex flex-column"
        title={t("router.branchesConfigured", { count: branchCount })}
        style={{
          padding: "3px 4px",
          lineHeight: "1",
          gap: "3px",
          height: "100%",
          boxSizing: "border-box", // Include padding in height calculation
          position: "relative",
        }}
      >
        {/* Row 1: Icon + Name */}
        <div className="d-flex align-items-center gap-1">
          <i
            className="bi bi-signpost-split text-warning"
            style={{ fontSize: "0.85rem", flexShrink: 0 }}
            role="img"
            aria-hidden="true"
          ></i>
          <NodeLabel
            name={data.name}
            maxWidth={nodeBoxWidth(data, MIN_CONTAINER_WIDTH) - 40}
            bold={false}
            fontSize="0.75rem"
            hasRightEdge={hasRightEdge}
          />
        </div>

        {/* Row 2: Dropdown - Positioned inside node, below name row */}
        {branches.length > 0 && (
          <div
            className="d-flex align-items-center"
            style={{
              marginTop: "4px", // Spacing from name row
            }}
          >
            {/* Branch selector dropdown */}
            <Form.Select
              size="sm"
              value={currentBranch || ""}
              onChange={handleBranchSelection}
              aria-label={t("router.selectBranch")}
              className="router-dropdown"
              style={{
                fontSize: "0.7rem",
                padding: "1px 20px 1px 4px",
                height: "20px",
                flex: "1 1 auto",
                minWidth: 0,
                lineHeight: "1",

                // Fix for node selection outline cutting through dropdown
                position: "relative",
                zIndex: 150, // Very high - above BaseNode's border overlay (which is at z-index 0)

                // Solid background to cover node border
                backgroundColor: "var(--bs-warning-bg-subtle)",

                // Strong visible border
                border: "1px solid rgba(var(--bs-warning-rgb), 0.5)",
                borderRadius: "0.25rem",

                // Box-shadow creates visual separation from node border
                boxShadow:
                  "0 1px 3px rgba(0, 0, 0, 0.12), 0 0 0 2px var(--bs-warning-bg-subtle)",

                // Extend beyond node padding to avoid outline overlap
                marginLeft: "-2px",
                marginRight: "-2px",
                marginBottom: "-2px",
              }}
            >
              <option value="">{t("router.chooseBranch")}</option>
              {branches.map((branch) => (
                <option key={branch.name} value={branch.name}>
                  {formatBranchOptionLabel(branch)}
                </option>
              ))}
            </Form.Select>
          </div>
        )}
      </div>
    </BaseNode>
  );
}

export default RouterNode;
