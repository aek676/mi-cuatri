import { hexToRgba } from '@/lib/color';
import { formatTime } from '@/lib/date';
import type { CalendarEvent } from '@/lib/types';
import { Bookmark, Clock, MapPin, X } from 'lucide-react';
import { memo } from 'react';
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from '@/components/ui';

function EventDialog({
  event,
  isOpen,
  onClose,
}: {
  event: CalendarEvent | null;
  isOpen: boolean;
  onClose: () => void;
}) {
  const headerColor = event?.color || 'var(--color-brand-main)';

  return (
    <Dialog open={isOpen} onOpenChange={(open) => !open && onClose()}>
      <DialogContent
        showCloseButton={false}
        className="max-w-md p-0 overflow-hidden border-brand-pale rounded-xl shadow-2xl"
        style={{
          backgroundColor: 'white',
        }}
      >
        <DialogHeader
          className="px-6 py-4 flex flex-row justify-between items-start"
          style={{ backgroundColor: headerColor }}
        >
          <DialogTitle className="text-xl font-bold text-white pr-4 leading-tight text-left">
            {event?.title}
          </DialogTitle>
          <button
            type="button"
            onClick={onClose}
            className="text-white/80 hover:text-white transition-colors"
          >
            <X size={24} />
          </button>
        </DialogHeader>

        <div className="p-6 space-y-4">
          <div className="flex items-start gap-3 text-brand-dark">
            <Bookmark className="w-5 h-5 mt-0.5 text-brand-light" />
            <div>
              <p className="font-semibold text-sm uppercase tracking-wide opacity-70">
                Subject
              </p>
              <p className="font-medium">{event?.subject}</p>
            </div>
          </div>

          <div className="flex items-start gap-3 text-brand-dark">
            <Clock className="w-5 h-5 mt-0.5 text-brand-light" />
            <div>
              <p className="font-semibold text-sm uppercase tracking-wide opacity-70">
                Time
              </p>
              <p className="font-medium">
                {event && new Date(event.start).toLocaleDateString('en-US', {
                  weekday: 'long',
                  day: 'numeric',
                  month: 'long',
                })}
                <br />
                {event && formatTime(event.start)} - {event && formatTime(event.end)}
              </p>
            </div>
          </div>

          {event?.location && (
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
                backgroundColor: event?.color ? hexToRgba(event.color, 0.15) : undefined,
                color: event?.color || 'var(--color-brand-dark)',
              }}
            >
              {event?.category}
            </span>
          </div>
        </div>
      </DialogContent>
    </Dialog>
  );
}

EventDialog.displayName = 'EventDialog';
export default memo(EventDialog);
