import type { CalendarDay } from '@/lib/calendarUtils';
import type { CalendarEvent } from '@/lib/types';
import { memo } from 'react';
import EventItem from './EventItem';

function MobileList({
  calendarDays,
  getEventsForDay,
  onEventClick,
  scrollRef,
}: {
  calendarDays: CalendarDay[];
  getEventsForDay: (
    day: number,
    month: number,
    year: number,
  ) => CalendarEvent[];
  onEventClick: (ev: CalendarEvent) => void;
  scrollRef?: React.RefObject<HTMLDivElement | null>;
}) {
  return (
    <div
      ref={scrollRef}
      className="md:hidden flex flex-col flex-1 min-h-0 overflow-y-auto bg-slate-50 pb-20"
    >
      {calendarDays
        .filter((d) => d.currentMonth)
        .map((date) => {
          const dayDate = new Date(date.year, date.month, date.day);
          const isToday = new Date().toDateString() === dayDate.toDateString();
          const dayEvents = getEventsForDay(date.day, date.month, date.year);
          const hasEvents = dayEvents.length > 0;

          return (
            <div
              key={`${date.year}-${date.month}-${date.day}`}
              className="bg-white mb-2 border-b border-brand-pale shadow-sm"
            >
              <div
                className={`sticky top-0 z-10 px-4 py-2 flex items-center gap-3 border-l-4 ${isToday ? 'bg-blue-50 border-brand-main' : 'bg-white border-transparent'} ${!hasEvents ? 'opacity-70' : ''}`}
              >
                <div
                  className={`flex flex-col items-center justify-center w-10 h-10 rounded-lg border ${isToday ? 'bg-brand-main text-white border-brand-main' : 'bg-slate-50 text-brand-dark border-brand-pale'}`}
                >
                  <span className="text-xl font-bold leading-none">
                    {date.day}
                  </span>
                </div>
                <div className="flex flex-col">
                  <span className="text-sm font-semibold uppercase text-brand-dark">
                    {dayDate.toLocaleDateString('en-US', { weekday: 'long' })}
                  </span>
                  {!hasEvents && (
                    <span className="text-xs text-gray-400">No events</span>
                  )}
                </div>
              </div>

              {hasEvents && (
                <div className="px-4 pb-4 pt-1 flex flex-col gap-2 pl-18">
                  {dayEvents.map((ev) => (
                    <EventItem
                      key={ev.calendarid}
                      ev={ev}
                      onClick={() => onEventClick(ev)}
                    />
                  ))}
                </div>
              )}
            </div>
          );
        })}
    </div>
  );
}

MobileList.displayName = 'MobileList';
export default memo(MobileList);
