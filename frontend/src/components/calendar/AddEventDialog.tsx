import {
  Button,
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  Input,
  Label,
} from '@/components/ui';
import type { CalendarEvent } from '@/lib/types';
import { Bookmark, Calendar, MapPin, Palette, Save, X } from 'lucide-react';
import { memo, useEffect, useState, useRef } from 'react';

function AddEventDialog({
  isOpen,
  onClose,
  onSave,
  defaultDate,
  eventToEdit,
}: {
  isOpen: boolean;
  onClose: () => void;
  onSave: (e: CalendarEvent) => Promise<void>;
  defaultDate?: Date;
  eventToEdit?: CalendarEvent | null;
}) {
  const [formData, setFormData] = useState({
    title: '',
    subject: '',
    start: '',
    end: '',
    location: '',
    color: '#315F94',
  });
  const [isSubmitting, setIsSubmitting] = useState(false);
  const titleInputRef = useRef<HTMLInputElement | null>(null);

  useEffect(() => {
    if (isOpen) {
      const t = setTimeout(() => titleInputRef.current?.focus(), 0);
      return () => clearTimeout(t);
    }
  }, [isOpen]);

  useEffect(() => {
    if (isOpen) {
      if (eventToEdit) {
        // Modo edición: cargar datos del evento existente
        const pad = (n: number) => n.toString().padStart(2, '0');
        const toLocalISO = (d: Date) => {
          const y = d.getFullYear();
          const m = pad(d.getMonth() + 1);
          const day = pad(d.getDate());
          const h = pad(d.getHours());
          const min = pad(d.getMinutes());
          return `${y}-${m}-${day}T${h}:${min}`;
        };

        setFormData({
          title: eventToEdit.title,
          subject: eventToEdit.subject || '',
          start: toLocalISO(new Date(eventToEdit.start)),
          end: toLocalISO(new Date(eventToEdit.end)),
          location: eventToEdit.location || '',
          color: eventToEdit.color || '#315F94',
        });
      } else {
        // Modo creación: fechas por defecto
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
          color: '#315F94',
        });
      }
    }
  }, [isOpen, defaultDate, eventToEdit]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!formData.title || !formData.start || !formData.end || isSubmitting) return;

    setIsSubmitting(true);
    try {
      const newEvent: CalendarEvent = {
        calendarid: eventToEdit ? eventToEdit.calendarid : `new_${Date.now()}`,
        ...formData,
        category: 'Personal',
        start: new Date(formData.start).toISOString(),
        end: new Date(formData.end).toISOString(),
        description: null,
      };
      await onSave(newEvent);
      onClose();
    } catch {
      // Error is already handled by the parent
    } finally {
      setIsSubmitting(false);
    }
  };

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
          className="px-6 py-4 flex flex-row justify-between items-center"
          style={{ backgroundColor: 'var(--color-brand-main)' }}
        >
          <DialogTitle className="text-xl font-bold text-white">
            {eventToEdit ? 'Edit Event' : 'Add New Event'}
          </DialogTitle>
          <button
            type="button"
            onClick={onClose}
            className="text-white/80 hover:text-white transition-colors"
          >
            <X size={24} />
          </button>
        </DialogHeader>

        <form onSubmit={handleSubmit} className="p-6 space-y-5">
          <fieldset disabled={isSubmitting} className="space-y-5">
            <div className="space-y-2">
              <Label
                htmlFor="ec-title"
                className="flex items-center gap-2 text-brand-dark font-semibold"
              >
                <Bookmark className="w-4 h-4 text-brand-light" />
                Title
              </Label>
              <Input
                ref={titleInputRef}
                id="ec-title"
                required
                value={formData.title}
                onChange={(e) =>
                  setFormData({ ...formData, title: e.target.value })
                }
                placeholder="e.g. Module 5 Lecture"
                className="border-gray-200 focus:border-brand-light"
              />
            </div>

            <div className="space-y-2">
              <Label
                htmlFor="ec-subject"
                className="flex items-center gap-2 text-brand-dark font-semibold"
              >
                <Bookmark className="w-4 h-4 text-brand-light" />
                Subject
              </Label>
              <Input
                id="ec-subject"
                value={formData.subject}
                onChange={(e) =>
                  setFormData({ ...formData, subject: e.target.value })
                }
                placeholder="e.g. Computer Security (optional)"
                className="border-gray-200 focus:border-brand-light"
              />
            </div>

            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2">
                <Label
                  htmlFor="ec-start"
                  className="flex items-center gap-2 text-brand-dark font-semibold"
                >
                  <Calendar className="w-4 h-4 text-brand-light" />
                  Start
                </Label>
                <Input
                  id="ec-start"
                  type="datetime-local"
                  required
                  className="text-sm border-gray-200 focus:border-brand-light"
                  value={formData.start}
                  onChange={(e) =>
                    setFormData({ ...formData, start: e.target.value })
                  }
                />
              </div>
              <div className="space-y-2">
                <Label
                  htmlFor="ec-end"
                  className="flex items-center gap-2 text-brand-dark font-semibold"
                >
                  <Calendar className="w-4 h-4 text-brand-light" />
                  End
                </Label>
                <Input
                  id="ec-end"
                  type="datetime-local"
                  required
                  className="text-sm border-gray-200 focus:border-brand-light"
                  value={formData.end}
                  onChange={(e) =>
                    setFormData({ ...formData, end: e.target.value })
                  }
                />
              </div>
            </div>

            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2">
                <Label
                  htmlFor="ec-location"
                  className="flex items-center gap-2 text-brand-dark font-semibold"
                >
                  <MapPin className="w-4 h-4 text-brand-light" />
                  Location
                </Label>
                <Input
                  id="ec-location"
                  value={formData.location}
                  onChange={(e) =>
                    setFormData({ ...formData, location: e.target.value })
                  }
                  placeholder="Optional"
                  className="border-gray-200 focus:border-brand-light"
                />
              </div>
              <div className="space-y-2">
                <Label
                  htmlFor="ec-color"
                  className="flex items-center gap-2 text-brand-dark font-semibold"
                >
                  <Palette className="w-4 h-4 text-brand-light" />
                  Color
                </Label>
                <div className="flex items-center gap-2">
                  <input
                    id="ec-color"
                    type="color"
                    className="w-12 h-10 px-1 py-1 border border-gray-200 rounded-lg cursor-pointer"
                    value={formData.color}
                    onChange={(e) =>
                      setFormData({ ...formData, color: e.target.value })
                    }
                  />
                  <span className="text-sm text-gray-500">{formData.color}</span>
                </div>
              </div>
            </div>

          </fieldset>

          <div className="pt-4 border-t border-brand-pale">
            <Button
              type="submit"
              disabled={isSubmitting}
              className="w-full flex items-center justify-center gap-2 bg-brand-main hover:bg-brand-dark disabled:opacity-50"
            >
              {isSubmitting ? (
                <>
                  <span className="w-4 h-4 border-2 border-white/30 border-t-white rounded-full animate-spin" />
                  {eventToEdit ? 'Updating...' : 'Saving...'}
                </>
              ) : (
                <>
                  <Save size={16} /> {eventToEdit ? 'Update Event' : 'Save Event'}
                </>
              )}
            </Button>
          </div>
        </form>
      </DialogContent>
    </Dialog>
  );
}

AddEventDialog.displayName = 'AddEventDialog';
export default memo(AddEventDialog);
