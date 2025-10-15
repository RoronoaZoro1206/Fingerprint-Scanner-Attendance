# Student Attendance Export Functionality Update

## Overview
Added Print and Export buttons to the Student Attendance page with the same style, functions, and format as the Faculty Reports page.

## Changes Made

### 1. View (StudentAttendance.cshtml)

#### Added Header Actions
```html
<div class="page-header">
    <h1 class="page-title">My Attendance Record</h1>
    <div class="header-actions">
        <button type="button" class="btn btn-outline btn-print" id="btnPrint">
            <i data-lucide="printer" class="icon-sm"></i>
            Print
        </button>
        <div class="dropdown">
            <button class="btn btn-outline dropdown-toggle" type="button" id="exportDropdown">
                <i data-lucide="download" class="icon-sm"></i>
                Export
            </button>
            <ul class="dropdown-menu">
                <li><a class="dropdown-item" href="#" id="btnExportExcel">Excel</a></li>
                <li><a class="dropdown-item" href="#" id="btnExportCSV">CSV</a></li>
                <li><a class="dropdown-item" href="#" id="btnExportPDF">PDF</a></li>
            </ul>
        </div>
    </div>
</div>
```

### 2. JavaScript (student_attendance.js)

#### Added Event Handlers
```javascript
$('#btnPrint').on('click', printAttendance);
$('#btnExportExcel').on('click', exportToExcel);
$('#btnExportCSV').on('click', exportToCSV);
$('#btnExportPDF').on('click', exportToPDF);
```

#### Export Functions

**getAttendanceData()**
- Extracts data from DataTables
- Formats for export (Date, Course, Status, Time In, Time Out)
- Respects current search/filter

**printAttendance()**
- Opens new window with formatted table
- Includes styled status badges
- Auto-triggers print dialog
- Shows generation timestamp

**exportToCSV()**
- Creates CSV file with proper escaping
- Downloads as `my_attendance_[timestamp].csv`
- Shows success toast notification

**exportToExcel()**
- Creates Excel-compatible HTML table
- Downloads as `my_attendance_[timestamp].xls`
- Shows success toast notification

**exportToPDF()**
- Redirects to print function (placeholder)
- Shows info toast about using print

### 3. CSS (student_attendance.css)

#### Header Styles
```css
.page-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 24px;
}

.page-title {
    font-size: 24px;
    font-weight: 600;
}

.header-actions {
    display: flex;
    align-items: center;
    gap: 12px;
}
```

#### Button Styles
```css
.btn-outline {
    display: flex;
    align-items: center;
    gap: 8px;
    padding: 10px 20px;
    font-weight: 500;
    border-radius: 8px;
    background-color: #fff;
    border: 1px solid #d1d5db;
    color: #374151;
    transition: all 0.2s;
}

.btn-outline:hover {
    background-color: #f3f4f6;
    border-color: #9ca3af;
}
```

#### Dropdown Styles
```css
.dropdown-menu {
    border-radius: 8px;
    border: 1px solid #e5e7eb;
    box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.1);
    padding: 8px;
    min-width: 160px;
}

.dropdown-item {
    display: flex;
    align-items: center;
    gap: 10px;
    padding: 10px 12px;
    border-radius: 6px;
    transition: all 0.2s;
}
```

#### Print Styles
```css
@media print {
    .page-header .header-actions,
    .dataTables_length,
    .dataTables_filter,
    .dataTables_info,
    .dataTables_paginate {
        display: none !important;
    }
}
```

#### Responsive Design
- **Tablet (768px)**: Header stacks, buttons flex to fill width
- **Mobile (576px)**: Buttons stack vertically, full width

## Features

### Print
- Opens in new window
- Formatted table with styled status badges
- Includes generation timestamp
- Auto-triggers print dialog
- Print-optimized styles (hides buttons, pagination)

### Export CSV
- Proper CSV formatting with quotes
- Handles special characters
- Filename: `my_attendance_[timestamp].csv`
- Success notification

### Export Excel
- HTML table format compatible with Excel
- Preserves formatting
- Filename: `my_attendance_[timestamp].xls`
- Success notification

### Export PDF
- Currently redirects to print function
- Shows informational message
- Can be enhanced with PDF library later

## Data Format

### Exported Columns
1. **Date**: YYYY-MM-DD format
2. **Course**: Course code
3. **Status**: Present/Late/Absent/Excused/Pending
4. **Time In**: 12-hour format with AM/PM
5. **Time Out**: 12-hour format with AM/PM

### Example CSV Output
```csv
Date,Course,Status,Time In,Time Out
2024-01-15,CS101,Present,08:30:00 AM,10:30:00 AM
2024-01-14,CS102,Late,09:15:00 AM,11:15:00 AM
```

### Example Print Output
```
My Attendance Record
Generated on 1/15/2024, 2:30:45 PM

┌────────────┬────────┬─────────┬──────────────┬──────────────┐
│ Date       │ Course │ Status  │ Time In      │ Time Out     │
├────────────┼────────┼─────────┼──────────────┼──────────────┤
│ 2024-01-15 │ CS101  │ Present │ 08:30:00 AM  │ 10:30:00 AM  │
│ 2024-01-14 │ CS102  │ Late    │ 09:15:00 AM  │ 11:15:00 AM  │
└────────────┴────────┴─────────┴──────────────┴──────────────┘
```

## Button Layout

**Desktop:**
```
[My Attendance Record]          [Print] [Export ▼]
```

**Tablet:**
```
[My Attendance Record]
[Print] [Export ▼]
```

**Mobile:**
```
[My Attendance Record]
[Print]
[Export ▼]
```

## Files Modified
1. `WebApplication1/Views/Student/StudentAttendance.cshtml`
2. `WebApplication1/wwwroot/js/student/student_attendance.js`
3. `WebApplication1/wwwroot/css/student/student_attendance.css`

## Benefits
- Students can now print their attendance records
- Export to CSV for spreadsheet analysis
- Export to Excel for further processing
- Consistent UI with Faculty Reports page
- Professional appearance matching admin pages
- Mobile-responsive design
- Respects DataTables search/filter

## Notes
- Export functions respect current DataTables search/filter
- CSV export properly escapes special characters
- Print function opens in new window to preserve current page state
- All exports include timestamp in filename
- Toast notifications provide user feedback
- PDF export is a placeholder that uses print function
