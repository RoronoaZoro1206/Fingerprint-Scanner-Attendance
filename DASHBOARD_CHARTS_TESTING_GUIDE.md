# ?? Dashboard Charts Testing Guide

## ?? Pre-Testing Checklist

- [ ] Project builds successfully (`dotnet build`)
- [ ] Database is running and accessible
- [ ] Test data exists in database
- [ ] All users have valid sessions
- [ ] Browser console is open (F12)

---

## ?? Test Data Setup

### Create Test Data (SQL)

```sql
-- Ensure test users exist
SELECT COUNT(*) FROM users WHERE role_id = 1; -- Admin
SELECT COUNT(*) FROM users WHERE role_id = 2; -- Faculty
SELECT COUNT(*) FROM users WHERE role_id = 3; -- Student

-- Ensure test attendance records exist (last 30 days)
SELECT COUNT(*) FROM attendance 
WHERE attend_date >= CURRENT_DATE - INTERVAL '30 days';

-- Ensure fingerprint records exist
SELECT COUNT(*) FROM fingerprint_templates;

-- Ensure classes exist
SELECT COUNT(*) FROM classes WHERE fac_id IS NOT NULL;

-- Ensure enrollments exist
SELECT COUNT(*) FROM enrollment;
```

If data is missing, add test records:

```sql
-- Add test attendance records
INSERT INTO attendance (class_id, stud_id, attend_date, attend_type, attend_status)
VALUES 
  (1, 1, CURRENT_DATE, 'in', 'present'),
  (1, 2, CURRENT_DATE, 'in', 'late'),
  (1, 3, CURRENT_DATE, 'in', 'absent');

-- Add test fingerprint record
INSERT INTO fingerprint_templates (user_id, ftemp_data, ftemp_name)
VALUES (1, E'\\x00', 'Test Fingerprint');
```

---

## ?? Admin Dashboard Tests

### Test 1: User Role Distribution Chart

**Steps:**
1. Login as Admin
2. Navigate to `/Admin/Index`
3. Observe "User Role Distribution" doughnut chart

**Expected Results:**
- [ ] Chart loads within 2 seconds
- [ ] Shows 3 segments (Admin, Faculty, Student)
- [ ] Correct counts match database query:
  ```sql
  SELECT role_name, COUNT(*) FROM users 
  JOIN user_roles USING(role_id) 
  GROUP BY role_name;
  ```
- [ ] Hover tooltip shows percentage
- [ ] Legend displays at bottom

**Validation:**
```javascript
// Browser Console
console.log('User Role Chart:', userRoleChart.data);
```

---

### Test 2: Daily Attendance Trend Chart

**Steps:**
1. Stay on Admin dashboard
2. Scroll to "Daily Attendance Trend" line chart

**Expected Results:**
- [ ] Chart loads within 2 seconds
- [ ] Shows data for last 30 days
- [ ] Line is smooth (tension: 0.4)
- [ ] X-axis shows dates (MMM dd format)
- [ ] Y-axis starts at 0
- [ ] Hover tooltip shows exact count

**Database Validation:**
```sql
SELECT attend_date, COUNT(*) as count
FROM attendance
WHERE attend_date >= CURRENT_DATE - INTERVAL '30 days'
GROUP BY attend_date
ORDER BY attend_date;
```

Compare SQL results with chart data points.

---

### Test 3: Fingerprint Registration Status Chart

**Steps:**
1. Locate "Fingerprint Registration Status" bar chart
2. Check registered vs. not registered counts

**Expected Results:**
- [ ] Chart loads within 2 seconds
- [ ] Shows 2 bars (Registered, Not Registered)
- [ ] Green bar for registered
- [ ] Red bar for not registered
- [ ] Counts match database:
  ```sql
  SELECT 
    COUNT(CASE WHEN ft.ftemp_id IS NOT NULL THEN 1 END) as registered,
 COUNT(CASE WHEN ft.ftemp_id IS NULL THEN 1 END) as not_registered
  FROM users u
  LEFT JOIN fingerprint_templates ft ON u.user_id = ft.user_id;
  ```

---

## ????? Faculty Dashboard Tests

