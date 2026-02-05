import { defineMiddleware } from 'astro:middleware';
import { createApiClient } from '@/lib/apiClient';

const isPublicPath = (pathname: string) => {
  return (
    pathname.startsWith('/_astro') ||
    pathname.startsWith('/_actions') ||
    pathname.startsWith('/assets') ||
    pathname.startsWith('/favicon') ||
    pathname.startsWith('/api/')
  );
};

const redirectToLogin = (requestUrl: string) => {
  return Response.redirect(new URL('/login', requestUrl), 302);
};

export const onRequest = defineMiddleware(async (context, next) => {
  const { request, cookies, locals } = context;
  const { pathname } = new URL(request.url);

  const sessionToken = cookies.get('bb_session')?.value ?? null;
  locals.sessionToken = sessionToken;
  locals.user = null;

  if (isPublicPath(pathname)) {
    return next();
  }

  if (pathname === '/login' && !sessionToken) {
    return next();
  }

  if (pathname === '/') {
    return Response.redirect(new URL('/ultra/calendar', request.url), 302);
  }

  if (!sessionToken) {
    return redirectToLogin(request.url);
  }

  try {
    const api = createApiClient(sessionToken);
    const response = await api.api.authMeList();

    if (!response.data?.isSuccess || !response.data.userData) {
      throw new Error('Invalid session data');
    }

    locals.user = response.data.userData;

    if (pathname === '/login') {
      return Response.redirect(new URL('/ultra/calendar', request.url), 302);
    }

    return next();
  } catch (error) {
    console.error('[Middleware] Session validation failed:', error);
    cookies.set('bb_session', '', {
      httpOnly: true,
      secure: import.meta.env.PROD,
      sameSite: 'lax',
      path: '/',
      maxAge: 0,
    });
    return redirectToLogin(request.url);
  }
});
