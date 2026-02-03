import { createApiClient } from '@/lib/apiClient';
import { z } from 'astro/zod';
import { ActionError, defineAction } from 'astro:actions';

export const google = {
  getStatus: defineAction({
    handler: async (_, context) => {
      try {
        const cookie = context.cookies.get('bb_session')?.value;
        if (!cookie) {
          throw new ActionError({
            code: 'UNAUTHORIZED',
            message: 'Session cookie is required',
          });
        }

        const api = createApiClient(cookie);
        const res = await api.api.calendarGoogleStatusList();

        if (res.error) {
          throw new ActionError({
            code: 'INTERNAL_SERVER_ERROR',
            message: res.error as any,
          });
        }

        return res.data;
      } catch (err: any) {
        console.error('[Google.getStatus Error]:', err);
        if (err instanceof ActionError) throw err;
        throw new ActionError({
          code: 'INTERNAL_SERVER_ERROR',
          message: err?.message || 'Error fetching Google status',
        });
      }
    },
  }),

  connect: defineAction({
    handler: async (_, context) => {
      try {
        const cookie = context.cookies.get('bb_session')?.value;
        if (!cookie) {
          throw new ActionError({
            code: 'UNAUTHORIZED',
            message: 'Session cookie is required',
          });
        }

        const api = createApiClient(cookie);
        const res = await api.api.authGoogleConnectList();

        if (res.error) {
          throw new ActionError({
            code: 'BAD_REQUEST',
            message: (res.error as any)?.message || 'Failed to get connect URL',
          });
        }

        return res.data;
      } catch (err: any) {
        console.error('[Google.connect Error]:', err);
        if (err instanceof ActionError) throw err;
        throw new ActionError({
          code: 'INTERNAL_SERVER_ERROR',
          message: err?.message || 'Error starting Google connect',
        });
      }
    },
  }),

  export: defineAction({
    accept: 'json',
    input: z.object({ from: z.string().optional() }),
    handler: async (input, context) => {
      try {
        const cookie = context.cookies.get('bb_session')?.value;
        if (!cookie) {
          throw new ActionError({
            code: 'UNAUTHORIZED',
            message: 'Session cookie is required',
          });
        }

        const api = createApiClient(cookie);
        const res = await api.api.calendarGoogleExportCreate({
          from: input.from,
        });

        if (res.error) {
          throw new ActionError({
            code: 'INTERNAL_SERVER_ERROR',
            message: res.error as any,
          });
        }

        return res.data;
      } catch (err: any) {
        console.error('[Google.export Error]:', err);
        if (err instanceof ActionError) throw err;
        throw new ActionError({
          code: 'INTERNAL_SERVER_ERROR',
          message: err?.message || 'Error exporting events',
        });
      }
    },
  }),
};
