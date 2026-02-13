import {
  Button,
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from '@/components/ui';
import { CalendarCategory } from '@/lib/api';
import { hexToRgba } from '@/lib/color';
import { formatTime } from '@/lib/date';
import type { CalendarEvent } from '@/lib/types';
import { Bookmark, Clock, Edit, MapPin, Trash2, X } from 'lucide-react';
import { memo } from 'react';

function EventDialog({
  event,
  isOpen,
  onClose,
  onEdit,
  onDelete,
}: {
  event: CalendarEvent | null;
  isOpen: boolean;
  onClose: () => void;
  onEdit: (event: CalendarEvent) => void;
  onDelete: (event: CalendarEvent) => void;
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
          <Button
            type="button"
            variant="ghost"
            size="sm"
            onClick={onClose}
            className="h-8 w-8 p-0 text-white/80 hover:text-white hover:bg-white/10"
          >
            <X className='h-6! w-6!' />
          </Button>
        </DialogHeader>

        <div className="p-6 space-y-4">
          {event?.subject && (
            <div className="flex items-start gap-3 text-brand-dark">
              <Bookmark className="w-5 h-5 mt-0.5 text-brand-light" />
              <div>
                <p className="font-semibold text-sm uppercase tracking-wide opacity-70">
                  Subject
                </p>
                <p className="font-medium">{event.subject}</p>
              </div>
            </div>
          )}

          <div className="flex items-start gap-3 text-brand-dark">
            <Clock className="w-5 h-5 mt-0.5 text-brand-light" />
            <div>
              <p className="font-semibold text-sm uppercase tracking-wide opacity-70">
                Time
              </p>
              <p className="font-medium">
                {event && (() => {
                  const startDate = new Date(event.start);
                  const endDate = new Date(event.end);
                  const isMultiDay = startDate.toDateString() !== endDate.toDateString();

                  if (isMultiDay) {
                    const startStr = startDate.toLocaleDateString('en-US', {
                      weekday: 'short',
                      day: 'numeric',
                      month: 'short',
                    });
                    const endStr = endDate.toLocaleDateString('en-US', {
                      weekday: 'short',
                      day: 'numeric',
                      month: 'short',
                    });
                    return `${startStr} - ${endStr}`;
                  }

                  return startDate.toLocaleDateString('en-US', {
                    weekday: 'long',
                    day: 'numeric',
                    month: 'long',
                  });
                })()}
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
            <div className="flex gap-2">
              {event?.category === CalendarCategory.Personal && (
                <>
                  <Button
                    variant="ghost"
                    size="sm"
                    onClick={() => onEdit(event)}
                    className="h-8 w-8 p-0"
                  >
                    <Edit className='h-5! w-5!' />
                  </Button>
                  <Button
                    variant="destructive"
                    size="sm"
                    onClick={() => onDelete(event)}
                    className="h-8 w-8 p-0"
                  >
                    <Trash2 className='h-5! w-5!' />
                  </Button>
                </>
              )}
            </div>
          </div>
        </div>
      </DialogContent>
    </Dialog>
  );
}

EventDialog.displayName = 'EventDialog';
export default memo(EventDialog);
