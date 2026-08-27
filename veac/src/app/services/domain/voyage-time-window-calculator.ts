// shared/utils/voyage-time-window-calculator.ts

export class VoyageTimeWindowCalculator {
  static MILLISECONDS_IN_HOUR = 3600 * 1000;

  static calculateTimeWindow(dateStr: string, timeStr: string, toleranceHours: number): { min: number, max: number } {
    const baseDate = new Date(dateStr);

    if (timeStr && timeStr !== 'Any time') {
      const [timePart, period] = timeStr.split(' ');
      const [hourStr, minuteStr] = timePart.split(':');
      let hour = parseInt(hourStr, 10);
      const minute = parseInt(minuteStr, 10);

      if (period === 'PM' && hour < 12) hour += 12;
      if (period === 'AM' && hour === 12) hour = 0;

      baseDate.setHours(hour, minute, 0, 0);
    }

    const baseMs = baseDate.getTime();
    const tolerance = toleranceHours * this.MILLISECONDS_IN_HOUR;

    return {
      min: baseMs - tolerance,
      max: baseMs + tolerance
    };
  }

  static getTimestampRangeFromForm(input: { 
    date: string, 
    time: string, 
    hoursTolerance: number,
    timestamp: number  // Add this
  }): { min: number, max: number } {
    const baseMs = input.timestamp;
    const tolerance = input.hoursTolerance * this.MILLISECONDS_IN_HOUR;

    return {
      min: baseMs - tolerance,
      max: baseMs + tolerance
    };
  }
}
