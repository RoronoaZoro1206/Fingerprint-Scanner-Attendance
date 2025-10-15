# Navbar Academic Term Display

## Overview
Added active academic term display to the main navbar on the right side, showing the current academic term for all users.

## Changes Made

### 1. New Controller (SharedDataController.cs)

Created a new controller to provide shared data across the application.

**Endpoint:** `GET /SharedData/GetActiveAcademicTerm`

**Response:**
```json
{
  "success": true,
  "data": {
    "termId": 1,
    "termName": "1st Semester AY 2025-2026",
    "startDate": "2025-08-01",
    "endDate": "2025-12-31"
  }
}
```

**Features:**
- Queries IAcademicTermRepository for all terms
- Filters for the active term (IsActive = true)
- Returns formatted term data
- Handles cases where no active term exists

### 2. Updated Navbar Partial (_MainNavbarPartial.cshtml)

#### Added Right Section
```html
<div class="navbar-right">
    <div class="academic-term-badge" id="academicTermBadge">
        <i data-lucide="calendar" class="term-icon"></i>
        <span class="term-text" id="academicTermText">Loading...</span>
    </div>
</div>
```

#### Added JavaScript
- Loads active academic term on page load
- Updates badge text with term name
- Handles error states (no active term, API failure)
- Reinitializes Lucide icons after content update

**States:**
- **Loading**: "Loading..."
- **Active Term**: Displays term name (e.g., "1st Semester AY 2025-2026")
- **No Active Term**: "No Active Term" with inactive styling
- **Error**: "Term Unavailable" with inactive styling

### 3. CSS Styling (main-layout.css)

#### Navbar Right Section
```css
.navbar-right {
    display: flex;
    align-items: center;
    gap: 1rem;
}
```

#### Academic Term Badge
```css
.academic-term-badge {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    padding: 0.5rem 1rem;
    background-color: #f0f9ff;  /* Light blue */
    border: 1px solid #bae6fd;
    border-radius: 8px;
    font-size: 0.875rem;
    font-weight: 500;
    color: #0369a1;  /* Dark blue */
    transition: all 0.2s ease;
}
```

#### Inactive State
```css
.academic-term-badge.inactive {
    background-color: #f3f4f6;  /* Gray */
    border-color: #d1d5db;
    color: #6b7280;
}
```

#### Icon Styling
```css
.academic-term-badge .term-icon {
    width: 16px;
    height: 16px;
    flex-shrink: 0;
}
```

#### Responsive Design
```css
@media (max-width: 768px) {
    .academic-term-badge {
        display: none;  /* Hidden on mobile */
    }
}
```

## Visual Design

### Active Term Badge
```
┌─────────────────────────────────────┐
│ 📅 1st Semester AY 2025-2026       │
└─────────────────────────────────────┘
```
- Light blue background (#f0f9ff)
- Blue border (#bae6fd)
- Dark blue text (#0369a1)
- Calendar icon (Lucide)

### Inactive/Error Badge
```
┌─────────────────────────────────────┐
│ 📅 No Active Term                  │
└─────────────────────────────────────┘
```
- Gray background (#f3f4f6)
- Gray border (#d1d5db)
- Gray text (#6b7280)

## Navbar Layout

**Desktop:**
```
┌────────────────────────────────────────────────────────────┐
│ [☰] School Attendance System    📅 1st Semester AY 2025-26│
└────────────────────────────────────────────────────────────┘
```

**Mobile:**
```
┌────────────────────────────────────┐
│ [☰] School Attendance System      │
└────────────────────────────────────┘
```
(Term badge hidden on mobile to save space)

## Features

### Automatic Loading
- Loads on every page load
- No manual refresh needed
- Updates immediately when page is accessed

### Error Handling
- Gracefully handles API failures
- Shows appropriate message for no active term
- Doesn't break page functionality if API fails

### Visual Feedback
- Different colors for active vs inactive states
- Calendar icon for visual recognition
- Smooth transitions

### Responsive
- Visible on desktop and tablet
- Hidden on mobile devices (< 768px)
- Maintains navbar layout integrity

## Use Cases

1. **Students**: See which semester they're currently enrolled in
2. **Faculty**: Know which term they're teaching
3. **Admins**: Verify the active academic term at a glance
4. **All Users**: Quick reference without navigating to settings

## Technical Details

### Dependencies
- jQuery (for AJAX call)
- Lucide Icons (for calendar icon)
- IAcademicTermRepository (for data access)

### Performance
- Single AJAX call per page load
- Lightweight response (< 1KB)
- No polling or continuous updates

### Accessibility
- Semantic HTML structure
- Clear text labels
- Icon with text (not icon-only)

## Files Modified/Created
1. **Created**: `WebApplication1/Controllers/SharedDataController.cs`
2. **Modified**: `WebApplication1/Views/Shared/_MainNavbarPartial.cshtml`
3. **Modified**: `WebApplication1/wwwroot/css/main-layout.css`

## Future Enhancements
- Click to view term details
- Dropdown to switch between terms (for admins)
- Show term start/end dates on hover
- Countdown to term end
- Term progress indicator
