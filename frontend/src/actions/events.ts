import { createApiClient } from '@/lib/apiClient';
import { CalendarCategory, type CreateEventDto, type EventDto, type UpdateEventDto } from '@/lib/api';
import { z } from 'astro/zod';
import { ActionError, defineAction } from 'astro:actions';

export const events = {
  create: defineAction({
    accept: 'json',
    input: z.object({
      title: z.string().min(1),
      subject: z.string().nullable().optional(),
      start: z.string().datetime(),
      end: z.string().datetime(),
      location: z.string().nullable().optional(),
      color: z.string().min(1),
      category: z.nativeEnum(CalendarCategory),
    }),
    handler: async (input, context): Promise<EventDto> => {
      try {
        const cookie = context.cookies.get('bb_session')?.value;
        if (!cookie) {
          throw new ActionError({
            code: 'UNAUTHORIZED',
            message: 'Session cookie is required',
          });
        }

        const createEventDto: CreateEventDto = {
          title: input.title,
          subject: input.subject,
          start: input.start,
          end: input.end,
          location: input.location,
          color: input.color,
          category: input.category,
        };

        const api = createApiClient(cookie);
        const res = await api.api.eventsCreate(createEventDto);

        if (res.error) {
          throw new ActionError({
            code: 'INTERNAL_SERVER_ERROR',
            message: (res.error as any)?.message || (res.error as any)?.title || 'Error creating event',
          });
        }

        return res.data;
      } catch (err: any) {
        console.error('[Events.create Error]:', err);
        if (err instanceof ActionError) throw err;
        throw new ActionError({
          code: 'INTERNAL_SERVER_ERROR',
          message: err?.message || 'Error creating event',
        });
      }
    },
  }),

  update: defineAction({
    accept: 'json',
    input: z.object({
      id: z.string().min(1),
      title: z.string().min(1).optional(),
      subject: z.string().nullable().optional(),
      start: z.string().datetime().optional(),
      end: z.string().datetime().optional(),
      location: z.string().nullable().optional(),
      color: z.string().min(1).optional(),
      category: z.nativeEnum(CalendarCategory).optional(),
    }),
    handler: async (input, context): Promise<EventDto> => {
      try {
        const cookie = context.cookies.get('bb_session')?.value;
        if (!cookie) {
          throw new ActionError({
            code: 'UNAUTHORIZED',
            message: 'Session cookie is required',
          });
        }

        const updateEventDto: UpdateEventDto = {
          title: input.title,
          subject: input.subject,
          start: input.start,
          end: input.end,
          location: input.location,
          color: input.color,
          category: input.category,
        };

        const api = createApiClient(cookie);
        const res = await api.api.eventsUpdate(input.id, updateEventDto);

        if (res.error || !res.data) {
          throw new ActionError({
            code: 'INTERNAL_SERVER_ERROR',
            message: (res.error as any)?.message || (res.error as any)?.title || 'Error updating event',
          });
        }

        return res.data;
      } catch (err: any) {
        console.error('[Events.update Error]:', err);
        if (err instanceof ActionError) throw err;
        throw new ActionError({
          code: 'INTERNAL_SERVER_ERROR',
          message: err?.message || 'Error updating event',
        });
      }
    },
  }),
};
