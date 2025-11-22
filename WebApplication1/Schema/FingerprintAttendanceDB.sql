-- ============================================================
-- EXTENSIONS
-- ============================================================

CREATE EXTENSION IF NOT EXISTS citext;

-- ============================================================
-- ROLES & USERS
-- ============================================================



CREATE TABLE user_roles (
                            role_id     SERIAL PRIMARY KEY,
                            role_name   VARCHAR(30) UNIQUE NOT NULL            -- 'admin', 'faculty', 'student'
);

CREATE TABLE users (
                       user_id             SERIAL PRIMARY KEY,
                       user_profile_image  VARCHAR(255) DEFAULT 'default_profile.png',
                       user_first_name     VARCHAR(50) NOT NULL,
                       user_middle_name    VARCHAR(50),
                       user_last_name      VARCHAR(50) NOT NULL,
                       user_gender         VARCHAR(10),
                       user_phone          VARCHAR(20),
                       user_address        TEXT,
                       user_birthdate      DATE,
                       user_email          CITEXT UNIQUE NOT NULL,
                       user_password       TEXT NOT NULL,
                       role_id             INT NOT NULL REFERENCES user_roles(role_id) ON DELETE RESTRICT,
                       user_created_at     TIMESTAMP NOT NULL DEFAULT NOW(),
                       user_updated_at     TIMESTAMP NOT NULL DEFAULT NOW(),
                       user_last_login     TIMESTAMP,
                       is_active           BOOLEAN NOT NULL DEFAULT TRUE
);


-- ============================================================
-- ADMINISTRATION (ADMINS)
-- ============================================================

CREATE TABLE administration (
                                admin_id         SERIAL PRIMARY KEY,
                                user_id          INT UNIQUE REFERENCES users(user_id) ON DELETE CASCADE,
                                admin_code       VARCHAR(20) UNIQUE,
                                admin_created_at TIMESTAMP NOT NULL DEFAULT NOW()
);

CREATE TABLE administration_audit (
                                      aud_id           SERIAL PRIMARY KEY,
                                      admin_id         INT NOT NULL REFERENCES administration(admin_id) ON DELETE CASCADE,
                                      aud_action       VARCHAR(100) NOT NULL,
                                      aud_details      TEXT,
                                      aud_action_time  TIMESTAMP NOT NULL DEFAULT NOW()
);


-- ============================================================
-- DEPARTMENT
-- ============================================================

CREATE TABLE departments (
                             dept_id          SERIAL PRIMARY KEY,
                             dept_code        VARCHAR(20) UNIQUE NOT NULL,
                             dept_name        VARCHAR(100) NOT NULL,
                             dept_desc        TEXT,
                             dept_created_at  TIMESTAMP NOT NULL DEFAULT NOW()
);


-- ============================================================
-- FACULTY
-- ============================================================

CREATE TABLE faculty (
                         fac_id           SERIAL PRIMARY KEY,
                         user_id          INT UNIQUE REFERENCES users(user_id) ON DELETE CASCADE,
                         dept_id          INT REFERENCES departments(dept_id),
                         fac_rank         VARCHAR(50),
                         fac_created_at   TIMESTAMP NOT NULL DEFAULT NOW()
);


-- ============================================================
-- PROGRAMS & COURSES
-- ============================================================

CREATE TABLE programs (
                          program_id         SERIAL PRIMARY KEY,
                          program_code       VARCHAR(20) UNIQUE NOT NULL,      -- e.g. BSIT, BSHM
                          program_name       VARCHAR(100) NOT NULL,            -- full name of program
                          program_desc       TEXT,
                          dept_id            INT REFERENCES departments(dept_id),
                          program_created_at TIMESTAMP NOT NULL DEFAULT NOW()
);

CREATE TABLE courses (
                         course_id         SERIAL PRIMARY KEY,
                         course_code       VARCHAR(20) UNIQUE NOT NULL,       -- e.g. IT101, MATH001
                         course_name       VARCHAR(100) NOT NULL,
                         course_desc       TEXT,
                         course_units      INT NOT NULL DEFAULT 3,
                         program_id        INT REFERENCES programs(program_id) ON DELETE SET NULL,
                         course_created_at TIMESTAMP NOT NULL DEFAULT NOW()
);


-- ============================================================
-- STUDENT ID POOL (Pre-generated Student IDs by Admin)
-- ============================================================

CREATE TABLE student_id_pool (
                                 pool_id          SERIAL PRIMARY KEY,
                                 student_id_code  VARCHAR(20) UNIQUE NOT NULL,        -- e.g. 2025-0156
                                 is_used          BOOLEAN NOT NULL DEFAULT FALSE,
                                 generated_by     INT REFERENCES administration(admin_id) ON DELETE SET NULL,
                                 generated_at     TIMESTAMP NOT NULL DEFAULT NOW(),
                                 used_at          TIMESTAMP,
                                 remarks          TEXT
);


-- ============================================================
-- SECTION
-- ============================================================

CREATE TABLE sections (
                          section_id         SERIAL PRIMARY KEY,
                          section_name       VARCHAR(50) UNIQUE NOT NULL,
                          program_id         INT REFERENCES programs(program_id),
                          year_level         INT,
                          section_created_at TIMESTAMP DEFAULT NOW()
);


-- ============================================================
-- STUDENTS
-- ============================================================

CREATE TABLE students (
                          stud_id           SERIAL PRIMARY KEY,
                          user_id           INT UNIQUE REFERENCES users(user_id) ON DELETE CASCADE,
                          stud_number       VARCHAR(20) UNIQUE NOT NULL,
                          program_id        INT REFERENCES programs(program_id),
                          stud_year_level   INT,
                          section_id        INT REFERENCES sections(section_id),
                          profile_submitted BOOLEAN NOT NULL DEFAULT FALSE,
                          profile_submitted_at TIMESTAMP,
                          stud_created_at   TIMESTAMP NOT NULL DEFAULT NOW()
);


-- ============================================================
-- ACADEMIC TERM
-- ============================================================

CREATE TABLE academic_terms (
                                term_id       SERIAL PRIMARY KEY,
                                term_name     VARCHAR(50) NOT NULL,                  -- e.g. "1st Semester AY 2025-2026"
                                start_date    DATE NOT NULL,
                                end_date      DATE NOT NULL,
                                is_active     BOOLEAN DEFAULT FALSE
);


-- ============================================================
-- ROOMS & CLASSES
-- ============================================================

CREATE TABLE rooms (
                       room_id            SERIAL PRIMARY KEY,
                       room_code          VARCHAR(20) UNIQUE NOT NULL,
                       room_description   VARCHAR(100)
);

CREATE TABLE classes (
                         class_id           SERIAL PRIMARY KEY,
                         course_id          INT REFERENCES courses(course_id) ON DELETE CASCADE,
                         room_id            INT REFERENCES rooms(room_id) ON DELETE CASCADE,
                         fac_id             INT REFERENCES faculty(fac_id) ON DELETE SET NULL,
                         term_id            INT REFERENCES academic_terms(term_id),
                         class_created_at   TIMESTAMP NOT NULL DEFAULT NOW(),
                         class_updated_at   TIMESTAMP NOT NULL DEFAULT NOW()
);

CREATE TABLE class_schedules (
                                 sched_id           SERIAL PRIMARY KEY,
                                 class_id           INT REFERENCES classes(class_id) ON DELETE CASCADE,
                                 sched_day_of_week  VARCHAR(10) NOT NULL,
                                 sched_start_time   TIME NOT NULL,
                                 sched_end_time     TIME NOT NULL
);