### Test 4: Today's Attendance Status Chart

**Steps:**
1. Login as Faculty
2. Navigate to `/Faculty/Index`
3. Observe "Today's Attendance Status" doughnut chart

**Expected Results:**
- [ ] Chart loads within 2 seconds
- [ ] Shows only TODAY's attendance
- [ ] 3 segments (Present, Absent, Late)
- [ ] Counts match database:
  ```sql
  SELECT 
 COUNT(CASE WHEN attend_status = 'present' THEN 1 END) as present,
    COUNT(CASE WHEN attend_status = 'absent' THEN 1 END) as absent,
    COUNT(CASE WHEN attend_status = 'late' THEN 1 END) as late
  FROM attendance a
  JOIN classes c ON a.class_id = c.class_id
  WHERE c.fac_id = [faculty_id] 
    AND a.attend_date = CURRENT_DATE
    AND a.attend_type = 'in';
  ```

**Edge Case:**
- [ ] If no classes today, shows all zeros

---

### Test 5: Weekly Attendance Trend Chart

**Steps:**
1. Scroll to "Weekly Attendance Trend" multi-line chart
2. Check all 3 lines (Present, Late, Absent)

**Expected Results:**
- [ ] Chart loads within 2 seconds
- [ ] Shows last 7 days
- [ ] 3 distinct colored lines
- [ ] Lines do not overlap confusingly
- [ ] Legend shows all 3 statuses
- [ ] Data matches database:
  ```sql
  SELECT 
    attend_date,
    COUNT(CASE WHEN attend_status = 'present' THEN 1 END) as present,
    COUNT(CASE WHEN attend_status = 'late' THEN 1 END) as late,
    COUNT(CASE WHEN attend_status = 'absent' THEN 1 END) as absent
  FROM attendance a
  JOIN classes c ON a.class_id = c.class_id
  WHERE c.fac_id = [faculty_id]
    AND a.attend_date >= CURRENT_DATE - INTERVAL '7 days'
  AND a.attend_type = 'in'
  GROUP BY attend_date
  ORDER BY attend_date;
  ```

---

### Test 6: Student Punctuality Analysis Chart

**Steps:**
1. Locate "Student Punctuality" bar chart
2. Check on-time vs. late breakdown

**Expected Results:**
- [ ] Chart loads within 2 seconds
- [ ] 2 bars (On Time, Late)
- [ ] Green for on-time, yellow for late
- [ ] Counts match database:
  ```sql
  SELECT 
    COUNT(CASE WHEN attend_status = 'present' THEN 1 END) as on_time,
    COUNT(CASE WHEN attend_status = 'late' THEN 1 END) as late
  FROM attendance a
  JOIN classes c ON a.class_id = c.class_id
  WHERE c.fac_id = [faculty_id]
    AND a.attend_date >= CURRENT_DATE - INTERVAL '7 days'
    AND a.attend_type = 'in';
```

---

## ?? Student Dashboard Tests

### Test 7: Personal Attendance Overview Chart

**Steps:**
1. Login as Student
2. Navigate to `/Student/Index`
3. Observe "My Attendance Overview" doughnut chart

**Expected Results:**
- [ ] Chart loads within 2 seconds
- [ ] Shows SEMESTER-LONG data (all time)
- [ ] 3 segments (Present, Absent, Late)
- [ ] Tooltip shows percentages
- [ ] Data matches database:
  ```sql
  SELECT 
    COUNT(CASE WHEN attend_status = 'present' THEN 1 END) as present,
    COUNT(CASE WHEN attend_status = 'absent' THEN 1 END) as absent,
    COUNT(CASE WHEN attend_status = 'late' THEN 1 END) as late
  FROM attendance
  WHERE stud_id = [student_id] AND attend_type = 'in';
  ```

---

### Test 8: Punctuality Trend Chart

**Steps:**
1. Scroll to "My Punctuality Trend" multi-line chart
2. Check personal attendance pattern

