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

CREATE TABLE IF NOT EXISTS daily_attendance_log (
    daily_attend_id     SERIAL PRIMARY KEY,
    stud_id             INT NOT NULL REFERENCES students(stud_id) ON DELETE CASCADE,
    class_id            INT REFERENCES classes(class_id) ON DELETE SET NULL,  -- ✅ ADDED: Links to specific class/course
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
    
    -- ✅ UPDATED: Business constraint: One record per student per class per date
    CONSTRAINT daily_attendance_unique_student_class_date UNIQUE (stud_id, class_id, attend_date),
    
    -- Business rule: time_out must be after time_in
    CONSTRAINT daily_attendance_time_check CHECK (time_out IS NULL OR time_out > time_in)
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
                            -- ✅ NEW COLUMNS FOR FACULTY ATTENDANCE FIX (Dual-Write Sync)
                            daily_attend_id   INT REFERENCES daily_attendance_log(daily_attend_id) ON DELETE CASCADE,
                            is_present        BOOLEAN DEFAULT TRUE,
                            UNIQUE (class_id, stud_id, attend_scan_time, attend_type)
);

CREATE TABLE scan_logs (
						   scan_id 			 SERIAL PRIMARY KEY,
                           user_id           INT REFERENCES users(user_id) ON DELETE SET NULL,
                           scan_time         TIMESTAMP NOT NULL DEFAULT NOW(),
                           scan_success      BOOLEAN,
                           scan_log_message  TEXT,
                           scan_created_at   TIMESTAMP NOT NULL DEFAULT NOW()
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


CREATE TABLE IF NOT EXISTS enrollment_blocks (
    block_id            SERIAL PRIMARY KEY,
    stud_id             INT NOT NULL REFERENCES students(stud_id) ON DELETE CASCADE,
    class_id            INT NOT NULL REFERENCES classes(class_id) ON DELETE CASCADE,
    blocked_by_fac_id   INT NOT NULL REFERENCES faculty(fac_id) ON DELETE SET NULL,
    block_reason        TEXT,
    blocked_at          TIMESTAMP NOT NULL DEFAULT NOW(),
    is_active           BOOLEAN NOT NULL DEFAULT TRUE,
    unblocked_at        TIMESTAMP,
    unblocked_by        INT REFERENCES users(user_id),
    
    -- Ensure one active block per student per class
    CONSTRAINT enrollment_blocks_unique_active UNIQUE (stud_id, class_id) 
);

-- Indexes for performance
CREATE INDEX IF NOT EXISTS idx_enrollment_blocks_stud_id ON enrollment_blocks(stud_id);
CREATE INDEX IF NOT EXISTS idx_enrollment_blocks_class_id ON enrollment_blocks(class_id);
CREATE INDEX IF NOT EXISTS idx_enrollment_blocks_active ON enrollment_blocks(is_active) WHERE is_active = TRUE;
CREATE INDEX IF NOT EXISTS idx_enrollment_blocks_fac_id ON enrollment_blocks(blocked_by_fac_id);

-- Comments
COMMENT ON TABLE enrollment_blocks IS 'Tracks students who have been force-unenrolled by faculty and are blocked from re-enrolling';
COMMENT ON COLUMN enrollment_blocks.is_active IS 'TRUE = student is currently blocked, FALSE = block has been lifted';
COMMENT ON COLUMN enrollment_blocks.block_reason IS 'Faculty-provided reason for the force unenrollment';

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
-- ✅ NEW: Index for dual-write sync foreign key
CREATE INDEX idx_attend_daily_log           ON attendance(daily_attend_id);

-- Classes table
CREATE INDEX idx_class_fac_id               ON classes(fac_id);
CREATE INDEX idx_class_room_id              ON classes(room_id);

-- Scan logs table
CREATE INDEX idx_scan_logs_time             ON scan_logs(scan_time);

-- Index on scan_logs.scan_success for performance
CREATE INDEX IF NOT EXISTS idx_scan_logs_success 
ON scan_logs(scan_success) WHERE scan_success = TRUE;

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
CREATE INDEX idx_daily_attend_class_id ON daily_attendance_log(class_id);  -- ✅ ADDED: For class filtering
CREATE INDEX idx_daily_attend_stud_class_date ON daily_attendance_log(stud_id, class_id, attend_date);  -- ✅ ADDED: Composite index

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


-- ============================================================
-- ✅ NEW: CRITICAL FUNCTION - Auto-Mark Absent on Missing Time Out
-- ============================================================
-- This function detects students who:
-- 1. Have Time In recorded (time_in IS NOT NULL)
-- 2. Do NOT have Time Out recorded (time_out IS NULL)
-- 3. Current time is PAST the class end time
-- Result: Mark their attendance status from 'present' to 'absent'
-- ============================================================

CREATE OR REPLACE FUNCTION auto_mark_absent_on_missing_timeout()
RETURNS TABLE (
    marked_absent_count INT,
    details TEXT
) AS $$
DECLARE
    v_count INT := 0;
    v_record RECORD;
BEGIN
    -- Loop through all daily attendance records with missing time_out
    FOR v_record IN (
        SELECT 
            dal.daily_attend_id,
            dal.stud_id,
            dal.class_id,
            dal.attend_date,
            dal.time_in,
            cs.sched_end_time,
            CASE 
                -- For today's records, check if current time > class end time
                WHEN dal.attend_date = CURRENT_DATE 
                    THEN CURRENT_TIME > cs.sched_end_time
                -- For past dates, all classes have ended
                WHEN dal.attend_date < CURRENT_DATE 
                    THEN TRUE
                ELSE FALSE
            END as class_has_ended
        FROM daily_attendance_log dal
        INNER JOIN classes c ON dal.class_id = c.class_id
        INNER JOIN class_schedules cs ON c.class_id = cs.class_id
        WHERE dal.time_in IS NOT NULL
          AND dal.time_out IS NULL
          AND dal.status = 'active'
          AND TRIM(cs.sched_day_of_week) = TRIM(TO_CHAR(dal.attend_date, 'Day'))
    ) LOOP
        -- Only process if class has ended
        IF v_record.class_has_ended THEN
            -- Update attendance table to mark as absent
            UPDATE attendance
            SET attend_status = 'absent',
                attend_remark = CONCAT(
                    COALESCE(attend_remark, ''),
                    ' | Auto-marked absent: Time In recorded at ',
                    TO_CHAR(v_record.time_in, 'HH:MI:SS'),
                    ' but no Time Out after class ended at ',
                    TO_CHAR(v_record.sched_end_time, 'HH:MI:SS')
                )
            WHERE stud_id = v_record.stud_id
              AND class_id = v_record.class_id
              AND attend_date = v_record.attend_date
              AND LOWER(attend_status::TEXT) IN ('present', 'late')  -- Only change if currently marked present/late
              AND time_out IS NULL;  -- Still missing time out
            
            -- Mark daily_attendance_log as incomplete
            UPDATE daily_attendance_log
            SET status = 'incomplete',
                forgot_timeout = TRUE,
                remarks = CONCAT(
                    COALESCE(remarks, ''),
                    ' | Forgot to time out after class ended at ',
                    TO_CHAR(v_record.sched_end_time, 'HH:MI:SS')
                ),
                updated_at = NOW()
            WHERE daily_attendance_id = v_record.daily_attend_id;
            
            v_count := v_count + 1;
            
            RAISE NOTICE 'Marked student % as absent for class % on % (no timeout after %)', v_record.stud_id, v_record.class_id, v_record.attend_date, v_record.sched_end_time;
        END IF;
    END LOOP;
    
    RETURN QUERY SELECT v_count, v_count || ' record(s) updated';
END;
$$ LANGUAGE plpgsql;

COMMENT ON FUNCTION auto_mark_absent_on_missing_timeout IS 
'🚨 CRITICAL: Automatically marks students as absent if they forget to clock out. Should be scheduled to run periodically (e.g., every 10 minutes).';

-- Example usage:  This can be called from a scheduled job to auto-correct attendance
-- SELECT * FROM auto_mark_absent_on_missing_timeout();

-- ============================================================
-- Scheduled Jobs (pg_cron)
-- ============================================================
-- NOTE: These jobs require the pg_cron extension and appropriate setup

-- 1. Mark absent on missing time out - every 10 minutes
-- SELECT cron.schedule('*/10 * * * *', $$
--     SELECT auto_mark_absent_on_missing_timeout();
-- $$);

-- 2. Process forgot timeout absences - every hour
-- SELECT cron.schedule('0 * * * *', $$
--     SELECT process_forgot_timeout_absences();
-- $$);


-- ============================================================
-- SUPPORTING FUNCTION - Process Forgot Timeout Absences
-- ============================================================
-- This function processes students who have forgotten to clock out:
-- 1. Marks their attendance as incomplete
-- 2. Sends notification to student
-- 3. Optionally, informs admin for manual review
-- ============================================================

CREATE OR REPLACE FUNCTION process_forgot_timeout_absences()
RETURNS VOID AS $$
DECLARE
    v_record RECORD;
    v_admin_users INT[];
BEGIN
    -- Find students who forgot to time out (active status, no time_out, flagged as forgot_timeout)
    FOR v_record IN (
        SELECT dal.*,
               s.user_id,
               u.user_email as student_email,
               u.user_first_name || ' ' || u.user_last_name as student_name
        FROM daily_attendance_log dal
        JOIN students s ON dal.stud_id = s.stud_id
        JOIN users u ON s.user_id = u.user_id
        WHERE dal.status = 'active'
          AND dal.time_out IS NULL
          AND dal.forgot_timeout = TRUE
          AND dal.attend_date = CURRENT_DATE
    ) LOOP
        -- Update daily_attendance_log to mark as incomplete
        UPDATE daily_attendance_log
        SET status = 'incomplete',
            remarks = COALESCE(remarks, '') || ' | Forgot to clock out',
            updated_at = NOW()
        WHERE daily_attendance_id = v_record.daily_attend_id;
        
        -- Optionally, notify admin for manual review
        IF FALSE THEN  -- SET TO TRUE to enable admin notifications
            -- Notify admin users
            PERFORM pg_notify('admin_channel', 
                'Student ' || v_record.student_name || ' forgot to clock out. Please review.'
            );
        END IF;
        
        -- ✅ FIXED: Removed send_email call (not a built-in PostgreSQL function)
        -- Email notifications should be handled by the application layer (C# BackgroundService)
        -- Log the event for application to process
        RAISE NOTICE 'Student % (%) forgot to clock out on %', 
            v_record.student_name, v_record.student_email, v_record.attend_date;
    END LOOP;
    
    -- Admin log entry
    INSERT INTO administration_audit (admin_id, aud_action, aud_details)
    VALUES (NULL, 'Processed forgot timeout absences', 
            'Reviewed and updated records for students who forgot to clock out');
END;
$$ LANGUAGE plpgsql;

COMMENT ON FUNCTION process_forgot_timeout_absences IS 
'Wrapper function for scheduled execution. Call this from C# BackgroundService or cron job to auto-detect and mark absences.';

-- ============================================================
-- ✅ UPDATED: record_time_in function to support class_id
-- ============================================================

CREATE OR REPLACE FUNCTION record_time_in(
    p_stud_id INT,
    p_class_id INT,  -- ✅ NEW PARAMETER
    p_timestamp TIMESTAMP DEFAULT NOW()
)
RETURNS TABLE (
    success BOOLEAN,
    message TEXT,
    daily_attend_id INT,
    incomplete_date DATE
) AS $$
DECLARE
    v_incomplete_record RECORD;
    v_new_attend_id INT;
    v_today DATE := p_timestamp::DATE;
BEGIN
    -- Check for incomplete previous records for THIS CLASS
    SELECT dal.daily_attend_id, dal.attend_date 
    INTO v_incomplete_record
    FROM daily_attendance_log dal
    WHERE dal.stud_id = p_stud_id
      AND dal.class_id = p_class_id
      AND dal.attend_date < v_today
      AND dal.time_out IS NULL
      AND dal.forgot_timeout = FALSE
    ORDER BY dal.attend_date DESC
    LIMIT 1;
    
    -- If incomplete record found, mark it as forgot to time out
    IF v_incomplete_record.daily_attend_id IS NOT NULL THEN
        UPDATE daily_attendance_log
        SET status = 'incomplete',
            forgot_timeout = TRUE,
            updated_at = NOW()
        WHERE daily_attendance_id = v_incomplete_record.daily_attend_id;
    END IF;
    
    -- Check if already timed in for THIS CLASS today
    IF EXISTS (
        SELECT 1 FROM daily_attendance_log 
        WHERE stud_id = p_stud_id 
          AND class_id = p_class_id
          AND attend_date = v_today 
          AND time_out IS NULL
    ) THEN
        RETURN QUERY 
        SELECT 
            FALSE as success,
            'Already timed in for this class today'::TEXT as message,
            NULL::INT as daily_attend_id,
            v_incomplete_record.attend_date as incomplete_date;
        RETURN;
    END IF;
    
    -- Create new time in record FOR THIS CLASS
    INSERT INTO daily_attendance_log (
        stud_id, 
        class_id,  -- ✅ ADDED
        attend_date, 
        time_in, 
        status,
        created_at,
        updated_at
    )
    VALUES (
        p_stud_id,
        p_class_id,  -- ✅ ADDED
        v_today,
        p_timestamp,
        'active',
        NOW(),
        NOW()
    )
    RETURNING daily_attendance_log.daily_attend_id INTO v_new_attend_id;
    
    RETURN QUERY 
    SELECT 
        TRUE as success,
        'Time in recorded successfully'::TEXT as message,
        v_new_attend_id as daily_attend_id,
        v_incomplete_record.attend_date as incomplete_date;
END;
$$ LANGUAGE plpgsql;

COMMENT ON FUNCTION record_time_in IS 
'✅ UPDATED: Records student time in for a SPECIFIC CLASS. Checks for incomplete previous records for the same class and auto-marks them as forgot_timeout.';

-- ============================================================
-- ✅ UPDATED: record_time_out function to support class_id
-- ============================================================

CREATE OR REPLACE FUNCTION record_time_out(
    p_stud_id INT,
    p_class_id INT,  -- ✅ NEW PARAMETER (optional, can be NULL for general timeout)
    p_timestamp TIMESTAMP DEFAULT NOW()
)
RETURNS TABLE (
    success BOOLEAN,
    message TEXT,
    daily_attend_id INT,
    time_in TIMESTAMP,
    time_out TIMESTAMP,
    duration_hours NUMERIC
) AS $$
DECLARE
    v_record RECORD;
    v_today DATE := p_timestamp::DATE;
BEGIN
    -- Find today's active record for THIS CLASS
    IF p_class_id IS NOT NULL THEN
        SELECT * INTO v_record
        FROM daily_attendance_log
        WHERE stud_id = p_stud_id
          AND class_id = p_class_id
          AND attend_date = v_today
          AND time_out IS NULL;
    ELSE
        -- Fallback: Find ANY active record for today (backward compatibility)
        SELECT * INTO v_record
        FROM daily_attendance_log
        WHERE stud_id = p_stud_id
          AND attend_date = v_today
          AND time_out IS NULL
        LIMIT 1;
    END IF;
    
    -- Check if no active time in
    IF v_record.daily_attend_id IS NULL THEN
        RETURN QUERY 
        SELECT 
            FALSE as success,
            'No active time in found for this class today'::TEXT as message,
            NULL::INT as daily_attend_id,
            NULL::TIMESTAMP as time_in,
            NULL::TIMESTAMP as time_out,
            NULL::NUMERIC as duration_hours;
        RETURN;
    END IF;
    
    -- Check if time out is before time in
    IF p_timestamp <= v_record.time_in THEN
        RETURN QUERY 
        SELECT 
            FALSE as success,
            'Time out cannot be before time in'::TEXT as message,
            NULL::INT as daily_attend_id,
            NULL::TIMESTAMP as time_in,
            NULL::TIMESTAMP as time_out,
            NULL::NUMERIC as duration_hours;
        RETURN;
    END IF;
    
    -- Record time out
    UPDATE daily_attendance_log
    SET time_out = p_timestamp,
        status = 'completed',
        updated_at = NOW()
    WHERE daily_attendance_id = v_record.daily_attend_id;
    
    RETURN QUERY 
    SELECT 
        TRUE as success,
        'Time out recorded successfully'::TEXT as message,
        v_record.daily_attend_id as daily_attend_id,
        v_record.time_in as time_in,
        p_timestamp as time_out,
        ROUND(EXTRACT(EPOCH FROM (p_timestamp - v_record.time_in))/3600, 2) as duration_hours;
END;
$$ LANGUAGE plpgsql;

COMMENT ON FUNCTION record_time_out IS 
'✅ UPDATED: Records student time out for a SPECIFIC CLASS (or any active record if class_id is NULL). Validates that time in exists and time out is after time in.';


DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 
        FROM information_schema.columns 
        WHERE table_name = 'daily_attendance_log' 
          AND column_name = 'class_id'
    ) THEN
        RAISE NOTICE '❌ class_id column is MISSING from daily_attendance_log';
        RAISE NOTICE '➡️ Adding class_id column now...';
        
        -- Step 2: Add class_id column
        ALTER TABLE daily_attendance_log 
        ADD COLUMN class_id INT REFERENCES classes(class_id) ON DELETE SET NULL;
        
        RAISE NOTICE '✅ class_id column added successfully';
    ELSE
        RAISE NOTICE '✅ class_id column already exists in daily_attendance_log';
    END IF;
END $$;

DO $$
BEGIN
    -- Check if old constraint exists
    IF EXISTS (
        SELECT 1 
        FROM pg_constraint 
        WHERE conname = 'daily_attendance_unique_student_date'
    ) THEN
        -- Drop old constraint
        ALTER TABLE daily_attendance_log 
        DROP CONSTRAINT daily_attendance_unique_student_date;
        
        RAISE NOTICE '✅ Dropped old unique constraint: daily_attendance_unique_student_date';
    END IF;
    
    -- Check if new constraint already exists
    IF NOT EXISTS (
        SELECT 1 
        FROM pg_constraint 
        WHERE conname = 'daily_attendance_unique_student_class_date'
    ) THEN
        -- Add new unique constraint
        ALTER TABLE daily_attendance_log 
        ADD CONSTRAINT daily_attendance_unique_student_class_date 
        UNIQUE (stud_id, class_id, attend_date);
        
        RAISE NOTICE '✅ Added new unique constraint: daily_attendance_unique_student_class_date';
    ELSE
        RAISE NOTICE '✅ Unique constraint already exists: daily_attendance_unique_student_class_date';
    END IF;
END $$;


-- ============================================================
-- EDIT CLASS SCHEDULE
-- ============================================================

-- Add sched_id column to link attendance to specific schedule
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'daily_attendance_log' 
        AND column_name = 'sched_id'
    ) THEN
        ALTER TABLE daily_attendance_log 
        ADD COLUMN sched_id INT REFERENCES class_schedules(sched_id) ON DELETE SET NULL;
        
        RAISE NOTICE '✅ Added sched_id column to daily_attendance_log';
    ELSE
        RAISE NOTICE '✅ sched_id column already exists in daily_attendance_log';
    END IF;
