import {
  Button,
  Dialog,
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
import type { CalendarEvent } from '@/lib/types';
import { Bookmark, Calendar, MapPin, Palette, Save, X } from 'lucide-react';
import { memo, useEffect, useState, useRef } from 'react';

function AddEventDialog({
  isOpen,
  onClose,
  onSave,
  defaultDate,
}: {
  isOpen: boolean;
  onClose: () => void;
  onSave: (e: CalendarEvent) => void;
  defaultDate?: Date;
}) {
  const [formData, setFormData] = useState({
    title: '',
    subject: '',
    start: '',
    end: '',
    location: '',
    category: 'Course',
    color: '#315F94',
  });
  const titleInputRef = useRef<HTMLInputElement | null>(null);

  useEffect(() => {
    if (isOpen) {
      const t = setTimeout(() => titleInputRef.current?.focus(), 0);
      return () => clearTimeout(t);
    }
  }, [isOpen]);

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
    };
    onSave(newEvent);
    onClose();
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
            Add New Event
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
              required
              value={formData.subject}
              onChange={(e) =>
                setFormData({ ...formData, subject: e.target.value })
              }
              placeholder="e.g. Computer Security"
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

          <div className="space-y-2">
            <Label
              htmlFor="ec-category"
              className="flex items-center gap-2 text-brand-dark font-semibold"
            >
              <Bookmark className="w-4 h-4 text-brand-light" />
              Category
            </Label>
            <Select
              value={formData.category}
              onValueChange={(val: string) =>
                setFormData({ ...formData, category: val })
              }
            >
              <SelectTrigger
                id="ec-category"
                className="w-full border-gray-200"
                size="default"
              >
                {formData.category === 'Course'
                  ? 'Course (Class)'
                  : 'Assignment / Exam'}
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="Course">Course (Class)</SelectItem>
                <SelectItem value="GradebookColumn">
                  Assignment / Exam
                </SelectItem>
              </SelectContent>
            </Select>
          </div>

          <div className="pt-4 border-t border-brand-pale">
            <Button
              type="submit"
              className="w-full flex items-center justify-center gap-2 bg-brand-main hover:bg-brand-dark"
            >
              <Save size={16} /> Save Event
            </Button>
          </div>
        </form>
      </DialogContent>
    </Dialog>
  );
}

AddEventDialog.displayName = 'AddEventDialog';
export default memo(AddEventDialog);
