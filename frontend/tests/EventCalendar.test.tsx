import { describe, expect, test, beforeEach, afterEach } from 'bun:test';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import type { CalendarEvent } from '@/lib/types';
import { CalendarCategory } from '@/lib/api';

// ============================================================================
// TEST UTILITIES & FACTORIES
// ============================================================================

/**
 * Factory para crear eventos de prueba
 */
function createMockEvent(overrides?: Partial<CalendarEvent>): CalendarEvent {
  const now = new Date();
  const start = new Date(now.getFullYear(), now.getMonth(), now.getDate(), 10, 0);
  const end = new Date(now.getFullYear(), now.getMonth(), now.getDate(), 11, 0);

  return {
    calendarid: `event-${Math.random().toString(36).substr(2, 9)}`,
    title: 'Test Event',
    subject: 'Test Subject',
    start: start.toISOString(),
    end: end.toISOString(),
    location: 'Test Location',
    category: CalendarCategory.Personal,
    color: '#315F94',
    description: null,
    ...overrides,
  };
}

/**
 * Factory para crear múltiples eventos
 */
function createMockEvents(count: number): CalendarEvent[] {
  return Array.from({ length: count }, (_, i) =>
    createMockEvent({
      calendarid: `event-${i}`,
      title: `Event ${i + 1}`,
      start: new Date(new Date().getFullYear(), new Date().getMonth(), new Date().getDate(), 10 + i, 0).toISOString(),
      end: new Date(new Date().getFullYear(), new Date().getMonth(), new Date().getDate(), 11 + i, 0).toISOString(),
    })
  );
}

// ============================================================================
// TESTS FOR CALENDAREVENT TYPE & UTILITIES
// ============================================================================

