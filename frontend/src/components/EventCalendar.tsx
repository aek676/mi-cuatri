import {
  Button,
  Dialog,
  DialogClose,
  DialogContent,
  DialogHeader,
  DialogTitle,
  Input,
  Label,
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
} from '@/components/ui';
import {
  Bookmark,
  Calendar as CalendarIcon,
  ChevronLeft,
  ChevronRight,
  Clock,
  Download,
  MapPin,
  Plus,
  Save,
  X,
} from 'lucide-react';
import React, { useEffect, useRef, useState } from 'react';

export interface CalendarEvent {
  calendarid: string;
  title: string;
  subject: string;
  start: string; // ISO
  end: string; // ISO
  location?: string | null;
  category?: string;
  color?: string | null;
  description?: string | null;
}

type Props = {
  events?: CalendarEvent[];
};

const getMonthData = (year: number, month: number) => {
  const firstDay = new Date(year, month, 1);
  const lastDay = new Date(year, month + 1, 0);

  // 0 = Sunday, 1 = Monday. We want Monday to be index 0
  let startDay = firstDay.getDay() - 1;
  if (startDay === -1) startDay = 6;

  const daysInMonth = lastDay.getDate();

  // Previous month padding
  const paddingDays = startDay;
  const prevMonthLastDay = new Date(year, month, 0).getDate();
  const prevMonthDays: any[] = [];
  for (let i = paddingDays - 1; i >= 0; i--) {
    prevMonthDays.push({
      day: prevMonthLastDay - i,
      month: month - 1,
      year: year,
      currentMonth: false,
    });
  }

  // Current month days
  const currentMonthDays: any[] = [];
  for (let i = 1; i <= daysInMonth; i++) {
    currentMonthDays.push({
      day: i,
      month: month,
      year: year,
      currentMonth: true,
      dateObj: new Date(year, month, i),
    });
  }

  // Next month padding to fill grid (42 cells total for 6 rows)
  const totalDays = prevMonthDays.length + currentMonthDays.length;
  const remainingCells = 42 - totalDays;
  const nextMonthDays: any[] = [];
  for (let i = 1; i <= remainingCells; i++) {
    nextMonthDays.push({
      day: i,
      month: month + 1,
      year: year,
      currentMonth: false,
    });
  }

  return [...prevMonthDays, ...currentMonthDays, ...nextMonthDays];
};

const formatTime = (dateStr: string) => {
  const date = new Date(dateStr);
  return date.toLocaleTimeString('en-US', {
    hour: '2-digit',
    minute: '2-digit',
    hour12: false,
  });
};

const hexToRgba = (hex?: string | null, alpha = 0.05) => {
  if (!hex) return `rgba(0,0,0,${alpha})`;

  let r = 0,
    g = 0,
    b = 0;
  if (hex.length === 4) {
    r = parseInt(hex[1] + hex[1], 16);
    g = parseInt(hex[2] + hex[2], 16);
    b = parseInt(hex[3] + hex[3], 16);
  } else if (hex.length === 7) {
    r = parseInt(hex.substring(1, 3), 16);
    g = parseInt(hex.substring(3, 5), 16);
    b = parseInt(hex.substring(5, 7), 16);
  }
  return `rgba(${r},${g},${b},${alpha})`;
};

