# ?? Dashboard Charts Implementation - Complete Guide

## Overview
This document provides a comprehensive guide to the newly implemented dashboard charts using Chart.js across all three user roles: Admin, Faculty, and Student.

---

## ?? Features Implemented

### 1. **Admin Dashboard** (`/Admin/Index`)

#### Charts:
1. **User Role Distribution** (Doughnut Chart)
   - **Purpose**: Visual breakdown of system users by role
   - **Data**: Count of Admin, Faculty, and Student users
- **Colors**: 
     - Admin: Indigo (`rgba(99, 102, 241, 0.8)`)
  - Faculty: Green (`rgba(34, 197, 94, 0.8)`)
     - Student: Yellow (`rgba(234, 179, 8, 0.8)`)

2. **Daily Attendance Trend** (Line Chart)
   - **Purpose**: Track attendance patterns over time
   - **Data**: Total attendance records per day for last 30 days
   - **X-Axis**: Dates (MMM dd format)
   - **Y-Axis**: Number of attendance records

3. **Fingerprint Registration Status** (Bar Chart)
   - **Purpose**: Monitor biometric registration progress
   - **Data**: Users with registered vs. not registered fingerprints
   - **Colors**:
- Registered: Green (`rgba(34, 197, 94, 0.8)`)
     - Not Registered: Red (`rgba(239, 68, 68, 0.8)`)

#### API Endpoints:
```
GET /api/admin/chart/user-roles
GET /api/admin/chart/attendance-trend?days=30
GET /api/admin/chart/fingerprint-status
```

---

### 2. **Faculty Dashboard** (`/Faculty/Index`)

#### Charts:
1. **Today's Attendance Status** (Doughnut Chart)
   - **Purpose**: Real-time snapshot of today's attendance
   - **Data**: Present, Absent, and Late counts for current day
   - **Scope**: All classes taught by the faculty member
   - **Colors**:
     - Present: Green (`rgba(34, 197, 94, 0.8)`)
  - Absent: Red (`rgba(239, 68, 68, 0.8)`)
     - Late: Yellow (`rgba(234, 179, 8, 0.8)`)

2. **Weekly Attendance Trend** (Multi-line Chart)
   - **Purpose**: Track attendance patterns over the past week
   - **Data**: Present, Late, and Absent counts for last 7 days
   - **X-Axis**: Dates (MMM dd format)
   - **Y-Axis**: Student count
   - **Lines**: Three separate lines for each status

3. **Student Punctuality Analysis** (Bar Chart)
   - **Purpose**: Analyze student punctuality
   - **Data**: On-time vs. Late arrivals for last 7 days
   - **Colors**:
     - On Time: Green (`rgba(34, 197, 94, 0.8)`)
     - Late: Yellow (`rgba(234, 179, 8, 0.8)`)

#### API Endpoints:
```
GET /api/faculty/chart/today-attendance
GET /api/faculty/chart/weekly-attendance
GET /api/faculty/chart/punctuality
```

---

### 3. **Student Dashboard** (`/Student/Index`)

#### Charts:
1. **Personal Attendance Overview** (Doughnut Chart)
   - **Purpose**: Semester-long attendance summary
   - **Data**: Total Present, Absent, and Late sessions
   - **Colors**:
     - Present: Green (`rgba(34, 197, 94, 0.8)`)
   - Absent: Red (`rgba(239, 68, 68, 0.8)`)
     - Late: Yellow (`rgba(234, 179, 8, 0.8)`)

2. **Punctuality Trend** (Multi-line Chart)
   - **Purpose**: Track personal attendance behavior
   - **Data**: Daily attendance status for last 14 days
 - **X-Axis**: Dates (MMM dd format)
   - **Y-Axis**: Session count
 - **Lines**: On Time, Late, Absent

3. **Course-wise Attendance Rate** (Bar Chart)
   - **Purpose**: Compare attendance across enrolled courses
   - **Data**: Attendance percentage per course
   - **Y-Axis**: Percentage (0-100%)
   - **Color Coding**:
     - 90-100%: Green (excellent)
     - 75-89%: Yellow (good)
     - Below 75%: Red (needs improvement)

#### API Endpoints:
```
GET /api/student/chart/attendance-overview
GET /api/student/chart/punctuality-trend
GET /api/student/chart/course-attendance
```

---

## ??? File Structure

### Backend (C#)
```
WebApplication1/
??? Controllers/
?   ??? Admin/
?   ?   ??? AdminDashboardApiController.cs (updated)
?   ??? Faculty/
?   ?   ??? FacultyDashboardApiController.cs (new)
?   ??? Student/
?       ??? StudentDashboardApiController.cs (new)
```

