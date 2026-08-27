import { Component, OnInit, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { CalendarDay } from '../../../../models/ui/calendar-day.model';

@Component({
  selector: 'app-calendar',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './calendar.component.html',
  styleUrls: ['./calendar.component.css']
})
export class CalendarComponent implements OnInit {
  @Input() selectedDate: Date = new Date();
  @Input() modalRef: any; // Reference to the modal for closing

  @Output() dateSelected = new EventEmitter<{ date: Date }>();
  @Output() modalDismiss = new EventEmitter<void>();

  weekDays: string[] = ['Mo', 'Tu', 'We', 'Th', 'Fr', 'Sa', 'Su'];
  calendarDays: CalendarDay[] = [];
  currentMonth: string = '';

  ngOnInit(): void {
    this.generateCalendar();
  }

  generateCalendar(): void {
    const currentDate = new Date();
    const selectedDate = this.selectedDate;
    const year = selectedDate.getFullYear();
    const month = selectedDate.getMonth();
    
    this.currentMonth = selectedDate.toLocaleString('default', { month: 'long', year: 'numeric' });
    
    // Get first day of the month
    const firstDay = new Date(year, month, 1);
    // Get last day of the month
    const lastDay = new Date(year, month + 1, 0);
    
    // Get day of week of first day (0 = Sunday, 1 = Monday, etc.)
    let firstDayOfWeek = firstDay.getDay();
    // Adjust to have Monday as first day of week
    firstDayOfWeek = firstDayOfWeek === 0 ? 6 : firstDayOfWeek - 1;
    
    this.calendarDays = [];
    
    // Add days from previous month
    const daysFromPrevMonth = firstDayOfWeek;
    const prevMonth = new Date(year, month, 0);
    for (let i = daysFromPrevMonth - 1; i >= 0; i--) {
      const day = new Date(year, month - 1, prevMonth.getDate() - i);
      this.calendarDays.push({
        date: day,
        currentMonth: false,
        isToday: this.isSameDay(day, currentDate),
        isSelected: this.isSameDay(day, selectedDate)
      });
    }
    
    // Add days from current month
    for (let i = 1; i <= lastDay.getDate(); i++) {
      const day = new Date(year, month, i);
      this.calendarDays.push({
        date: day,
        currentMonth: true,
        isToday: this.isSameDay(day, currentDate),
        isSelected: this.isSameDay(day, selectedDate)
      });
    }
    
    // Add days from next month
    const remainingDays = 42 - this.calendarDays.length;
    for (let i = 1; i <= remainingDays; i++) {
      const day = new Date(year, month + 1, i);
      this.calendarDays.push({
        date: day,
        currentMonth: false,
        isToday: this.isSameDay(day, currentDate),
        isSelected: this.isSameDay(day, selectedDate)
      });
    }
  }

  isSameDay(date1: Date, date2: Date): boolean {
    return date1.getDate() === date2.getDate() && 
           date1.getMonth() === date2.getMonth() && 
           date1.getFullYear() === date2.getFullYear();
  }

  selectDate(day: CalendarDay): void {
    this.selectedDate = new Date(day.date);
    this.calendarDays.forEach(d => d.isSelected = this.isSameDay(d.date, day.date));
  }

  previousMonth(): void {
    this.selectedDate = new Date(
      this.selectedDate.getFullYear(),
      this.selectedDate.getMonth() - 1,
      this.selectedDate.getDate()
    );
    this.generateCalendar();
  }
  
  nextMonth(): void {
    this.selectedDate = new Date(
      this.selectedDate.getFullYear(),
      this.selectedDate.getMonth() + 1,
      this.selectedDate.getDate()
    );
    this.generateCalendar();
  }

  goToToday(): void {
    const today = new Date();
    this.selectedDate = new Date(today);
    this.generateCalendar();
  }

  applySelection(): void {
    this.dateSelected.emit({
      date: this.selectedDate
    });
    
    if (this.modalRef) {
      this.modalRef.close();
    }
  }

  cancel(): void {
    this.modalDismiss.emit();
    
    if (this.modalRef) {
      this.modalRef.dismiss();
    }
  }
  
  formatDate(date: Date): string {
    // Format: Feb 19, 2025
    const options: Intl.DateTimeFormatOptions = { 
      month: 'short', 
      day: 'numeric', 
      year: 'numeric' 
    };
    return date.toLocaleDateString('en-US', options);
  }
}