import type { ExportSummaryDto, GoogleConnectResponse, GoogleStatusDto } from '@/lib/api';
import { api } from '@/lib/apiClient';
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

        const res = await api.api.calendarGoogleStatusList({
          headers: { 'X-Session-Cookie': cookie },
        });

        if (res.error) {
          throw new ActionError({
            code: 'INTERNAL_SERVER_ERROR',
            message: res.error as any,
          });
        }

        return res.data as GoogleStatusDto;
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

        const res = await api.api.authGoogleConnectList({
          headers: { 'X-Session-Cookie': cookie },
        });

        if (res.error) {
          throw new ActionError({
            code: 'BAD_REQUEST',
            message: (res.error as any)?.message || 'Failed to get connect URL',
          });
        }

        return res.data as GoogleConnectResponse;
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

        const res = await api.api.calendarGoogleExportCreate(
          { from: input.from },
          { headers: { 'X-Session-Cookie': cookie } },
        );

        if (res.error) {
          throw new ActionError({
            code: 'INTERNAL_SERVER_ERROR',
            message: res.error as any,
          });
        }

        return res.data as ExportSummaryDto;
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
