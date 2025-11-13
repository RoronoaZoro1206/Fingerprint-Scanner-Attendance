-- ============================================================
-- VERIFICATION QUERIES FOR DAILY ATTENDANCE SYSTEM
-- ============================================================

-- ============================================================
-- PART 1: TABLE STRUCTURE VERIFICATION
-- ============================================================

-- Check if daily_attendance_log table exists
SELECT 
    table_name,
    table_type
FROM information_schema.tables
WHERE table_name = 'daily_attendance_log';

-- Verify daily_attendance_log structure
SELECT 
    column_name,
    data_type,
    character_maximum_length,
    is_nullable,
    column_default
FROM information_schema.columns
WHERE table_name = 'daily_attendance_log'
ORDER BY ordinal_position;

-- Check constraints on daily_attendance_log
SELECT
    tc.constraint_name,
    tc.constraint_type,
    kcu.column_name,
    ccu.table_name AS foreign_table_name,
    ccu.column_name AS foreign_column_name
FROM information_schema.table_constraints tc
LEFT JOIN information_schema.key_column_usage kcu
    ON tc.constraint_name = kcu.constraint_name
LEFT JOIN information_schema.constraint_column_usage ccu
    ON tc.constraint_name = ccu.constraint_name
WHERE tc.table_name = 'daily_attendance_log'
ORDER BY tc.constraint_type, tc.constraint_name;

-- ============================================================
-- PART 2: INDEX VERIFICATION
-- ============================================================

-- List all indexes on daily_attendance_log
SELECT 
    schemaname,
    tablename,
    indexname,
    indexdef
FROM pg_indexes
WHERE tablename = 'daily_attendance_log'
ORDER BY indexname;

-- Check index usage statistics (run after some usage)
SELECT 
    schemaname,
    tablename,
    indexname,
    idx_scan as index_scans,
    idx_tup_read as tuples_read,
    idx_tup_fetch as tuples_fetched
FROM pg_stat_user_indexes
WHERE tablename = 'daily_attendance_log'
ORDER BY idx_scan DESC;

-- ============================================================
-- PART 3: VIEW VERIFICATION
-- ============================================================

-- List all attendance-related views
SELECT 
    table_name as view_name,
    view_definition
FROM information_schema.views
WHERE table_name LIKE 'v_%attendance%'
ORDER BY table_name;

-- Test v_student_daily_schedule view
SELECT COUNT(*) as total_records FROM v_student_daily_schedule;

-- Test v_today_attendance_summary view
SELECT COUNT(*) as total_records FROM v_today_attendance_summary;

-- Test v_student_attendance_history view (limit for safety)
SELECT COUNT(*) as total_records FROM v_student_attendance_history;

-- ============================================================
-- PART 4: FUNCTION VERIFICATION
-- ============================================================

-- List all attendance-related functions
SELECT 
    routine_name,
    routine_type,
    data_type as return_type,
    type_udt_name
FROM information_schema.routines
WHERE routine_name IN (
    'get_daily_attendance_record',
    'check_incomplete_attendance',
    'record_time_in',
    'record_time_out',
    'auto_mark_incomplete_attendance',
    'migrate_existing_attendance_to_daily_log',
    'update_daily_attendance_timestamp'
)
ORDER BY routine_name;

-- ============================================================
-- PART 5: FUNCTIONAL TESTS
-- ============================================================

-- Test 1: Test get_daily_attendance_record function
-- Replace 1 with actual student ID
SELECT * FROM get_daily_attendance_record(1, CURRENT_DATE);

-- Test 2: Test check_incomplete_attendance function
-- Replace 1 with actual student ID
SELECT * FROM check_incomplete_attendance(1);

-- Test 3: Verify backup table exists
SELECT 
    COUNT(*) as backup_record_count 
FROM attendance_backup_20250109;

-- Test 4: Check attendance table modifications
SELECT 
    column_name,
    data_type,
    is_nullable
FROM information_schema.columns
WHERE table_name = 'attendance'
AND column_name IN ('daily_attend_id', 'is_present')
ORDER BY column_name;

-- ============================================================
-- PART 6: DATA INTEGRITY CHECKS
-- ============================================================

-- Check for any NULL time_in values (should be none for active/completed records)
SELECT COUNT(*) as null_time_in_count
FROM daily_attendance_log
WHERE time_in IS NULL;

-- Check for invalid time_out (before time_in)
SELECT COUNT(*) as invalid_time_out_count
FROM daily_attendance_log
WHERE time_out IS NOT NULL 
AND time_out <= time_in;

