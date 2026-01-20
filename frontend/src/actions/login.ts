import { api } from '@/lib/apiClient';
import { z } from 'astro/zod';
import { ActionError, defineAction } from 'astro:actions';

export const login = defineAction({
  accept: 'form',
  input: z.object({
    username: z.string({
      message: 'Username is required',
    }),
    password: z.string({
      message: 'Password is required',
    }),
  }),
  handler: async (input) => {
    try {
      const res = await api.api.authLoginUalCreate({
        username: input.username,
        password: input.password,
      });

      return {
        success: true,
        sessionCookie: res.data.sessionCookie,
        message: res.data.message,
      };
    } catch (err: any) {
      console.error('Login error:', err);

      let errorMessage = 'Error logging in';
      let code: 'UNAUTHORIZED' | 'INTERNAL_SERVER_ERROR' =
        'INTERNAL_SERVER_ERROR';

      if (err && typeof err === 'object' && 'message' in err) {
        errorMessage = err.message;
        code = 'UNAUTHORIZED';
      } else if (err instanceof Error) {
        errorMessage = err.message;
      }

      throw new ActionError({
        code: code,
        message: errorMessage,
      });
    }
  },
});
