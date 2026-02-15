import { describe, expect, test } from 'bun:test';
import type { CalendarEvent } from '@/lib/types';
import { CalendarCategory } from '@/lib/api';

// ============================================================================
// MOCK IMPLEMENTATION OF REDUCER FROM EVENTCALENDAR
// ============================================================================

/**
 * Reproduzco el reducer del componente EventCalendar para testear su lógica
 */
type EventAction =
  | { type: 'set'; payload: CalendarEvent[] }
  | { type: 'add'; payload: CalendarEvent }
  | { type: 'confirm'; tempId: string; realEvent: CalendarEvent }
  | { type: 'remove'; id: string }
  | { type: 'update'; payload: CalendarEvent };

function eventsReducer(state: CalendarEvent[], action: EventAction): CalendarEvent[] {
  switch (action.type) {
    case 'set':
      return action.payload;
    case 'add':
      return [...state, action.payload];
    case 'confirm':
      return state.map((evt) =>
        evt.calendarid === action.tempId ? action.realEvent : evt
      );
    case 'remove':
      return state.filter((evt) => evt.calendarid !== action.id);
    case 'update':
      return state.map((evt) =>
        evt.calendarid === action.payload.calendarid ? action.payload : evt
      );
    default:
      return state;
  }
}

// ============================================================================
// UTILITY FUNCTIONS
// ============================================================================

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

// ============================================================================
// REDUCER TESTS
// ============================================================================

