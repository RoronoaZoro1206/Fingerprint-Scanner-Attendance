
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
                                 sched_end_time     TIME NOT NULL,
                                 UNIQUE (class_id, sched_day_of_week, sched_start_time, sched_end_time)
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

-- Function to auto-update updated_at timestamp
CREATE OR REPLACE FUNCTION update_excuse_request_timestamp()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = NOW();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Trigger for student_excuse_requests
DROP TRIGGER IF EXISTS trg_excuse_request_updated_at ON student_excuse_requests;
CREATE TRIGGER trg_excuse_request_updated_at
    BEFORE UPDATE ON student_excuse_requests
    FOR EACH ROW
EXECUTE FUNCTION update_excuse_request_timestamp();


-- Update timestamp on class update
CREATE OR REPLACE FUNCTION update_updated_at_column()
    RETURNS TRIGGER AS $$
BEGIN
    NEW.class_updated_at = NOW();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trigger_update_classes_updated_at
    BEFORE UPDATE ON classes
    FOR EACH ROW
EXECUTE FUNCTION update_updated_at_column();


-- Ensure only one active academic term
CREATE OR REPLACE FUNCTION enforce_single_active_term()
    RETURNS TRIGGER AS $$
BEGIN
    IF NEW.is_active = TRUE THEN
        UPDATE academic_terms
        SET is_active = FALSE
        WHERE term_id <> NEW.term_id;
    END IF;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_enforce_single_active_term
    BEFORE INSERT OR UPDATE ON academic_terms
    FOR EACH ROW
EXECUTE FUNCTION enforce_single_active_term();

-- Add comment to explain the purpose
COMMENT ON COLUMN students.profile_submitted IS 'Indicates if student has submitted their profile with academic information for the first time';
COMMENT ON COLUMN students.profile_submitted_at IS 'Timestamp when student first submitted their profile with academic information';

-- Add new column to link to daily attendance log
DO $$ 
BEGIN
    -- Add daily_attend_id column if it doesn't exist
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'attendance' 
        AND column_name = 'daily_attend_id'
    ) THEN
        ALTER TABLE attendance ADD COLUMN daily_attend_id INT;
        
        -- Add foreign key constraint
        ALTER TABLE attendance ADD CONSTRAINT fk_attendance_daily_log
        FOREIGN KEY (daily_attend_id) 
        REFERENCES daily_attendance_log(daily_attend_id) 
        ON DELETE CASCADE;
        
        -- Add index for foreign key
        CREATE INDEX idx_attendance_daily_log ON attendance(daily_attend_id);
    END IF;

    -- Add is_present column if it doesn't exist
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'attendance' 
        AND column_name = 'is_present'
    ) THEN
        ALTER TABLE attendance ADD COLUMN is_present BOOLEAN DEFAULT TRUE;
    END IF;
END $$;

-- Drop old unique constraint (if exists)
ALTER TABLE attendance DROP CONSTRAINT IF EXISTS attendance_class_id_stud_id_attend_scan_time_attend_type_key;

-- Add new unique constraint: one attendance record per student per class per date
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint 
        WHERE conname = 'attendance_class_student_date_unique'
    ) THEN
        ALTER TABLE attendance ADD CONSTRAINT attendance_class_student_date_unique 
        UNIQUE (class_id, stud_id, attend_date);
    END IF;
END $$;

-- Update comments
COMMENT ON COLUMN attendance.daily_attend_id IS 
'Foreign key to daily_attendance_log. Links class attendance to daily time in/out record.';

COMMENT ON COLUMN attendance.is_present IS 
'Indicates if student was marked present for this specific class. Used for manual overrides.';

COMMENT ON COLUMN attendance.attend_scan_time IS 
'DEPRECATED: Use daily_attendance_log.time_in instead. Kept for backward compatibility.';

COMMENT ON COLUMN attendance.attend_type IS 
'DEPRECATED: No longer used in daily time in/out system. Kept for backward compatibility.';

COMMENT ON TABLE attendance IS 
'Class-specific attendance records. Each record links to a daily_attendance_log entry and determines if student was Late/Present/Absent for a specific class based on their daily Time In.';


-- Function 1: Get or Create Daily Attendance Record
CREATE OR REPLACE FUNCTION get_daily_attendance_record(
    p_stud_id INT,
    p_date DATE DEFAULT CURRENT_DATE
)
RETURNS TABLE (
    daily_attend_id INT,
    time_in TIMESTAMP,
    time_out TIMESTAMP,
    status VARCHAR(20),
    forgot_timeout BOOLEAN,
    can_time_in BOOLEAN,
    can_time_out BOOLEAN
) AS $$
BEGIN
    RETURN QUERY
    SELECT 
        dal.daily_attend_id,
        dal.time_in,
        dal.time_out,
        dal.status,
        dal.forgot_timeout,
        -- Can time in if: no record OR time_out is set (completed previous day)
        CASE 
            WHEN dal.daily_attend_id IS NULL THEN TRUE
            WHEN dal.time_out IS NOT NULL THEN TRUE
            ELSE FALSE
        END as can_time_in,
        -- Can time out if: time_in is set AND time_out is NULL
        CASE 
            WHEN dal.time_in IS NOT NULL AND dal.time_out IS NULL THEN TRUE
            ELSE FALSE
        END as can_time_out
    FROM (SELECT p_stud_id as stud_id, p_date as attend_date) as student_day
    LEFT JOIN daily_attendance_log dal 
        ON dal.stud_id = student_day.stud_id 
        AND dal.attend_date = student_day.attend_date;
END;
$$ LANGUAGE plpgsql;