END $$;

-- Add is_invalidated flag
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'daily_attendance_log' 
        AND column_name = 'is_invalidated'
    ) THEN
        ALTER TABLE daily_attendance_log 
        ADD COLUMN is_invalidated BOOLEAN NOT NULL DEFAULT FALSE;
        
        RAISE NOTICE '✅ Added is_invalidated column to daily_attendance_log';
    ELSE
        RAISE NOTICE '✅ is_invalidated column already exists in daily_attendance_log';
    END IF;
END $$;

-- Add invalidation_reason column
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'daily_attendance_log' 
        AND column_name = 'invalidation_reason'
    ) THEN
        ALTER TABLE daily_attendance_log 
        ADD COLUMN invalidation_reason TEXT;
        
        RAISE NOTICE '✅ Added invalidation_reason column to daily_attendance_log';
    ELSE
        RAISE NOTICE '✅ invalidation_reason column already exists in daily_attendance_log';
    END IF;
END $$;

-- Add invalidated_at timestamp
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'daily_attendance_log' 
        AND column_name = 'invalidated_at'
    ) THEN
        ALTER TABLE daily_attendance_log 
        ADD COLUMN invalidated_at TIMESTAMP;
        
        RAISE NOTICE '✅ Added invalidated_at column to daily_attendance_log';
    ELSE
        RAISE NOTICE '✅ invalidated_at column already exists in daily_attendance_log';
    END IF;
