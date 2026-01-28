import { memo } from 'react';
import { hexToRgba } from '@/lib/color';
import { formatTime } from '@/lib/date';
import type { CalendarEvent } from '@/lib/types';
import { MapPin } from 'lucide-react';

function EventItem({
  ev,
  onClick,
  compact = false,
}: {
  ev: CalendarEvent;
  onClick: () => void;
  compact?: boolean;
}) {
  if (compact) {
    return (
      <button
        type="button"
        onClick={onClick}
        style={{
          borderLeftColor: ev.color || 'var(--color-brand-main)',
          backgroundColor: hexToRgba(ev.color, 0.1) || 'rgba(49, 95, 148, 0.1)',
        }}
        className="w-full text-left px-2 py-1.5 rounded text-xs font-medium border-l-[3px] shadow-sm transition-all hover:scale-[1.02] active:scale-95 text-brand-dark hover:opacity-90"
      >
        <div className="flex justify-between items-center mb-0.5 w-full">
          <span className="font-bold opacity-90 text-[10px] truncate pr-1 w-full block">
            {ev.subject}
          </span>
        </div>
        <div className="truncate leading-tight w-full block">{ev.title}</div>
      </button>
    );
  }

  return (
    <button
      type="button"
      onClick={onClick}
      aria-label={`${ev.subject}: ${ev.title}. ${formatTime(ev.start)} - ${formatTime(ev.end)}`}
      style={{
        borderLeftColor: ev.color || 'var(--color-brand-main)',
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

      <h4 className="font-bold text-brand-dark text-sm mt-1">{ev.title}</h4>
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
  );
}

EventItem.displayName = 'EventItem';
export default memo(EventItem);
