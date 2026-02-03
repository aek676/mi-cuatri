import { api } from '@/lib/apiClient';
import { z } from 'astro/zod';
import { ActionError, defineAction } from 'astro:actions';

export const login = defineAction({
  accept: 'form',
  input: z.object({
    username: z.string().min(1, 'Username is required').trim(),
    password: z.string().min(1, 'Password is required'),
  }),
  handler: async (input, context) => {
    try {
      const res = await api.api.authLoginUalCreate({
        username: input.username,
        password: input.password,
      });

      if (!res.data.isSuccess || !res.data.sessionCookie) {
        throw new ActionError({
          code: 'UNAUTHORIZED',
          message: res.data.message || 'Invalid credentials',
        });
      }

      context.cookies.set('bb_session', res.data.sessionCookie, {
        httpOnly: true,
        secure: import.meta.env.PROD,
        sameSite: 'lax',
        path: '/',
        maxAge: 60 * 60 * 24,
      });

      return {
        success: true,
        message: 'Logged in successfully',
      };
    } catch (err: any) {
      console.error('[Login Action Error]:', err);

      if (err instanceof ActionError) {
        throw err;
      }

      throw new ActionError({
        code: 'INTERNAL_SERVER_ERROR',
        message: 'An unexpected error occurred while attempting to log in.',
      });
    }
  },
});
