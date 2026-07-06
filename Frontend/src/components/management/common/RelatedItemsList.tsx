import React from "react";

interface RelatedItemsListProps {
  icon: string;
  label: string;
  count: number;
  items?: Array<{ id: string; name: string }>;
  emptyText?: string;
  badgeVariant?: string;
  maxItems?: number;
}

export const RelatedItemsList: React.FC<RelatedItemsListProps> = ({
  icon,
  label,
  count,
  items = [],
  emptyText = "None assigned",
  badgeVariant = "info",
  maxItems = 5,
}) => {
  const displayItems = items.slice(0, maxItems);
  const hasMore = items.length > maxItems;

  return (
    <div className="related-items">
      <div className="d-flex align-items-center mb-2">
        <i
          className={`${icon} me-2`}
          style={{ color: "var(--app-text-muted)" }}
        ></i>
        <small className="text-muted fw-medium">{label}</small>
        <span className={`badge bg-${badgeVariant} ms-auto`}>{count}</span>
      </div>

      {count > 0 ? (
        <div className="d-flex flex-wrap gap-1">
          {displayItems.map((item) => (
            <span
              key={item.id}
              className={`badge bg-${badgeVariant} bg-opacity-75`}
            >
              {item.name}
            </span>
          ))}
          {hasMore && (
            <span className={`badge bg-${badgeVariant} bg-opacity-50`}>
              +{items.length - maxItems} more
            </span>
          )}
        </div>
      ) : (
        <small className="text-muted fst-italic">{emptyText}</small>
      )}
    </div>
  );
};