export function EventCalendar({ events = [] }: Props) {
  const [currentDate, setCurrentDate] = useState(new Date());
  const [calendarDays, setCalendarDays] = useState<any[]>([]);
  const [items, setItems] = useState<CalendarEvent[]>(events || []);
  const [selectedEvent, setSelectedEvent] = useState<CalendarEvent | null>(
    null,
  );
  const [isAddEventOpen, setIsAddEventOpen] = useState(false);
  const scrollRef = useRef<HTMLDivElement | null>(null);

  useEffect(() => {
    const year = currentDate.getFullYear();
    const month = currentDate.getMonth();
    setCalendarDays(getMonthData(year, month));
    if (scrollRef.current) scrollRef.current.scrollTop = 0;
  }, [currentDate]);

  useEffect(() => {
    setItems(events || []);
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

  const handleAddEvent = (newEvent: CalendarEvent) =>
    setItems((s) => [...s, newEvent]);

  const weekDays = [
    'Monday',
    'Tuesday',
    'Wednesday',
    'Thursday',
    'Friday',
    'Saturday',
    'Sunday',
  ];

  const getEventsForDay = (day: number, month: number, year: number) => {
    return items
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
  };

  // --- Small modals inlined ---
  const EventDialog = ({
    event,
    isOpen,
    onClose,
  }: {
    event: CalendarEvent | null;
    isOpen: boolean;
    onClose: () => void;
  }) => {
    if (!isOpen || !event) return null;

    // Use event color or fallback to brand main
    const headerColor = event.color || 'var(--color-brand-main)';

    return (
      <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 backdrop-blur-sm animate-in fade-in duration-200 p-4">
        <div className="bg-white rounded-xl shadow-2xl w-full max-w-md overflow-hidden animate-in zoom-in-95 duration-200 border border-brand-pale">
          {/* Header - Now uses event color */}
          <div
            className="px-6 py-4 flex justify-between items-start"
            style={{ backgroundColor: headerColor }}
          >
            <h3 className="text-xl font-bold text-white pr-4 leading-tight">
              {event.title}
            </h3>
            <button
              onClick={onClose}
              className="text-white/80 hover:text-white transition-colors"
            >
              <X size={24} />
            </button>
          </div>

          {/* Body */}
          <div className="p-6 space-y-4">
            <div className="flex items-start gap-3 text-brand-dark">
              <Bookmark className="w-5 h-5 mt-0.5 text-brand-light" />
              <div>
                <p className="font-semibold text-sm uppercase tracking-wide opacity-70">
                  Subject
                </p>
                <p className="font-medium">{event.subject}</p>
              </div>
            </div>

            <div className="flex items-start gap-3 text-brand-dark">
              <Clock className="w-5 h-5 mt-0.5 text-brand-light" />
              <div>
                <p className="font-semibold text-sm uppercase tracking-wide opacity-70">
                  Time
                </p>
                <p className="font-medium">
                  {new Date(event.start).toLocaleDateString('en-US', {
                    weekday: 'long',
                    day: 'numeric',
                    month: 'long',
                  })}
                  <br />
                  {formatTime(event.start)} - {formatTime(event.end)}
                </p>
              </div>
            </div>

            {event.location && (
              <div className="flex items-start gap-3 text-brand-dark">
                <MapPin className="w-5 h-5 mt-0.5 text-brand-light" />
                <div>
                  <p className="font-semibold text-sm uppercase tracking-wide opacity-70">
                    Location
                  </p>
                  <p className="font-medium">{event.location}</p>
                </div>
              </div>
            )}

            <div className="mt-4 pt-4 border-t border-brand-pale flex justify-between items-center">
              <span
                className="px-3 py-1 rounded-full text-xs font-semibold"
                style={{
                  backgroundColor: hexToRgba(event.color, 0.15),
                  color: event.color || 'var(--color-brand-dark)',
                }}
              >
                {event.category === 'Course' ? 'Course' : 'Assignment / Exam'}
              </span>
            </div>
          </div>
        </div>
      </div>
    );
  };

  const AddEventDialog = ({
    isOpen,
    onClose,
    onSave,
    defaultDate,
  }: {
    isOpen: boolean;
    onClose: () => void;
    onSave: (e: CalendarEvent) => void;
    defaultDate?: Date;
  }) => {
    const [formData, setFormData] = useState({
      title: '',
      subject: '',
      start: '',
      end: '',
      location: '',
      category: 'Course',
      color: '#315F94',
    });

    useEffect(() => {
      if (isOpen) {
        const baseDate = defaultDate ? new Date(defaultDate) : new Date();
        const pad = (n: number) => n.toString().padStart(2, '0');
        const toLocalISO = (d: Date, hourOffset = 0) => {
          const y = d.getFullYear();
          const m = pad(d.getMonth() + 1);
          const day = pad(d.getDate());
          const h = pad(d.getHours() + hourOffset);
          const min = pad(d.getMinutes());
          return `${y}-${m}-${day}T${h}:${min}`;
        };

        const startDate = new Date(baseDate);
        startDate.setHours(new Date().getHours() + 1, 0, 0, 0);
        const endDate = new Date(startDate);
        endDate.setHours(startDate.getHours() + 1);

        setFormData({
          title: '',
          subject: '',
          start: toLocalISO(startDate),
          end: toLocalISO(endDate),
          location: '',
          category: 'Course',
          color: '#315F94',
        });
      }
    }, [isOpen, defaultDate]);

    const handleSubmit = (e: React.FormEvent) => {
      e.preventDefault();
      if (!formData.title || !formData.start || !formData.end) return;
      const newEvent: CalendarEvent = {
        calendarid: `new_${Date.now()}`,
        ...formData,
        start: new Date(formData.start).toISOString(),
        end: new Date(formData.end).toISOString(),
        description: null,
      } as any;
      onSave(newEvent);
      onClose();
    };

    return (
      <Dialog
        open={isOpen}
        onOpenChange={(open) => {
          if (!open) onClose();
        }}
      >
        <DialogContent showCloseButton={false}>
          <DialogHeader
            className="px-6 py-4 flex justify-between items-center"
            style={{ backgroundColor: 'var(--color-brand-main)' }}
          >
            <DialogTitle className="text-xl font-bold text-white">
              Add New Event
            </DialogTitle>
            <DialogClose className="text-white/80 hover:text-white transition-colors">
              <X size={20} />
            </DialogClose>
          </DialogHeader>

          <form onSubmit={handleSubmit} className="p-6 space-y-4">
            <div>
              <Label htmlFor="ec-title">Title</Label>
              <Input
                id="ec-title"
                required
                value={formData.title}
                onChange={(e) =>
                  setFormData({ ...formData, title: e.target.value })
                }
                placeholder="e.g. Module 5 Lecture"
              />
            </div>

            <div>
              <Label htmlFor="ec-subject">Subject</Label>
              <Input
                id="ec-subject"
                required
                value={formData.subject}
                onChange={(e) =>
                  setFormData({ ...formData, subject: e.target.value })
                }
                placeholder="e.g. Computer Security"
              />
            </div>

            <div className="grid grid-cols-2 gap-4">
              <div>
                <Label htmlFor="ec-start">Start</Label>
                <Input
                  id="ec-start"
                  type="datetime-local"
                  required
                  className="text-sm"
                  value={formData.start}
                  onChange={(e) =>
                    setFormData({ ...formData, start: e.target.value })
                  }
                />
              </div>
              <div>
                <Label htmlFor="ec-end">End</Label>
                <Input
                  id="ec-end"
                  type="datetime-local"
                  required
                  className="text-sm"
                  value={formData.end}
                  onChange={(e) =>
                    setFormData({ ...formData, end: e.target.value })
                  }
                />
              </div>
            </div>

            <div className="grid grid-cols-2 gap-4">
              <div>
                <Label htmlFor="ec-location">Location</Label>
                <Input
                  id="ec-location"
                  value={formData.location}
                  onChange={(e) =>
                    setFormData({ ...formData, location: e.target.value })
                  }
                  placeholder="Optional"
                />
              </div>
              <div>
                <Label htmlFor="ec-color">Color</Label>
                <input
                  id="ec-color"
                  type="color"
                  className="w-full h-[42px] px-1 py-1 border border-gray-300 rounded-lg cursor-pointer"
                  value={formData.color}
                  onChange={(e) =>
                    setFormData({ ...formData, color: e.target.value })
                  }
                />
              </div>
            </div>

            <div>
              <Label htmlFor="ec-category">Category</Label>
              <Select
                onValueChange={(val: any) =>
                  setFormData({ ...formData, category: val })
                }
              >
                <SelectTrigger
                  id="ec-category"
                  className="w-full"
                  size="default"
                >
                  {formData.category}
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="Course">Course (Class)</SelectItem>
                  <SelectItem value="GradebookColumn">
                    Assignment / Exam
                  </SelectItem>
                </SelectContent>
              </Select>
            </div>

            <Button
              type="submit"
              className="w-full mt-4 flex items-center justify-center gap-2"
            >
              <Save size={16} /> Save Event
            </Button>
          </form>
        </DialogContent>
      </Dialog>
    );
  };

  return (
    <div className="flex flex-col h-full flex-1 min-h-0 bg-[#F8FAFC] font-sans">
      <style>{`:root{--color-brand-dark:#1C3A5B;--color-brand-main:#315F94;--color-brand-light:#6B9AC4;--color-brand-pale:#D6E4F0;--color-brand-white:#FFFFFF;}`}</style>

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
              className="p-1.5 hover:bg-white rounded-md text-brand-main"
            >
              <ChevronLeft size={16} />
            </button>
            <button
              type="button"
              onClick={nextMonth}
              className="p-1.5 hover:bg-white rounded-md text-brand-main"
            >
              <ChevronRight size={16} />
            </button>
          </div>
        </div>

        <div className="flex items-center gap-3">
          <div className="flex items-center gap-2">
            <Button
              onClick={() => setIsAddEventOpen(true)}
              className="flex items-center gap-2 px-3 py-1.5 bg-brand-main text-white rounded-md font-semibold text-sm hover:bg-brand-dark transition-colors shadow-sm"
            >
              <Plus size={14} />
              <span className="hidden sm:inline">Add Event</span>
            </Button>
            <Button
              variant="outline"
              onClick={handleExport}
              className="flex items-center gap-2 px-3 py-1.5 bg-white border border-brand-main text-brand-main rounded-md font-semibold text-sm hover:bg-brand-pale transition-colors shadow-sm"
            >
              <Download size={14} />
              <span className="hidden sm:inline">Export</span>
            </Button>
          </div>

          <div className="w-px h-8 bg-brand-pale mx-1 hidden md:block"></div>

          <div className="hidden md:flex items-center gap-2 bg-brand-pale/30 p-1 rounded-lg border border-brand-pale">
            <button
              type="button"
              onClick={prevMonth}
              className="p-2 hover:bg-white rounded-md text-brand-main transition-all shadow-sm hover:shadow"
            >
              <ChevronLeft size={18} />
            </button>
            <button
              type="button"
              onClick={goToToday}
              className="px-4 py-1.5 text-sm font-semibold text-brand-dark hover:bg-white rounded-md transition-all"
            >
              Today
            </button>
            <button
              type="button"
              onClick={nextMonth}
              className="p-2 hover:bg-white rounded-md text-brand-main transition-all shadow-sm hover:shadow"
            >
              <ChevronRight size={18} />
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
                    {new Date(date.year, date.month).toLocaleDateString(
                      'en-US',
                      { month: 'short' },
                    )}
                  </span>
                )}
              </div>

              <div className="flex flex-col gap-1.5 mt-1 overflow-y-auto overflow-x-hidden max-h-[120px] custom-scrollbar w-full">
                {dayEvents.map((ev) => (
                  <button
                    type="button"
                    key={ev.calendarid}
                    onClick={(e) => {
                      e.stopPropagation();
                      setSelectedEvent(ev);
                    }}
                    style={{
                      borderLeftColor: ev.color || 'var(--color-brand-main)',
                      backgroundColor:
                        hexToRgba(ev.color, 0.1) || 'rgba(49, 95, 148, 0.1)',
                    }}
                    className="w-full text-left px-2 py-1.5 rounded text-xs font-medium border-l-[3px] shadow-sm transition-all hover:scale-[1.02] active:scale-95 text-brand-dark hover:opacity-90"
                  >
                    <div className="flex justify-between items-center mb-0.5 w-full">
                      <span className="font-bold opacity-90 text-[10px] truncate pr-1 w-full block">
                        {ev.subject}
                      </span>
                    </div>
                    <div className="truncate leading-tight w-full block">
                      {ev.title}
                    </div>
                  </button>
                ))}
              </div>
            </div>
          );
        })}
      </div>

      <div
        ref={scrollRef}
        className="md:hidden flex flex-col flex-1 min-h-0 overflow-y-auto bg-slate-50 pb-20"
      >
        {calendarDays
          .filter((d) => d.currentMonth)
          .map((date) => {
            const dayDate = new Date(date.year, date.month, date.day);
            const isToday =
              new Date().toDateString() === dayDate.toDateString();
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
                      <button
                        type="button"
                        key={ev.calendarid}
                        onClick={() => setSelectedEvent(ev)}
                        style={{
                          borderLeftColor:
                            ev.color || 'var(--color-brand-main)',
                          backgroundColor: hexToRgba(ev.color, 0.05),
                        }}
                        className="w-full text-left p-3 rounded-lg border-l-4 border-t border-r border-b border-gray-100 shadow-sm flex flex-col gap-1 active:scale-98 transition-transform"
                      >
                        <div className="flex justify-between items-start">
                          <span
                            className="text-xs font-bold px-2 py-0.5 rounded-full max-w-[70%] truncate"
                            style={{
                              backgroundColor: hexToRgba(ev.color, 0.15),
                              color: ev.color || 'var(--color-brand-main)',
                            }}
                          >
                            {ev.subject}
                          </span>
                          <span className="text-[10px] uppercase font-bold text-gray-500 tracking-wide">
                            {ev.category}
                          </span>
                        </div>

                        <h4 className="font-bold text-brand-dark text-sm mt-1">
                          {ev.title}
                        </h4>
                        <div className="text-xs text-gray-500 truncate">
                          {formatTime(ev.start)} - {formatTime(ev.end)}
                        </div>
                        {ev.location && (
                          <div className="flex items-center gap-1 text-xs text-gray-400 mt-1">
                            <MapPin size={12} />
                            <span className="truncate">{ev.location}</span>
                          </div>
                        )}
                      </button>
                    ))}
                  </div>
                )}
              </div>
            );
          })}
      </div>

      <EventDialog
        event={selectedEvent}
        isOpen={!!selectedEvent}
        onClose={() => setSelectedEvent(null)}
      />
      <AddEventDialog
        isOpen={isAddEventOpen}
        onClose={() => setIsAddEventOpen(false)}
        onSave={handleAddEvent}
        defaultDate={currentDate}
      />
    </div>
  );
}

export default EventCalendar;
