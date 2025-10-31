# ?? Dashboard Charts - Quick Reference

## ?? Chart Types by Role

### Admin Dashboard
| Chart | Type | Endpoint |
|-------|------|----------|
| User Role Distribution | Doughnut | `/api/admin/chart/user-roles` |
| Daily Attendance Trend | Line | `/api/admin/chart/attendance-trend?days=30` |
| Fingerprint Status | Bar | `/api/admin/chart/fingerprint-status` |

### Faculty Dashboard
| Chart | Type | Endpoint |
|-------|------|----------|
| Today's Attendance | Doughnut | `/api/faculty/chart/today-attendance` |
| Weekly Attendance | Multi-Line | `/api/faculty/chart/weekly-attendance` |
| Punctuality Analysis | Bar | `/api/faculty/chart/punctuality` |

### Student Dashboard
| Chart | Type | Endpoint |
|-------|------|----------|
| Attendance Overview | Doughnut | `/api/student/chart/attendance-overview` |
| Punctuality Trend | Multi-Line | `/api/student/chart/punctuality-trend` |
| Course Attendance | Bar | `/api/student/chart/course-attendance` |

---

## ?? Color Scheme

### Status Colors:
```javascript
Present/On Time/Registered: 'rgba(34, 197, 94, 0.8)'   // Green
Late/Warning:    'rgba(234, 179, 8, 0.8)'    // Yellow
Absent/Not Registered:     'rgba(239, 68, 68, 0.8)'    // Red
```

### Role Colors:
```javascript
Admin:   'rgba(99, 102, 241, 0.8)'   // Indigo
Faculty: 'rgba(34, 197, 94, 0.8)'    // Green
Student: 'rgba(234, 179, 8, 0.8)'    // Yellow
```

---

## ?? Auto-Refresh Settings

All charts refresh automatically:
```javascript
setInterval(loadChart, 300000);  // 5 minutes
```

---

## ?? Responsive Breakpoints

```css
@media (max-width: 768px) {
    /* Tablet: Single column, 250px height */
}

@media (max-width: 480px) {
    /* Mobile: Single column, 200px height */
}
```

---

## ??? Adding a New Chart

### 1. Backend (C#)
```csharp
[HttpGet("chart/new-chart")]
public async Task<IActionResult> GetNewChart()
{
    // Query database
    var query = "SELECT ...";
    var dt = await _db.ExecuteQueryAsync(query);
    
    // Return JSON
    return Ok(new { labels = ..., data = ... });
}
```

### 2. Frontend (JS)
```javascript
async function loadNewChart() {
    const response = await fetch('/api/role/chart/new-chart');
    const data = await response.json();
    
    new Chart(ctx, {
        type: 'line',  // or 'bar', 'doughnut'
        data: { labels: data.labels, datasets: [...] },
        options: { ... }
    });
}
```

### 3. View (Razor)
```razor
<div class="chart-card">
    <div class="chart-container" style="height: 300px;">
        <canvas id="newChart"></canvas>
    </div>
</div>
```

---

## ?? Quick Debugging

### Chart Not Showing:
1. Check browser console: `F12 ? Console`
2. Verify endpoint: `/api/role/chart/...`
3. Test API: Use browser or Postman

### Empty Data:
```sql
-- Verify data exists
SELECT COUNT(*) FROM attendance WHERE attend_date >= CURRENT_DATE - INTERVAL '7 days';
```

### Authentication Error:
```javascript
// Check session
console.log('UserId:', HttpContext.Session.GetInt32("UserId"));
```

---

## ?? Chart.js Cheat Sheet

### Common Chart Types:
```javascript
// Line Chart
{ type: 'line', fill: true, tension: 0.4 }

// Bar Chart
{ type: 'bar', borderRadius: 6 }

// Doughnut Chart
{ type: 'doughnut', cutout: '50%' }
```

### Common Options:
```javascript
options: {
  responsive: true,
  maintainAspectRatio: false,
  plugins: {
    legend: { position: 'bottom' },
    tooltip: { backgroundColor: 'rgba(0,0,0,0.8)' }
  },
  scales: {
    y: { beginAtZero: true }
  }
}
```

---

## ?? Security Checklist

- [x] API endpoints protected with `[RoleAuthorize]`
- [x] Session validation in controllers
- [x] SQL injection prevention (parameterized queries)
- [x] XSS prevention (HTML escaping)

---

## ?? Required Files

### Admin:
- `Controllers/Admin/AdminDashboardApiController.cs`
- `wwwroot/js/admin/admin_charts.js`
- `Views/Admin/Index.cshtml`

### Faculty:
- `Controllers/Faculty/FacultyDashboardApiController.cs`
- `wwwroot/js/faculty/faculty_charts.js`
- `Views/Faculty/Index.cshtml`

### Student:
- `Controllers/Student/StudentDashboardApiController.cs`
- `wwwroot/js/student/student_charts.js`
- `Views/Student/Index.cshtml`

### Shared:
- `wwwroot/css/dashboard_charts.css`
- Chart.js CDN (included in views)

---

## ?? Testing Commands

### Build Project:
```bash
dotnet build
```

### Run Project:
```bash
dotnet run
```

### Test Endpoints:
```bash
# Admin
curl -X GET "https://localhost:5001/api/admin/chart/user-roles"

# Faculty
curl -X GET "https://localhost:5001/api/faculty/chart/today-attendance"

# Student
curl -X GET "https://localhost:5001/api/student/chart/attendance-overview"
```

---

## ?? Quick Fixes

### Clear Chart Instance:
```javascript
if (chartVariable) {
    chartVariable.destroy();
}
```

### Force Refresh:
```javascript
loadChart();  // Manual refresh
```

### Reset Auto-refresh:
```javascript
clearInterval(intervalId);
intervalId = setInterval(loadChart, 300000);
```

---

**Last Updated**: 2025-01-30  
**Chart.js Version**: 4.4.1