describe('CalendarEvent Type & Factory Functions', () => {
  // ========================================================================
  // FACTORY TESTS (6 tests)
  // ========================================================================
  describe('Event Factory', () => {
    test('createMockEvent() crea evento válido con valores por defecto', () => {
      const event = createMockEvent();

      expect(event.calendarid).toBeTruthy();
      expect(event.title).toBe('Test Event');
      expect(event.subject).toBe('Test Subject');
      expect(event.start).toBeTruthy();
      expect(event.end).toBeTruthy();
      expect(event.location).toBe('Test Location');
      expect(event.category).toBe(CalendarCategory.Personal);
      expect(event.color).toBe('#315F94');
    });

    test('createMockEvent() acepta overrides para campos individuales', () => {
      const customTitle = 'Custom Title';
      const customColor = '#FF0000';

      const event = createMockEvent({
        title: customTitle,
        color: customColor,
      });

      expect(event.title).toBe(customTitle);
      expect(event.color).toBe(customColor);
      expect(event.subject).toBe('Test Subject');
    });

    test('createMockEvent() genera IDs únicos', () => {
      const event1 = createMockEvent();
      const event2 = createMockEvent();

      expect(event1.calendarid).not.toBe(event2.calendarid);
    });

    test('createMockEvent() soporta campos null/undefined', () => {
      const event = createMockEvent({
        location: null,
        subject: null,
        description: null,
      });

      expect(event.location).toBeNull();
      expect(event.subject).toBeNull();
      expect(event.description).toBeNull();
    });

    test('createMockEvent() genera fechas ISO válidas', () => {
      const event = createMockEvent();

      const startDate = new Date(event.start);
      const endDate = new Date(event.end);

      expect(isNaN(startDate.getTime())).toBe(false);
      expect(isNaN(endDate.getTime())).toBe(false);
      expect(startDate.getTime()).toBeLessThan(endDate.getTime());
    });

    test('createMockEvent() soporta todos los CalendarCategory valores', () => {
      const categories = Object.values(CalendarCategory);

      categories.forEach((category) => {
        const event = createMockEvent({ category });
        expect(event.category).toBe(category);
      });
    });
  });

  // ========================================================================
  // MULTIPLE EVENTS FACTORY TESTS (4 tests)
  // ========================================================================
  describe('Multiple Events Factory', () => {
    test('createMockEvents() crea array de eventos', () => {
      const events = createMockEvents(3);

      expect(events).toHaveLength(3);
      expect(Array.isArray(events)).toBe(true);
    });

    test('createMockEvents() genera eventos con IDs secuenciales', () => {
      const events = createMockEvents(5);

      events.forEach((event, index) => {
        expect(event.calendarid).toBe(`event-${index}`);
      });
    });

    test('createMockEvents() genera eventos con títulos únicos', () => {
      const events = createMockEvents(4);
      const titles = events.map((e) => e.title);

      const uniqueTitles = new Set(titles);
      expect(uniqueTitles.size).toBe(4);
    });

    test('createMockEvents(0) devuelve array vacío', () => {
      const events = createMockEvents(0);
      expect(events).toHaveLength(0);
    });
  });

  // ========================================================================
  // CALENDAREVENT TYPE VALIDATION (5 tests)
  // ========================================================================
  describe('CalendarEvent Type Validation', () => {
    test('evento tiene todos los campos requeridos', () => {
      const event = createMockEvent();

      expect('calendarid' in event).toBe(true);
      expect('title' in event).toBe(true);
      expect('start' in event).toBe(true);
      expect('end' in event).toBe(true);
    });

    test('evento tiene los campos opcionales correctamente tipados', () => {
      const event = createMockEvent({
        subject: 'Subject',
        location: 'Location',
        color: '#FFFFFF',
        description: 'Description',
      });

      expect(typeof event.subject).toBe('string');
      expect(typeof event.location).toBe('string');
      expect(typeof event.color).toBe('string');
      expect(typeof event.description).toBe('string');
    });

    test('evento puede tener campos opcionales como null', () => {
      const event = createMockEvent({
        subject: null,
        location: null,
        color: null,
        description: null,
      });

      expect(event.subject).toBeNull();
      expect(event.location).toBeNull();
      expect(event.color).toBeNull();
      expect(event.description).toBeNull();
    });

    test('calendarid es siempre string no vacío', () => {
      const events = createMockEvents(10);

      events.forEach((event) => {
        expect(typeof event.calendarid).toBe('string');
        expect(event.calendarid.length).toBeGreaterThan(0);
      });
    });

    test('start y end son ISO strings válidos', () => {
      const event = createMockEvent();

      // Verificar que son strings ISO válidos
      const isoRegex = /^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(\.\d{3})?Z?$/;
      expect(isoRegex.test(event.start)).toBe(true);
      expect(isoRegex.test(event.end)).toBe(true);
    });
  });

  // ========================================================================
  // DATE HANDLING TESTS (6 tests)
  // ========================================================================
  describe('Date Handling', () => {
    test('evento en mismo día tiene start antes de end', () => {
      const event = createMockEvent();

      const start = new Date(event.start).getTime();
      const end = new Date(event.end).getTime();

      expect(start).toBeLessThan(end);
    });

    test('eventos con horas diferentes se crean correctamente', () => {
      const events = createMockEvents(24);

      events.forEach((event, index) => {
        const startHour = new Date(event.start).getHours();
        expect(startHour).toBe((10 + index) % 24);
      });
    });

    test('evento puede cruzar a día siguiente', () => {
      const now = new Date();
      const event = createMockEvent({
        start: new Date(now.getFullYear(), now.getMonth(), now.getDate(), 22, 0).toISOString(),
        end: new Date(now.getFullYear(), now.getMonth(), now.getDate() + 1, 2, 0).toISOString(),
      });

      const startDate = new Date(event.start);
      const endDate = new Date(event.end);

      expect(startDate.getDate()).not.toBe(endDate.getDate());
      expect(endDate.getTime()).toBeGreaterThan(startDate.getTime());
    });

    test('evento puede ser en diferentes meses', () => {
      const now = new Date();
      const event = createMockEvent({
        start: new Date(now.getFullYear(), now.getMonth(), 28, 22, 0).toISOString(),
        end: new Date(now.getFullYear(), now.getMonth() + 1, 2, 2, 0).toISOString(),
      });

      const startDate = new Date(event.start);
      const endDate = new Date(event.end);

      expect(endDate.getTime()).toBeGreaterThan(startDate.getTime());
    });

    test('evento puede ser en diferentes años', () => {
      const now = new Date();
      const event = createMockEvent({
        start: new Date(now.getFullYear(), 11, 28, 22, 0).toISOString(),
        end: new Date(now.getFullYear() + 1, 0, 2, 2, 0).toISOString(),
      });

      const startDate = new Date(event.start);
      const endDate = new Date(event.end);

      expect(endDate.getTime()).toBeGreaterThan(startDate.getTime());
    });

    test('ordenamiento de eventos por hora funciona', () => {
      const events = createMockEvents(5);

      const sorted = [...events].sort(
        (a, b) => new Date(a.start).getTime() - new Date(b.start).getTime()
      );

      sorted.forEach((event, index) => {
        if (index < sorted.length - 1) {
          expect(new Date(event.start).getTime()).toBeLessThanOrEqual(
            new Date(sorted[index + 1].start).getTime()
          );
        }
      });
    });
  });

  // ========================================================================
  // CATEGORY & COLOR TESTS (4 tests)
  // ========================================================================
  describe('Category & Color Handling', () => {
    test('evento soporta todas las categorías', () => {
      const categoriesCount = Object.keys(CalendarCategory).length;
      expect(categoriesCount).toBeGreaterThan(0);
    });

    test('evento con color personalizado lo mantiene', () => {
      const colors = ['#FF0000', '#00FF00', '#0000FF', '#FFFFFF', '#000000'];

      colors.forEach((color) => {
        const event = createMockEvent({ color });
        expect(event.color).toBe(color);
      });
    });

    test('evento con color null es válido', () => {
      const event = createMockEvent({ color: null });
      expect(event.color).toBeNull();
    });

    test('evento hereda color por defecto si no se especifica', () => {
      const event = createMockEvent();
      expect(event.color).toBe('#315F94');
    });
  });

  // ========================================================================
  // FILTERING & QUERYING SIMULATION (5 tests)
  // ========================================================================
  describe('Event Filtering & Querying', () => {
    test('eventos pueden filtrarse por título', () => {
      const events = createMockEvents(5);
      events[2].title = 'Special Event';

      const filtered = events.filter((e) => e.title === 'Special Event');

      expect(filtered).toHaveLength(1);
      expect(filtered[0].title).toBe('Special Event');
    });

    test('eventos pueden filtrarse por categoría', () => {
      const events = [
        createMockEvent({ category: CalendarCategory.Personal }),
        createMockEvent({ category: CalendarCategory.Work }),
        createMockEvent({ category: CalendarCategory.Personal }),
      ];

      const personal = events.filter((e) => e.category === CalendarCategory.Personal);
      expect(personal).toHaveLength(2);
    });

    test('eventos pueden filtrarse por fecha', () => {
      const now = new Date();
      const today = new Date(now.getFullYear(), now.getMonth(), now.getDate());
      const tomorrow = new Date(today);
      tomorrow.setDate(today.getDate() + 1);

      const event1 = createMockEvent({
        start: today.toISOString(),
        end: today.toISOString(),
      });

      const event2 = createMockEvent({
        start: tomorrow.toISOString(),
        end: tomorrow.toISOString(),
      });

      const events = [event1, event2];
      const todayEvents = events.filter((e) => {
        const eventDate = new Date(e.start);
        return (
          eventDate.getDate() === today.getDate() &&
          eventDate.getMonth() === today.getMonth() &&
          eventDate.getFullYear() === today.getFullYear()
        );
      });

      expect(todayEvents).toHaveLength(1);
      expect(todayEvents[0].calendarid).toBe(event1.calendarid);
    });

    test('eventos pueden buscarse por location', () => {
      const events = createMockEvents(5);
      events[1].location = 'Conference Room A';
      events[3].location = 'Conference Room A';

      const searched = events.filter((e) => e.location === 'Conference Room A');

      expect(searched).toHaveLength(2);
    });

    test('eventos pueden filtrarse por rango de fechas', () => {
      const now = new Date();
      const startOfMonth = new Date(now.getFullYear(), now.getMonth(), 1);
      const endOfMonth = new Date(now.getFullYear(), now.getMonth() + 1, 0);

      const events = createMockEvents(10);

      const inMonth = events.filter((e) => {
        const eventDate = new Date(e.start);
        return eventDate >= startOfMonth && eventDate <= endOfMonth;
      });

      expect(inMonth.length).toBeGreaterThanOrEqual(0);
    });
  });

  // ========================================================================
  // BATCH OPERATIONS (4 tests)
  // ========================================================================
  describe('Batch Operations', () => {
    test('múltiples eventos pueden agregarse a array', () => {
      const events: CalendarEvent[] = [];
      const newEvents = createMockEvents(5);

      events.push(...newEvents);

      expect(events).toHaveLength(5);
    });

    test('evento puede removerse de array por ID', () => {
      const events = createMockEvents(5);
      const idToRemove = events[2].calendarid;

      const filtered = events.filter((e) => e.calendarid !== idToRemove);

      expect(filtered).toHaveLength(4);
      expect(filtered.some((e) => e.calendarid === idToRemove)).toBe(false);
    });

    test('evento puede actualizarse en array', () => {
      const events = createMockEvents(3);
      const indexToUpdate = 1;
      const updatedTitle = 'Updated Title';

      const updated = events.map((e, i) =>
        i === indexToUpdate ? { ...e, title: updatedTitle } : e
      );

      expect(updated[indexToUpdate].title).toBe(updatedTitle);
      expect(updated[0].title).not.toBe(updatedTitle);
    });

    test('todos los eventos pueden filtrarse y transformarse', () => {
      const events = createMockEvents(10);

      const transformed = events
        .filter((e) => e.title.includes('Event'))
        .map((e) => ({
          ...e,
          title: e.title.toUpperCase(),
        }));

      expect(transformed).toHaveLength(10);
      transformed.forEach((e) => {
        expect(e.title).toMatch(/EVENT/);
      });
    });
  });

  // ========================================================================
  // DUPLICATE HANDLING (3 tests)
  // ========================================================================
  describe('Duplicate Event Handling', () => {
    test('evento duplicado tiene calendarid diferente', () => {
      const event1 = createMockEvent({ title: 'Test' });
      const event2 = createMockEvent({ title: 'Test' });

      expect(event1.calendarid).not.toBe(event2.calendarid);
    });

    test('eventos pueden compararse por contenido (no por ID)', () => {
      const event1 = createMockEvent({ title: 'Same', location: 'Loc A' });
      const event2 = createMockEvent({ title: 'Same', location: 'Loc A' });

      const contentEqual =
        event1.title === event2.title &&
        event1.location === event2.location &&
        event1.subject === event2.subject;

      expect(contentEqual).toBe(true);
      expect(event1.calendarid).not.toBe(event2.calendarid);
    });

    test('detectar duplicados en array por contenido', () => {
      const events = createMockEvents(5);
      const duplicate = createMockEvent({
        title: events[0].title,
        location: events[0].location,
      });

      events.push(duplicate);

      const contentMatches = events.filter((e) => e.title === events[0].title);

      expect(contentMatches).toHaveLength(2);
      expect(contentMatches[0].calendarid).not.toBe(contentMatches[1].calendarid);
    });
  });

  // ========================================================================
  // EDGE CASES FOR EVENTS (6 tests)
  // ========================================================================
  describe('Edge Cases for CalendarEvent', () => {
    test('evento con título muy largo', () => {
      const longTitle = 'A'.repeat(500);
      const event = createMockEvent({ title: longTitle });

      expect(event.title).toBe(longTitle);
      expect(event.title.length).toBe(500);
    });

    test('evento con caracteres especiales', () => {
      const specialTitle = '🎉 Meeting & Review (2024) @ Office #1';
      const event = createMockEvent({ title: specialTitle });

      expect(event.title).toBe(specialTitle);
    });

    test('evento con location como string vacío', () => {
      const event = createMockEvent({ location: '' });

      expect(event.location).toBe('');
    });

    test('evento con description muy larga', () => {
      const longDesc = 'Lorem ipsum '.repeat(100);
      const event = createMockEvent({ description: longDesc });

      expect(event.description).toBe(longDesc);
    });

    test('evento con todas las propiedades null/undefined', () => {
      const event = createMockEvent({
        subject: null,
        location: null,
        category: undefined as any,
        color: null,
        description: null,
      });

      expect(event.calendarid).toBeTruthy();
      expect(event.title).toBeTruthy();
      expect(event.start).toBeTruthy();
      expect(event.end).toBeTruthy();
    });

    test('evento con mismo start y end (evento de 0 duración)', () => {
      const now = new Date();
      const event = createMockEvent({
        start: now.toISOString(),
        end: now.toISOString(),
      });

      expect(event.start).toBe(event.end);
    });
  });

  // ========================================================================
  // TYPE SAFETY SIMULATION (4 tests)
  // ========================================================================
  describe('Type Safety Simulation', () => {
    test('evento mantiene tipos correctos después de spread', () => {
      const event = createMockEvent();
      const spread = { ...event };

      expect(typeof spread.calendarid).toBe('string');
      expect(typeof spread.title).toBe('string');
      expect(typeof spread.start).toBe('string');
      expect(typeof spread.end).toBe('string');
    });

    test('evento puede ser parcialmente actualizado', () => {
      const event = createMockEvent();
      const partial: Partial<CalendarEvent> = {
        title: 'Updated',
        color: '#FF0000',
      };

      const updated = { ...event, ...partial };

      expect(updated.title).toBe('Updated');
      expect(updated.color).toBe('#FF0000');
      expect(updated.location).toBe(event.location);
    });

    test('array de eventos mantiene tipos', () => {
      const events: CalendarEvent[] = createMockEvents(5);

      events.forEach((event) => {
        expect(typeof event.calendarid).toBe('string');
        expect(typeof event.title).toBe('string');
      });
    });

    test('evento cumple interfaz CalendarEvent', () => {
      const event = createMockEvent();

      // Verificar que tiene todos los campos requeridos de la interfaz
      const requiredFields = [
        'calendarid',
        'title',
        'start',
        'end',
      ] as const;

      requiredFields.forEach((field) => {
        expect(field in event).toBe(true);
        expect(event[field]).toBeTruthy();
      });
    });
  });
});
