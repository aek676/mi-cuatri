import { hexToRgba } from '@/lib/color';
import { formatTime } from '@/lib/date';
import type { CalendarEvent } from '@/lib/types';
import { Bookmark, Clock, MapPin, X } from 'lucide-react';
import { memo } from 'react';

function EventDialog({
  event,
  isOpen,
  onClose,
}: {
  event: CalendarEvent | null;
  isOpen: boolean;
  onClose: () => void;
}) {
  if (!isOpen || !event) return null;

  const headerColor = event.color || 'var(--color-brand-main)';

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 backdrop-blur-sm animate-in fade-in duration-200 p-4">
      <div className="bg-white rounded-xl shadow-2xl w-full max-w-md overflow-hidden animate-in zoom-in-95 duration-200 border border-brand-pale">
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
}

EventDialog.displayName = 'EventDialog';
export default memo(EventDialog);
