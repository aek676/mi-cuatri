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
import type { CalendarEvent } from '@/lib/types';
import { Save, X } from 'lucide-react';
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
      // Focus the title input when dialog opens
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
    <Dialog
      open={isOpen}
      onOpenChange={(open) => {
        if (!open) onClose();
      }}
    >
      <DialogContent showCloseButton={false}>
        <div id="add-event-dialog" role="dialog" aria-modal="true" aria-labelledby="add-event-dialog-title">
          <DialogHeader
            className="px-6 py-4 flex justify-between items-center"
            style={{ backgroundColor: 'var(--color-brand-main)' }}
          >
            <DialogTitle id="add-event-dialog-title" className="text-xl font-bold text-white">
              Add New Event
            </DialogTitle>
            <DialogClose aria-label="Close dialog" className="text-white/80 hover:text-white transition-colors">
              <X size={20} aria-hidden />
            </DialogClose>
          </DialogHeader>

          <form onSubmit={handleSubmit} className="p-6 space-y-4" aria-labelledby="add-event-dialog-title">
          <div>
            <Label htmlFor="ec-title">Title</Label>
            <Input
              ref={titleInputRef}
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
              onValueChange={(val: string) =>
                setFormData({ ...formData, category: val })
              }
            >
              <SelectTrigger id="ec-category" className="w-full" size="default">
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
        </div>
      </DialogContent>
    </Dialog>
  );
}

AddEventDialog.displayName = 'AddEventDialog';
export default memo(AddEventDialog);
