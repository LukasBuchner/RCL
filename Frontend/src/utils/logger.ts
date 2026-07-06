/**
 * Leveled, namespaced logger for the web frontend.
 *
 * This is the ONLY module permitted to call `console.*` (enforced by the ESLint
 * `no-console` rule). All application code logs through `createLogger`.
 *
 * Best practice:
 *  - Use one namespaced logger per module: `const log = createLogger("Flow");`.
 *  - Level semantics:
 *      error — an unexpected failure a developer must see (logged alongside, never
 *              instead of, user-facing `ErrorContext`/`addError`).
 *      warn  — a handled but notable condition (e.g. WebSocket reconnect).
 *      info  — low-frequency lifecycle one-shots (connection opened, procedure loaded).
 *      debug — mutation/subscription lifecycle, summarized to counts/ids.
 *      trace — per-render / per-item firehose.
 *  - Never log per render or per item above `trace`, and only via the `*Lazy` form so
 *    the payload is built solely when the level is active.
 *  - Summarize collections (log a count, not the whole array). Pass live objects, not
 *    pre-stringified text, so DevTools can inspect them.
 *  - Developer logs are not user-facing errors: surface those through the ErrorContext.
 *  - Never log secrets, tokens, PII, or raw request/response bodies.
 *
 * Active level resolution (first match wins): URL `?logLevel=<level>`,
 * `localStorage.logLevel`, then the environment default (`debug` in dev, `warn` in prod).
 * In development, `window.__setLogLevel(level)` flips the level live without a rebuild.
 */

export type LogLevel = "silent" | "error" | "warn" | "info" | "debug" | "trace";

type EmittableLevel = Exclude<LogLevel, "silent">;
type LazyArgs = () => readonly unknown[];

const RANK: Record<LogLevel, number> = {
  silent: 0,
  error: 1,
  warn: 2,
  info: 3,
  debug: 4,
  trace: 5,
};

function isLogLevel(value: string | null): value is LogLevel {
  return value !== null && Object.prototype.hasOwnProperty.call(RANK, value);
}

function resolveInitialLevel(): LogLevel {
  if (typeof window !== "undefined") {
    try {
      const fromUrl = new URLSearchParams(window.location.search).get(
        "logLevel",
      );
      if (isLogLevel(fromUrl)) return fromUrl;
    } catch {
      // Ignore malformed location/search access.
    }
    try {
      const fromStorage = window.localStorage.getItem("logLevel");
      if (isLogLevel(fromStorage)) return fromStorage;
    } catch {
      // Ignore storage access errors (private mode, disabled storage, etc.).
    }
  }
  return import.meta.env.DEV ? "debug" : "warn";
}

let currentLevel: LogLevel = resolveInitialLevel();

function resolveInitialFilter(): string | null {
  if (typeof window === "undefined") return null;
  try {
    const fromUrl = new URLSearchParams(window.location.search).get(
      "logFilter",
    );
    if (fromUrl) return fromUrl.toLowerCase();
  } catch {
    // Ignore malformed location/search access.
  }
  try {
    const fromStorage = window.localStorage.getItem("logFilter");
    if (fromStorage) return fromStorage.toLowerCase();
  } catch {
    // Ignore storage access errors (private mode, disabled storage, etc.).
  }
  return null;
}

let currentFilter: string | null = resolveInitialFilter();

/**
 * Receives every emitted log line. Used to forward logs to an external service.
 * No sinks are registered by default, so there is no behavioural change until one is added.
 */
export type LogSink = (
  level: EmittableLevel,
  namespace: string,
  args: readonly unknown[],
) => void;

const sinks: LogSink[] = [];

/**
 * Registers an external log sink (e.g. an error-reporting service).
 * Returns a function that unregisters the sink.
 * @param sink The sink to forward emitted log lines to.
 * @returns A function that removes the sink.
 */
export function addLogSink(sink: LogSink): () => void {
  sinks.push(sink);
  return () => {
    const index = sinks.indexOf(sink);
    if (index >= 0) sinks.splice(index, 1);
  };
}

/**
 * Sets the active log level at runtime.
 * @param level The minimum level that will be emitted.
 */
export function setLogLevel(level: LogLevel): void {
  currentLevel = level;
}