describe('EventCalendar Reducer Logic', () => {
  // ========================================================================
  // SET ACTION (3 tests)
  // ========================================================================
  describe('SET Action', () => {
    test("'set' action reemplaza todos los eventos", () => {
      const initialState = [createMockEvent({ calendarid: 'event-1' })];
      const newEvents = [
        createMockEvent({ calendarid: 'event-new-1' }),
        createMockEvent({ calendarid: 'event-new-2' }),
      ];

      const result = eventsReducer(initialState, {
        type: 'set',
        payload: newEvents,
      });

      expect(result).toEqual(newEvents);
      expect(result).toHaveLength(2);
      expect(result[0].calendarid).toBe('event-new-1');
    });

    test("'set' action puede vaciarse", () => {
      const initialState = [
        createMockEvent({ calendarid: 'event-1' }),
        createMockEvent({ calendarid: 'event-2' }),
      ];

      const result = eventsReducer(initialState, {
        type: 'set',
        payload: [],
      });

      expect(result).toHaveLength(0);
    });

    test("'set' action mantiene estructura de eventos", () => {
      const newEvents = [createMockEvent(), createMockEvent()];

      const result = eventsReducer([], {
        type: 'set',
        payload: newEvents,
      });

      result.forEach((event, index) => {
        expect(event.calendarid).toBe(newEvents[index].calendarid);
        expect(event.title).toBe(newEvents[index].title);
        expect(event.start).toBe(newEvents[index].start);
      });
    });
  });

  // ========================================================================
  // ADD ACTION (4 tests)
  // ========================================================================
  describe('ADD Action', () => {
    test("'add' agrega evento al final del array", () => {
      const initialState = [
        createMockEvent({ calendarid: 'event-1' }),
        createMockEvent({ calendarid: 'event-2' }),
      ];
      const newEvent = createMockEvent({ calendarid: 'event-3' });

      const result = eventsReducer(initialState, {
        type: 'add',
        payload: newEvent,
      });

      expect(result).toHaveLength(3);
      expect(result[2].calendarid).toBe('event-3');
    });

    test("'add' a estado vacío funciona", () => {
      const newEvent = createMockEvent();

      const result = eventsReducer([], {
        type: 'add',
        payload: newEvent,
      });

      expect(result).toHaveLength(1);
      expect(result[0]).toEqual(newEvent);
    });

    test("'add' mantiene eventos existentes sin modificar", () => {
      const existing = createMockEvent({ calendarid: 'event-1', title: 'Original' });
      const newEvent = createMockEvent({ calendarid: 'event-2' });

      const result = eventsReducer([existing], {
        type: 'add',
        payload: newEvent,
      });

      expect(result[0].title).toBe('Original');
      expect(result[0].calendarid).toBe('event-1');
    });

    test("'add' múltiples veces acumula eventos", () => {
      let state: CalendarEvent[] = [];

      for (let i = 0; i < 5; i++) {
        state = eventsReducer(state, {
          type: 'add',
          payload: createMockEvent({ calendarid: `event-${i}` }),
        });
      }

      expect(state).toHaveLength(5);
    });
  });

  // ========================================================================
  // CONFIRM ACTION (4 tests)
  // ========================================================================
  describe('CONFIRM Action', () => {
    test("'confirm' reemplaza evento temporal por real", () => {
      const tempEvent = createMockEvent({ calendarid: 'optimistic_123' });
      const realEvent = createMockEvent({
        calendarid: 'event-real-id',
        title: tempEvent.title,
      });

      const state = [tempEvent];

      const result = eventsReducer(state, {
        type: 'confirm',
        tempId: 'optimistic_123',
        realEvent,
      });

      expect(result).toHaveLength(1);
      expect(result[0].calendarid).toBe('event-real-id');
      expect(result[0].title).toBe(tempEvent.title);
    });

    test("'confirm' mantiene otros eventos intactos", () => {
      const tempEvent = createMockEvent({ calendarid: 'optimistic_123', title: 'Temp' });
      const existingEvent = createMockEvent({ calendarid: 'event-existing', title: 'Existing' });
      const realEvent = createMockEvent({ calendarid: 'event-real', title: 'Real' });

      const state = [tempEvent, existingEvent];

      const result = eventsReducer(state, {
        type: 'confirm',
        tempId: 'optimistic_123',
        realEvent,
      });

      expect(result).toHaveLength(2);
      expect(result[1].title).toBe('Existing');
    });

    test("'confirm' con tempId no existente no cambia nada", () => {
      const event = createMockEvent({ calendarid: 'event-1' });
      const realEvent = createMockEvent({ calendarid: 'event-real' });

      const state = [event];

      const result = eventsReducer(state, {
        type: 'confirm',
        tempId: 'non-existent',
        realEvent,
      });

      expect(result).toHaveLength(1);
      expect(result[0].calendarid).toBe('event-1');
    });

    test("'confirm' preserva propiedades del evento real", () => {
      const tempEvent = createMockEvent({ calendarid: 'optimistic_123' });
      const realEvent = createMockEvent({
        calendarid: 'event-real',
        title: 'Updated Title',
        location: 'Updated Location',
        color: '#FF0000',
      });

      const state = [tempEvent];

      const result = eventsReducer(state, {
        type: 'confirm',
        tempId: 'optimistic_123',
        realEvent,
      });

      expect(result[0].title).toBe('Updated Title');
      expect(result[0].location).toBe('Updated Location');
      expect(result[0].color).toBe('#FF0000');
    });
  });

  // ========================================================================
  // REMOVE ACTION (4 tests)
  // ========================================================================
  describe('REMOVE Action', () => {
    test("'remove' elimina evento por ID", () => {
      const events = [
        createMockEvent({ calendarid: 'event-1' }),
        createMockEvent({ calendarid: 'event-2' }),
        createMockEvent({ calendarid: 'event-3' }),
      ];

      const result = eventsReducer(events, {
        type: 'remove',
        id: 'event-2',
      });

      expect(result).toHaveLength(2);
      expect(result[0].calendarid).toBe('event-1');
      expect(result[1].calendarid).toBe('event-3');
    });

    test("'remove' con ID no existente no cambia array", () => {
      const events = [
        createMockEvent({ calendarid: 'event-1' }),
        createMockEvent({ calendarid: 'event-2' }),
      ];

      const result = eventsReducer(events, {
        type: 'remove',
        id: 'non-existent',
      });

      expect(result).toHaveLength(2);
    });

    test("'remove' último evento vacía array", () => {
      const events = [createMockEvent({ calendarid: 'event-1' })];

      const result = eventsReducer(events, {
        type: 'remove',
        id: 'event-1',
      });

      expect(result).toHaveLength(0);
    });

    test("'remove' múltiples eventos", () => {
      let events = [
        createMockEvent({ calendarid: 'event-1' }),
        createMockEvent({ calendarid: 'event-2' }),
        createMockEvent({ calendarid: 'event-3' }),
      ];

      events = eventsReducer(events, { type: 'remove', id: 'event-1' });
      events = eventsReducer(events, { type: 'remove', id: 'event-3' });

      expect(events).toHaveLength(1);
      expect(events[0].calendarid).toBe('event-2');
    });
  });

  // ========================================================================
  // UPDATE ACTION (5 tests)
  // ========================================================================
  describe('UPDATE Action', () => {
    test("'update' modifica evento existente", () => {
      const original = createMockEvent({ calendarid: 'event-1', title: 'Original Title' });
      const updated = createMockEvent({
        calendarid: 'event-1',
        title: 'Updated Title',
      });

      const state = [original];

      const result = eventsReducer(state, {
        type: 'update',
        payload: updated,
      });

      expect(result[0].title).toBe('Updated Title');
      expect(result[0].calendarid).toBe('event-1');
    });

    test("'update' mantiene otros eventos intactos", () => {
      const event1 = createMockEvent({ calendarid: 'event-1', title: 'Event 1' });
      const event2 = createMockEvent({ calendarid: 'event-2', title: 'Event 2' });

      const updated = createMockEvent({
        calendarid: 'event-1',
        title: 'Updated Event 1',
      });

      const state = [event1, event2];

      const result = eventsReducer(state, {
        type: 'update',
        payload: updated,
      });

      expect(result).toHaveLength(2);
      expect(result[0].title).toBe('Updated Event 1');
      expect(result[1].title).toBe('Event 2');
    });

    test("'update' con ID no existente no cambia nada", () => {
      const event = createMockEvent({ calendarid: 'event-1' });
      const nonExistentUpdate = createMockEvent({ calendarid: 'event-999' });

      const state = [event];

      const result = eventsReducer(state, {
        type: 'update',
        payload: nonExistentUpdate,
      });

      expect(result).toHaveLength(1);
      expect(result[0].calendarid).toBe('event-1');
    });

    test("'update' puede cambiar múltiples propiedades", () => {
      const original = createMockEvent({
        calendarid: 'event-1',
        title: 'Original',
        location: 'Old Location',
        color: '#000000',
      });

      const updated = createMockEvent({
        calendarid: 'event-1',
        title: 'New Title',
        location: 'New Location',
        color: '#FFFFFF',
      });

      const state = [original];

      const result = eventsReducer(state, {
        type: 'update',
        payload: updated,
      });

      expect(result[0].title).toBe('New Title');
      expect(result[0].location).toBe('New Location');
      expect(result[0].color).toBe('#FFFFFF');
    });

    test("'update' múltiples veces el mismo evento", () => {
      let state = [createMockEvent({ calendarid: 'event-1', title: 'V1' })];

      state = eventsReducer(state, {
        type: 'update',
        payload: createMockEvent({ calendarid: 'event-1', title: 'V2' }),
      });

      state = eventsReducer(state, {
        type: 'update',
        payload: createMockEvent({ calendarid: 'event-1', title: 'V3' }),
      });

      expect(state[0].title).toBe('V3');
    });
  });

  // ========================================================================
  // COMPLEX SCENARIOS (6 tests)
  // ========================================================================
  describe('Complex Reducer Scenarios', () => {
    test('secuencia completa: add → confirm → update → remove', () => {
      let state: CalendarEvent[] = [];

      // Add optimista
      const tempEvent = createMockEvent({ calendarid: 'optimistic_1' });
      state = eventsReducer(state, { type: 'add', payload: tempEvent });
      expect(state).toHaveLength(1);

      // Confirm
      const realEvent = createMockEvent({ calendarid: 'event-real' });
      state = eventsReducer(state, {
        type: 'confirm',
        tempId: 'optimistic_1',
        realEvent,
      });
      expect(state[0].calendarid).toBe('event-real');

      // Update
      const updated = createMockEvent({ calendarid: 'event-real', title: 'Updated' });
      state = eventsReducer(state, { type: 'update', payload: updated });
      expect(state[0].title).toBe('Updated');

      // Remove
      state = eventsReducer(state, { type: 'remove', id: 'event-real' });
      expect(state).toHaveLength(0);
    });

    test('múltiples eventos en operaciones complejas', () => {
      let state = [
        createMockEvent({ calendarid: 'event-1' }),
        createMockEvent({ calendarid: 'event-2' }),
      ];

      // Agregar
      state = eventsReducer(state, {
        type: 'add',
        payload: createMockEvent({ calendarid: 'event-3' }),
      });

      // Actualizar el primero
      state = eventsReducer(state, {
        type: 'update',
        payload: createMockEvent({ calendarid: 'event-1', title: 'Updated' }),
      });

      // Remover el segundo
      state = eventsReducer(state, { type: 'remove', id: 'event-2' });

      expect(state).toHaveLength(2);
      expect(state[0].title).toBe('Updated');
    });

    test('set reemplaza todo incluyendo temporales', () => {
      let state = [
        createMockEvent({ calendarid: 'optimistic_1' }),
        createMockEvent({ calendarid: 'event-1' }),
      ];

      const newState = [
        createMockEvent({ calendarid: 'event-100' }),
        createMockEvent({ calendarid: 'event-101' }),
      ];

      state = eventsReducer(state, { type: 'set', payload: newState });

      expect(state).toHaveLength(2);
      expect(state.some((e) => e.calendarid === 'optimistic_1')).toBe(false);
    });

    test('múltiples confirms seguidos', () => {
      let state = [
        createMockEvent({ calendarid: 'temp-1' }),
        createMockEvent({ calendarid: 'temp-2' }),
      ];

      state = eventsReducer(state, {
        type: 'confirm',
        tempId: 'temp-1',
        realEvent: createMockEvent({ calendarid: 'real-1' }),
      });

      state = eventsReducer(state, {
        type: 'confirm',
        tempId: 'temp-2',
        realEvent: createMockEvent({ calendarid: 'real-2' }),
      });

      expect(state).toHaveLength(2);
      expect(state[0].calendarid).toBe('real-1');
      expect(state[1].calendarid).toBe('real-2');
    });

    test('intercalación de adds y removes', () => {
      let state: CalendarEvent[] = [];

      state = eventsReducer(state, {
        type: 'add',
        payload: createMockEvent({ calendarid: 'event-1' }),
      });
      state = eventsReducer(state, {
        type: 'add',
        payload: createMockEvent({ calendarid: 'event-2' }),
      });
      state = eventsReducer(state, {
        type: 'add',
        payload: createMockEvent({ calendarid: 'event-3' }),
      });

      state = eventsReducer(state, { type: 'remove', id: 'event-2' });

      state = eventsReducer(state, {
        type: 'add',
        payload: createMockEvent({ calendarid: 'event-4' }),
      });

      expect(state).toHaveLength(3);
      expect(state.map((e) => e.calendarid)).toEqual([
        'event-1',
        'event-3',
        'event-4',
      ]);
    });

    test('estado se mantiene inmutable', () => {
      const originalState = [createMockEvent({ calendarid: 'event-1' })];
      const originalLength = originalState.length;
      const originalId = originalState[0].calendarid;

      eventsReducer(originalState, {
        type: 'add',
        payload: createMockEvent({ calendarid: 'event-2' }),
      });

      expect(originalState).toHaveLength(originalLength);
      expect(originalState[0].calendarid).toBe(originalId);
    });
  });

  // ========================================================================
  // EDGE CASES (5 tests)
  // ========================================================================
  describe('Reducer Edge Cases', () => {
    test('maneja estado vacío correctamente', () => {
      const result = eventsReducer([], {
        type: 'set',
        payload: [],
      });

      expect(result).toHaveLength(0);
    });

    test('maneja eventos con mismo título', () => {
      const event1 = createMockEvent({ calendarid: 'event-1', title: 'Same' });
      const event2 = createMockEvent({ calendarid: 'event-2', title: 'Same' });

      let state = [event1, event2];

      state = eventsReducer(state, {
        type: 'update',
        payload: createMockEvent({ calendarid: 'event-1', title: 'Different' }),
      });

      expect(state[0].title).toBe('Different');
      expect(state[1].title).toBe('Same');
    });

    test('maneja eventos con propiedades null', () => {
      const event = createMockEvent({
        location: null,
        description: null,
      });

      let state = [event];

      state = eventsReducer(state, {
        type: 'update',
        payload: createMockEvent({
          calendarid: event.calendarid,
          location: 'New Location',
        }),
      });

      expect(state[0].location).toBe('New Location');
      expect(state[0].title).toBe('Test Event');
    });

    test('maneja IDs muy largos', () => {
      const longId = 'event-' + 'x'.repeat(1000);
      const event = createMockEvent({ calendarid: longId });

      let state = [event];

      state = eventsReducer(state, { type: 'remove', id: longId });

      expect(state).toHaveLength(0);
    });

    test('maneja acción tipo desconocida', () => {
      const state = [createMockEvent()];

      const result = eventsReducer(state, {
        type: 'unknown' as any,
      });

      expect(result).toEqual(state);
    });
  });

  // ========================================================================
  // PERFORMANCE (2 tests)
  // ========================================================================
  describe('Reducer Performance', () => {
    test('maneja array grande de eventos', () => {
      const largeState = Array.from({ length: 1000 }, (_, i) =>
        createMockEvent({ calendarid: `event-${i}` })
      );

      const result = eventsReducer(largeState, {
        type: 'remove',
        id: 'event-500',
      });

      expect(result).toHaveLength(999);
    });

    test('operaciones en cascada con muchos eventos', () => {
      let state = Array.from({ length: 100 }, (_, i) =>
        createMockEvent({ calendarid: `event-${i}` })
      );

      for (let i = 0; i < 10; i++) {
        state = eventsReducer(state, {
          type: 'add',
          payload: createMockEvent({ calendarid: `new-${i}` }),
        });
      }

      expect(state).toHaveLength(110);
    });
  });
});
