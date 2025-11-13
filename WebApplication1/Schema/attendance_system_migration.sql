-- ============================================================
-- MIGRATION: Time In/Time Out Daily Attendance System
-- Date: 2025-01-09
-- Description: Implements daily attendance log with toggle logic
-- Version: 1.0.0
-- ============================================================
-- 
-- OVERVIEW:
-- This migration transforms the attendance system from a class-specific 
-- scan-based model to a daily Time In/Time Out toggle system where:
--   1. Students scan ONCE per day to Time In (applies to ALL classes)
--   2. Students scan ONCE per day to Time Out (end of day)
--   3. Late/Absent status is determined per class based on Time In
--   4. System tracks "forgot to time out" scenarios
--
-- CHANGES:
--   - NEW: daily_attendance_log table (primary daily time tracking)
--   - MODIFIED: attendance table (now links to daily log, stores class-specific status)
--   - NEW: Helper views for attendance summaries
--   - NEW: Database functions for attendance operations
--
-- ============================================================

BEGIN;

-- ============================================================
-- STEP 1: BACKUP EXISTING DATA
-- ============================================================

-- Create backup of current attendance table
CREATE TABLE IF NOT EXISTS attendance_backup_20250109 AS
SELECT * FROM attendance;

COMMENT ON TABLE attendance_backup_20250109 IS 
'Backup of attendance table before daily time in/out migration - Created: 2025-01-09';

-- ============================================================
-- STEP 2: CREATE DAILY ATTENDANCE LOG TABLE
-- ============================================================

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

-- Table comments
COMMENT ON TABLE daily_attendance_log IS 
'Primary table for daily Time In/Time Out tracking. Each student has ONE record per day regardless of number of classes.';

COMMENT ON COLUMN daily_attendance_log.time_in IS 
'Timestamp when student scanned fingerprint to Time In (start of day)';

COMMENT ON COLUMN daily_attendance_log.time_out IS 
'Timestamp when student scanned fingerprint to Time Out (end of day). NULL if still active.';

COMMENT ON COLUMN daily_attendance_log.status IS 
'Current status: active (timed in), completed (timed out), incomplete (forgot to time out)';

COMMENT ON COLUMN daily_attendance_log.forgot_timeout IS 
'Flagged TRUE if student forgot to time out and system auto-marked as incomplete';

-- ============================================================
-- STEP 3: CREATE INDEXES FOR DAILY ATTENDANCE LOG
-- ============================================================

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
-- STEP 4: MODIFY ATTENDANCE TABLE
-- ============================================================

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

-- ============================================================
-- STEP 5: CREATE SUPPORTING VIEWS
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

-- ============================================================
-- STEP 6: CREATE DATABASE FUNCTIONS
-- ============================================================

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

-- ============================================================
-- STEP 7: CREATE TRIGGER FOR UPDATED_AT
-- ============================================================

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
-- STEP 8: DATA MIGRATION HELPER FUNCTION (OPTIONAL)
-- ============================================================

CREATE OR REPLACE FUNCTION migrate_existing_attendance_to_daily_log()
RETURNS TABLE (
    migrated_records INT,
    details TEXT
) AS $$
DECLARE
    v_count INT := 0;
    v_record RECORD;
BEGIN
    -- Migrate existing attendance records to daily log
    -- This creates ONE daily_attendance_log entry per student per date
    -- using the earliest 'in' scan as time_in and latest 'out' scan as time_out
    
    FOR v_record IN (
        SELECT 
            stud_id,
            attend_date,
            MIN(CASE WHEN attend_type = 'in' THEN attend_scan_time END) as first_time_in,
            MAX(CASE WHEN attend_type = 'out' THEN attend_scan_time END) as last_time_out
        FROM attendance_backup_20250109
        WHERE stud_id IS NOT NULL
        GROUP BY stud_id, attend_date
        ORDER BY stud_id, attend_date
    ) LOOP
        -- Insert into daily_attendance_log
        INSERT INTO daily_attendance_log (
            stud_id,
            attend_date,
            time_in,
            time_out,
            status,
            created_at,
            updated_at
        )
        VALUES (
            v_record.stud_id,
            v_record.attend_date,
            v_record.first_time_in,
            v_record.last_time_out,
            CASE 
                WHEN v_record.last_time_out IS NOT NULL THEN 'completed'
                WHEN v_record.attend_date < CURRENT_DATE THEN 'incomplete'
                ELSE 'active'
            END,
            NOW(),
            NOW()
        )
        ON CONFLICT (stud_id, attend_date) DO NOTHING;
        
        v_count := v_count + 1;
    END LOOP;
    
    RETURN QUERY 
    SELECT 
        v_count as migrated_records,
        format('Migrated %s daily attendance records from backup', v_count) as details;
END;
$$ LANGUAGE plpgsql;

COMMENT ON FUNCTION migrate_existing_attendance_to_daily_log IS 
'One-time migration function to convert old attendance records to new daily log format';