/**
 * Returns the active log level.
 * @returns The minimum level currently being emitted.
 */
export function getLogLevel(): LogLevel {
  return currentLevel;
}

/**
 * Restricts `info`/`debug`/`trace` output to lines whose `[namespace] message` head contains
 * the given substring (case-insensitive). `error` and `warn` are never filtered. This is a
 * console-display convenience for focusing on one source during debugging.
 * @param filter The substring to match, or null to clear the filter.
 */
export function setLogFilter(filter: string | null): void {
  currentFilter = filter ? filter.toLowerCase() : null;
}

/**
 * Returns the active log filter substring (lowercased), or null when no filter is set.
 * @returns The active filter substring, or null.
 */
export function getLogFilter(): string | null {
  return currentFilter;
}

function isEnabled(level: EmittableLevel): boolean {
  return RANK[level] <= RANK[currentLevel];
}

function passesFilter(
  level: EmittableLevel,
  namespace: string,
  args: readonly unknown[],
): boolean {
  if (currentFilter === null) return true;
  if (level === "error" || level === "warn") return true;
  const head = `[${namespace}] ${typeof args[0] === "string" ? args[0] : ""}`;
  return head.toLowerCase().includes(currentFilter);
}

const CONSOLE: Record<EmittableLevel, (...args: unknown[]) => void> = {
  error: (...args) => console.error(...args),
  warn: (...args) => console.warn(...args),
  info: (...args) => console.log(...args),
  debug: (...args) => console.debug(...args),
  trace: (...args) => console.debug(...args),
};

function emit(
  level: EmittableLevel,
  namespace: string,
  args: readonly unknown[],
): void {
  if (!passesFilter(level, namespace, args)) return;
  CONSOLE[level](...(namespace ? [`[${namespace}]`, ...args] : args));
  for (const sink of sinks) {
    try {
      sink(level, namespace, args);
    } catch {
      // A faulty sink must never break logging or the application.
    }
  }
}

/**
 * A namespaced logger. Each method emits only when its level is at or above the active level.
 * The `*Lazy` variants build their payload only when the level is active, making them safe
 * to call on hot paths (per-render, per-item).
 */
export interface Logger {
  error(...args: unknown[]): void;
  warn(...args: unknown[]): void;
  info(...args: unknown[]): void;
  debug(...args: unknown[]): void;
  trace(...args: unknown[]): void;
  errorLazy(make: LazyArgs): void;
  warnLazy(make: LazyArgs): void;
  infoLazy(make: LazyArgs): void;
  debugLazy(make: LazyArgs): void;
  traceLazy(make: LazyArgs): void;
  readonly namespace: string;
}

function eager(level: EmittableLevel, namespace: string) {
  return (...args: unknown[]): void => {
    if (isEnabled(level)) emit(level, namespace, args);
  };
}

function lazy(level: EmittableLevel, namespace: string) {
  return (makeArgs: LazyArgs): void => {
    if (isEnabled(level)) emit(level, namespace, makeArgs());
  };
}

/**
 * Creates a logger that prefixes every line with `[namespace]`.
 * @param namespace A short module identifier, e.g. "Flow" or "RouterNode".
 * @returns A logger bound to the given namespace.
 */
export function createLogger(namespace: string): Logger {
  return {
    namespace,
    error: eager("error", namespace),
    warn: eager("warn", namespace),
    info: eager("info", namespace),
    debug: eager("debug", namespace),
    trace: eager("trace", namespace),
    errorLazy: lazy("error", namespace),
    warnLazy: lazy("warn", namespace),
    infoLazy: lazy("info", namespace),
    debugLazy: lazy("debug", namespace),
    traceLazy: lazy("trace", namespace),
  };
}

/** Default, unnamespaced logger for ad-hoc use. Prefer a namespaced logger in modules. */
export const logger: Logger = createLogger("");

if (import.meta.env.DEV && typeof window !== "undefined") {
  const devWindow = window as unknown as {
    __setLogLevel?: (level: LogLevel) => void;
    __setLogFilter?: (filter: string | null) => void;
    __clearLogFilter?: () => void;
  };
  devWindow.__setLogLevel = setLogLevel;
  devWindow.__setLogFilter = setLogFilter;
  devWindow.__clearLogFilter = () => setLogFilter(null);
}
