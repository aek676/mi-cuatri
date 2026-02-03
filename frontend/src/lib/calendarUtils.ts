export type CalendarDay = {
  day: number;
  month: number;
  year: number;
  currentMonth: boolean;
  dateObj?: Date;
};

export const getMonthData = (year: number, month: number): CalendarDay[] => {
  const firstDay = new Date(year, month, 1);
  const lastDay = new Date(year, month + 1, 0);

  // 0 = Sunday, 1 = Monday. We want Monday to be index 0
  let startDay = firstDay.getDay() - 1;
  if (startDay === -1) startDay = 6;

  const daysInMonth = lastDay.getDate();

  // Previous month padding
  const paddingDays = startDay;
  const prevMonthLastDay = new Date(year, month, 0).getDate();
  const prevMonthDays: CalendarDay[] = [];
  for (let i = paddingDays - 1; i >= 0; i--) {
    prevMonthDays.push({
      day: prevMonthLastDay - i,
      month: month - 1,
      year: year,
      currentMonth: false,
    });
  }

  // Current month days
  const currentMonthDays: CalendarDay[] = [];
  for (let i = 1; i <= daysInMonth; i++) {
    currentMonthDays.push({
      day: i,
      month: month,
      year: year,
      currentMonth: true,
      dateObj: new Date(year, month, i),
    });
  }

  // Next month padding to fill grid (42 cells total for 6 rows)
  const totalDays = prevMonthDays.length + currentMonthDays.length;
  const remainingCells = 42 - totalDays;
  const nextMonthDays: CalendarDay[] = [];
  for (let i = 1; i <= remainingCells; i++) {
    nextMonthDays.push({
      day: i,
      month: month + 1,
      year: year,
      currentMonth: false,
    });
  }

  return [...prevMonthDays, ...currentMonthDays, ...nextMonthDays];
};