### Frontend (JavaScript)
```
WebApplication1/wwwroot/
??? js/
?   ??? admin/
?   ?   ??? admin_charts.js (new)
?   ??? faculty/
?   ?   ??? faculty_charts.js (new)
?   ??? student/
?       ??? student_charts.js (new)
```

### Views (Razor)
```
WebApplication1/Views/
??? Admin/
?   ??? Index.cshtml (updated)
??? Faculty/
?   ??? Index.cshtml (updated)
??? Student/
    ??? Index.cshtml (updated)
```

### Styles (CSS)
```
WebApplication1/wwwroot/css/
??? dashboard_charts.css (new)
```

---

## ?? Technical Implementation

### Database Queries

#### Admin Charts:
```sql
-- User Role Distribution
SELECT ur.role_name, COUNT(u.user_id) as count
FROM user_roles ur
LEFT JOIN users u ON ur.role_id = u.role_id
WHERE ur.role_name IN ('admin', 'faculty', 'student')
GROUP BY ur.role_name
ORDER BY ur.role_id;

-- Attendance Trend (Last 30 Days)
SELECT attend_date, COUNT(*) as count
FROM attendance
WHERE attend_date >= CURRENT_DATE - INTERVAL '30 days'
GROUP BY attend_date
ORDER BY attend_date;

-- Fingerprint Registration Status
SELECT 
  COUNT(CASE WHEN ft.ftemp_id IS NOT NULL THEN 1 END) as registered,
  COUNT(CASE WHEN ft.ftemp_id IS NULL THEN 1 END) as not_registered
FROM users u
LEFT JOIN fingerprint_templates ft ON u.user_id = ft.user_id;
```

#### Faculty Charts:
```sql
-- Today's Attendance
SELECT 
  COUNT(CASE WHEN a.attend_status = 'present' THEN 1 END) as present_count,
  COUNT(CASE WHEN a.attend_status = 'absent' THEN 1 END) as absent_count,
  COUNT(CASE WHEN a.attend_status = 'late' THEN 1 END) as late_count
FROM attendance a
INNER JOIN classes c ON a.class_id = c.class_id
WHERE c.fac_id = @facId 
  AND a.attend_date = CURRENT_DATE 
  AND a.attend_type = 'in';

-- Weekly Attendance
SELECT 
  a.attend_date,
  COUNT(CASE WHEN a.attend_status = 'present' THEN 1 END) as present,
  COUNT(CASE WHEN a.attend_status = 'late' THEN 1 END) as late,
  COUNT(CASE WHEN a.attend_status = 'absent' THEN 1 END) as absent
FROM attendance a
INNER JOIN classes c ON a.class_id = c.class_id
WHERE c.fac_id = @facId 
  AND a.attend_date >= CURRENT_DATE - INTERVAL '7 days'
  AND a.attend_type = 'in'
GROUP BY a.attend_date
ORDER BY a.attend_date;
```

#### Student Charts:
```sql
-- Attendance Overview
SELECT 
  COUNT(CASE WHEN attend_status = 'present' THEN 1 END) as present_count,
  COUNT(CASE WHEN attend_status = 'absent' THEN 1 END) as absent_count,
  COUNT(CASE WHEN attend_status = 'late' THEN 1 END) as late_count
FROM attendance
WHERE stud_id = @studId AND attend_type = 'in';

-- Course-wise Attendance
SELECT 
  co.course_code,
  COUNT(CASE WHEN a.attend_status IN ('present', 'late') THEN 1 END) as attended,
  COUNT(*) as total_sessions,
  CASE 
    WHEN COUNT(*) > 0 
    THEN ROUND((COUNT(CASE WHEN a.attend_status IN ('present', 'late') THEN 1 END)::DECIMAL / COUNT(*)) * 100)
    ELSE 0
  END as attendance_rate
FROM enrollment e
INNER JOIN classes c ON e.class_id = c.class_id
INNER JOIN courses co ON c.course_id = co.course_id
LEFT JOIN attendance a ON e.class_id = a.class_id AND e.stud_id = a.stud_id AND a.attend_type = 'in'
WHERE e.stud_id = @studId
GROUP BY co.course_code
ORDER BY co.course_code;
```

---

## ?? Dependencies

### Chart.js
- **Version**: 4.4.1
- **CDN**: `https://cdn.jsdelivr.net/npm/chart.js@4.4.1/dist/chart.umd.min.js`
- **License**: MIT

### Integration in Views:
```razor
@section Scripts {
    <script src="https://cdn.jsdelivr.net/npm/chart.js@4.4.1/dist/chart.umd.min.js"></script>
 <script src="~/js/[role]/[role]_charts.js" asp-append-version="true"></script>
}
```

