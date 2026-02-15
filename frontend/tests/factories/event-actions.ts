import type { CalendarEvent } from '@/lib/types';

export type EventAction =
  | { type: 'set'; payload: CalendarEvent[] }
  | { type: 'add'; payload: CalendarEvent }
  | { type: 'confirm'; tempId: string; realEvent: CalendarEvent }
  | { type: 'remove'; id: string }
  | { type: 'update'; payload: CalendarEvent };

export function eventsReducer(state: CalendarEvent[], action: EventAction): CalendarEvent[] {
  switch (action.type) {
    case 'set':
      return action.payload;
    case 'add':
      return [...state, action.payload];
    case 'confirm':
      return state.map((evt) =>
        evt.calendarid === action.tempId ? action.realEvent : evt
      );
    case 'remove':
      return state.filter((evt) => evt.calendarid !== action.id);
    case 'update':
      return state.map((evt) =>
        evt.calendarid === action.payload.calendarid ? action.payload : evt
      );
    default:
      return state;
  }
}

export const EventActionCreators = {
  set: (payload: CalendarEvent[]): EventAction => ({ type: 'set', payload }),
  add: (payload: CalendarEvent): EventAction => ({ type: 'add', payload }),
  confirm: (tempId: string, realEvent: CalendarEvent): EventAction => ({ type: 'confirm', tempId, realEvent }),
  remove: (id: string): EventAction => ({ type: 'remove', id }),
  update: (payload: CalendarEvent): EventAction => ({ type: 'update', payload }),
};