**Expected Results:**
- [ ] Chart loads within 2 seconds
- [ ] Shows last 14 days
- [ ] 3 lines (On Time, Late, Absent)
- [ ] Data is student-specific
- [ ] Matches database:
  ```sql
  SELECT 
    attend_date,
    COUNT(CASE WHEN attend_status = 'present' THEN 1 END) as on_time,
    COUNT(CASE WHEN attend_status = 'late' THEN 1 END) as late,
    COUNT(CASE WHEN attend_status = 'absent' THEN 1 END) as absent
  FROM attendance
  WHERE stud_id = [student_id]
  AND attend_date >= CURRENT_DATE - INTERVAL '14 days'
    AND attend_type = 'in'
  GROUP BY attend_date
  ORDER BY attend_date;
  ```

---

### Test 9: Course-wise Attendance Rate Chart

**Steps:**
1. Locate "Course-wise Attendance Rate" bar chart
2. Check attendance percentage per course

**Expected Results:**
- [ ] Chart loads within 2 seconds
- [ ] One bar per enrolled course
- [ ] Bars are color-coded:
  - Green (90-100%)
  - Yellow (75-89%)
  - Red (<75%)
- [ ] Y-axis shows 0-100%
- [ ] Tooltip shows percentage with '%' symbol
- [ ] Data matches database:
  ```sql
  SELECT 
    co.course_code,
ROUND((COUNT(CASE WHEN a.attend_status IN ('present', 'late') THEN 1 END)::DECIMAL 
           / NULLIF(COUNT(*), 0)) * 100) as attendance_rate
  FROM enrollment e
  JOIN classes c ON e.class_id = c.class_id
  JOIN courses co ON c.course_id = co.course_id
  LEFT JOIN attendance a ON e.class_id = a.class_id 
    AND e.stud_id = a.stud_id 
    AND a.attend_type = 'in'
  WHERE e.stud_id = [student_id]
  GROUP BY co.course_code
  ORDER BY co.course_code;
  ```

---

## ?? Auto-Refresh Tests

### Test 10: Auto-Refresh Functionality

**Steps:**
1. Open any dashboard
2. Wait 5 minutes (or use browser dev tools to speed up)
3. Add new data to database
4. Observe chart updates

**Expected Results:**
- [ ] Charts refresh automatically after 5 minutes
- [ ] No page reload occurs
- [ ] New data appears in charts
- [ ] Console logs show fetch requests

**Manual Trigger:**
```javascript
// Browser Console - Force refresh
loadUserRoleChart();        // Admin
loadTodayAttendanceChart(); // Faculty
loadAttendanceOverviewChart(); // Student
```

---

## ?? Responsive Design Tests

### Test 11: Mobile Responsiveness

**Steps:**
1. Open any dashboard
2. Press F12 ? Device Toolbar (Ctrl+Shift+M)
3. Test different screen sizes:
   - iPhone SE (375px)
   - iPad (768px)
   - Desktop (1920px)

**Expected Results:**
- [ ] **Mobile (<480px)**:
  - Charts stack vertically
  - Height: 200px
  - Readable labels
- [ ] **Tablet (768px)**:
  - Charts in 1 column
  - Height: 250px
  - Touch-friendly
- [ ] **Desktop (>768px)**:
  - Charts in 2-column grid
- Height: 300-350px
  - Full features

---

## ?? Error Handling Tests

### Test 12: Network Failure

**Steps:**
1. Open dashboard
2. Open Network tab (F12)
3. Set throttling to "Offline"
4. Wait for auto-refresh or reload page

**Expected Results:**
- [ ] Error message displays: "Failed to load chart"
- [ ] Console logs error
- [ ] No white screen or crash
- [ ] Other page elements still work

---

### Test 13: Empty Data

**Steps:**
1. Create a new faculty/student with no data
2. Login as that user
3. Check dashboard

**Expected Results:**
- [ ] Charts show with all zeros
- [ ] No JavaScript errors
- [ ] "No data" is clear from tooltip
- [ ] Charts are still interactive

---

### Test 14: Session Expiration

**Steps:**
1. Login to dashboard
2. Clear session cookie manually
3. Try to load charts (refresh or wait for auto-refresh)

**Expected Results:**
- [ ] API returns 401 Unauthorized
- [ ] User redirected to login page
- [ ] No sensitive data exposed

---

## ?? Security Tests