-- ============================================================
-- AUTHENTICATION AUDIT TABLE (UNIVERSAL)
-- Stores ALL login/logout events for ALL user roles
-- ============================================================

CREATE TABLE IF NOT EXISTS authentication_audit (
    auth_audit_id       SERIAL PRIMARY KEY,
    user_id             INT REFERENCES users(user_id) ON DELETE CASCADE,
    auth_action         VARCHAR(100) NOT NULL,           -- 'Password Login', 'Biometric Login', 'Failed Login', 'Logout'
    auth_details        TEXT,                            -- Additional details (email, role, IP, etc.)
    auth_action_time    TIMESTAMP NOT NULL DEFAULT NOW(),
    auth_ip_address     VARCHAR(45),                     -- Optional: Store IP for security
    auth_user_agent     TEXT                             -- Optional: Store browser info
);


-- ============================================================
-- ENROLLMENT
-- ============================================================

CREATE TABLE enrollment (
                            enroll_id      SERIAL PRIMARY KEY,
                            stud_id        INT REFERENCES students(stud_id) ON DELETE CASCADE,
                            class_id       INT REFERENCES classes(class_id) ON DELETE CASCADE,
                            enroll_at      TIMESTAMP NOT NULL DEFAULT NOW(),
                            UNIQUE (stud_id, class_id)
);


-- ============================================================
-- BIOMETRIC & ATTENDANCE
-- ============================================================

DO $$
    BEGIN
        CREATE TYPE attendance_status AS ENUM ('pending', 'present', 'late', 'absent', 'excused');
    EXCEPTION
        WHEN duplicate_object THEN NULL;
    END $$;

DO $$
    BEGIN
        CREATE TYPE scan_type_enum AS ENUM ('in', 'out');
    EXCEPTION
        WHEN duplicate_object THEN NULL;
    END $$;

CREATE TABLE fingerprint_templates (
                                       ftemp_id          SERIAL PRIMARY KEY,
                                       user_id           INT UNIQUE REFERENCES users(user_id) ON DELETE CASCADE,
                                       ftemp_data        BYTEA NOT NULL,
                                       ftemp_name        VARCHAR(20),
                                       ftemp_created_at  TIMESTAMP NOT NULL DEFAULT NOW()
);

CREATE TABLE attendance (
                            attend_id         SERIAL PRIMARY KEY,
                            class_id          INT REFERENCES classes(class_id) ON DELETE CASCADE,
                            stud_id           INT REFERENCES students(stud_id) ON DELETE CASCADE,
                            attend_date       DATE NOT NULL DEFAULT CURRENT_DATE,
                            attend_scan_time  TIMESTAMP NOT NULL DEFAULT NOW(),
                            attend_type       scan_type_enum NOT NULL,
                            attend_status     attendance_status NOT NULL DEFAULT 'pending',
                            attend_remark     TEXT,
                            attend_created_at TIMESTAMP NOT NULL DEFAULT NOW(),
                            UNIQUE (class_id, stud_id, attend_scan_time, attend_type)
);

