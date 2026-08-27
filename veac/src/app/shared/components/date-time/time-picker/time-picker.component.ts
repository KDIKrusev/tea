import { Component, Input, Output, EventEmitter, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

interface TimeOption {
  hour: number;
  minute: number;
  display: string;
}

@Component({
  selector: 'app-time-picker',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './time-picker.component.html',
  styleUrls: ['./time-picker.component.css']
})
export class TimePickerComponent implements OnInit {
  @Input() selectedTime: Date | null = null;
  @Input() tolerance: number = 0;
  @Input() modalRef: any; // Reference to the modal for closing
  @Input() use24HourFormat: boolean = true; // 24-hour format by default

  @Output() timeSelected = new EventEmitter<{ time: Date, tolerance: number }>();
  @Output() modalDismiss = new EventEmitter<void>();

  timeOptions: TimeOption[] = [];
  filteredTimeOptions: TimeOption[] = [];
  
  minuteOptions: number[] = [];
  
  toleranceOptions: number[] = [0, 1, 2,3,4,5,6,7 ,8,9, 10,11,12,13,14,15,16,17,18,19,20,21,22,23,24];
  
  searchText: string = '';
  
  showToleranceDropdown: boolean = false;
  
  ngOnInit(): void {
    // Generate minute options in 5-minute intervals
    for (let min = 0; min < 60; min += 5) {
      this.minuteOptions.push(min);
    }
    
    this.generateTimeOptions();
    
    // Initialize with selected time if provided
    if (this.selectedTime) {
      // Round to nearest 5 minutes
      const minutes = this.selectedTime.getMinutes();
      const roundedMinutes = Math.round(minutes / 5) * 5;
      if (roundedMinutes === 60) {
        this.selectedTime.setHours(this.selectedTime.getHours() + 1, 0, 0, 0);
      } else {
        this.selectedTime.setMinutes(roundedMinutes, 0, 0);
      }
    }
    
    // Make sure tolerance is one of the valid options
    if (!this.toleranceOptions.includes(this.tolerance)) {
      this.tolerance = 0; // Default to 0 if not in valid range
    }

    this.filterTimeOptions();
  }

  generateTimeOptions(): void {
    // Generate all time options with hour and minute combinations
    this.timeOptions = [];
    
    for (let hour = 0; hour < 24; hour++) {
      for (const minute of this.minuteOptions) {
        let display: string;
        
        if (this.use24HourFormat) {
          // 24-hour format: "14:05"
          display = `${String(hour).padStart(2, '0')}:${String(minute).padStart(2, '0')}`;
        } else {
          // 12-hour format: "2:05 PM"
          const formattedHour = hour % 12 === 0 ? 12 : hour % 12;
          const amPm = hour < 12 ? 'AM' : 'PM';
          display = `${formattedHour}:${String(minute).padStart(2, '0')} ${amPm}`;
        }
        
        this.timeOptions.push({
          hour,
          minute,
          display
        });
      }
    }
    
    this.filterTimeOptions();
  }
  
  filterTimeOptions(searchText?: string): void {
    this.searchText = searchText || this.searchText;
    
    if (this.searchText) {
      // Filter based on search text
      this.filteredTimeOptions = this.timeOptions.filter(time => 
        time.display.toLowerCase().includes(this.searchText.toLowerCase())
      );
    } else {
      // No search, show all options
      this.filteredTimeOptions = [...this.timeOptions];
    }
  }
  
  selectTime(time: TimeOption): void {
    // Create a new date with the selected time
    const date = new Date();
    date.setHours(time.hour, time.minute, 0, 0);
    this.selectedTime = date;
  }
  
  selectAnyTime(): void {
    this.selectedTime = null;
  }
  
  formatTime(date: Date | null): string {
    if (!date) return 'Any time';
    
    const hours = date.getHours();
    const minutes = date.getMinutes();
    
    if (this.use24HourFormat) {
      // 24-hour format: "14:05"
      return `${String(hours).padStart(2, '0')}:${String(minutes).padStart(2, '0')}`;
    } else {
      // 12-hour format: "2:05 PM"
      const formattedHour = hours % 12 === 0 ? 12 : hours % 12;
      const amPm = hours < 12 ? 'AM' : 'PM';
      return `${formattedHour}:${String(minutes).padStart(2, '0')} ${amPm}`;
    }
  }
  
  toggleToleranceDropdown(): void {
    this.showToleranceDropdown = !this.showToleranceDropdown;
  }
  
  selectTolerance(tolerance: number): void {
    this.tolerance = tolerance;
    this.showToleranceDropdown = false;
  }

  closeToleranceDropdown(): void {
    this.showToleranceDropdown = false;
  }

  onSearchChange(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.filterTimeOptions(input.value);
  }

  applySelection(): void {
    if (this.selectedTime) {
      this.timeSelected.emit({
        time: this.selectedTime,
        tolerance: this.tolerance
      });
    } else {
      // Emit null for "Any time" with the tolerance
      this.timeSelected.emit({
        time: null as any, // For compatibility with existing code
        tolerance: this.tolerance
      });
    }
    
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
  
  isSelectedTime(time: TimeOption): boolean {
    if (!this.selectedTime) return false;
    
    return this.selectedTime.getHours() === time.hour && 
           this.selectedTime.getMinutes() === time.minute;
  }
}