END $$;

-- Add sched_id column to link attendance to specific schedule
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'attendance' 
        AND column_name = 'sched_id'
    ) THEN
        ALTER TABLE attendance 
        ADD COLUMN sched_id INT REFERENCES class_schedules(sched_id) ON DELETE SET NULL;
        
        RAISE NOTICE '✅ Added sched_id column to attendance';
    ELSE
        RAISE NOTICE '✅ sched_id column already exists in attendance';
    END IF;
END $$;

-- Add is_invalidated flag
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'attendance' 
        AND column_name = 'is_invalidated'
    ) THEN
        ALTER TABLE attendance 
        ADD COLUMN is_invalidated BOOLEAN NOT NULL DEFAULT FALSE;
        
        RAISE NOTICE '✅ Added is_invalidated column to attendance';
    ELSE
        RAISE NOTICE '✅ is_invalidated column already exists in attendance';
    END IF;
END $$;

-- Add invalidation_reason column
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'attendance' 
        AND column_name = 'invalidation_reason'
    ) THEN
        ALTER TABLE attendance 
        ADD COLUMN invalidation_reason TEXT;
        
        RAISE NOTICE '✅ Added invalidation_reason column to attendance';
    ELSE
        RAISE NOTICE '✅ invalidation_reason column already exists in attendance';
    END IF;