### Test 15: Role-Based Access

**Steps:**
1. Login as Student
2. Try to access `/api/admin/chart/user-roles` directly

**Expected Result:**
- [ ] 403 Forbidden or redirect to unauthorized page

**Test All Combinations:**
```
Student trying Admin endpoints   ? ? Blocked
Student trying Faculty endpoints ? ? Blocked
Faculty trying Admin endpoints   ? ? Blocked
Faculty trying Student endpoints ? ? Blocked
Admin accessing all endpoints    ? ? Allowed
```

---

### Test 16: SQL Injection Prevention

**Steps:**
1. Try malicious input in URL parameters
2. Example: `/api/admin/chart/attendance-trend?days=30; DROP TABLE users;`

**Expected Results:**
- [ ] Query fails safely
- [ ] No SQL execution
- [ ] Error logged
- [ ] Database intact

---

## ? Performance Tests

### Test 17: Load Time

**Steps:**
1. Open Network tab (F12)
2. Hard refresh (Ctrl+Shift+R)
3. Measure chart load times

**Expected Results:**
- [ ] API response < 500ms
- [ ] Chart render < 200ms
- [ ] Total load < 2 seconds

---

### Test 18: Concurrent Users

**Steps:**
1. Open 10+ tabs with different user sessions
2. All accessing dashboards simultaneously

**Expected Results:**
- [ ] All charts load correctly
- [ ] No database timeout
- [ ] No memory leaks
- [ ] Server remains responsive

---

## ?? Visual Regression Tests

### Test 19: Chart Appearance

**Checklist:**
- [ ] Fonts are readable (size 12-16px)
- [ ] Colors match design system
- [ ] Borders and padding consistent
- [ ] Animations smooth (no flickering)
- [ ] Legends positioned correctly
- [ ] Tooltips appear on hover

---

### Test 20: Cross-Browser Compatibility

**Test in:**
- [ ] Chrome (latest)
- [ ] Firefox (latest)
- [ ] Edge (latest)
- [ ] Safari (latest)

**Expected:**
- Charts render identically
- No console errors
- All interactions work

---

## ?? Database Query Performance

### Test 21: Query Optimization

**Steps:**
1. Run `EXPLAIN ANALYZE` on all chart queries
2. Check for:
   - Index usage
   - Seq scans (should be minimal)
   - Query time

**Example:**
```sql
EXPLAIN ANALYZE
SELECT attend_date, COUNT(*) 
FROM attendance
WHERE attend_date >= CURRENT_DATE - INTERVAL '30 days'
GROUP BY attend_date
ORDER BY attend_date;
```

**Expected:**
- [ ] Uses indexes (idx_attendance_date)
- [ ] Query time < 100ms
- [ ] No full table scans

---

## ? Final Checklist

### Pre-Production:
- [ ] All 21 tests pass
- [ ] No console errors
- [ ] Build succeeds
- [ ] Database queries optimized
- [ ] Security validated
- [ ] Responsive design confirmed
- [ ] Auto-refresh working
- [ ] Error handling robust

### Post-Deployment:
- [ ] Monitor API logs
- [ ] Check database performance
- [ ] Gather user feedback
- [ ] Track chart usage analytics

---

## ?? Bug Reporting Template

If a test fails, report using this format:

```markdown
**Test Number:** [e.g., Test 7]
**Dashboard:** [Admin/Faculty/Student]
**Chart:** [Name of chart]
**Issue:** [Brief description]
**Steps to Reproduce:**
1. 
2. 
3. 

**Expected:** [What should happen]
**Actual:** [What actually happened]
**Browser:** [Chrome/Firefox/etc.]
**Screenshots:** [Attach if applicable]
**Console Logs:** [Paste any errors]
```

---

## ?? Support

For testing assistance:
- Review `DASHBOARD_CHARTS_IMPLEMENTATION.md`
- Check `DASHBOARD_CHARTS_QUICK_REFERENCE.md`
- Examine browser console logs
- Verify database connection

---

**Testing Completed By:** _______________  
**Date:** _______________  
**All Tests Passed:** ? Yes ? No  
**Notes:** _______________

---

**Good luck testing! ???**
