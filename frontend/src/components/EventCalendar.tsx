import { Button } from '@/components/ui';
import { getMonthData } from '@/lib/calendarUtils';
import type { CalendarEvent } from '@/lib/types';
import {
  Calendar as CalendarIcon,
  ChevronLeft,
  ChevronRight,
  Download,
  Plus,
} from 'lucide-react';
import { useEffect, useReducer, useRef, useState } from 'react';
import AddEventDialog from './calendar/AddEventDialog';
import EventDialog from './calendar/EventDialog';
import MobileList from './calendar/MobileList';
import MonthGrid from './calendar/MonthGrid';

type Props = {
  events?: CalendarEvent[];
};

export function EventCalendar({ events = [] }: Props) {
  const [currentDate, setCurrentDate] = useState(new Date());
  const [calendarDays, setCalendarDays] = useState<
    import('@/lib/calendarUtils').CalendarDay[]
  >([]);
  type EventAction =
    | { type: 'set'; payload: CalendarEvent[] }
    | { type: 'add'; payload: CalendarEvent };

  const eventsReducer = (state: CalendarEvent[], action: EventAction) => {
    switch (action.type) {
      case 'set':
        return action.payload;
      case 'add':
        return [...state, action.payload];
      default:
        return state;
    }
  };

  const [items, dispatch] = useReducer(eventsReducer, events || []);
  const [selectedEvent, setSelectedEvent] = useState<CalendarEvent | null>(
    null,
  );
  const [isAddEventOpen, setIsAddEventOpen] = useState(false);
  const scrollRef = useRef<HTMLDivElement | null>(null);
  const addButtonRef = useRef<HTMLButtonElement | null>(null);

  useEffect(() => {
    const year = currentDate.getFullYear();
    const month = currentDate.getMonth();
    setCalendarDays(getMonthData(year, month));
    if (scrollRef.current) scrollRef.current.scrollTop = 0;
  }, [currentDate]);

  useEffect(() => {
    dispatch({ type: 'set', payload: events || [] });
  }, [events]);

  const nextMonth = () =>
    setCurrentDate(
      new Date(currentDate.getFullYear(), currentDate.getMonth() + 1, 1),
    );
  const prevMonth = () =>
    setCurrentDate(
      new Date(currentDate.getFullYear(), currentDate.getMonth() - 1, 1),
    );
  const goToToday = () => setCurrentDate(new Date());
  const handleExport = () => console.log('exporting events', items.length);

  const handleAddEvent = (newEvent: CalendarEvent) => {
    dispatch({ type: 'add', payload: newEvent });
  };

  const weekDays = [
    'Monday',
    'Tuesday',
    'Wednesday',
    'Thursday',
    'Friday',
    'Saturday',
    'Sunday',
  ];

  const getEventsForDay = (day: number, month: number, year: number) =>
    items
      .filter((event) => {
        const eventDate = new Date(event.start);
        return (
          eventDate.getDate() === day &&
          eventDate.getMonth() === month &&
          eventDate.getFullYear() === year
        );
      })
      .sort(
        (a, b) => new Date(a.start).getTime() - new Date(b.start).getTime(),
      );

  return (
    <div className="flex flex-col h-full flex-1 min-h-0 bg-[#F8FAFC] font-sans">
      <header className="px-4 md:px-6 py-4 bg-white border-b border-brand-pale shadow-sm flex flex-col md:flex-row justify-between items-center gap-4 z-10 shrink-0">
        <div className="flex items-center gap-3 w-full md:w-auto justify-between md:justify-start">
          <div className="flex items-center gap-3">
            <div className="bg-brand-main p-2 rounded-lg text-white">
              <CalendarIcon size={20} />
            </div>
            <h1 className="text-xl md:text-2xl font-bold text-brand-dark capitalize truncate">
              {currentDate.toLocaleDateString('en-US', {
                month: 'long',
                year: 'numeric',
              })}
            </h1>
          </div>

          <div className="md:hidden flex items-center gap-1 bg-brand-pale/30 p-1 rounded-lg border border-brand-pale">
            <button
              type="button"
              onClick={prevMonth}
              aria-label="Previous month"
              className="p-1.5 hover:bg-white rounded-md text-brand-main"
            >
              <ChevronLeft size={16} aria-hidden />
            </button>
            <button
              type="button"
              onClick={nextMonth}
              aria-label="Next month"
              className="p-1.5 hover:bg-white rounded-md text-brand-main"
            >
              <ChevronRight size={16} aria-hidden />
            </button>
          </div>
        </div>

        <div className="flex items-center gap-3">
          <div className="flex items-center gap-2">
            <Button
              ref={addButtonRef}
              onClick={() => setIsAddEventOpen(true)}
              aria-haspopup="dialog"
              aria-controls="add-event-dialog"
              aria-expanded={isAddEventOpen}
              aria-label="Add event"
              className="flex items-center gap-2 px-3 py-1.5 bg-brand-main text-white rounded-md font-semibold text-sm hover:bg-brand-dark transition-colors shadow-sm"
            >
              <Plus size={14} aria-hidden />
              <span className="hidden sm:inline">Add Event</span>
            </Button>
            <Button
              variant="outline"
              onClick={handleExport}
              aria-label="Export events"
              className="flex items-center gap-2 px-3 py-1.5 bg-white border border-brand-main text-brand-main rounded-md font-semibold text-sm hover:bg-brand-pale transition-colors shadow-sm"
            >
              <Download size={14} aria-hidden />
              <span className="hidden sm:inline">Export</span>
            </Button>
          </div>

          <div className="w-px h-8 bg-brand-pale mx-1 hidden md:block"></div>

          <div className="hidden md:flex items-center gap-2 bg-brand-pale/30 p-1 rounded-lg border border-brand-pale">
            <button
              type="button"
              onClick={prevMonth}
              aria-label="Previous month"
              className="p-2 hover:bg-white rounded-md text-brand-main transition-all shadow-sm hover:shadow"
            >
              <ChevronLeft size={18} aria-hidden />
            </button>
            <button
              type="button"
              onClick={goToToday}
              aria-label="Go to today"
              className="px-4 py-1.5 text-sm font-semibold text-brand-dark hover:bg-white rounded-md transition-all"
            >
              Today
            </button>
            <button
              type="button"
              onClick={nextMonth}
              aria-label="Next month"
              className="p-2 hover:bg-white rounded-md text-brand-main transition-all shadow-sm hover:shadow"
            >
              <ChevronRight size={18} aria-hidden />
            </button>
          </div>
        </div>
      </header>

      <div className="hidden md:grid grid-cols-7 border-b border-brand-pale bg-white shrink-0">
        {weekDays.map((day) => (
          <div
            key={day}
            className="py-3 text-center text-sm font-semibold text-brand-light uppercase tracking-wider"
          >
            {day}
          </div>
        ))}
      </div>

      <MonthGrid
        calendarDays={calendarDays}
        getEventsForDay={getEventsForDay}
        onEventClick={(ev) => setSelectedEvent(ev)}
      />

      <MobileList
        calendarDays={calendarDays}
        getEventsForDay={getEventsForDay}
        onEventClick={(ev) => setSelectedEvent(ev)}
        scrollRef={scrollRef}
      />

      <EventDialog
        event={selectedEvent}
        isOpen={!!selectedEvent}
        onClose={() => setSelectedEvent(null)}
      />
      <AddEventDialog
        isOpen={isAddEventOpen}
        onClose={() => {
          setIsAddEventOpen(false);
          addButtonRef.current?.focus();
        }}
        onSave={handleAddEvent}
        defaultDate={currentDate}
      />
    </div>
  );
}

export default EventCalendar;