-- ============================================================
-- STEP 9: GRANT PERMISSIONS (Adjust as needed)
-- ============================================================

-- Grant permissions on new table to application role
-- UNCOMMENT and modify based on your database roles:
-- GRANT SELECT, INSERT, UPDATE, DELETE ON daily_attendance_log TO app_user;
-- GRANT USAGE ON SEQUENCE daily_attendance_log_daily_attend_id_seq TO app_user;
-- GRANT SELECT ON v_student_daily_schedule TO app_user;
-- GRANT SELECT ON v_today_attendance_summary TO app_user;
-- GRANT SELECT ON v_student_attendance_history TO app_user;

-- ============================================================
-- MIGRATION COMPLETE
-- ============================================================

COMMIT;

-- ============================================================
-- POST-MIGRATION STEPS (MANUAL)
-- ============================================================
-- 
-- 1. Run data migration (if needed):
--    SELECT * FROM migrate_existing_attendance_to_daily_log();
--
-- 2. Set up scheduled job for auto-marking incomplete:
--    Schedule: Daily at 00:00 (midnight)
--    Query: SELECT * FROM auto_mark_incomplete_attendance();
--
-- 3. Update application code to:
--    - Use daily_attendance_log for Time In/Time Out operations
--    - Link attendance records to daily_attend_id
--    - Call get_daily_attendance_record() before allowing scan
--    - Handle incomplete_date warnings when timing in
--
-- 4. Test the new system:
--    - Test time in operation
--    - Test time out operation
--    - Test forgot to time out scenario
--    - Test daily auto-mark function
--
-- ============================================================

-- ============================================================
-- VERIFICATION QUERIES
-- ============================================================

-- Verify table creation
SELECT 
    table_name, 
    (SELECT COUNT(*) FROM information_schema.columns WHERE table_name = t.table_name) as column_count
FROM information_schema.tables t
WHERE table_name IN ('daily_attendance_log', 'attendance', 'attendance_backup_20250109')
ORDER BY table_name;

-- Verify indexes
SELECT 
    schemaname,
    tablename,
    indexname,
    indexdef
FROM pg_indexes
WHERE tablename = 'daily_attendance_log'
ORDER BY indexname;

-- Verify views
SELECT 
    table_name as view_name,
    view_definition
FROM information_schema.views
WHERE table_name LIKE 'v_%attendance%'
ORDER BY table_name;

-- Verify functions
SELECT 
    routine_name,
    routine_type,
    data_type as return_type
FROM information_schema.routines
WHERE routine_name IN (
    'get_daily_attendance_record',
    'check_incomplete_attendance',
    'record_time_in',
    'record_time_out',
    'auto_mark_incomplete_attendance',
    'migrate_existing_attendance_to_daily_log'
)
ORDER BY routine_name;

-- ============================================================
-- ROLLBACK INSTRUCTIONS
-- ============================================================
-- 
-- IF YOU NEED TO ROLLBACK THIS MIGRATION:
--
-- BEGIN;
--
-- -- 1. Drop functions
-- DROP FUNCTION IF EXISTS migrate_existing_attendance_to_daily_log();
-- DROP FUNCTION IF EXISTS auto_mark_incomplete_attendance();
-- DROP FUNCTION IF EXISTS record_time_out(INT, TIMESTAMP);
-- DROP FUNCTION IF EXISTS record_time_in(INT, TIMESTAMP);
-- DROP FUNCTION IF EXISTS check_incomplete_attendance(INT);
-- DROP FUNCTION IF EXISTS get_daily_attendance_record(INT, DATE);
-- DROP FUNCTION IF EXISTS update_daily_attendance_timestamp();
--
-- -- 2. Drop views
-- DROP VIEW IF EXISTS v_student_attendance_history;
-- DROP VIEW IF EXISTS v_today_attendance_summary;
-- DROP VIEW IF EXISTS v_student_daily_schedule;
--
-- -- 3. Restore attendance table
-- ALTER TABLE attendance DROP COLUMN IF EXISTS daily_attend_id;
-- ALTER TABLE attendance DROP COLUMN IF EXISTS is_present;
-- ALTER TABLE attendance DROP CONSTRAINT IF EXISTS attendance_class_student_date_unique;
--
-- -- Restore old constraint
-- ALTER TABLE attendance ADD CONSTRAINT attendance_class_id_stud_id_attend_scan_time_attend_type_key 
--     UNIQUE (class_id, stud_id, attend_scan_time, attend_type);
--
-- -- 4. Drop new table
-- DROP TABLE IF EXISTS daily_attendance_log CASCADE;
--
-- -- 5. Restore data from backup (if needed)
-- -- TRUNCATE attendance;
-- -- INSERT INTO attendance SELECT * FROM attendance_backup_20250109;
--
-- COMMIT;
-- 
-- ============================================================
