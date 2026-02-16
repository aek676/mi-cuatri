import type { CalendarEvent } from '@/lib/types';
import { CalendarCategory } from '@/lib/api';

export function createMockEvent(overrides?: Partial<CalendarEvent>): CalendarEvent {
  const now = new Date();
  const start = new Date(now.getFullYear(), now.getMonth(), now.getDate(), 10, 0);
  const end = new Date(now.getFullYear(), now.getMonth(), now.getDate(), 11, 0);

  return {
    calendarid: `event-${Math.random().toString(36).substr(2, 9)}`,
    title: 'Test Event',
    subject: 'Test Subject',
    start: start.toISOString(),
    end: end.toISOString(),
    location: 'Test Location',
    category: CalendarCategory.Personal,
    color: '#315F94',
    description: null,
    ...overrides,
  };
}

export function createMockEvents(count: number): CalendarEvent[] {
  return Array.from({ length: count }, (_, i) =>
    createMockEvent({
      calendarid: `event-${i}`,
      title: `Event ${i + 1}`,
      start: new Date(new Date().getFullYear(), new Date().getMonth(), new Date().getDate(), 10 + i, 0).toISOString(),
      end: new Date(new Date().getFullYear(), new Date().getMonth(), new Date().getDate(), 11 + i, 0).toISOString(),
    })
  );
}
