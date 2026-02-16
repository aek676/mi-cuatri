import type { CalendarEvent } from '@/lib/types';
import { CalendarCategory } from '@/lib/api';

export const SampleEvents: CalendarEvent[] = [
  {
    calendarid: 'sample-1',
    title: 'Sample Event 1',
    subject: 'Subject 1',
    start: new Date().toISOString(),
    end: new Date(Date.now() + 3600000).toISOString(),
    location: 'Location A',
    category: CalendarCategory.Personal,
    color: '#315F94',
    description: 'Sample description 1',
  },
  {
    calendarid: 'sample-2',
    title: 'Sample Event 2',
    subject: 'Subject 2',
    start: new Date(Date.now() + 7200000).toISOString(),
    end: new Date(Date.now() + 10800000).toISOString(),
    location: 'Location B',
    category: CalendarCategory.Course,
    color: '#28A745',
    description: 'Sample description 2',
  },
];

export const SampleEvent: CalendarEvent = SampleEvents[0];
