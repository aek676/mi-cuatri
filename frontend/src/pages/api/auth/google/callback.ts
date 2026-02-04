import { createApiClient } from '@/lib/apiClient';
import type { APIRoute } from 'astro';

export const GET: APIRoute = async (context) => {
  const { request, redirect, cookies } = context;
  const url = new URL(request.url);

  const code = url.searchParams.get('code');
  const state = url.searchParams.get('state');
  const error = url.searchParams.get('error');
  const errorDescription = url.searchParams.get('error_description');

  if (error) {
    console.error(
      '[OAuth Callback] Google returned error:',
      error,
      errorDescription,
    );
    return redirect(
      `/login?error=${encodeURIComponent(error)}&error_description=${encodeURIComponent(errorDescription || '')}`,
    );
  }

  if (!code || !state) {
    console.error('[OAuth Callback] Missing code or state parameter');
    return redirect(
      '/login?error=invalid_callback&error_description=Missing+OAuth+parameters',
    );
  }

  try {
    const sessionCookie = cookies.get('bb_session')?.value;

    const api = createApiClient(sessionCookie);

    const response = await api.api.authGoogleCallbackList({
      code,
      state,
    });

    if (!response.ok) {
      const errorData = response.error as any;
      console.error('[OAuth Callback] Backend error:', errorData);

      const errorMessage =
        errorData?.message || 'Google account linking failed';
      return redirect(
        `/login?error=callback_failed&error_description=${encodeURIComponent(errorMessage)}`,
      );
    }

    console.log('[OAuth Callback] Success: Google account linked');

    return redirect('/ultra/calendar');
  } catch (error) {
    console.error('[OAuth Callback] Error:', error);
    const errorMessage =
      error instanceof Error
        ? error.message
        : 'Unknown error occurred during callback';
    return redirect(
      `/login?error=proxy_error&error_description=${encodeURIComponent(errorMessage)}`,
    );
  }
};