CREATE TABLE scan_logs (
                           scan_id           SERIAL PRIMARY KEY,
                           user_id           INT REFERENCES users(user_id) ON DELETE SET NULL,
                           scan_time         TIMESTAMP NOT NULL DEFAULT NOW(),
                           scan_success      BOOLEAN,
                           scan_log_message  TEXT,
                           scan_created_at   TIMESTAMP NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS daily_attendance_log (
    daily_attend_id     SERIAL PRIMARY KEY,
    stud_id             INT NOT NULL REFERENCES students(stud_id) ON DELETE CASCADE,
    attend_date         DATE NOT NULL DEFAULT CURRENT_DATE,
    time_in             TIMESTAMP,
    time_out            TIMESTAMP,
    status              VARCHAR(20) NOT NULL DEFAULT 'active',
                        -- Possible values:
                        -- 'active': Only time_in recorded (student is "in" for the day)
                        -- 'completed': Both time_in and time_out recorded
                        -- 'incomplete': Forgot to time out (flagged next day)
    forgot_timeout      BOOLEAN NOT NULL DEFAULT FALSE,
    remarks             TEXT,
    created_at          TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at          TIMESTAMP NOT NULL DEFAULT NOW(),
    
    -- Business constraint: One record per student per date
    CONSTRAINT daily_attendance_unique_student_date UNIQUE (stud_id, attend_date),
    
    -- Business rule: time_out must be after time_in
    CONSTRAINT daily_attendance_time_check CHECK (time_out IS NULL OR time_out > time_in)
);


CREATE TABLE IF NOT EXISTS student_excuse_requests (
    excuse_id           SERIAL PRIMARY KEY,
    stud_id             INT NOT NULL REFERENCES students(stud_id) ON DELETE CASCADE,
    class_id            INT NOT NULL REFERENCES classes(class_id) ON DELETE CASCADE,
    attend_date         DATE NOT NULL,
    excuse_type         VARCHAR(50) NOT NULL,  -- 'sick', 'emergency', 'school_activity', 'other'
    excuse_reason       TEXT NOT NULL,
    attachment_path     VARCHAR(255),          -- Path to uploaded document (medical cert, etc.)
    status              VARCHAR(20) NOT NULL DEFAULT 'pending',  -- 'pending', 'approved', 'rejected'
    submitted_at        TIMESTAMP NOT NULL DEFAULT NOW(),
    reviewed_by         INT REFERENCES faculty(fac_id),
    reviewed_at         TIMESTAMP,
    review_notes        TEXT,
    created_at          TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at          TIMESTAMP NOT NULL DEFAULT NOW(),
    
    CONSTRAINT valid_excuse_type CHECK (excuse_type IN ('sick', 'emergency', 'school_activity', 'family', 'other')),
    CONSTRAINT valid_status CHECK (status IN ('pending', 'approved', 'rejected'))
);

-- Indexes for performance
CREATE INDEX IF NOT EXISTS idx_excuse_stud_id ON student_excuse_requests(stud_id);
CREATE INDEX IF NOT EXISTS idx_excuse_class_id ON student_excuse_requests(class_id);
CREATE INDEX IF NOT EXISTS idx_excuse_status ON student_excuse_requests(status);
CREATE INDEX IF NOT EXISTS idx_excuse_date ON student_excuse_requests(attend_date);
CREATE INDEX IF NOT EXISTS idx_excuse_reviewed_by ON student_excuse_requests(reviewed_by);

-- Comments
COMMENT ON TABLE student_excuse_requests IS 'Student-submitted excuse requests for absences or late arrivals';
COMMENT ON COLUMN student_excuse_requests.excuse_type IS 'Type of excuse: sick, emergency, school_activity, family, other';
COMMENT ON COLUMN student_excuse_requests.status IS 'Request status: pending (awaiting review), approved (excused), rejected';
COMMENT ON COLUMN student_excuse_requests.attachment_path IS 'Path to uploaded medical certificate or supporting document';


CREATE TABLE IF NOT EXISTS faculty_attendance_actions (
    action_id           SERIAL PRIMARY KEY,
    fac_id              INT NOT NULL REFERENCES faculty(fac_id) ON DELETE CASCADE,
    attend_id           INT REFERENCES attendance(attend_id) ON DELETE SET NULL,
    action_type         VARCHAR(50) NOT NULL,  -- 'excuse', 'change_status', 'add_manual', 'approve_request', 'reject_request'
    old_status          VARCHAR(20),
    new_status          VARCHAR(20),
    reason              TEXT NOT NULL,
    metadata            JSONB,                 -- Additional data (request_id, original_time, etc.)
    action_timestamp    TIMESTAMP NOT NULL DEFAULT NOW()
);

-- Indexes
CREATE INDEX IF NOT EXISTS idx_faculty_actions_fac_id ON faculty_attendance_actions(fac_id);
CREATE INDEX IF NOT EXISTS idx_faculty_actions_attend_id ON faculty_attendance_actions(attend_id);
CREATE INDEX IF NOT EXISTS idx_faculty_actions_timestamp ON faculty_attendance_actions(action_timestamp);
CREATE INDEX IF NOT EXISTS idx_faculty_actions_type ON faculty_attendance_actions(action_type);

-- Comments
COMMENT ON TABLE faculty_attendance_actions IS 'Complete audit trail of all faculty modifications to attendance records';
COMMENT ON COLUMN faculty_attendance_actions.action_type IS 'Type of action: excuse, change_status, add_manual, approve_request, reject_request';
COMMENT ON COLUMN faculty_attendance_actions.metadata IS 'JSON field for additional context (request_id, student_count for bulk, etc.)';


CREATE TABLE IF NOT EXISTS attendance_notifications (
    notification_id     SERIAL PRIMARY KEY,
    user_id             INT NOT NULL REFERENCES users(user_id) ON DELETE CASCADE,
    notification_type   VARCHAR(50) NOT NULL,  -- 'excuse_approved', 'excuse_rejected', 'attendance_changed', 'excuse_pending'
    title               VARCHAR(200) NOT NULL,
    message             TEXT NOT NULL,
    related_entity_type VARCHAR(50),           -- 'attendance', 'excuse_request'
    related_entity_id   INT,
    is_read             BOOLEAN NOT NULL DEFAULT FALSE,
    created_at          TIMESTAMP NOT NULL DEFAULT NOW()
);

-- Indexes
CREATE INDEX IF NOT EXISTS idx_notifications_user_id ON attendance_notifications(user_id);
CREATE INDEX IF NOT EXISTS idx_notifications_is_read ON attendance_notifications(is_read) WHERE is_read = FALSE;
CREATE INDEX IF NOT EXISTS idx_notifications_created_at ON attendance_notifications(created_at DESC);
CREATE INDEX IF NOT EXISTS idx_notifications_type ON attendance_notifications(notification_type);

-- Comments
COMMENT ON TABLE attendance_notifications IS 'User notifications for attendance-related events';
COMMENT ON COLUMN attendance_notifications.related_entity_type IS 'Type of entity: attendance, excuse_request';
-- ============================================================
-- DEFAULT ROLES
-- ============================================================

INSERT INTO user_roles (role_name)
VALUES ('admin'), ('faculty'), ('student')
ON CONFLICT (role_name) DO NOTHING;


-- ============================================================
-- INDEXES
-- ============================================================

-- Users table
CREATE INDEX idx_users_role_id              ON users(role_id);
CREATE INDEX idx_users_email                ON users(user_email);

-- Students table
CREATE INDEX idx_students_stud_number       ON students(stud_number);

-- Enrollment table
CREATE INDEX idx_enroll_stud_id             ON enrollment(stud_id);
CREATE INDEX idx_enroll_class_id            ON enrollment(class_id);

-- Attendance table
CREATE INDEX idx_attend_class_id            ON attendance(class_id);
CREATE INDEX idx_attend_stud_id             ON attendance(stud_id);
CREATE INDEX idx_attend_scan_time           ON attendance(attend_scan_time);

-- Classes table
CREATE INDEX idx_class_fac_id               ON classes(fac_id);
CREATE INDEX idx_class_room_id              ON classes(room_id);

-- Scan logs table
CREATE INDEX idx_scan_logs_time             ON scan_logs(scan_time);

-- Fingerprint templates table
CREATE INDEX idx_fingerprint_user_id ON fingerprint_templates(user_id);

-- Improve JOIN performance
CREATE INDEX idx_classes_course_id          ON classes(course_id);
CREATE INDEX idx_classes_term_id            ON classes(term_id);
CREATE INDEX idx_courses_program_id         ON courses(program_id);
CREATE INDEX idx_students_program_id        ON students(program_id);
CREATE INDEX idx_students_user_id           ON students(user_id);
CREATE INDEX idx_faculty_user_id            ON faculty(user_id);
CREATE INDEX idx_class_schedules_class_id   ON class_schedules(class_id);

-- Improve date-based queries
CREATE INDEX idx_attendance_date            ON attendance(attend_date);
CREATE INDEX idx_attendance_class_date      ON attendance(class_id, attend_date);
CREATE INDEX idx_attendance_stud_date       ON attendance(stud_id, attend_date);


-- Index for performance
CREATE INDEX idx_auth_audit_user_id ON authentication_audit(user_id);
CREATE INDEX idx_auth_audit_action ON authentication_audit(auth_action);
CREATE INDEX idx_auth_audit_time ON authentication_audit(auth_action_time);

-- Comment 
COMMENT ON TABLE authentication_audit IS 'Universal audit log for ALL authentication events (password login, biometric login, logout) across all user roles (Admin, Faculty, Student)';


-- Primary query patterns
CREATE INDEX idx_daily_attend_stud_date ON daily_attendance_log(stud_id, attend_date);
CREATE INDEX idx_daily_attend_date ON daily_attendance_log(attend_date);
CREATE INDEX idx_daily_attend_status ON daily_attendance_log(status);

-- Specialized index for finding incomplete records
CREATE INDEX idx_daily_attend_forgot ON daily_attendance_log(forgot_timeout) 
WHERE forgot_timeout = TRUE;

-- Performance index for today's active students
CREATE INDEX idx_daily_attend_today_active ON daily_attendance_log(attend_date, status)
WHERE status = 'active';

-- ============================================================
-- TRIGGERS
-- ============================================================

-- Mark student ID as used when assigned
CREATE OR REPLACE FUNCTION mark_student_id_used()
    RETURNS TRIGGER AS $$
BEGIN
    UPDATE student_id_pool
    SET is_used = TRUE,
        used_at = NOW()
    WHERE student_id_code = NEW.stud_number;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_mark_student_id_used
    AFTER INSERT ON students
    FOR EACH ROW
EXECUTE FUNCTION mark_student_id_used();

-- ✅ NEW TRIGGER: Automatically mark student IDs as used when students are created
-- This provides a database-level guarantee that pool status stays synchronized
-- NOTE: Remarks are managed by application code to distinguish manual vs auto assignment
CREATE OR REPLACE FUNCTION auto_mark_student_id_used()
RETURNS TRIGGER AS $$
BEGIN
    -- Mark the student ID as used in the pool
    UPDATE student_id_pool
    SET 
        is_used = TRUE,
        used_at = NOW()
    WHERE student_id_code = NEW.stud_number
      AND is_used = FALSE;
    
    -- Log if student ID was not found in pool
    IF NOT FOUND THEN
        RAISE WARNING 'Student ID % was not found in student_id_pool or is already used', NEW.stud_number;
    END IF;
    
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Drop old trigger if it exists and create new one
DROP TRIGGER IF EXISTS trg_mark_student_id_used ON students;
CREATE TRIGGER trg_mark_student_id_used
    AFTER INSERT ON students
    FOR EACH ROW
EXECUTE FUNCTION auto_mark_student_id_used();

-- ✅ NEW TRIGGER: Automatically release student IDs back to pool when students are deleted
-- NOTE: Remarks are managed by application code to provide context for the release
CREATE OR REPLACE FUNCTION auto_release_student_id()
RETURNS TRIGGER AS $$
BEGIN
    -- Release the student ID back to the pool
    UPDATE student_id_pool
    SET 
        is_used = FALSE,
        used_at = NULL
    WHERE student_id_code = OLD.stud_number
      AND is_used = TRUE;
    
    -- Log if student ID was not found in pool
    IF NOT FOUND THEN
        RAISE WARNING 'Student ID % was not found in student_id_pool or is not marked as used', OLD.stud_number;
    END IF;
    
    RETURN OLD;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_release_student_id
    AFTER DELETE ON students
    FOR EACH ROW
EXECUTE FUNCTION auto_release_student_id();

-- ✅ NEW TRIGGER: Handle student number changes (release old, mark new)
-- NOTE: Remarks are managed by application code to provide context for the reassignment
CREATE OR REPLACE FUNCTION auto_handle_student_id_change()
RETURNS TRIGGER AS $$
BEGIN
    -- Only process if student number actually changed
    IF OLD.stud_number != NEW.stud_number THEN
        -- Release old student ID back to pool
        UPDATE student_id_pool
        SET 
            is_used = FALSE,
            used_at = NULL
        WHERE student_id_code = OLD.stud_number
          AND is_used = TRUE;
        
        -- Mark new student ID as used
        UPDATE student_id_pool
        SET 
            is_used = TRUE,
            used_at = NOW()
        WHERE student_id_code = NEW.stud_number
          AND is_used = FALSE;
        
        -- Log the change
        RAISE NOTICE 'Student ID changed from % to % for student %', OLD.stud_number, NEW.stud_number, NEW.stud_id;
    END IF;
    
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_handle_student_id_change
    AFTER UPDATE ON students
    FOR EACH ROW
EXECUTE FUNCTION auto_handle_student_id_change();


-- ============================================================
--              CREATE SUPPORTING VIEWS
-- ============================================================


-- View 1: Student Daily Schedule
CREATE OR REPLACE VIEW v_student_daily_schedule AS
SELECT 
    e.stud_id,
    s.user_id,
    s.stud_number,
    u.user_first_name,
    u.user_last_name,
    u.user_email,
    c.class_id,
    co.course_id,
    co.course_code,
    co.course_name,
    co.course_units,
    cs.sched_day_of_week,
    cs.sched_start_time,
    cs.sched_end_time,
    r.room_code,
    r.room_description,
    f.fac_id,
    fu.user_first_name as faculty_first_name,
    fu.user_last_name as faculty_last_name,
    at.term_id,
    at.term_name,
    at.is_active as term_is_active
FROM enrollment e
JOIN classes c ON e.class_id = c.class_id
JOIN courses co ON c.course_id = co.course_id
JOIN class_schedules cs ON c.class_id = cs.class_id
LEFT JOIN rooms r ON c.room_id = r.room_id
LEFT JOIN faculty f ON c.fac_id = f.fac_id
LEFT JOIN users fu ON f.user_id = fu.user_id
JOIN students s ON e.stud_id = s.stud_id
JOIN users u ON s.user_id = u.user_id
LEFT JOIN academic_terms at ON c.term_id = at.term_id
WHERE at.is_active = TRUE OR at.term_id IS NULL;

COMMENT ON VIEW v_student_daily_schedule IS 
'Shows all classes a student is enrolled in with full schedule details for the active term';

-- View 2: Today's Attendance Summary
CREATE OR REPLACE VIEW v_today_attendance_summary AS
SELECT 
    dal.daily_attend_id,
    dal.stud_id,
    s.stud_number,
    u.user_first_name,
    u.user_last_name,
    u.user_email,
    dal.attend_date,
    dal.time_in,
    dal.time_out,
    dal.status,
    dal.forgot_timeout,
    -- Count how many classes have attendance marked
    COUNT(a.attend_id) as classes_marked,
    -- Count status types
    COUNT(CASE WHEN a.attend_status = 'late' THEN 1 END) as late_count,
    COUNT(CASE WHEN a.attend_status = 'absent' THEN 1 END) as absent_count,
    COUNT(CASE WHEN a.attend_status = 'present' THEN 1 END) as present_count,
    COUNT(CASE WHEN a.attend_status = 'excused' THEN 1 END) as excused_count,
    -- Total enrolled classes for today
    (SELECT COUNT(*) 
     FROM enrollment e 
     JOIN classes c ON e.class_id = c.class_id 
     JOIN class_schedules cs ON c.class_id = cs.class_id
     WHERE e.stud_id = dal.stud_id 
     AND cs.sched_day_of_week = TO_CHAR(dal.attend_date, 'Day')
    ) as total_classes_today
FROM daily_attendance_log dal
JOIN students s ON dal.stud_id = s.stud_id
JOIN users u ON s.user_id = u.user_id
LEFT JOIN attendance a ON dal.stud_id = a.stud_id 
    AND dal.attend_date = a.attend_date
WHERE dal.attend_date = CURRENT_DATE
GROUP BY 
    dal.daily_attend_id, dal.stud_id, s.stud_number, 
    u.user_first_name, u.user_last_name, u.user_email,
    dal.attend_date, dal.time_in, dal.time_out, 
    dal.status, dal.forgot_timeout;

COMMENT ON VIEW v_today_attendance_summary IS 
'Complete summary of today''s attendance showing time in/out and class-specific attendance counts';

-- View 3: Student Attendance History
CREATE OR REPLACE VIEW v_student_attendance_history AS
SELECT 
    dal.daily_attend_id,
    dal.stud_id,
    s.stud_number,
    u.user_first_name || ' ' || u.user_last_name as student_name,
    dal.attend_date,
    dal.time_in,
    dal.time_out,
    dal.status,
    dal.forgot_timeout,
    -- Calculate time duration
    CASE 
        WHEN dal.time_out IS NOT NULL THEN 
            EXTRACT(EPOCH FROM (dal.time_out - dal.time_in))/3600
        ELSE NULL
    END as hours_in_school,
    -- Class attendance details
    json_agg(
        json_build_object(
            'class_id', c.class_id,
            'course_code', co.course_code,
            'course_name', co.course_name,
            'status', a.attend_status,
            'remark', a.attend_remark
        )
    ) FILTER (WHERE a.attend_id IS NOT NULL) as class_details
FROM daily_attendance_log dal
JOIN students s ON dal.stud_id = s.stud_id
JOIN users u ON s.user_id = u.user_id
LEFT JOIN attendance a ON dal.stud_id = a.stud_id 
    AND dal.attend_date = a.attend_date
LEFT JOIN classes c ON a.class_id = c.class_id
LEFT JOIN courses co ON c.course_id = co.course_id
GROUP BY 
    dal.daily_attend_id, dal.stud_id, s.stud_number,
    u.user_first_name, u.user_last_name,
    dal.attend_date, dal.time_in, dal.time_out,
    dal.status, dal.forgot_timeout
ORDER BY dal.attend_date DESC;

COMMENT ON VIEW v_student_attendance_history IS 
'Complete attendance history for each student with class-level details as JSON';

-- View: Pending excuse requests with student/class details
CREATE OR REPLACE VIEW v_pending_excuse_requests AS
SELECT 
    ser.excuse_id,
    ser.stud_id,
    s.stud_number,
    u.user_first_name || ' ' || u.user_last_name as student_name,
    ser.class_id,
    c.course_id,
    co.course_code,
    co.course_name,
    ser.attend_date,
    ser.excuse_type,
    ser.excuse_reason,
    ser.attachment_path,
    ser.status,
    ser.submitted_at,
    ser.reviewed_by,
    ser.reviewed_at,
    ser.review_notes,
    CASE WHEN ser.attachment_path IS NOT NULL THEN TRUE ELSE FALSE END as has_attachment
FROM student_excuse_requests ser
INNER JOIN students s ON ser.stud_id = s.stud_id
INNER JOIN users u ON s.user_id = u.user_id
INNER JOIN classes c ON ser.class_id = c.class_id
INNER JOIN courses co ON c.course_id = co.course_id
WHERE ser.status = 'pending'
ORDER BY ser.submitted_at ASC;

COMMENT ON VIEW v_pending_excuse_requests IS 'All pending excuse requests with student and class details for faculty review';

-- View: Faculty action history
CREATE OR REPLACE VIEW v_faculty_action_history AS
SELECT 
    faa.action_id,
    faa.fac_id,
    fu.user_first_name || ' ' || fu.user_last_name as faculty_name,
    faa.attend_id,
    faa.action_type,
    faa.old_status,
    faa.new_status,
    faa.reason,
    faa.metadata,
    faa.action_timestamp,
    CASE 
        WHEN faa.attend_id IS NOT NULL THEN
            (SELECT s.stud_number FROM attendance a 
             INNER JOIN students s ON a.stud_id = s.stud_id 
             WHERE a.attend_id = faa.attend_id)
        ELSE NULL
    END as student_number
FROM faculty_attendance_actions faa
INNER JOIN faculty f ON faa.fac_id = f.fac_id
INNER JOIN users fu ON f.user_id = fu.user_id
ORDER BY faa.action_timestamp DESC;

COMMENT ON VIEW v_faculty_action_history IS 'Complete audit history of faculty attendance modifications';

-- This ensures existing students with academic info are also locked
UPDATE students 
SET profile_submitted = TRUE, 
    profile_submitted_at = stud_created_at 
WHERE program_id IS NOT NULL 
  AND profile_submitted = FALSE;

-- Ensure action_type is valid
ALTER TABLE faculty_attendance_actions 
DROP CONSTRAINT IF EXISTS valid_action_type;

ALTER TABLE faculty_attendance_actions
ADD CONSTRAINT valid_action_type 
CHECK (action_type IN ('excuse', 'change_status', 'add_manual', 'approve_request', 'reject_request', 'bulk_excuse'));

-- Add class_id column to daily_attendance_log
ALTER TABLE daily_attendance_log 
ADD COLUMN IF NOT EXISTS class_id INT REFERENCES classes(class_id) ON DELETE SET NULL;

-- Add index for performance
CREATE INDEX IF NOT EXISTS idx_daily_attend_class_id ON daily_attendance_log(class_id);

-- Update constraint to prevent duplicate time-ins for same class
-- (Currently allows one record per student per date, but should be per student per class per date)
ALTER TABLE daily_attendance_log 
DROP CONSTRAINT IF EXISTS daily_attendance_unique_student_date;

ALTER TABLE daily_attendance_log 
ADD CONSTRAINT daily_attendance_unique_student_class_date 
UNIQUE (stud_id, class_id, attend_date);
-- ============================================================
-- COMPLETE DATABASE TABLE QUERIES
-- ============================================================

-- CORE USER & ROLE TABLES
SELECT * FROM user_roles ORDER BY role_id;
SELECT * FROM users ORDER BY user_id;
SELECT * FROM authentication_audit ORDER BY auth_action_time DESC;

-- ADMINISTRATION TABLES
SELECT * FROM administration ORDER BY admin_id;
SELECT * FROM administration_audit ORDER BY aud_action_time DESC;

-- ACADEMIC STRUCTURE TABLES
SELECT * FROM departments ORDER BY dept_id;
SELECT * FROM programs ORDER BY program_id;
SELECT * FROM courses ORDER BY course_id;
SELECT * FROM sections ORDER BY section_id;
SELECT * FROM rooms ORDER BY room_id;
SELECT * FROM academic_terms ORDER BY term_id;

-- FACULTY TABLES
SELECT * FROM faculty ORDER BY fac_id;

-- STUDENT TABLES
SELECT * FROM students ORDER BY stud_id;
SELECT * FROM student_id_pool ORDER BY pool_id;

-- CLASS & ENROLLMENT TABLES
SELECT * FROM classes ORDER BY class_id;
SELECT * FROM class_schedules ORDER BY sched_id;
SELECT * FROM enrollment ORDER BY enroll_id;


-- ATTENDANCE TABLES
SELECT * FROM attendance ORDER BY attend_id DESC;
SELECT * FROM daily_attendance_log ORDER BY attend_date DESC, daily_attend_id DESC;
SELECT * FROM scan_logs ORDER BY scan_time DESC;
SELECT * FROM student_excuse_requests ORDER BY submitted_at DESC;
SELECT * FROM faculty_attendance_actions ORDER BY action_timestamp DESC;
SELECT * FROM attendance_notifications ORDER BY created_at DESC;

-- BIOMETRIC TABLES
SELECT * FROM fingerprint_templates ORDER BY ftemp_id;

-- ============================================================
-- USEFUL DIAGNOSTIC QUERIES
-- ============================================================

-- Find users WITHOUT student records (THIS IS YOUR PROBLEM!)
SELECT 
    u.user_id,
    u.user_email,
    u.user_first_name,
    u.user_last_name,
    u.role_id,
    ur.role_name,
    CASE WHEN s.stud_id IS NULL THEN '❌ NO STUDENT RECORD' ELSE '✅ Has Student Record' END as status
FROM users u
JOIN user_roles ur ON u.role_id = ur.role_id
LEFT JOIN students s ON u.user_id = s.user_id
WHERE u.role_id = 3  -- Student role
ORDER BY s.stud_id NULLS FIRST;

-- Count records in each table
SELECT 
    'user_roles' as table_name, COUNT(*) as record_count FROM user_roles
UNION ALL SELECT 'users', COUNT(*) FROM users
UNION ALL SELECT 'administration', COUNT(*) FROM administration
UNION ALL SELECT 'departments', COUNT(*) FROM departments
UNION ALL SELECT 'faculty', COUNT(*) FROM faculty
UNION ALL SELECT 'programs', COUNT(*) FROM programs
UNION ALL SELECT 'courses', COUNT(*) FROM courses
UNION ALL SELECT 'sections', COUNT(*) FROM sections
UNION ALL SELECT 'students', COUNT(*) FROM students
UNION ALL SELECT 'student_id_pool', COUNT(*) FROM student_id_pool
UNION ALL SELECT 'rooms', COUNT(*) FROM rooms
UNION ALL SELECT 'classes', COUNT(*) FROM classes
UNION ALL SELECT 'class_schedules', COUNT(*) FROM class_schedules
UNION ALL SELECT 'enrollment', COUNT(*) FROM enrollment
UNION ALL SELECT 'academic_terms', COUNT(*) FROM academic_terms
UNION ALL SELECT 'attendance', COUNT(*) FROM attendance
UNION ALL SELECT 'daily_attendance_log', COUNT(*) FROM daily_attendance_log
UNION ALL SELECT 'scan_logs', COUNT(*) FROM scan_logs
UNION ALL SELECT 'fingerprint_templates', COUNT(*) FROM fingerprint_templates
UNION ALL SELECT 'student_excuse_requests', COUNT(*) FROM student_excuse_requests
UNION ALL SELECT 'faculty_attendance_actions', COUNT(*) FROM faculty_attendance_actions
UNION ALL SELECT 'attendance_notifications', COUNT(*) FROM attendance_notifications
UNION ALL SELECT 'authentication_audit', COUNT(*) FROM authentication_audit
UNION ALL SELECT 'administration_audit', COUNT(*) FROM administration_audit
ORDER BY table_name;

-- View all students with their enrollment status
SELECT 
    s.stud_id,
    s.stud_number,
    u.user_first_name || ' ' || u.user_last_name as full_name,
    u.user_email,
    p.program_name,
    s.stud_year_level,
    sec.section_name,
    s.profile_submitted,
    s.profile_submitted_at,
    COUNT(DISTINCT e.class_id) as enrolled_classes
FROM students s
JOIN users u ON s.user_id = u.user_id
LEFT JOIN programs p ON s.program_id = p.program_id
LEFT JOIN sections sec ON s.section_id = sec.section_id
LEFT JOIN enrollment e ON s.stud_id = e.stud_id
GROUP BY s.stud_id, s.stud_number, u.user_first_name, u.user_last_name, 
         u.user_email, p.program_name, s.stud_year_level, sec.section_name,
         s.profile_submitted, s.profile_submitted_at
ORDER BY s.stud_id;

-- View all classes with enrollment counts
SELECT 
    c.class_id,
    co.course_code,
    co.course_name,
    r.room_code,
    u.user_first_name || ' ' || u.user_last_name as faculty_name,
    at.term_name,
    COUNT(e.enroll_id) as enrolled_students
FROM classes c
JOIN courses co ON c.course_id = co.course_id
LEFT JOIN rooms r ON c.room_id = r.room_id
LEFT JOIN faculty f ON c.fac_id = f.fac_id
LEFT JOIN users u ON f.user_id = u.user_id
LEFT JOIN academic_terms at ON c.term_id = at.term_id
LEFT JOIN enrollment e ON c.class_id = e.class_id
GROUP BY c.class_id, co.course_code, co.course_name, r.room_code,
         u.user_first_name, u.user_last_name, at.term_name
ORDER BY c.class_id;

-- Check for orphaned records (users with student role but no student record)
SELECT 
    'CRITICAL: Students without records' as issue_type,
    COUNT(*) as count
FROM users u
LEFT JOIN students s ON u.user_id = s.user_id
WHERE u.role_id = 3 AND s.stud_id IS NULL
UNION ALL
SELECT 
    'Faculty without records' as issue_type,
    COUNT(*) as count
FROM users u
LEFT JOIN faculty f ON u.user_id = f.user_id
WHERE u.role_id = 2 AND f.fac_id IS NULL
UNION ALL
SELECT 
    'Admins without records' as issue_type,
    COUNT(*) as count
FROM users u
LEFT JOIN administration a ON u.user_id = a.user_id
WHERE u.role_id = 1 AND a.admin_id IS NULL;






SELECT '=== TEST 1: STUDENT VERIFICATION (2025-0001) ===' as test_name;

SELECT 
    s.stud_id,
    s.stud_number,
    CONCAT(u.user_first_name, ' ', COALESCE(u.user_middle_name || ' ', ''), u.user_last_name) as full_name,
    u.user_email,
    u.role_id,
    ur.role_name,
    p.program_code,
    p.program_name,
    s.stud_year_level,
    sec.section_name,
    CASE WHEN s.stud_id IS NOT NULL THEN '✅ PASS' ELSE '❌ FAIL' END as status
FROM students s
INNER JOIN users u ON s.user_id = u.user_id
INNER JOIN user_roles ur ON u.role_id = ur.role_id
LEFT JOIN programs p ON s.program_id = p.program_id
LEFT JOIN sections sec ON s.section_id = sec.section_id
WHERE s.stud_number = '2025-0001';

SELECT '=== TEST 2: ENROLLMENT VERIFICATION (2025-0001) ===' as test_name;

SELECT 
    e.enroll_id,
    e.stud_id,
    e.class_id,
    co.course_code,
    co.course_name,
    CASE WHEN e.enroll_id IS NOT NULL THEN '✅ PASS' ELSE '❌ FAIL' END as status
FROM students s
INNER JOIN enrollment e ON s.stud_id = e.stud_id
INNER JOIN classes c ON e.class_id = c.class_id
INNER JOIN courses co ON c.course_id = co.course_id
WHERE s.stud_number = '2025-0001'
ORDER BY e.class_id;

SELECT '=== TEST 3: ALL SCHEDULES FOR STUDENT 2025-0001 ===' as test_name;

SELECT 
    cs.sched_id,
    cs.class_id,
    co.course_code,
    co.course_name,
    cs.sched_day_of_week,
    LENGTH(cs.sched_day_of_week) as day_length,
    cs.sched_start_time,
    cs.sched_end_time,
    -- Validation checks
    CASE WHEN cs.sched_day_of_week = TRIM(cs.sched_day_of_week) THEN '✅ No Whitespace' ELSE '❌ Has Whitespace' END as whitespace_check,
    CASE WHEN cs.sched_start_time IS NOT NULL THEN '✅ Has Start Time' ELSE '❌ NULL Start Time' END as start_time_check,
    CASE WHEN cs.sched_end_time IS NOT NULL THEN '✅ Has End Time' ELSE '❌ NULL End Time' END as end_time_check
FROM students s
INNER JOIN enrollment e ON s.stud_id = e.stud_id
INNER JOIN classes c ON e.class_id = c.class_id
INNER JOIN courses co ON c.course_id = co.course_id
INNER JOIN class_schedules cs ON c.class_id = cs.class_id
WHERE s.stud_number = '2025-0001'
ORDER BY co.course_code, cs.sched_day_of_week, cs.sched_start_time;


SELECT '=== TEST 4: FRIDAY 1:33 PM TIME MATCHING ===' as test_name;

SELECT 
    cs.sched_id,
    co.course_code,
    cs.sched_day_of_week,
    cs.sched_start_time,
    cs.sched_end_time,
    '13:33:00'::TIME as current_time,
    TO_CHAR(CURRENT_DATE, 'Day') as current_day_postgres,
    TO_CHAR(CURRENT_DATE, 'FMDay') as current_day_trimmed,
    -- Day matching tests
    CASE 
        WHEN LOWER(TRIM(cs.sched_day_of_week)) = LOWER(TRIM(TO_CHAR(CURRENT_DATE, 'Day'))) 
        THEN '✅ Day Match (with trim)' 
        WHEN LOWER(TRIM(cs.sched_day_of_week)) = LOWER(TRIM(TO_CHAR(CURRENT_DATE, 'FMDay'))) 
        THEN '✅ Day Match (FMDay)' 
        WHEN cs.sched_day_of_week = TO_CHAR(CURRENT_DATE, 'Day') 
        THEN '✅ Day Match (exact)' 
        ELSE '❌ Day Mismatch' 
    END as day_match,
    -- Time matching tests (for 1:33 PM = 13:33:00)
    CASE 
        WHEN '13:33:00'::TIME >= cs.sched_start_time THEN '✅ After Start' 
        ELSE '❌ Before Start' 
    END as after_start,
    CASE 
        WHEN '13:33:00'::TIME <= cs.sched_end_time THEN '✅ Before End' 
        ELSE '❌ After End' 
    END as before_end,
    -- Overall matching result
    CASE 
        WHEN (LOWER(TRIM(cs.sched_day_of_week)) = LOWER(TRIM(TO_CHAR(CURRENT_DATE, 'Day'))) 
              OR LOWER(TRIM(cs.sched_day_of_week)) = LOWER(TRIM(TO_CHAR(CURRENT_DATE, 'FMDay'))))
         AND '13:33:00'::TIME >= cs.sched_start_time 
         AND '13:33:00'::TIME <= cs.sched_end_time 
        THEN '✅✅✅ SHOULD ALLOW TIME IN ✅✅✅' 
        ELSE '❌ Should NOT match' 
    END as overall_match
FROM students s
INNER JOIN enrollment e ON s.stud_id = e.stud_id
INNER JOIN classes c ON e.class_id = c.class_id
INNER JOIN courses co ON c.course_id = co.course_id
INNER JOIN class_schedules cs ON c.class_id = cs.class_id
WHERE s.stud_number = '2025-0001'
ORDER BY cs.sched_start_time;


-- ==============================================================================
SELECT '=== TEST 5: SATURDAY SCHEDULE FOR IT203 ===' as test_name;

SELECT 
    cs.sched_id,
    co.course_code,
    cs.sched_day_of_week,
    cs.sched_start_time,
    cs.sched_end_time,
    -- Saturday test time: 1:01 PM = 13:01:00
    '13:01:00'::TIME as test_time,
    CASE 
        WHEN LOWER(TRIM(cs.sched_day_of_week)) = 'saturday' THEN '✅ Is Saturday' 
        ELSE '❌ Not Saturday' 
    END as is_saturday,
    CASE 
        WHEN '13:01:00'::TIME >= cs.sched_start_time 
         AND '13:01:00'::TIME <= cs.sched_end_time 
        THEN '✅ Time Match (1:01 PM in range)' 
        ELSE '❌ Time Mismatch' 
    END as time_match_saturday
FROM students s
INNER JOIN enrollment e ON s.stud_id = e.stud_id
INNER JOIN classes c ON e.class_id = c.class_id
INNER JOIN courses co ON c.course_id = co.course_id
INNER JOIN class_schedules cs ON c.class_id = cs.class_id
WHERE s.stud_number = '2025-0001'
  AND co.course_code = 'IT203'
ORDER BY cs.sched_day_of_week;

SELECT '=== TEST 6: FINGERPRINT REGISTRATION ===' as test_name;

SELECT 
    s.stud_id,
    s.stud_number,
    u.user_id,
    CONCAT(u.user_first_name, ' ', u.user_last_name) as student_name,
    ft.ftemp_id,
    ft.ftemp_name,
    LENGTH(ft.ftemp_data) as template_size_bytes,
    ft.ftemp_created_at,
    CASE 
        WHEN ft.ftemp_id IS NOT NULL THEN '✅ Fingerprint Registered' 
        ELSE '❌ No Fingerprint' 
    END as fingerprint_status
FROM students s
INNER JOIN users u ON s.user_id = u.user_id
LEFT JOIN fingerprint_templates ft ON u.user_id = ft.user_id
WHERE s.stud_number = '2025-0001';

SELECT '=== TEST 7: EXISTING ATTENDANCE RECORDS ===' as test_name;

SELECT 
    dal.daily_attend_id,
    dal.stud_id,
    dal.class_id,
    c.class_id as class_id_check,
    co.course_code,
    dal.attend_date,
    dal.time_in,
    dal.time_out,
    dal.status,
    dal.forgot_timeout,
    CASE 
        WHEN dal.status = 'completed' THEN '? Completed (Both In/Out)' 
        WHEN dal.status = 'active' THEN '?? Active (Only In)' 
        WHEN dal.status = 'incomplete' THEN '? Incomplete (Forgot Out)' 
        ELSE CONCAT('? Unknown Status: "', COALESCE(dal.status, 'NULL'), '"')
    END as status_explanation
FROM students s
LEFT JOIN daily_attendance_log dal ON s.stud_id = dal.stud_id
LEFT JOIN classes c ON dal.class_id = c.class_id
LEFT JOIN courses co ON c.course_id = co.course_id
WHERE s.stud_number = '2025-0001'
ORDER BY dal.attend_date DESC, dal.time_in DESC;

SELECT '=== TEST 8: ACADEMIC TERM STATUS ===' as test_name;

SELECT 
    at.term_id,
    at.term_name,
    at.start_date,
    at.end_date,
    at.is_active,
    CASE 
        WHEN at.is_active = TRUE THEN '✅ Active Term' 
        ELSE '❌ Inactive Term' 
    END as term_status,
    CASE 
        WHEN CURRENT_DATE >= at.start_date AND CURRENT_DATE <= at.end_date THEN '✅ Within Date Range' 
        ELSE '⚠️ Outside Date Range' 
    END as date_range_status
FROM academic_terms at
WHERE at.is_active = TRUE
ORDER BY at.term_id DESC
LIMIT 1;

SELECT '=== DIAGNOSTIC SUMMARY FOR 2025-0001 ===' as section;

WITH verification_results AS (
    SELECT 
        (SELECT COUNT(*) FROM students WHERE stud_number = '2025-0001') as student_exists,
        (SELECT COUNT(*) FROM enrollment e INNER JOIN students s ON e.stud_id = s.stud_id WHERE s.stud_number = '2025-0001') as enrollments_count,
        (SELECT COUNT(*) FROM class_schedules cs 
         INNER JOIN classes c ON cs.class_id = c.class_id 
         INNER JOIN enrollment e ON c.class_id = e.class_id 
         INNER JOIN students s ON e.stud_id = s.stud_id 
         WHERE s.stud_number = '2025-0001') as total_schedules,
        (SELECT COUNT(*) FROM fingerprint_templates ft
         INNER JOIN users u ON ft.user_id = u.user_id
         INNER JOIN students s ON u.user_id = s.user_id
         WHERE s.stud_number = '2025-0001') as has_fingerprint,
        (SELECT COUNT(*) FROM academic_terms WHERE is_active = TRUE) as active_term_exists
)
SELECT 
    student_exists,
    CASE WHEN student_exists = 1 THEN '✅' ELSE '❌' END as student_check,
    enrollments_count,
    CASE WHEN enrollments_count >= 3 THEN '✅' ELSE '❌' END as enrollment_check,
    total_schedules,
    CASE WHEN total_schedules > 0 THEN '✅' ELSE '❌' END as schedule_check,
    has_fingerprint,
    CASE WHEN has_fingerprint = 1 THEN '✅' ELSE '❌' END as fingerprint_check,
    active_term_exists,
    CASE WHEN active_term_exists = 1 THEN '✅' ELSE '❌' END as term_check,
    CASE 
        WHEN student_exists = 1 
         AND enrollments_count >= 3 
         AND total_schedules > 0 
         AND has_fingerprint = 1 
         AND active_term_exists = 1 
        THEN '✅ ALL CHECKS PASSED!' 
        ELSE '❌ SOME CHECKS FAILED!' 
    END as final_verdict
FROM verification_results;


SELECT '=== STEP 1: IT203 SCHEDULE VERIFICATION ===' as test_name;

SELECT 
    cs.sched_id,
    cs.class_id,
    c.course_id,
    co.course_code,
    co.course_name,
    cs.sched_day_of_week,
    LENGTH(cs.sched_day_of_week) as day_name_length,
    cs.sched_start_time,
    cs.sched_end_time,
    -- Check for whitespace
    CASE 
        WHEN cs.sched_day_of_week = TRIM(cs.sched_day_of_week) THEN '✅ No Whitespace'
        ELSE '❌ Has Whitespace (CRITICAL BUG!)'
    END as whitespace_check,
    -- Check if matches "Saturday"
    CASE 
        WHEN LOWER(TRIM(cs.sched_day_of_week)) = 'saturday' THEN '✅ Matches Saturday'
        ELSE CONCAT('❌ Does NOT match Saturday (value: "', cs.sched_day_of_week, '")')
    END as saturday_match
FROM class_schedules cs
INNER JOIN classes c ON cs.class_id = c.class_id
INNER JOIN courses co ON c.course_id = co.course_id
WHERE co.course_code = 'IT203'
ORDER BY cs.sched_day_of_week, cs.sched_start_time;

SELECT '=== STEP 2: TIME MATCHING TEST (Saturday 1:50 PM) ===' as test_name;

WITH test_time AS (
    SELECT 
        '13:50:00'::TIME as current_time,
        'Saturday' as current_day
)
SELECT 
    cs.sched_id,
    co.course_code,
    cs.sched_day_of_week,
    LENGTH(cs.sched_day_of_week) as db_day_length,
    cs.sched_start_time,
    cs.sched_end_time,
    tt.current_time,
    tt.current_day,
    -- Day matching tests
    CASE 
        WHEN LOWER(TRIM(cs.sched_day_of_week)) = LOWER(tt.current_day) 
        THEN '✅ DAY MATCH' 
        ELSE CONCAT('❌ DAY MISMATCH (DB: "', cs.sched_day_of_week, '" vs Current: "', tt.current_day, '")') 
    END as day_match_result,
    -- Time matching tests
    CASE 
        WHEN tt.current_time >= cs.sched_start_time 
        THEN '✅ After Start' 
        ELSE CONCAT('❌ Before Start (', tt.current_time, ' < ', cs.sched_start_time, ')') 
    END as after_start_check,
    CASE 
        WHEN tt.current_time <= cs.sched_end_time 
        THEN '✅ Before End' 
        ELSE CONCAT('❌ After End (', tt.current_time, ' > ', cs.sched_end_time, ')') 
    END as before_end_check,
    -- Overall result
    CASE 
        WHEN LOWER(TRIM(cs.sched_day_of_week)) = LOWER(tt.current_day)
         AND tt.current_time >= cs.sched_start_time 
         AND tt.current_time <= cs.sched_end_time 
        THEN '✅✅✅ SHOULD ALLOW TIME IN ✅✅✅' 
        ELSE '❌ Should REJECT (debug above checks)' 
    END as final_result
FROM class_schedules cs
INNER JOIN classes c ON cs.class_id = c.class_id
INNER JOIN courses co ON c.course_id = co.course_id
CROSS JOIN test_time tt
WHERE co.course_code = 'IT203';

SELECT '=== STEP 3: STUDENT ENROLLMENT IN IT203 ===' as test_name;

SELECT 
    s.stud_id,
    s.stud_number,
    e.class_id,
    c.course_id,
    co.course_code,
    co.course_name,
    CASE WHEN e.enroll_id IS NOT NULL THEN '✅ ENROLLED' ELSE '❌ NOT ENROLLED' END as enrollment_status
FROM students s
LEFT JOIN enrollment e ON s.stud_id = e.stud_id
LEFT JOIN classes c ON e.class_id = c.class_id
LEFT JOIN courses co ON c.course_id = co.course_id
WHERE s.stud_number = '2025-0001'
  AND co.course_code = 'IT203';

  SELECT '=== STEP 4: C# SCHEDULE QUERY SIMULATION ===' as test_name;

-- This simulates what HomeController.GetActiveClassForStudentAsync does
SELECT 
    cs.sched_id,
    cs.class_id,
    c.course_id,
    co.course_code,
    co.course_name,
    cs.sched_day_of_week,
    cs.sched_start_time,
    cs.sched_end_time
FROM enrollment e
INNER JOIN classes c ON e.class_id = c.class_id
INNER JOIN courses co ON c.course_id = co.course_id
INNER JOIN class_schedules cs ON c.class_id = cs.class_id
INNER JOIN students s ON e.stud_id = s.stud_id
WHERE s.stud_number = '2025-0001'
  AND LOWER(TRIM(cs.sched_day_of_week)) = 'saturday'
  AND '13:50:00'::TIME >= cs.sched_start_time
  AND '13:50:00'::TIME <= cs.sched_end_time;


SELECT '=== STEP 5: DAILY ATTENDANCE LOG CHECK ===' as test_name;

SELECT 
    dal.daily_attend_id,
    dal.stud_id,
    dal.class_id,
    dal.attend_date,
    dal.time_in,
    dal.time_out,
    dal.status,
    CASE 
        WHEN dal.attend_date = CURRENT_DATE AND dal.class_id = (
            SELECT c.class_id FROM classes c 
            INNER JOIN courses co ON c.course_id = co.course_id 
            WHERE co.course_code = 'IT203' LIMIT 1
        ) THEN '⚠️ ALREADY HAS RECORD FOR IT203 TODAY!'
        ELSE '✅ No duplicate'
    END as duplicate_check
FROM daily_attendance_log dal
INNER JOIN students s ON dal.stud_id = s.stud_id
WHERE s.stud_number = '2025-0001'
  AND dal.attend_date = CURRENT_DATE;

  SELECT '=== STEP 6: WHITESPACE FIX (if needed) ===' as test_name;

-- Check if any schedules have whitespace
SELECT 
    sched_id,
    sched_day_of_week,
    LENGTH(sched_day_of_week) as length_with_whitespace,
    LENGTH(TRIM(sched_day_of_week)) as length_trimmed,
    CASE 
        WHEN sched_day_of_week != TRIM(sched_day_of_week) THEN '❌ HAS WHITESPACE - NEEDS FIX'
        ELSE '✅ OK'
    END as whitespace_status
FROM class_schedules
WHERE sched_day_of_week != TRIM(sched_day_of_week);


SELECT '=== STEP 7: COMPLETE SYSTEM DIAGNOSTIC ===' as test_name;

WITH student_check AS (
    SELECT stud_id FROM students WHERE stud_number = '2025-0001'
),
enrollment_check AS (
    SELECT class_id FROM enrollment WHERE stud_id = (SELECT stud_id FROM student_check)
),
schedule_check AS (
    SELECT cs.* 
    FROM class_schedules cs
    WHERE cs.class_id IN (SELECT class_id FROM enrollment_check)
      AND LOWER(TRIM(cs.sched_day_of_week)) = 'saturday'
      AND '13:50:00'::TIME >= cs.sched_start_time
      AND '13:50:00'::TIME <= cs.sched_end_time
)
SELECT 
    'Student Exists' as check_type,
    CASE WHEN EXISTS(SELECT 1 FROM student_check) THEN '✅ PASS' ELSE '❌ FAIL' END as status
UNION ALL
SELECT 
    'Has Enrollments',
    CASE WHEN EXISTS(SELECT 1 FROM enrollment_check) THEN '✅ PASS' ELSE '❌ FAIL' END
UNION ALL
SELECT 
    'Has Matching Schedule',
    CASE WHEN EXISTS(SELECT 1 FROM schedule_check) THEN '✅ PASS' ELSE '❌ FAIL' END;

	SELECT '=== DIAGNOSTIC SUMMARY ===' as section;

SELECT 
    'If all checks above passed but time in still fails,' as diagnosis,
    'the issue is in C# code (HomeController.cs GetActiveClassForStudentAsync)' as location,
    'Check application logs for [SCHEDULE DEBUG] messages' as next_step;
