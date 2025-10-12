# Faculty Profile - Routing Guide

## ?? Overview
This guide explains how the Faculty "My Classes" page is routed and displayed.

---

## ?? Routing Flow

### 1. **URL to Action Mapping**

| URL | Controller | Action | View |
|-----|------------|--------|------|
| `/Faculty/MyClasses` | `FacultyController` | `MyClasses()` | `Views/Faculty/MyClasses.cshtml` |
| `/Faculty/GetMyClasses` | `FacultyController` | `GetMyClasses()` | JSON response |

---

### 2. **Sidebar Navigation**

**File:** `Views/Shared/_FacultySidebarPartial.cshtml`

```razor
<a href="@Url.Action("MyClasses", "Faculty")" class="sidebar-link">
    <i data-lucide="book-open"></i>
    <span>My Classes</span>
</a>
```

**Generated URL:** `/Faculty/MyClasses`

---

### 3. **Controller Actions**

**File:** `Controllers/Faculty/FacultyController.cs`

#### **Display View Action:**
```csharp
public IActionResult MyClasses()
{
    return View();  // Returns Views/Faculty/MyClasses.cshtml
}
```

#### **Get Data API Action:**
```csharp
[HttpGet]
public async Task<IActionResult> GetMyClasses()
{
    // 1. Get logged-in user ID from session
    var userId = HttpContext.Session.GetInt32("UserId");
    
    // 2. Get faculty record
    var faculty = await _facultyRepository.GetByUserIdAsync(userId.Value);
    
    // 3. Get all classes for this faculty
    var classes = await _classRepository.GetClassesByFacultyIdAsync(faculty.FacId);
    
    // 4. Get schedules and enrollment for each class
    // 5. Return JSON data
    return Json(new { success = true, data = classesData });
}
```

---

### 4. **View Structure**

**File:** `Views/Faculty/MyClasses.cshtml`

```razor
@{
    ViewData["Title"] = "My Classes";
    Layout = "~/Views/Shared/_MainLayout.cshtml";
}

<link rel="stylesheet" href="~/css/faculty-classes.css" />

<div id="classesContainer" class="row g-4">
    <!-- Classes loaded via JavaScript -->
</div>

@section Scripts {
    <script src="~/js/ProfileFaculty.js"></script>
}
```

---

### 5. **JavaScript Flow**

**File:** `wwwroot/js/ProfileFaculty.js`

```javascript
$(document).ready(function () {
    loadMyClasses();  // Called on page load
});

async function loadMyClasses() {
    // 1. AJAX call to /Faculty/GetMyClasses
    const response = await $.ajax({
        url: '/Faculty/GetMyClasses',
        type: 'GET'
    });
    
    // 2. Display classes in cards
    displayClasses(response.data);
}

function displayClasses(classes) {
    // 3. Build HTML for each class card
    // 4. Insert into #classesContainer
}
```

---

## ??? Database Queries

### **Repository Methods Used:**

1. **FacultyRepository.GetByUserIdAsync()**
   - Gets faculty record for logged-in user
   ```sql
   SELECT * FROM faculty WHERE user_id = @userId
   ```

2. **ClassRepository.GetClassesByFacultyIdAsync()**
   - Gets all classes taught by the faculty
   ```sql
   SELECT c.*, co.course_code, co.course_name, r.room_code, t.term_name,
          (SELECT COUNT(*) FROM enrollment e WHERE e.class_id = c.class_id) as student_count
   FROM classes c
   LEFT JOIN courses co ON c.course_id = co.course_id
   LEFT JOIN rooms r ON c.room_id = r.room_id
   LEFT JOIN academic_terms t ON c.term_id = t.term_id
   WHERE c.fac_id = @facId
   ```

3. **ClassScheduleRepository.GetByClassIdAsync()**
   - Gets schedules for each class
   ```sql
   SELECT * FROM class_schedules 
   WHERE class_id = @classId 
   ORDER BY sched_day_of_week, sched_start_time
   ```

4. **EnrollmentRepository.GetByClassIdAsync()**
   - Gets enrolled students count
   ```sql
   SELECT * FROM enrollment WHERE class_id = @classId
   ```

---

## ?? UI Components

### **Class Card Display:**

Each class is displayed as a card with:
- **Header:** Course code + Student count badge
- **Body:** Course name, room, schedule
- **Footer:** Attendance button

**Design Features:**
- Gradient purple header
- Hover animations
- Responsive grid layout (4 columns on large screens, 2 on medium, 1 on mobile)
- Lucide icons throughout

---

## ?? Key Files Modified/Created

| File | Purpose |
|------|---------|
| `Controllers/Faculty/FacultyController.cs` | Added MyClasses() and GetMyClasses() actions |
| `Views/Faculty/MyClasses.cshtml` | View template for displaying classes |
| `wwwroot/js/ProfileFaculty.js` | JavaScript to load and display classes |
| `wwwroot/css/faculty-classes.css` | Styles for class cards |
| `Views/Shared/_FacultySidebarPartial.cshtml` | Updated navigation links |
| `Repositories/Interfaces/IClassRepository.cs` | Added GetClassesByFacultyIdAsync() |
| `Repositories/Implementations/ClassRepository.cs` | Implemented GetClassesByFacultyIdAsync() |
| `Repositories/Interfaces/IClassScheduleRepository.cs` | Added GetByClassIdAsync() |
| `Repositories/Implementations/ClassScheduleRepository.cs` | Implemented GetByClassIdAsync() |
| `Repositories/Interfaces/IEnrollmentRepository.cs` | Added GetByClassIdAsync() |
| `Repositories/Implementations/EnrollmentRepository.cs` | Implemented GetByClassIdAsync() |

---

## ?? How to Access

1. **Login as Faculty** (RoleId = 2)
2. **Session stores:**
   - `UserId` - User's ID
   - `RoleId` - Role (2 = Faculty)
   - `UserName` - Display name
3. **Click "My Classes"** in sidebar
4. **Route:** `/Faculty/MyClasses`
5. **Page loads** ? JavaScript calls `/Faculty/GetMyClasses`
6. **Data fetched** from database
7. **Cards displayed** on screen

---

## ?? Data Flow Diagram

```
User Clicks "My Classes"
        ?
URL: /Faculty/MyClasses
        ?
FacultyController.MyClasses()
        ?
Returns MyClasses.cshtml
        ?
View renders with layout
        ?
ProfileFaculty.js executes
        ?
AJAX: /Faculty/GetMyClasses
        ?
FacultyController.GetMyClasses()
        ?
Get UserId from Session
        ?
Get Faculty record
        ?
Get Classes for Faculty
        ?
Get Schedules & Enrollments
        ?
Return JSON data
        ?
JavaScript displays cards
```

---

## ?? Testing the Route

1. **Direct URL:** Navigate to `http://localhost:PORT/Faculty/MyClasses`
2. **Sidebar Click:** Click "My Classes" in faculty sidebar
3. **API Test:** Navigate to `http://localhost:PORT/Faculty/GetMyClasses` (returns JSON)

---

## ? Success Criteria

- ? Build successful with no errors
- ? Route properly configured
- ? Database queries optimized
- ? UI matches design mockup
- ? Responsive on all devices
- ? Icons render correctly (Lucide)
- ? Session-based authentication
- ? Clean separation of concerns (MVC pattern)