END $$;

-- Index for finding invalidated records
CREATE INDEX IF NOT EXISTS idx_daily_attend_invalidated 
ON daily_attendance_log(is_invalidated) WHERE is_invalidated = TRUE;

-- Index for sched_id lookups
CREATE INDEX IF NOT EXISTS idx_daily_attend_sched_id 
ON daily_attendance_log(sched_id);

-- Index for attendance table
CREATE INDEX IF NOT EXISTS idx_attendance_invalidated 
ON attendance(is_invalidated) WHERE is_invalidated = TRUE;

CREATE INDEX IF NOT EXISTS idx_attendance_sched_id 
ON attendance(sched_id);

CREATE OR REPLACE FUNCTION invalidate_attendance_on_schedule_change(
    p_class_id INT,
    p_sched_id INT,
    p_old_start_time TIME,
    p_old_end_time TIME,
    p_new_start_time TIME,
    p_new_end_time TIME,
    p_day_of_week VARCHAR(10)
)
RETURNS TABLE (
    invalidated_daily_count INT,
    invalidated_attendance_count INT,
    details TEXT
) AS $$
DECLARE
    v_daily_count INT := 0;
    v_attend_count INT := 0;
    v_reason TEXT;
    v_today DATE := CURRENT_DATE;
BEGIN
    -- Build invalidation reason with old and new times
    v_reason := format(
        'Schedule changed from %s-%s to %s-%s on %s',
        TO_CHAR(p_old_start_time, 'HH24:MI'),
        TO_CHAR(p_old_end_time, 'HH24:MI'),
        TO_CHAR(p_new_start_time, 'HH24:MI'),
        TO_CHAR(p_new_end_time, 'HH24:MI'),
        p_day_of_week
    );
    
    -- ✅ STEP 1: Invalidate daily_attendance_log records for TODAY only
    -- (Past records should remain as historical data)
    UPDATE daily_attendance_log
    SET is_invalidated = TRUE,
        invalidation_reason = v_reason,
        invalidated_at = NOW(),
        remarks = CONCAT(COALESCE(remarks, ''), ' | INVALIDATED: ', v_reason),
        updated_at = NOW()
    WHERE class_id = p_class_id
      AND attend_date = v_today
      AND is_invalidated = FALSE  -- Only invalidate non-invalidated records
      AND status IN ('active', 'completed');  -- Only active/completed records
    
    GET DIAGNOSTICS v_daily_count = ROW_COUNT;
    
    -- ✅ STEP 2: Invalidate corresponding attendance records for TODAY
    UPDATE attendance
    SET is_invalidated = TRUE,
        invalidation_reason = v_reason,
        attend_remark = CONCAT(COALESCE(attend_remark, ''), ' | INVALIDATED: ', v_reason)
    WHERE class_id = p_class_id
      AND attend_date = v_today
      AND is_invalidated = FALSE;
    
    GET DIAGNOSTICS v_attend_count = ROW_COUNT;
    
    -- Log the action
    IF v_daily_count > 0 OR v_attend_count > 0 THEN
        RAISE NOTICE '✅ Invalidated % daily attendance and % class attendance records for class % on % due to schedule change',
            v_daily_count, v_attend_count, p_class_id, v_today;
    END IF;
    
    RETURN QUERY SELECT 
        v_daily_count as invalidated_daily_count,
        v_attend_count as invalidated_attendance_count,
        format('Invalidated %s daily + %s attendance records for class %s. Reason: %s', 
               v_daily_count, v_attend_count, p_class_id, v_reason) as details;
