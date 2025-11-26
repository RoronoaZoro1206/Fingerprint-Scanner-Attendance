# Fingerprint Attendance System - API Reference

## 📋 Table of Contents
1. [Authentication](#authentication)
2. [Health Check API](#health-check-api)
3. [Attendance API](#attendance-api)
4. [Admin API](#admin-api)
5. [Faculty API](#faculty-api)
6. [Student API](#student-api)
7. [Error Handling](#error-handling)
8. [Data Models](#data-models)

---

## 🔐 Authentication

### Session-Based Authentication
All API endpoints (except health checks) require authentication via session cookies.

**Login Flow:**
1. POST to `/Auth/Login` with email/password
2. Session cookie set on successful authentication
3. Include cookie in subsequent requests

**Session Variables:**
- `UserId`: Authenticated user's ID
- `UserRole`: User's role (Admin/Faculty/Student)
- `UserEmail`: User's email address

---

## ❤️ Health Check API

### GET /health
Comprehensive system health status.

**Response (200 OK):**
```json
{
  "status": "Healthy",
  "timestamp": "2025-11-26T10:30:00Z",
  "uptime": "02:30:45",
  "version": "1.0.0",
  "components": {
    "database": {
      "status": "Healthy",
      "message": "Database connection is healthy",
      "data": {
        "responseTimeMs": 5,
        "serverTime": "2025-11-26 10:30:00",
        "version": "PostgreSQL 14.0"
      }
    },
    "scanner": {
      "status": "Healthy",
      "message": "Fingerprint scanner is connected and ready",
      "data": {
        "deviceStatus": "Device Connected",
        "connected": true
      }
    },
    "memory": {
      "status": "Healthy",
      "message": "Memory usage normal: 256.50 MB",
      "data": {
        "usedMB": 256.50,
        "usedBytes": 268959744
      }
    }
  },
  "metrics": {
    "memoryUsedBytes": 268959744,
    "memoryUsedMB": 256.50,
    "threadCount": 25,
    "processorCount": 8
  }
}
```

### GET /health/live
Kubernetes liveness probe.

**Response (200 OK):**
```json
{
  "status": "Alive",
  "timestamp": "2025-11-26T10:30:00Z"
}
```

### GET /health/ready
Kubernetes readiness probe (checks database).

**Response (200 OK):**
```json
{
  "status": "Ready",
  "timestamp": "2025-11-26T10:30:00Z",
  "database": "Connected"
}
```

**Response (503 Service Unavailable):**
```json
{
  "status": "Not Ready",
  "timestamp": "2025-11-26T10:30:00Z",
  "database": "Disconnected",
  "error": "Connection refused"
}
```

### GET /health/scanner
Scanner-specific health check.

### GET /health/database
Database-specific health check.

---

## 📋 Attendance API

Base URL: `/api/v1/attendance`

### POST /scan
Record attendance via fingerprint scan.

**Response (200 OK):**
```json
{
  "success": true,
  "message": "Attendance processed successfully",
  "data": {
    "action": "time_in",
    "timestamp": "2025-11-26T08:00:00",
    "student": {
      "studId": 1,
      "studNumber": "2025-0001",
      "fullName": "John Doe",
      "programName": "BSIT"
    },
    "classInfo": {
      "courseCode": "IT101",
      "courseName": "Introduction to Computing"
    },
    "status": "Present"
  },
  "metadata": {
    "processingTimeMs": 150,
    "timestamp": "2025-11-26T08:00:00Z"
  }
}
```

### GET /scanner/status
Check fingerprint scanner device status.

**Response:**
```json
{
  "success": true,
  "message": "Scanner online",
  "data": {
    "isAvailable": true,
    "status": "Device Connected",
    "message": "Scanner ready"
  }
}
```

### GET /today/summary
Get today's attendance summary statistics.

**Response:**
```json
{
  "success": true,
  "message": "Summary retrieved successfully",
  "data": {
    "totalStudents": 150,
    "timedIn": 120,
    "timedOut": 80,
    "incomplete": 40,
    "absent": 30
  }
}
```

### GET /student/{studId}/today
Get specific student's attendance for today.

### GET /student/{studId}/history
Get student's attendance history.

**Query Parameters:**
- `days` (optional): Number of days to retrieve (default: 30)

### GET /incomplete
Get all incomplete (forgot time out) records.

### POST /incomplete/{dailyAttendId}/resolve
Manually resolve an incomplete record.

**Request Body:**
```json
{
  "manualTimeOut": "2025-11-26T17:00:00"
}
```

### POST /absences/process
Manually trigger absence detection.

**Query Parameters:**
- `date` (optional): Date to process (format: yyyy-MM-dd)

---

## 👨‍💼 Admin API

### User Management

#### GET /AdminUser/GetAll
Get all users with pagination support.

**Response:**
```json
{
  "data": [
    {
      "userId": 1,
      "userEmail": "admin@example.com",
      "userFirstName": "Admin",
      "userLastName": "User",
      "roleId": 1,
      "roleName": "admin",
      "isActive": true,
      "userCreatedAt": "2025-01-01T00:00:00"
    }
  ]
}
```

#### POST /AdminUser/Create
Create a new user.

**Request Body:**
```json
{
  "userEmail": "newuser@example.com",
  "userPassword": "SecurePass123",
  "userFirstName": "New",
  "userLastName": "User",
  "roleId": 3,
  "userGender": "Male",
  "userPhone": "09123456789"
}
```

#### PUT /AdminUser/Update
Update an existing user.

#### DELETE /AdminUser/Delete/{id}
Delete a user.

### Student Management

#### GET /AdminStudent/GetAll
Get all students with details.

#### POST /AdminStudent/Create
Create a new student.

**Request Body:**
```json
{
  "userId": 5,
  "studNumber": "2025-0001",
  "programId": 1,
  "studYearLevel": 1,
  "sectionId": 1
}
```

### Class Management

#### GET /AdminClass/GetAll
Get all classes with course, faculty, and room details.

#### POST /AdminClass/Create
Create a new class.

**Request Body:**
```json
{
  "courseId": 1,
  "roomId": 1,
  "facId": 1,
  "termId": 1
}
```

### Schedule Management

#### POST /AdminClassSchedule/CreateMultiple
Create multiple schedules for a class.

**Request Body:**
```json
[
  {
    "classId": 1,
    "schedDayOfWeek": "Monday",
    "schedStartTime": "08:00",
    "schedEndTime": "10:00"
  },
  {
    "classId": 1,
    "schedDayOfWeek": "Wednesday",
    "schedStartTime": "08:00",
    "schedEndTime": "10:00"
  }
]
```

### Student ID Pool

#### POST /AdminStudentIdPool/Generate
Generate student IDs.

**Request Body:**
```json
{
  "year": 2025,
  "startNumber": 1,
  "count": 100,
  "generatedBy": 1,
  "remarks": "Batch generation for AY 2025-2026"
}
```

---

## 👨‍🏫 Faculty API

### GET /FacultyAttendance/GetClassAttendance
Get attendance for a specific class.

**Query Parameters:**
- `classId` (required): Class ID
- `date` (optional): Date (default: today)

**Response:**
```json
{
  "success": true,
  "data": {
    "courseCode": "IT101",
    "courseName": "Introduction to Computing",
    "date": "2025-11-26",
    "students": [
      {
        "studId": 1,
        "studentNumber": "2025-0001",
        "name": "John Doe",
        "profileImage": "default_profile.png",
        "timeIn": "08:00:00",
        "timeOut": "10:00:00",
        "status": "Present",
        "attendId": 123
      }
    ]
  }
}
```

### PUT /FacultyAttendance/UpdateAttendance
Update student attendance status.

**Request Body:**
```json
{
  "classId": 1,
  "studId": 1,
  "status": "Excused",
  "remarks": "Medical certificate submitted",
  "date": "2025-11-26"
}
```

### DELETE /FacultyAttendance/DeleteAttendance
Delete attendance record.

**Request Body:**
```json
{
  "classId": 1,
  "studId": 1,
  "date": "2025-11-26"
}
```

### POST /FacultyAttendance/MarkAllPresent
Mark all students as present.

**Request Body:**
```json
{
  "classId": 1
}
```

---

## 👨‍🎓 Student API

### GET /StudentEnrollment/GetAvailableClasses
Get classes available for enrollment.

**Response:**
```json
{
  "success": true,
  "data": [
    {
      "classId": 1,
      "courseCode": "IT101",
      "courseName": "Introduction to Computing",
      "facultyName": "Prof. Smith",
      "roomCode": "Room 101",
      "schedule": "MWF 8:00-10:00",
      "enrolledCount": 25,
      "maxCapacity": 40
    }
  ]
}
```

### POST /StudentEnrollment/Enroll
Enroll in a class.

**Request Body:**
```json
{
  "classId": 1
}
```

**Error Response (Schedule Conflict):**
```json
{
  "success": false,
  "message": "⚠️ Schedule Conflict Detected!\n\nYou are already enrolled in 'IT102' on Monday from 08:00 to 10:00.",
  "isConflict": true
}
```

### POST /StudentEnrollment/Unenroll
Unenroll from a class.

**Request Body:**
```json
{
  "enrollId": 1
}
```

### GET /StudentAttendance/GetAttendanceHistory
Get student's attendance history.

---

## ⚠️ Error Handling

### Standard Error Response
```json
{
  "success": false,
  "message": "Error description",
  "errorCode": "ERROR_CODE",
  "details": "Additional details (debug mode only)",
  "timestamp": "2025-11-26T10:30:00Z"
}
```

### Error Codes
| Code | HTTP Status | Description |
|------|-------------|-------------|
| `INVALID_OPERATION` | 400 | Invalid operation attempted |
| `UNAUTHORIZED` | 401 | Not authenticated |
| `MISSING_PARAMETER` | 400 | Required parameter missing |
| `TIMEOUT` | 408 | Operation timed out |
| `DATABASE_ERROR` | 500 | Database operation failed |
| `INTERNAL_ERROR` | 500 | Unexpected server error |
| `SCANNER_NOT_AVAILABLE` | 503 | Fingerprint scanner offline |
| `FINGERPRINT_NOT_RECOGNIZED` | 400 | Fingerprint not in database |
| `NO_SCHEDULE` | 400 | No class scheduled at this time |

---

## 📦 Data Models

### User
```json
{
  "userId": 1,
  "userProfileImage": "default_profile.png",
  "userFirstName": "John",
  "userMiddleName": "M",
  "userLastName": "Doe",
  "userGender": "Male",
  "userPhone": "09123456789",
  "userAddress": "123 Main St",
  "userBirthdate": "2000-01-01",
  "userEmail": "john@example.com",
  "roleId": 3,
  "isActive": true,
  "userCreatedAt": "2025-01-01T00:00:00"
}
```

### Student
```json
{
  "studId": 1,
  "userId": 5,
  "studNumber": "2025-0001",
  "programId": 1,
  "studYearLevel": 1,
  "sectionId": 1,
  "profileSubmitted": true,
  "studCreatedAt": "2025-01-01T00:00:00"
}
```

### Class
```json
{
  "classId": 1,
  "courseId": 1,
  "roomId": 1,
  "facId": 1,
  "termId": 1,
  "classCreatedAt": "2025-01-01T00:00:00"
}
```

### Attendance
```json
{
  "attendId": 1,
  "classId": 1,
  "studId": 1,
  "attendDate": "2025-11-26",
  "attendScanTime": "2025-11-26T08:00:00",
  "attendType": "in",
  "attendStatus": "present",
  "attendRemark": null,
  "dailyAttendId": 1,
  "isPresent": true
}
```

### DailyAttendanceLog
```json
{
  "dailyAttendId": 1,
  "studId": 1,
  "classId": 1,
  "attendDate": "2025-11-26",
  "timeIn": "2025-11-26T08:00:00",
  "timeOut": "2025-11-26T17:00:00",
  "status": "completed",
  "forgotTimeout": false,
  "remarks": null
}
```

---

*API Reference for Fingerprint Attendance System v1.0.0*
