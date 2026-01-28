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

export interface EventFormData {
  title: string;
  subject: string;
  start: string; // local `datetime-local` value
  end: string; // local `datetime-local` value
  location?: string;
  category: string;
  color: string;
}