END;
$$ LANGUAGE plpgsql;

COMMENT ON FUNCTION invalidate_attendance_on_schedule_change IS 
'🔄 Invalidates attendance records when a class schedule time is changed. 
Allows students to re-attend for the new time slot while preserving history.';

DO $$
BEGIN
    -- Check if the old constraint exists and drop it
    IF EXISTS (
        SELECT 1 FROM pg_constraint 
        WHERE conname = 'daily_attendance_unique_student_class_date'
    ) THEN
        ALTER TABLE daily_attendance_log 
        DROP CONSTRAINT daily_attendance_unique_student_class_date;
        
        RAISE NOTICE '✅ Dropped old unique constraint: daily_attendance_unique_student_class_date';
    END IF;
END $$;

-- Create a new partial unique index that excludes invalidated records
CREATE UNIQUE INDEX IF NOT EXISTS idx_daily_attend_unique_active
ON daily_attendance_log (stud_id, class_id, attend_date)
WHERE is_invalidated = FALSE;

COMMENT ON INDEX idx_daily_attend_unique_active IS 
'Partial unique index: Only one ACTIVE (non-invalidated) attendance per student per class per date.
Allows re-attendance after schedule changes invalidate previous records.';

CREATE OR REPLACE VIEW v_active_daily_attendance AS
SELECT 
    dal.*,
    s.stud_number,
    u.user_first_name,
    u.user_last_name,
    co.course_code,
    co.course_name
FROM daily_attendance_log dal
JOIN students s ON dal.stud_id = s.stud_id
JOIN users u ON s.user_id = u.user_id
LEFT JOIN classes c ON dal.class_id = c.class_id
LEFT JOIN courses co ON c.course_id = co.course_id
WHERE dal.is_invalidated = FALSE;

COMMENT ON VIEW v_active_daily_attendance IS 
'Shows only active (non-invalidated) daily attendance records';

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

SELECT column_name, data_type, column_default, is_nullable
FROM information_schema.columns
WHERE table_name = 'scan_logs'
ORDER BY ordinal_position;

SELECT scan_id, user_id, scan_time, scan_success, scan_log_message, scan_created_at
FROM scan_logs
ORDER BY scan_created_at DESC
LIMIT 10;
