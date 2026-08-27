// date-time.component.ts - Enhanced with modal management
import { Component, Input, Output, EventEmitter, ViewChild, TemplateRef, Renderer2, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { NgbModal, NgbModalRef } from '@ng-bootstrap/ng-bootstrap';
import { CalendarComponent } from '../date-time/calendar/calendar.component';
import { TimePickerComponent } from '../date-time/time-picker/time-picker.component';

export type DateTimeType = 'etd' | 'eta';

export interface CalendarDay {
  date: Date;
  currentMonth: boolean;
  isToday: boolean;
  isSelected: boolean;
}

@Component({
  selector: 'app-date-time',
  standalone: true,
  imports: [CommonModule, FormsModule, CalendarComponent, TimePickerComponent],
  templateUrl: './date-time.component.html',
  styleUrls: ['./date-time.component.css']
})
export class DateTimeComponent implements OnDestroy {
  @ViewChild('datePickerModal') datePickerModal!: TemplateRef<any>;
  @ViewChild('timePickerModal') timePickerModal!: TemplateRef<any>;

  // Inputs
  @Input() heading?: string;
  @Input() type: DateTimeType = 'etd';
  @Input() defaultDateText: string = 'Select date';
  @Input() dateFormatted: string = '';
  @Input() timeFormatted: string = 'Any time';
  @Input() daysTolerance: number = 0;
  @Input() hoursTolerance: number = 0;
  
  // Outputs - simplified, we handle the modal logic internally
  @Output() dateChanged = new EventEmitter<{ date: string, tolerance: number, type: DateTimeType }>();
  @Output() timeChanged = new EventEmitter<{ time: string, tolerance: number, type: DateTimeType }>();
  
  // Internal modal state
  private modalRef?: NgbModalRef;
  selectedDate: Date = new Date();
  selectedTime: Date | null = null;
  selectedDateTolerance: number = 1;
  currentMonth: string = '';
  weekDays: string[] = ['Mo', 'Tu', 'We', 'Th', 'Fr', 'Sa', 'Su'];
  calendarDays: CalendarDay[] = [];

  constructor(
    private modalService: NgbModal,
    private renderer: Renderer2
  ) {
    this.generateCalendar();
  }

  ngOnDestroy(): void {
    if (this.modalRef) {
      this.modalRef.close();
    }
  }

  // ============================================================================
  // PUBLIC METHODS - Called from template
  // ============================================================================

  openDatePicker(event: MouseEvent): void {
    event.stopPropagation();
    this.setupDatePicker();
    this.openModalAtPosition(event, this.datePickerModal, 'date-picker-modal', 380);
  }
  
  openTimePicker(event: MouseEvent): void {
    event.stopPropagation();
    this.setupTimePicker();
    this.openModalAtPosition(event, this.timePickerModal, 'time-picker-modal', 320);
  }

  // ============================================================================
  // CALENDAR EVENT HANDLERS
  // ============================================================================

  onCalendarDateSelected(data: { date: Date }): void {
    this.selectedDate = new Date(data.date);
    this.generateCalendar();
    
    // Update the displayed date immediately (but don't close modal yet)
    const formattedDate = this.formatDate(this.selectedDate);
    
    // Emit the date change immediately so parent component updates the display
    this.dateChanged.emit({
      date: formattedDate,
      tolerance: this.selectedDateTolerance,
      type: this.type
    });
  }

  onTimeSelected(event: { time: Date, tolerance: number }): void {
    this.selectedTime = event.time;
    const formattedTime = event.time ? this.formatTime(event.time) : 'Any time';
    
    // Emit the change
    this.timeChanged.emit({
      time: formattedTime,
      tolerance: event.tolerance,
      type: this.type
    });

    // Close modal
    if (this.modalRef) {
      this.modalRef.close();
    }
  }

  applyDateSelection(): void {
    const formattedDate = this.formatDate(this.selectedDate);
    // Emit the final change with tolerance
    this.dateChanged.emit({
      date: formattedDate,
      tolerance: this.selectedDateTolerance,
      type: this.type
    });

    // Close modal
    if (this.modalRef) {
      this.modalRef.close();
    }
  }

   onModalDismiss(): void {
  }

  // ============================================================================
  // PRIVATE METHODS - Modal Management
  // ============================================================================

  private setupDatePicker(): void {
    // Initialize date picker state
    this.selectedDateTolerance = this.daysTolerance;
    
    if (this.dateFormatted && this.dateFormatted !== this.defaultDateText) {
      try {
        this.selectedDate = new Date(this.dateFormatted);
      } catch (e) {
        this.selectedDate = new Date();
      }
    } else {
      this.selectedDate = new Date();
    }
    
    this.generateCalendar();
  }

  private setupTimePicker(): void {
    // Initialize time picker state
    if (this.timeFormatted && this.timeFormatted !== 'Any time') {
      this.selectedTime = this.parseTimeString(this.timeFormatted);
    } else {
      // Default to current time rounded to nearest 5 minutes
      const now = new Date();
      const minutes = Math.round(now.getMinutes() / 5) * 5;
      now.setMinutes(minutes, 0, 0);
      this.selectedTime = now;
    }
  }


private openModalAtPosition(
  event: MouseEvent, 
  template: TemplateRef<any>, 
  modalClass: string, 
  modalHeight: number
): void {
  // Close any existing modal
  if (this.modalRef) {
    this.modalRef.close();
  }

  const clickedElement = event.currentTarget as HTMLElement;
  const rect = clickedElement.getBoundingClientRect();
  
  // Create positioned container
  const containerDiv = document.createElement('div');
  containerDiv.className = 'positioned-modal-container';
  document.body.appendChild(containerDiv);
  
  // Style the container
  this.renderer.setStyle(containerDiv, 'position', 'fixed');
  this.renderer.setStyle(containerDiv, 'pointer-events', 'none');
  this.renderer.setStyle(containerDiv, 'top', '0');
  this.renderer.setStyle(containerDiv, 'left', '0');
  this.renderer.setStyle(containerDiv, 'width', '100%');
  this.renderer.setStyle(containerDiv, 'height', '100%');
  this.renderer.setStyle(containerDiv, 'z-index', '1050');

  // Calculate safe positioning with more conservative margins
  const windowHeight = window.innerHeight;
  const windowWidth = window.innerWidth;
  const spaceBelow = windowHeight - rect.bottom;
  const spaceAbove = rect.top;
  const safetyMargin = 30; // Increased margin for safety
  
  // Use a more conservative modal height estimate
  const estimatedModalHeight = modalHeight + 50; // Add buffer for actual content
  
  let position: 'above' | 'below' | 'center';
  let topPosition: number;
  let leftPosition: number = rect.left;
  
  // More conservative positioning logic
  if (spaceBelow >= estimatedModalHeight + safetyMargin) {
    // Definitely fits below
    position = 'below';
    topPosition = rect.bottom + 8;
  } else if (spaceAbove >= estimatedModalHeight + safetyMargin) {
    // Definitely fits above
    position = 'above';
    topPosition = Math.max(safetyMargin, rect.top - estimatedModalHeight - 8);
  } else {
    // Use center positioning for safety
    position = 'center';
    topPosition = Math.max(safetyMargin, (windowHeight - estimatedModalHeight) / 2);
    
    // If even centered it's too tall, position at top with margin
    if (topPosition + estimatedModalHeight > windowHeight - safetyMargin) {
      topPosition = safetyMargin;
    }
  }
  
  // Ensure horizontal positioning is safe
  const modalWidth = 350;
  if (leftPosition + modalWidth > windowWidth - safetyMargin) {
    leftPosition = windowWidth - modalWidth - safetyMargin;
  }
  if (leftPosition < safetyMargin) {
    leftPosition = safetyMargin;
  }
  
  // Calculate maximum safe height for modal
  const maxSafeHeight = windowHeight - topPosition - safetyMargin;
  const finalMaxHeight = Math.min(modalHeight + 100, maxSafeHeight);

  // Create positioning styles with guaranteed button visibility
  const styleEl = document.createElement('style');
  styleEl.innerHTML = `
    .${modalClass} .modal-content {
      opacity: 0 ;
      position: absolute ;
      top: ${topPosition}px ;
      left: ${leftPosition}px ;
      width: auto ;
      min-width: 300px ;
      max-width: 400px ;
      height: auto ;
      max-height: ${finalMaxHeight}px ;
      pointer-events: auto ;
      transition: opacity 0.15s ease-out ;
      border-radius: 8px ;
      box-shadow: 0 4px 20px rgba(0, 0, 0, 0.15) ;
      overflow: hidden ;
      display: flex ;
      flex-direction: column ;
    }
    
    .${modalClass} .modal-header {
      flex-shrink: 0 ;
      border-bottom: 1px solid #dee2e6 ;
    }
    
    .${modalClass} .modal-body {
      flex: 1 1 auto ;
      overflow-y: auto ;
      max-height: ${finalMaxHeight - 140}px ;
      min-height: 100px ;
    }
    
    .${modalClass} .modal-footer {
      flex-shrink: 0 ;
      border-top: 1px solid #dee2e6 ;
      padding: 12px 16px ;
      background-color: white ;
      border-bottom-left-radius: 8px ;
      border-bottom-right-radius: 8px ;
      display: flex ;
      justify-content: flex-end ;
      gap: 8px ;
      min-height: 60px ;
    }
    
    .${modalClass} .modal-footer button {
      min-height: 36px ;
      padding: 8px 16px ;
    }
    
    .modal-backdrop {
      opacity: 0 ;
      transition: opacity 0.15s ease-out ;
    }
    
    /* Emergency fallback for very small screens */
    @media (max-height: 500px) {
      .${modalClass} .modal-content {
        position: fixed ;
        top: 10px ;
        left: 50% ;
        transform: translateX(-50%) ;
        max-height: calc(100vh - 20px) ;
        width: 90vw ;
        max-width: 400px ;
      }
      
      .${modalClass} .modal-body {
        max-height: calc(100vh - 160px) ;
      }
    }
  `;
  document.head.appendChild(styleEl);
  
  // Open modal
  this.modalRef = this.modalService.open(template, {
    backdropClass: 'light-backdrop fade-in',
    animation: false,
    centered: false,
    windowClass: `${modalClass} no-animation`,
    container: containerDiv
  });
  
  // Animate in with post-render height check
  setTimeout(() => {
    document.head.removeChild(styleEl);
    
    const modalContent = document.querySelector(`.${modalClass} .modal-content`) as HTMLElement;
    if (modalContent) {
      // Apply initial positioning
      this.renderer.setStyle(modalContent, 'position', 'absolute');
      this.renderer.setStyle(modalContent, 'top', `${topPosition}px`);
      this.renderer.setStyle(modalContent, 'left', `${leftPosition}px`);
      this.renderer.setStyle(modalContent, 'width', 'auto');
      this.renderer.setStyle(modalContent, 'min-width', '300px');
      this.renderer.setStyle(modalContent, 'max-width', '400px');
      this.renderer.setStyle(modalContent, 'max-height', `${finalMaxHeight}px`);
      this.renderer.setStyle(modalContent, 'overflow', 'hidden');
      this.renderer.setStyle(modalContent, 'display', 'flex');
      this.renderer.setStyle(modalContent, 'flex-direction', 'column');
      this.renderer.setStyle(modalContent, 'pointer-events', 'auto');
      
      // Ensure proper flex layout
      const modalBody = modalContent.querySelector('.modal-body') as HTMLElement;
      if (modalBody) {
        this.renderer.setStyle(modalBody, 'flex', '1 1 auto');
        this.renderer.setStyle(modalBody, 'overflow-y', 'auto');
        this.renderer.setStyle(modalBody, 'max-height', `${finalMaxHeight - 140}px`);
      }
      
      const modalFooter = modalContent.querySelector('.modal-footer') as HTMLElement;
      if (modalFooter) {
        this.renderer.setStyle(modalFooter, 'flex-shrink', '0');
        this.renderer.setStyle(modalFooter, 'min-height', '60px');
      }
      
      // Post-render adjustment: Check actual height and reposition if needed
      setTimeout(() => {
        const actualHeight = modalContent.offsetHeight;
        const currentTop = parseInt(modalContent.style.top);
        
        // If modal extends beyond screen, adjust position
        if (currentTop + actualHeight > windowHeight - safetyMargin) {
          const adjustedTop = Math.max(safetyMargin, windowHeight - actualHeight - safetyMargin);
          this.renderer.setStyle(modalContent, 'top', `${adjustedTop}px`);
        }
        
        // Now show the modal
        this.renderer.setStyle(modalContent, 'opacity', '1');
      }, 20);
    }
    
    const backdrop = document.querySelector('.modal-backdrop') as HTMLElement;
    if (backdrop) {
      setTimeout(() => {
        this.renderer.setStyle(backdrop, 'opacity', '0.3');
      }, 10);
    }
  }, 0);
  
  // Cleanup on close
  this.modalRef.closed.subscribe(() => {
    if (document.body.contains(containerDiv)) {
      document.body.removeChild(containerDiv);
    }
  });
  
  this.modalRef.dismissed.subscribe(() => {
    if (document.body.contains(containerDiv)) {
      document.body.removeChild(containerDiv);
    }
  });
}
  // ============================================================================
  // UTILITY METHODS
  // ============================================================================

  private formatTime(time: Date): string {
    const hours = time.getHours();
    const minutes = time.getMinutes();
    const formattedHour = hours % 12 === 0 ? 12 : hours % 12;
    const amPm = hours < 12 ? 'AM' : 'PM';
    
    return `${formattedHour}:${String(minutes).padStart(2, '0')} ${amPm}`;
  }

  private formatDate(date: Date): string {
    const options: Intl.DateTimeFormatOptions = { 
      month: 'short', 
      day: 'numeric', 
      year: 'numeric' 
    };
    return date.toLocaleDateString('en-US', options);
  }
  
  private parseTimeString(timeStr: string): Date | null {
    if (timeStr === 'Any time') return null;
    
    try {
      const [timePart, period] = timeStr.split(' ');
      const [hourStr, minuteStr] = timePart.split(':');
      
      let hour = parseInt(hourStr, 10);
      const minute = parseInt(minuteStr, 10);
      
      if (period === 'PM' && hour < 12) {
        hour += 12;
      } else if (period === 'AM' && hour === 12) {
        hour = 0;
      }
      
      const date = new Date();
      date.setHours(hour, minute, 0, 0);
      return date;
    } catch (e) {
      console.error('Error parsing time string:', e);
      return null;
    }
  }

  // ============================================================================
  // CALENDAR METHODS
  // ============================================================================

  generateCalendar(): void {
    const currentDate = new Date();
    const selectedDate = this.selectedDate;
    const year = selectedDate.getFullYear();
    const month = selectedDate.getMonth();
    
    this.currentMonth = selectedDate.toLocaleString('default', { month: 'long', year: 'numeric' });
    
    const firstDay = new Date(year, month, 1);
    const lastDay = new Date(year, month + 1, 0);
    
    let firstDayOfWeek = firstDay.getDay();
    firstDayOfWeek = firstDayOfWeek === 0 ? 6 : firstDayOfWeek - 1;
    
    this.calendarDays = [];
    
    // Previous month days
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
    
    // Current month days
    for (let i = 1; i <= lastDay.getDate(); i++) {
      const day = new Date(year, month, i);
      this.calendarDays.push({
        date: day,
        currentMonth: true,
        isToday: this.isSameDay(day, currentDate),
        isSelected: this.isSameDay(day, selectedDate)
      });
    }
    
    // Next month days
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

  private isSameDay(date1: Date, date2: Date): boolean {
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

  decreaseTolerance(): void {
    if (this.selectedDateTolerance > 0) {
      this.selectedDateTolerance--;
    }
  }

  increaseTolerance(): void {
    if (this.selectedDateTolerance < 14) {
      this.selectedDateTolerance++;
    }
  }

  goToToday(): void {
    const today = new Date();
    this.selectedDate = new Date(today);
    this.generateCalendar();
  }
}