import { Api } from './api';

const baseUrl = import.meta.env.PUBLIC_API_BASE_URL || 'http://localhost:5042';

let instance: Api<unknown> | null = null;

export function getApi() {
  if (!instance) {
    instance = new Api({ baseUrl });
  }
  return instance;
}

export const api = getApi();
