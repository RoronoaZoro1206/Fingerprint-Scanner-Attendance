# Fingerprint Attendance System - Complete Documentation

## 📋 Table of Contents
1. [System Overview](#system-overview)
2. [Technology Stack](#technology-stack)
3. [Architecture](#architecture)
4. [Database Schema](#database-schema)
5. [User Roles & Permissions](#user-roles--permissions)
6. [Core Features](#core-features)
7. [API Endpoints](#api-endpoints)
8. [Services](#services)
9. [Security Features](#security-features)
10. [Configuration](#configuration)
11. [Project Structure](#project-structure)

---

## 🎯 System Overview

The **Fingerprint Attendance System** is a comprehensive biometric-based attendance management solution built for educational institutions. It enables students to record their attendance using fingerprint scanning, while providing faculty and administrators with powerful tools to manage classes, enrollments, and attendance records.

### Key Capabilities
- **Biometric Authentication**: ZKFinger SDK integration for fingerprint capture and matching
- **Real-time Attendance**: Time In/Time Out tracking with automatic status determination
- **Multi-Role Support**: Admin, Faculty, and Student portals with role-specific features
- **Academic Management**: Courses, Classes, Schedules, Enrollments, and Academic Terms
- **Audit Trail**: Complete logging of all authentication and administrative actions
- **Real-time Notifications**: SignalR-based live updates for attendance events

---

## 🛠 Technology Stack

### Backend
| Component | Technology |
|-----------|------------|
| Framework | ASP.NET Core 9.0 (MVC + Web API) |
| Language | C# 12 |
| Database | PostgreSQL |
| ORM | Dapper (via DatabaseHelper) |
| Real-time | SignalR |
| Fingerprint SDK | ZKFinger SDK (libzkfpcsharp.dll) |

### Frontend
| Component | Technology |
|-----------|------------|
| Views | Razor (.cshtml) |
| CSS Framework | Bootstrap 5 |
| JavaScript | Vanilla JS + jQuery |
| Data Tables | DataTables.js |
| Notifications | Toastify.js, SweetAlert2 |
| Charts | Chart.js |

### Development Tools
| Tool | Purpose |
|------|---------|
| Swagger/OpenAPI | API Documentation |
| TypeScript | Type definitions for JS |
| Node.js | Package management |

---

## 🏗 Architecture

### Design Patterns
- **Repository Pattern**: Data access abstraction via interfaces
- **Service Layer**: Business logic encapsulation
- **Dependency Injection**: IoC container for loose coupling
- **MVC Pattern**: Model-View-Controller separation
- **SOLID Principles**: Interface segregation, dependency inversion

### Layer Structure
```
┌─────────────────────────────────────────────────────────┐
│                    PRESENTATION LAYER                    │
│  Controllers (MVC + API) │ Views (Razor) │ wwwroot (JS/CSS)│
├─────────────────────────────────────────────────────────┤
│                     SERVICE LAYER                        │
│  FingerprintService │ AttendanceProcessingService │ etc. │
├─────────────────────────────────────────────────────────┤
│                   REPOSITORY LAYER                       │
│  IUserRepository │ IAttendanceRepository │ etc.          │
├─────────────────────────────────────────────────────────┤
│                      DATA LAYER                          │
│  DatabaseHelper (Dapper) │ PostgreSQL                    │
└─────────────────────────────────────────────────────────┘
```

---

## 🗄 Database Schema

### Core Tables

#### User Management
| Table | Description |
|-------|-------------|
| `user_roles` | Role definitions (Admin, Faculty, Student) |
| `users` | All system users with authentication data |
| `administration` | Admin-specific data |
| `faculty` | Faculty-specific data with department |
| `students` | Student-specific data with program/section |

#### Academic Structure
| Table | Description |
|-------|-------------|
| `departments` | Academic departments |
| `programs` | Degree programs (BSIT, BSHM, etc.) |
| `courses` | Individual courses/subjects |
| `sections` | Class sections |
| `academic_terms` | Semesters/terms |
| `rooms` | Physical classrooms |
| `classes` | Course instances per term |
| `class_schedules` | Day/time schedules for classes |
| `enrollment` | Student-class enrollments |

#### Attendance System
| Table | Description |
|-------|-------------|
| `daily_attendance_log` | Daily Time In/Out records |
| `attendance` | Class-specific attendance records |
| `fingerprint_templates` | Stored biometric data |
| `scan_logs` | All scan attempts (success/failure) |
| `student_excuse_requests` | Absence excuse submissions |
| `faculty_attendance_actions` | Faculty modifications audit |
| `attendance_notifications` | User notifications |

#### Audit & Security
| Table | Description |
|-------|-------------|
| `authentication_audit` | Login/logout events |
| `administration_audit` | Admin action logs |
| `student_id_pool` | Pre-generated student IDs |

### Key Relationships
```
users ─┬─> administration (1:1)
       ├─> faculty (1:1) ──> departments
       └─> students (1:1) ──> programs ──> sections

classes ──> courses ──> programs
        ──> faculty
        ──> rooms
        ──> academic_terms
        ──> class_schedules (1:N)

enrollment ──> students
           ──> classes

attendance ──> classes
           ──> students
           ──> daily_attendance_log
```

---

## 👥 User Roles & Permissions

### Role Hierarchy
```
ADMIN (role_id: 1)
├── Full system access
├── User management (CRUD all users)
├── Academic structure management
├── Fingerprint registration for all users
├── System maintenance
└── Audit log access

FACULTY (role_id: 2)
├── View assigned classes
├── Manage class attendance
├── Approve/reject excuse requests
├── Generate attendance reports
├── Register own fingerprint
└── View student attendance history

STUDENT (role_id: 3)
├── View enrolled classes
├── Self-enrollment in classes
├── Record attendance via fingerprint
├── View own attendance history
├── Submit excuse requests
└── Register own fingerprint
```

### Authorization
- **RoleAuthorizeAttribute**: Custom attribute for role-based access control
- Applied at controller/action level
- Validates session-based authentication

---

## ⚡ Core Features

### 1. Fingerprint Attendance Recording

**Flow:**
1. Student places finger on scanner
2. System captures fingerprint template
3. Template matched against database (threshold: 50)
4. Student identity verified
5. Schedule validation (must have class at current time)
6. Time In or Time Out recorded based on current state
7. Attendance status determined (Present/Late based on grace period)
8. Real-time notification sent via SignalR

**Status Determination:**
- **Present**: Scanned within grace period (default: 15 minutes after class start)
- **Late**: Scanned after grace period
- **Absent**: No scan recorded (marked at end of day)
- **Excused**: Faculty-approved excuse

### 2. Academic Management (Admin)
- **Departments**: Create/manage academic departments
- **Programs**: Degree programs linked to departments
- **Courses**: Subjects with units, linked to programs
- **Sections**: Class sections with year levels
- **Rooms**: Physical classroom management
- **Classes**: Course instances with faculty, room, term
- **Schedules**: Day/time assignments for classes
- **Academic Terms**: Semester management with active term

### 3. Student Management
- **Student ID Pool**: Pre-generated IDs for registration
- **Profile Management**: Student information and program assignment
- **Enrollment**: Self-service class enrollment with conflict detection
- **Attendance History**: View personal attendance records

### 4. Faculty Features
- **Class Management**: View assigned classes and schedules
- **Attendance Management**: Mark/modify student attendance
- **Excuse Handling**: Approve/reject student excuse requests
- **Reports**: Generate attendance reports and statistics

### 5. Real-time Features
- **SignalR Hub**: `AttendanceHub` for live updates
- **Notifications**: Toast notifications for attendance events
- **Dashboard Updates**: Live statistics refresh

---

## 🔌 API Endpoints

### Health Check Endpoints
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/health` | Comprehensive system health |
| GET | `/health/live` | Kubernetes liveness probe |
| GET | `/health/ready` | Kubernetes readiness probe |
| GET | `/health/scanner` | Scanner device status |
| GET | `/health/database` | Database connectivity |

### Attendance API (`/api/v1/attendance`)
| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/scan` | Record attendance via fingerprint |
| GET | `/scanner/status` | Check scanner availability |
| GET | `/today/summary` | Today's attendance summary |
| GET | `/student/{id}/today` | Student's today attendance |
| GET | `/student/{id}/history` | Student attendance history |
| GET | `/incomplete` | Incomplete (forgot timeout) records |
| POST | `/incomplete/{id}/resolve` | Resolve incomplete record |
| POST | `/absences/process` | Trigger absence detection |

### Admin Controllers
| Controller | Base Route | Purpose |
|------------|------------|---------|
| AdminUserController | `/AdminUser` | User CRUD operations |
| AdminStudentController | `/AdminStudent` | Student management |
| AdminFacultyController | `/AdminFaculty` | Faculty management |
| AdminCourseController | `/AdminCourse` | Course management |
| AdminClassController | `/AdminClass` | Class management |
| AdminClassScheduleController | `/AdminClassSchedule` | Schedule management |
| AdminRoomController | `/AdminRoom` | Room management |
| AdminDepartmentController | `/AdminDepartment` | Department management |
| AdminProgramController | `/AdminProgram` | Program management |
| AdminSectionController | `/AdminSection` | Section management |
| AdminAcademicTermController | `/AdminAcademicTerm` | Term management |
| AdminStudentIdPoolController | `/AdminStudentIdPool` | Student ID generation |
| AdminAuditController | `/AdminAudit` | Audit log viewing |

### Faculty Controllers
| Controller | Base Route | Purpose |
|------------|------------|---------|
| FacultyAttendanceController | `/FacultyAttendance` | Attendance management |
| FacultyClassController | `/FacultyClass` | View assigned classes |
| FacultyReportController | `/FacultyReport` | Generate reports |

### Student Controllers
| Controller | Base Route | Purpose |
|------------|------------|---------|
| StudentEnrollmentController | `/StudentEnrollment` | Class enrollment |
| StudentAttendanceController | `/StudentAttendance` | View attendance |
| StudentClassController | `/StudentClass` | View enrolled classes |
| StudentProfileController | `/StudentProfile` | Profile management |

---

## ⚙️ Services

### Core Services

#### FingerprintService (`IFingerprintService`)
- SDK initialization and device management
- Fingerprint capture with retry logic
- Template matching and scoring
- LED/buzzer feedback control
- Image conversion for display

#### FingerprintMatchingService (`IFingerprintMatchingService`)
- Fingerprint matching against database
- Role validation for attendance
- Scan logging
- LED feedback orchestration
- Attendance status determination

#### AttendanceProcessingService
- Main attendance pipeline orchestration
- Scanner validation
- Fingerprint capture with timeout
- Schedule validation
- Incomplete record handling

#### DailyAttendanceService
- Time In/Time Out processing
- Daily attendance log management
- Status transitions

#### AbsenceDetectionService
- End-of-day absence marking
- Background service integration
- Forgot timeout detection

### Support Services

| Service | Purpose |
|---------|---------|
| `AdminAuditService` | Log admin actions |
| `UniversalAuditService` | Cross-role audit logging |
| `AuthenticationAuditService` | Login/logout tracking |
| `AcademicTermService` | Active term management |
| `LateDetectionService` | Late arrival detection |
| `MissedTimeOutService` | Forgot timeout handling |
| `SuperAdminInitializer` | Initial admin account setup |

### Background Services

#### AttendanceBackgroundService
- Runs at 11:59 PM daily
- Marks absent students
- Flags incomplete records

---

## 🔒 Security Features

### Authentication
- **Password-based**: Email/password login with BCrypt hashing
- **Biometric**: Fingerprint-based login option
- **Session Management**: Server-side session with configurable timeout

### Authorization
- **Role-based Access Control**: Custom `RoleAuthorizeAttribute`
- **Session Validation**: User ID and role stored in session

### Rate Limiting
- **Login Protection**: 5 attempts per minute per IP
- **Prevents Brute Force**: Fixed window rate limiter

### Audit Trail
- **Authentication Audit**: All login/logout events
- **Administration Audit**: All admin actions
- **Faculty Actions**: Attendance modifications
- **Scan Logs**: All fingerprint scan attempts

### Data Protection
- **Password Hashing**: BCrypt with salt
- **Sensitive Data Masking**: Request logging masks passwords/tokens
- **Input Validation**: Model validation attributes on all entities

---

## ⚙️ Configuration

### appsettings.json
```json
{
  "AttendanceSettings": {
    "FingerprintMatchThreshold": 50,
    "LateGracePeriodMinutes": 15,
    "SuccessLedDurationMs": 300,
    "ErrorLedDurationMs": 500,
    "RoleViolationLedDurationMs": 1000,
    "DeviceResetDelayMs": 500,
    "MaxCaptureRetries": 3,
    "CaptureRetryDelayMs": 300,
    "TemplateBufferSize": 2048,
    "MinTemplateSize": 100,
    "CaptureTimeoutSeconds": 10
  }
}
```

### Environment Variables (.env)
```
DB_HOST=localhost
DB_PORT=5432
DB_NAME=fingerprint_attendance_db
DB_USER=postgres
DB_PASS=your_password
```

---

## 📁 Project Structure

```
Fingerprint_Attendance_System/
├── Attributes/
│   └── RoleAuthorizeAttribute.cs       # Custom authorization
├── Controllers/
│   ├── Admin/                          # 20 admin controllers
│   ├── API/                            # REST API controllers
│   ├── Auth/                           # Authentication controllers
│   ├── Faculty/                        # 6 faculty controllers
│   ├── Home/                           # Home/landing page
│   ├── Student/                        # 7 student controllers
│   ├── AttendanceController.cs         # API attendance
│   ├── AttendanceScanController.cs     # MVC attendance
│   ├── HealthController.cs             # Health checks
│   └── ...
├── Data/
│   └── DatabaseHelper.cs               # Dapper wrapper
├── Exceptions/
│   └── AttendanceExceptions.cs         # Custom exceptions
├── Hubs/
│   └── AttendanceHub.cs                # SignalR hub
├── Middleware/
│   ├── GlobalExceptionHandlerMiddleware.cs
│   └── RequestResponseLoggingMiddleware.cs
├── Models/
│   ├── DTOs/                           # 35+ Data Transfer Objects
│   ├── Enums/
│   │   └── UserRolesEnum.cs
│   └── [27 entity models]
├── Repositories/
│   ├── Implementations/                # 23 repository implementations
│   └── Interfaces/                     # 25 repository interfaces
├── Schema/
│   └── FingerprintAttendanceDB.sql     # Database schema
├── Services/
│   ├── Interfaces/
│   │   ├── IFingerprintService.cs
│   │   └── IFingerprintMatchingService.cs
│   └── [16 service classes]
├── Views/
│   ├── Admin/                          # 17 admin views
│   ├── Auth/                           # 4 auth views
│   ├── Faculty/                        # 5 faculty views
│   ├── Home/                           # 1 home view
│   ├── Shared/                         # 14 shared partials/layouts
│   └── Student/                        # 7 student views
├── wwwroot/
│   ├── css/                            # Stylesheets
│   ├── js/                             # JavaScript files
│   ├── images/                         # Static images
│   └── lib/                            # Third-party libraries
├── Program.cs                          # Application entry point
├── appsettings.json                    # Configuration
└── Fingerprint_Attendance_System.csproj
```

---

## 📊 Statistics

| Category | Count |
|----------|-------|
| Controllers | 35+ |
| Models | 27 |
| DTOs | 35+ |
| Repositories | 23 |
| Services | 16 |
| Views | 48+ |
| Database Tables | 20+ |
| API Endpoints | 50+ |

---

## 🚀 Getting Started

### Prerequisites
1. .NET 9.0 SDK
2. PostgreSQL 14+
3. ZKFinger SDK (libzkfpcsharp.dll)
4. Node.js (for TypeScript compilation)

### Setup Steps
1. Clone repository
2. Create PostgreSQL database
3. Run `Schema/FingerprintAttendanceDB.sql`
4. Configure `.env` with database credentials
5. Run `dotnet restore`
6. Run `dotnet run`
7. Access at `https://localhost:7048`

### Default Admin Account
- Created automatically on first run via `SuperAdminInitializer`
- Check logs for credentials or configure in service

---

*Documentation generated for Fingerprint Attendance System v1.0.0*
*Last updated: November 2025*
