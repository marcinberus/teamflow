export const API_ROOT = '/api';
export const API_VERSION = 'v1';
export const API_BASE_URL = `${API_ROOT}/${API_VERSION}`;

export function isTeamFlowApiUrl(url: string): boolean {
  const path = url.split(/[?#]/, 1)[0];
  return path === API_BASE_URL || path.startsWith(`${API_BASE_URL}/`);
}
