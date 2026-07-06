import "@testing-library/jest-dom/vitest";
import { vi } from "vitest";
import { TextEncoder, TextDecoder } from "util";

// Polyfill TextEncoder/TextDecoder for jsdom
// eslint-disable-next-line @typescript-eslint/no-explicit-any
(global as any).TextEncoder = TextEncoder;
// eslint-disable-next-line @typescript-eslint/no-explicit-any
(global as any).TextDecoder = TextDecoder;

// Initialize i18n for tests (must be before any component import)
import "../i18n";

// Mock ResizeObserver (jsdom has no layout engine; React Flow observes node/pane sizes)
class ResizeObserverMock {
  observe() {}
  unobserve() {}
  disconnect() {}
}
// eslint-disable-next-line @typescript-eslint/no-explicit-any
(global as any).ResizeObserver = ResizeObserverMock;
// eslint-disable-next-line @typescript-eslint/no-explicit-any
(window as any).ResizeObserver = ResizeObserverMock;

// Mock window.matchMedia
Object.defineProperty(window, "matchMedia", {
  writable: true,
  value: vi.fn().mockImplementation((query) => ({
    matches: false,
    media: query,
    onchange: null,
    addListener: vi.fn(),
    removeListener: vi.fn(),
    addEventListener: vi.fn(),
    removeEventListener: vi.fn(),
    dispatchEvent: vi.fn(),
  })),
});
