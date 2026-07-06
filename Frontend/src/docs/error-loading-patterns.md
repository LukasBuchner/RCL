# Unified Error Handling and Loading State Patterns

This document describes the unified error handling and loading state patterns implemented in the application.

## Overview

The application uses a centralized approach for handling errors and loading states to provide a consistent user experience across all components.

## Error Handling

### Components

1. **ErrorProvider** - Context provider for global error management
2. **ErrorBoundary** - React error boundary for catching component errors
3. **ErrorState** - Display component for error messages with retry capabilities
4. **ErrorToastContainer** - Toast notifications for non-critical errors

### Usage

#### Basic Error Handling
```tsx
import { useError } from "../hooks";

function MyComponent() {
  const { handleError } = useError();
  
  try {
    // Some operation
  } catch (error) {
    handleError(error, {
      source: "validation",
      component: "MyComponent"
    });
  }
}
```

#### Apollo Error Handling
```tsx
import { useApolloError } from "../hooks";

function MyComponent() {
  const { data, loading, error } = useQuery(MY_QUERY);
  
  // Automatically handles and displays Apollo errors
  useApolloError(error, {
    componentName: "MyComponent",
    operation: "MyQuery"
  });
}
```

#### Error State Component
```tsx
import { ErrorState } from "../components/error";

<ErrorState
  title="Custom Error"
  message="Something went wrong"
  severity="error"
  onRetry={() => refetch()}
/>
```

### Error Severities

- **info** - Informational messages
- **warning** - Non-critical issues
- **error** - Standard errors
- **critical** - System-critical errors (full-screen display)

## Loading States

### Components

1. **LoadingProvider** - Context provider for global loading management
2. **LoadingState** - Main loading display component
3. **LoadingOverlay** - Overlay for mutation/action loading
4. **SkeletonLoader** - Skeleton screens for better perceived performance

### Usage

#### Basic Loading State
```tsx
import { LoadingState } from "../components/loading";

if (loading) {
  return <LoadingState text="Loading data..." />;
}
```

#### Loading with Progress
```tsx
<LoadingState 
  text="Uploading files..." 
  progress={75} 
/>
```

#### Loading Overlay
```tsx
import { LoadingOverlay } from "../components/loading";

<div className="position-relative">
  <LoadingOverlay show={isMutating} text="Saving..." />
  {/* Your content */}
</div>
```

#### Skeleton Loaders
```tsx
import { SkeletonCard, SkeletonListItem } from "../components/loading";

// While loading
if (loading) {
  return (
    <>
      <SkeletonCard />
      <SkeletonListItem />
    </>
  );
}
```

#### Using Loading Hook
```tsx
import { useLoadingState } from "../hooks";

function MyComponent() {
  const { startLoading, stopLoading, updateProgress } = useLoadingState("myOperation");
  
  const handleUpload = async () => {
    startLoading("Uploading file...");
    
    // Simulate progress
    for (let i = 0; i <= 100; i += 10) {
      updateProgress(i);
      await delay(100);
    }
    
    stopLoading();
  };
}
```

## Async State Management

The `useAsyncState` hook combines loading and error handling:

```tsx
import { useAsyncState } from "../hooks";

function MyComponent() {
  const { data, isLoading, error, execute } = useAsyncState("myOperation", {
    onSuccess: (data) => console.log("Success!", data),
    onError: (error) => console.error("Failed!", error)
  });
  
  const handleAction = () => {
    execute(async () => {
      const response = await fetch("/api/data");
      return response.json();
    });
  };
  
  if (isLoading) return <LoadingState />;
  if (error) return <ErrorState message={error.message} onRetry={handleAction} />;
  
  return <div>{data}</div>;
}
```

## Best Practices

1. **Always use the provided components** instead of creating custom error/loading UI
2. **Handle errors at the appropriate level** - use ErrorBoundary for critical errors, toast for minor ones
3. **Provide meaningful error messages** that help users understand what went wrong
4. **Include retry functionality** where appropriate
5. **Use skeleton loaders** for better perceived performance
6. **Show progress** for long-running operations
7. **Differentiate between initial load and refetch** states

## Migration Guide

To migrate existing components:

1. Replace `LoadingState` imports:
   ```tsx
   // Old
   import LoadingState from "./components/states/LoadingState";
   
   // New
   import { LoadingState } from "./components/loading";
   ```

2. Replace `ErrorState` imports:
   ```tsx
   // Old
   import ErrorState from "./components/states/ErrorState";
   
   // New
   import { ErrorState } from "./components/error";
   ```

3. Add error handling to Apollo queries:
   ```tsx
   const { data, loading, error } = useQuery(MY_QUERY);
   useApolloError(error, { componentName: "MyComponent" });
   ```

4. Wrap async operations with `useAsyncState` for automatic error/loading handling