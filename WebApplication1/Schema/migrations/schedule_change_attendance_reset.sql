-- ============================================================
-- MIGRATION: Schedule Change Attendance Reset Feature
-- Date: 2025-01-09
-- Description: When admin modifies a class schedule time, 
--              previously recorded attendance is invalidated
--              so students can re-attend for the new time slot
-- Version: 1.0.0
-- ============================================================
--
-- PROBLEM SOLVED:
-- When an Admin updates a class time (e.g., 4:35 PM to 4:42 PM),
-- students who already attended the original time were blocked 
-- by "Attendance Already Completed" error.
--
-- SOLUTION:
-- 1. Add sched_id to link attendance to specific schedule instance
-- 2. Add is_invalidated flag and invalidation_reason columns
-- 3. When schedule time changes, mark old attendance as invalidated
-- 4. Invalidated attendance allows re-attendance for new schedule
-- 5. Original records preserved for audit trail
--
-- ============================================================

BEGIN;

-- ============================================================
-- STEP 1: ADD COLUMNS TO daily_attendance_log TABLE
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

-- ============================================================
-- STEP 2: ADD SIMILAR COLUMNS TO attendance TABLE
-- ============================================================

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

-- ============================================================
-- STEP 3: CREATE INDEXES FOR PERFORMANCE
-- ============================================================

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

-- ============================================================
-- STEP 4: FUNCTION TO INVALIDATE ATTENDANCE ON SCHEDULE CHANGE
-- ============================================================

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

-- ============================================================
-- STEP 5: MODIFY UNIQUE CONSTRAINT TO ALLOW RE-ATTENDANCE
-- ============================================================
-- The existing unique constraint prevents duplicate attendance per day.
-- We need to modify the check to exclude invalidated records OR add 
-- application-level handling.
--
-- APPROACH: Keep the constraint but check is_invalidated in application code
-- This preserves data integrity while allowing re-attendance after invalidation.
-- ============================================================

-- Create a partial unique index that only applies to non-invalidated records
-- This allows re-attendance after old records are invalidated

-- First, drop the existing constraint if it conflicts
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

-- ============================================================
-- STEP 6: VIEW FOR ACTIVE (NON-INVALIDATED) ATTENDANCE
-- ============================================================

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
-- VERIFICATION QUERIES
-- ============================================================

-- Verify new columns exist
SELECT column_name, data_type, column_default
FROM information_schema.columns
WHERE table_name = 'daily_attendance_log'
  AND column_name IN ('sched_id', 'is_invalidated', 'invalidation_reason', 'invalidated_at')
ORDER BY column_name;

-- Verify indexes exist
SELECT indexname, indexdef
FROM pg_indexes
WHERE tablename = 'daily_attendance_log'
  AND indexname LIKE '%invalidated%' OR indexname LIKE '%sched%';

COMMIT;

-- ============================================================
-- USAGE EXAMPLE
-- ============================================================
-- When admin changes schedule from 4:35 PM to 4:42 PM:
--
-- SELECT * FROM invalidate_attendance_on_schedule_change(
--     123,              -- class_id
--     456,              -- sched_id
--     '16:35:00'::TIME, -- old start time
--     '17:35:00'::TIME, -- old end time
--     '16:42:00'::TIME, -- new start time
--     '17:42:00'::TIME, -- new end time
--     'Monday'          -- day of week
-- );
--
-- After invalidation, students can time in again for the new schedule.
-- ============================================================
