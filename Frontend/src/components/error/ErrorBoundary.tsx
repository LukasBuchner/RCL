import { Component, ErrorInfo, ReactNode } from "react";
import { Alert, Button } from "react-bootstrap";
import { MotionContainer } from "../motion";
import { createLogger } from "../../utils/logger";

const log = createLogger("ErrorBoundary");

interface Props {
  children: ReactNode;
  fallback?: (error: Error, resetError: () => void) => ReactNode;
  onError?: (error: Error, errorInfo: ErrorInfo) => void;
}

interface State {
  hasError: boolean;
  error: Error | null;
}

export class ErrorBoundary extends Component<Props, State> {
  constructor(props: Props) {
    super(props);
    this.state = { hasError: false, error: null };
  }

  static getDerivedStateFromError(error: Error): State {
    return { hasError: true, error };
  }

  componentDidCatch(error: Error, errorInfo: ErrorInfo) {
    log.error("ErrorBoundary caught an error:", error, errorInfo);
    this.props.onError?.(error, errorInfo);
  }

  resetError = () => {
    this.setState({ hasError: false, error: null });
  };

  render() {
    if (this.state.hasError && this.state.error) {
      if (this.props.fallback) {
        return this.props.fallback(this.state.error, this.resetError);
      }

      return (
        <MotionContainer className="p-4">
          <Alert variant="danger">
            <Alert.Heading>
              <i className="bi bi-exclamation-triangle me-2"></i>
              Something went wrong
            </Alert.Heading>
            <p>{this.state.error.message}</p>
            <hr />
            <div className="d-flex justify-content-end">
              <Button variant="outline-danger" onClick={this.resetError}>
                Try Again
              </Button>
            </div>
          </Alert>
        </MotionContainer>
      );
    }

    return this.props.children;
  }
}