COMMENT ON FUNCTION get_daily_attendance_record IS 
'Retrieves daily attendance record with metadata about what actions are allowed (time in/out)';

-- Function 2: Check for Incomplete Previous Records
CREATE OR REPLACE FUNCTION check_incomplete_attendance(p_stud_id INT)
RETURNS TABLE (
    daily_attend_id INT,
    attend_date DATE,
    time_in TIMESTAMP,
    days_incomplete INT
) AS $$
BEGIN
    RETURN QUERY
    SELECT 
        dal.daily_attend_id,
        dal.attend_date,
        dal.time_in,
        (CURRENT_DATE - dal.attend_date)::INT as days_incomplete
    FROM daily_attendance_log dal
    WHERE dal.stud_id = p_stud_id
    AND dal.attend_date < CURRENT_DATE
    AND dal.time_out IS NULL
    AND dal.forgot_timeout = FALSE
    ORDER BY dal.attend_date DESC
    LIMIT 1;
END;
$$ LANGUAGE plpgsql;

COMMENT ON FUNCTION check_incomplete_attendance IS 
'Finds the most recent incomplete attendance record (forgot to time out) for a student';

-- Function 3: Time In Operation
CREATE OR REPLACE FUNCTION record_time_in(
    p_stud_id INT,
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
    -- Check for incomplete previous records
    SELECT * INTO v_incomplete_record
    FROM check_incomplete_attendance(p_stud_id);
    
    -- If incomplete record found, mark it as forgot to time out
    IF v_incomplete_record.daily_attend_id IS NOT NULL THEN
        UPDATE daily_attendance_log
        SET status = 'incomplete',
            forgot_timeout = TRUE,
            updated_at = NOW()
        WHERE daily_attend_id = v_incomplete_record.daily_attend_id;
    END IF;
    
    -- Check if already timed in today
    IF EXISTS (
        SELECT 1 FROM daily_attendance_log 
        WHERE stud_id = p_stud_id 
        AND attend_date = v_today 
        AND time_out IS NULL
    ) THEN
        RETURN QUERY 
        SELECT 
            FALSE as success,
            'Already timed in for today'::TEXT as message,
            NULL::INT as daily_attend_id,
            v_incomplete_record.attend_date as incomplete_date;
        RETURN;
    END IF;
    
    -- Create new time in record
    INSERT INTO daily_attendance_log (
        stud_id, 
        attend_date, 
        time_in, 
        status,
        created_at,
        updated_at
    )
    VALUES (
        p_stud_id,
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
'Records student time in. Checks for incomplete previous records and auto-marks them as forgot_timeout.';

-- Function 4: Time Out Operation
CREATE OR REPLACE FUNCTION record_time_out(
    p_stud_id INT,
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
    -- Find today's active record
    SELECT * INTO v_record
    FROM daily_attendance_log
    WHERE stud_id = p_stud_id
    AND attend_date = v_today
    AND time_out IS NULL;
    
    -- Check if no active time in
    IF v_record.daily_attend_id IS NULL THEN
        RETURN QUERY 
        SELECT 
            FALSE as success,
            'No active time in found for today'::TEXT as message,
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
    WHERE daily_attend_id = v_record.daily_attend_id;
    
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
'Records student time out. Validates that time in exists and time out is after time in.';

-- Function 5: Auto-mark Incomplete Attendance (Run daily via cron/scheduler)
CREATE OR REPLACE FUNCTION auto_mark_incomplete_attendance()
RETURNS TABLE (
    marked_count INT,
    details TEXT
) AS $$
DECLARE
    v_count INT;
BEGIN
    -- Mark all records from yesterday that don't have time_out as incomplete
    UPDATE daily_attendance_log
    SET status = 'incomplete',
        forgot_timeout = TRUE,
        updated_at = NOW()
    WHERE attend_date < CURRENT_DATE
    AND time_out IS NULL
    AND status = 'active';
    
    GET DIAGNOSTICS v_count = ROW_COUNT;
    
    RETURN QUERY 
    SELECT 
        v_count as marked_count,
        format('Marked %s record(s) as incomplete for date(s) before %s', v_count, CURRENT_DATE) as details;
END;
$$ LANGUAGE plpgsql;

COMMENT ON FUNCTION auto_mark_incomplete_attendance IS 
'Scheduled function to auto-mark forgotten time outs. Should be run daily at midnight.';


CREATE OR REPLACE FUNCTION update_daily_attendance_timestamp()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = NOW();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_daily_attendance_updated_at
    BEFORE UPDATE ON daily_attendance_log
    FOR EACH ROW
EXECUTE FUNCTION update_daily_attendance_timestamp();

COMMENT ON TRIGGER trg_daily_attendance_updated_at ON daily_attendance_log IS 
'Auto-updates updated_at timestamp on every record modification';



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
LEFT JOIN attendance a ON dal.daily_attend_id = a.daily_attend_id
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
LEFT JOIN attendance a ON dal.daily_attend_id = a.daily_attend_id
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

SELECT * FROM academic_terms;
SELECT * FROM administration;
SELECT * FROM administration_audit;
SELECT * FROM authentication_audit
SELECT * FROM attendance;
SELECT * FROM class_schedules;
SELECT * FROM classes;
SELECT * FROM courses;
SELECT * FROM departments;
SELECT * FROM enrollment;
SELECT * FROM faculty;
SELECT * FROM fingerprint_templates;
SELECT * FROM programs;
SELECT * FROM rooms;
SELECT * FROM scan_logs;
SELECT * FROM sections;
SELECT * FROM student_id_pool;
SELECT * FROM students;
SELECT * FROM user_roles;
SELECT * FROM users;
