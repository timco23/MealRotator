// Theme choice lives in the browser. Storage can be blocked (private window), so never let it throw.

export function get(key) {
  try {
    return localStorage.getItem(key) ?? "";
  } catch {
    return "";
  }
}

export function set(key, value) {
  try {
    localStorage.setItem(key, value);
  } catch {
    // Not persisted; the choice still applies for this session.
  }
}
