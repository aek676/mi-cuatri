import type { CalendarDay } from '@/lib/calendarUtils';
import type { CalendarEvent } from '@/lib/types';
import { memo } from 'react';
import EventItem from './EventItem';

function MonthGrid({
  calendarDays,
  getEventsForDay,
  onEventClick,
}: {
  calendarDays: CalendarDay[];
  getEventsForDay: (
    day: number,
    month: number,
    year: number,
  ) => CalendarEvent[];
  onEventClick: (ev: CalendarEvent) => void;
}) {
  return (
    <div className="hidden md:grid grid-cols-7 flex-1 min-h-0 auto-rows-fr bg-brand-pale gap-px overflow-y-auto">
      {calendarDays.map((date) => {
        const isToday =
          new Date().getDate() === date.day &&
          new Date().getMonth() === date.month &&
          new Date().getFullYear() === date.year;
        const dayEvents = getEventsForDay(date.day, date.month, date.year);

        return (
          <div
            key={`${date.year}-${date.month}-${date.day}`}
            className={`min-h-[100px] bg-white p-2 flex flex-col gap-1 transition-colors hover:bg-slate-50 overflow-hidden ${!date.currentMonth ? 'bg-slate-50/50 text-gray-400' : 'text-brand-dark'}`}
          >
            <div className="flex justify-between items-start">
              <span
                className={`text-sm font-semibold w-7 h-7 flex items-center justify-center rounded-full ${isToday ? 'bg-brand-main text-white' : ''}`}
              >
                {date.day}
              </span>
              {date.day === 1 && date.currentMonth && (
                <span className="text-xs font-bold text-brand-light uppercase px-1">
                  {new Date(date.year, date.month).toLocaleDateString('en-US', {
                    month: 'short',
                  })}
                </span>
              )}
            </div>

            <div className="flex flex-col gap-1.5 mt-1 overflow-y-auto overflow-x-hidden max-h-[120px] custom-scrollbar w-full">
              {dayEvents.map((ev) => (
                <EventItem
                  key={ev.calendarid}
                  ev={ev}
                  onClick={() => onEventClick(ev)}
                  compact
                />
              ))}
            </div>
          </div>
        );
      })}
    </div>
  );
}

MonthGrid.displayName = 'MonthGrid';
export default memo(MonthGrid);
