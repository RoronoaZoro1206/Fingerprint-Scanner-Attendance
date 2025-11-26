# Fingerprint Attendance System - Quick Reference

## 🚀 Quick Start

### Prerequisites
- .NET 9.0 SDK
- PostgreSQL 14+
- ZKFinger SDK (libzkfpcsharp.dll)

### Setup
```bash
# 1. Clone and navigate
cd Fingerprint_Attendance_System

# 2. Create database
psql -U postgres -c "CREATE DATABASE fingerprint_attendance_db;"
psql -U postgres -d fingerprint_attendance_db -f Schema/FingerprintAttendanceDB.sql

# 3. Configure environment
# Create .env file:
DB_HOST=localhost
DB_PORT=5432
DB_NAME=fingerprint_attendance_db
DB_USER=postgres
DB_PASS=your_password

# 4. Run
dotnet restore
dotnet run

# 5. Access
# https://localhost:7048
```

---

## 📁 Key Files

| File | Purpose |
|------|---------|
| `Program.cs` | Application entry, DI configuration |
| `appsettings.json` | Application settings |
| `.env` | Database credentials |
| `Schema/FingerprintAttendanceDB.sql` | Database schema |

---

## 🔑 User Roles

| Role | ID | Access |
|------|-----|--------|
| Admin | 1 | Full system access |
| Faculty | 2 | Class & attendance management |
| Student | 3 | Enrollment & attendance |

---

## 🌐 Key URLs

| URL | Purpose |
|-----|---------|
| `/` | Home/Attendance Scanner |
| `/Auth` | Login page |
| `/Admin` | Admin dashboard |
| `/Faculty` | Faculty dashboard |
| `/Student` | Student dashboard |
| `/health` | System health check |
| `/api/docs` | Swagger API docs (dev) |

---

## 📊 Database Tables

### Core Tables
```
users              → All system users
students           → Student profiles
faculty            → Faculty profiles
administration     → Admin profiles
```

### Academic Tables
```
departments        → Academic departments
programs           → Degree programs
courses            → Subjects/courses
sections           → Class sections
academic_terms     → Semesters
rooms              → Classrooms
classes            → Course instances
class_schedules    → Day/time schedules
enrollment         → Student enrollments
```

### Attendance Tables
```
daily_attendance_log    → Daily Time In/Out
attendance              → Class-specific attendance
fingerprint_templates   → Biometric data
scan_logs               → Scan attempts
```

---

## ⚙️ Configuration

### AttendanceSettings (appsettings.json)
```json
{
  "AttendanceSettings": {
    "FingerprintMatchThreshold": 50,
    "LateGracePeriodMinutes": 15,
    "MaxCaptureRetries": 3,
    "CaptureTimeoutSeconds": 10
  }
}
```

---

## 🔧 Common Commands

### Build & Run
```bash
dotnet build
dotnet run
dotnet watch run  # Hot reload
```

### Database
```bash
# Connect to database
psql -U postgres -d fingerprint_attendance_db

# Run migrations
psql -U postgres -d fingerprint_attendance_db -f Schema/migrations/xxx.sql
```

### Publish
```bash
dotnet publish -c Release -o ./publish
```

---

## 🐛 Troubleshooting

### Scanner Not Detected
1. Check USB connection
2. Verify libzkfpcsharp.dll in project root
3. Check `/health/scanner` endpoint
4. Restart application

### Database Connection Failed
1. Verify PostgreSQL is running
2. Check .env credentials
3. Test connection: `psql -U postgres -d fingerprint_attendance_db`
4. Check `/health/database` endpoint

### Fingerprint Not Recognized
1. Verify fingerprint is registered
2. Check match threshold (default: 50)
3. Clean scanner surface
4. Re-register fingerprint

---

## 📝 Logging

### Log Levels
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

### View Logs
- Console output during development
- Check `RequestResponseLoggingMiddleware` for request logs
- Check `scan_logs` table for scan attempts

---

## 🔐 Security Checklist

- [ ] Change default admin password
- [ ] Configure HTTPS in production
- [ ] Set secure session timeout
- [ ] Enable rate limiting
- [ ] Review audit logs regularly
- [ ] Backup fingerprint templates

---

## 📞 Support

### Health Endpoints
- `/health` - Full system status
- `/health/live` - Liveness probe
- `/health/ready` - Readiness probe
- `/health/scanner` - Scanner status
- `/health/database` - Database status

### Audit Tables
- `authentication_audit` - Login/logout events
- `administration_audit` - Admin actions
- `scan_logs` - All scan attempts
- `faculty_attendance_actions` - Attendance modifications

---

*Quick Reference for Fingerprint Attendance System v1.0.0*
