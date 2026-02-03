import { Api } from './api';

const baseUrl =
  process.env.INTERNAL_API_BASE_URL || import.meta.env.INTERNAL_API_BASE_URL;

export function createApiClient(sessionToken?: string | null) {
  const api = new Api({
    baseUrl,
    baseApiParams: {
      secure: true,
      credentials: 'same-origin',
      headers: {},
      redirect: 'follow',
      referrerPolicy: 'no-referrer',
    },
    securityWorker: (securityData) => {
      if (!securityData) return {};
      return {
        headers: {
          'X-Session-Cookie': String(securityData),
        },
      };
    },
  });

  api.setSecurityData(sessionToken ?? null);
  return api;
}