-- Check unique constraint (should return 0 duplicates)
SELECT 
    stud_id, 
    attend_date, 
    COUNT(*) as duplicate_count
FROM daily_attendance_log
GROUP BY stud_id, attend_date
HAVING COUNT(*) > 1;

-- Check status distribution
SELECT 
    status,
    COUNT(*) as count,
    ROUND(COUNT(*) * 100.0 / SUM(COUNT(*)) OVER (), 2) as percentage
FROM daily_attendance_log
GROUP BY status
ORDER BY count DESC;

-- Check forgot_timeout flagged records
SELECT 
    COUNT(*) as forgot_timeout_count,
    MIN(attend_date) as earliest_date,
    MAX(attend_date) as latest_date
FROM daily_attendance_log
WHERE forgot_timeout = TRUE;

-- ============================================================
-- PART 7: PERFORMANCE VERIFICATION
-- ============================================================

-- Check table size
SELECT 
    schemaname,
    tablename,
    pg_size_pretty(pg_total_relation_size(schemaname||'.'||tablename)) AS size
FROM pg_tables
WHERE tablename IN ('daily_attendance_log', 'attendance', 'attendance_backup_20250109')
ORDER BY pg_total_relation_size(schemaname||'.'||tablename) DESC;

-- Check index size
SELECT 
    schemaname,
    tablename,
    indexname,
    pg_size_pretty(pg_relation_size(indexrelid)) as index_size
FROM pg_stat_user_indexes
WHERE tablename = 'daily_attendance_log'
ORDER BY pg_relation_size(indexrelid) DESC;

-- ============================================================
-- PART 8: ATTENDANCE TABLE LINKAGE VERIFICATION
-- ============================================================

-- Check how many attendance records are linked to daily_attendance_log
SELECT 
    COUNT(*) as total_attendance_records,
    COUNT(daily_attend_id) as linked_to_daily_log,
    COUNT(*) - COUNT(daily_attend_id) as not_linked
FROM attendance;

-- Check orphaned attendance records (not linked to any daily log)
SELECT COUNT(*) as orphaned_count
FROM attendance a
WHERE a.daily_attend_id IS NOT NULL
AND NOT EXISTS (
    SELECT 1 FROM daily_attendance_log dal
    WHERE dal.daily_attend_id = a.daily_attend_id
);

-- ============================================================
-- PART 9: SAMPLE DATA QUERIES
-- ============================================================

-- Get today's attendance summary
SELECT * FROM v_today_attendance_summary
ORDER BY time_in DESC
LIMIT 10;

-- Get recent attendance history for a student (replace 1 with actual student ID)
SELECT * FROM v_student_attendance_history
WHERE stud_id = 1
ORDER BY attend_date DESC
LIMIT 10;

-- Get student's schedule for a specific day
SELECT 
    course_code,
    course_name,
    sched_day_of_week,
    sched_start_time,
    sched_end_time,
    room_code,
    faculty_first_name || ' ' || faculty_last_name as faculty_name
FROM v_student_daily_schedule
WHERE stud_id = 1
AND sched_day_of_week = TO_CHAR(CURRENT_DATE, 'Day')
ORDER BY sched_start_time;

-- ============================================================
-- PART 10: TRIGGER VERIFICATION
-- ============================================================

-- Check if trigger exists
SELECT 
    trigger_name,
    event_manipulation,
    event_object_table,
    action_statement
FROM information_schema.triggers
WHERE trigger_name = 'trg_daily_attendance_updated_at';

-- Test trigger (insert and update a test record)
-- DO NOT RUN IN PRODUCTION WITHOUT PROPER TEST STUDENT ID
-- BEGIN;
-- INSERT INTO daily_attendance_log (stud_id, attend_date, time_in, status)
-- VALUES (999999, CURRENT_DATE, NOW(), 'active');
-- 
-- SELECT created_at, updated_at FROM daily_attendance_log WHERE stud_id = 999999;
-- 
-- -- Wait a second, then update
-- SELECT pg_sleep(1);
-- UPDATE daily_attendance_log SET remarks = 'Test' WHERE stud_id = 999999;
-- 
-- SELECT created_at, updated_at FROM daily_attendance_log WHERE stud_id = 999999;
-- 
-- ROLLBACK;

-- ============================================================
-- VERIFICATION COMPLETE
-- ============================================================

SELECT 'Migration verification queries completed' as status;