---

## ?? Auto-Refresh

All charts implement automatic data refresh:
- **Refresh Interval**: 5 minutes (300,000 ms)
- **Implementation**:
```javascript
setInterval(loadChart, 300000);
```

---

## ?? Chart Configuration

### Common Options:
```javascript
{
  responsive: true,
  maintainAspectRatio: false,
  plugins: {
    legend: {
      position: 'bottom',
labels: {
        padding: 15,
        font: { size: 12 }
    }
    },
    title: {
      display: true,
      text: 'Chart Title',
      font: { size: 16, weight: 'bold' },
      padding: { bottom: 20 }
 },
    tooltip: {
      backgroundColor: 'rgba(0, 0, 0, 0.8)',
      padding: 12
    }
  }
}
```

---

## ?? Security & Authorization

All API endpoints are protected with:
```csharp
[RoleAuthorize(USER_ROLES.ADMIN)]  // or FACULTY, STUDENT
```

Session-based authentication:
```csharp
var userId = HttpContext.Session.GetInt32("UserId");
if (!userId.HasValue)
    return Unauthorized(new { error = "User not authenticated" });
```

---

## ?? Responsive Design

Charts adapt to different screen sizes:
```css
@media (max-width: 768px) {
    .content-grid {
        grid-template-columns: 1fr;
    }
    .chart-container {
        height: 250px !important;
  }
}

@media (max-width: 480px) {
    .chart-container {
        height: 200px !important;
    }
}
```

---

## ?? Testing Checklist

### Admin Dashboard:
- [ ] User role distribution shows correct counts
- [ ] Attendance trend displays data for last 30 days
- [ ] Fingerprint status shows registered vs. unregistered users
- [ ] Charts refresh automatically every 5 minutes
- [ ] Charts are responsive on mobile devices

### Faculty Dashboard:
- [ ] Today's attendance shows current day's data only
- [ ] Weekly trend displays last 7 days
- [ ] Punctuality chart shows on-time vs. late breakdown
- [ ] Charts load only for authenticated faculty
- [ ] Charts update when faculty navigates between pages

### Student Dashboard:
- [ ] Attendance overview shows semester-long data
- [ ] Punctuality trend displays last 14 days
- [ ] Course-wise attendance shows all enrolled courses
- [ ] Percentage colors change based on attendance rate
- [ ] Charts display "No data" message when appropriate

---

## ?? Troubleshooting

### Chart Not Loading:
1. Check browser console for errors
2. Verify Chart.js CDN is loaded
3. Confirm API endpoint returns valid JSON
4. Check user session authentication

### Empty Charts:
1. Verify database has relevant data
2. Check date range filters in queries
3. Confirm user has associated records (classes, enrollments)

### Styling Issues:
1. Clear browser cache
2. Verify CSS file is included
3. Check grid layout on different screen sizes

---

## ?? Performance Optimization

### Backend:
- **Single query per chart** (no N+1 queries)
- **Database indexes** on frequently queried columns
- **Optimized JOINs** for related data

### Frontend:
- **Chart instance caching** (destroy before recreate)
- **Debounced refresh** (5-minute intervals)
- **Lazy loading** (charts load after page content)

---

## ?? Future Enhancements

1. **Export Charts** - Add PDF/PNG download functionality
2. **Date Range Selector** - Allow users to customize time periods
3. **Real-time Updates** - Implement WebSocket for live data
4. **Drill-down** - Click chart elements for detailed views
5. **Comparative Analysis** - Compare data across semesters/years
6. **Custom Themes** - Allow users to choose color schemes

---

## ?? References

- [Chart.js Documentation](https://www.chartjs.org/docs/latest/)
- [ASP.NET Core Web APIs](https://learn.microsoft.com/en-us/aspnet/core/web-api/)
- [PostgreSQL Date/Time Functions](https://www.postgresql.org/docs/current/functions-datetime.html)

---

## ? Implementation Status

| Feature | Admin | Faculty | Student | Status |
|---------|-------|---------|---------|--------|
| Backend API | ? | ? | ? | Complete |
| Frontend JS | ? | ? | ? | Complete |
| View Integration | ? | ? | ? | Complete |
| Styling | ? | ? | ? | Complete |
| Auto-refresh | ? | ? | ? | Complete |
| Responsive Design | ? | ? | ? | Complete |
| Error Handling | ? | ? | ? | Complete |
| Documentation | ? | ? | ? | Complete |

---

**Implementation Date**: 2025-01-30  
**Chart.js Version**: 4.4.1  
**.NET Version**: 9.0  
**Database**: PostgreSQL
