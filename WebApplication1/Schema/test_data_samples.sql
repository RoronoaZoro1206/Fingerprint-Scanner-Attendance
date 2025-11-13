-- ============================================================
-- SAMPLE DATA FOR TESTING DAILY ATTENDANCE SYSTEM
-- ============================================================
-- Use these queries to test the new daily attendance system
-- ============================================================

BEGIN;

-- ============================================================
-- TEST SCENARIO 1: Normal Time In/Time Out Flow
-- ============================================================

-- Test 1.1: Student times in at 7:30 AM
SELECT * FROM record_time_in(
    1,  -- Replace with actual student ID
    '2025-01-09 07:30:00'::TIMESTAMP
);

-- Verify time in was recorded
SELECT * FROM daily_attendance_log 
WHERE stud_id = 1 AND attend_date = '2025-01-09';

-- Test 1.2: Student times out at 5:00 PM
SELECT * FROM record_time_out(
    1,  -- Same student ID
    '2025-01-09 17:00:00'::TIMESTAMP
);

-- Verify time out was recorded
SELECT * FROM daily_attendance_log 
WHERE stud_id = 1 AND attend_date = '2025-01-09';

-- ============================================================
-- TEST SCENARIO 2: Forgot to Time Out
-- ============================================================

-- Test 2.1: Student times in yesterday but forgot to time out
INSERT INTO daily_attendance_log (stud_id, attend_date, time_in, status)
VALUES (
    2,  -- Different student ID
    CURRENT_DATE - INTERVAL '1 day',
    (CURRENT_DATE - INTERVAL '1 day' + TIME '08:00:00')::TIMESTAMP,
    'active'
);

-- Test 2.2: Student tries to time in today (should flag yesterday as incomplete)
SELECT * FROM record_time_in(2, NOW());

-- Verify yesterday was marked as forgot_timeout
SELECT * FROM daily_attendance_log 
WHERE stud_id = 2 
AND attend_date = CURRENT_DATE - INTERVAL '1 day';

-- ============================================================
-- TEST SCENARIO 3: Check Incomplete Records
-- ============================================================

-- Test 3.1: Check for incomplete records
SELECT * FROM check_incomplete_attendance(2);

-- Test 3.2: View all incomplete records in system
SELECT 
    dal.stud_id,
    s.stud_number,
    u.user_first_name || ' ' || u.user_last_name as student_name,
    dal.attend_date,
    dal.time_in,
    dal.forgot_timeout
FROM daily_attendance_log dal
JOIN students s ON dal.stud_id = s.stud_id
JOIN users u ON s.user_id = u.user_id
WHERE dal.status = 'incomplete'
ORDER BY dal.attend_date DESC;

-- ============================================================
-- TEST SCENARIO 4: Get Daily Attendance Record
-- ============================================================

-- Test 4.1: Get today's attendance record for a student
SELECT * FROM get_daily_attendance_record(1, CURRENT_DATE);

-- Test 4.2: Check if student can time in/out
SELECT 
    stud_id,
    time_in,
    time_out,
    status,
    can_time_in,
    can_time_out
FROM get_daily_attendance_record(1, CURRENT_DATE);

-- ============================================================
-- TEST SCENARIO 5: Auto-Mark Incomplete Attendance
-- ============================================================

-- Test 5.1: Create several old active records (simulate forgot to time out)
INSERT INTO daily_attendance_log (stud_id, attend_date, time_in, status)
SELECT 
    s.stud_id,
    CURRENT_DATE - (n || ' days')::INTERVAL,
    (CURRENT_DATE - (n || ' days')::INTERVAL + TIME '08:00:00')::TIMESTAMP,
    'active'
FROM students s
CROSS JOIN generate_series(2, 5) as n
WHERE s.stud_id <= 3
ON CONFLICT (stud_id, attend_date) DO NOTHING;

-- Test 5.2: Run auto-mark function
SELECT * FROM auto_mark_incomplete_attendance();

-- Test 5.3: Verify records were marked
SELECT 
    attend_date,
    COUNT(*) as incomplete_count
FROM daily_attendance_log
WHERE status = 'incomplete'
GROUP BY attend_date
ORDER BY attend_date DESC;

-- ============================================================
-- TEST SCENARIO 6: View Today's Attendance Summary
-- ============================================================

-- Test 6.1: View complete today's summary
SELECT * FROM v_today_attendance_summary
ORDER BY time_in DESC;

-- Test 6.2: View students who forgot to time out
SELECT 
    stud_number,
    user_first_name || ' ' || user_last_name as student_name,
    attend_date,
    time_in
FROM v_today_attendance_summary
WHERE forgot_timeout = TRUE;

-- ============================================================
-- TEST SCENARIO 7: View Student Schedule
-- ============================================================

-- Test 7.1: View student's schedule for Monday
SELECT 
    course_code,
    course_name,
    sched_start_time,
    sched_end_time,
    room_code,
    faculty_first_name || ' ' || faculty_last_name as faculty_name
FROM v_student_daily_schedule
WHERE stud_id = 1
AND sched_day_of_week = 'Monday'
ORDER BY sched_start_time;

-- Test 7.2: View all enrolled students' schedules for today
SELECT 
    stud_number,
    user_first_name || ' ' || user_last_name as student_name,
    course_code,
    course_name,
    sched_start_time,
    sched_end_time,
    room_code
FROM v_student_daily_schedule
WHERE sched_day_of_week = TO_CHAR(CURRENT_DATE, 'Day')
ORDER BY stud_number, sched_start_time;

-- ============================================================
-- TEST SCENARIO 8: View Attendance History
-- ============================================================

-- Test 8.1: View student's attendance history
SELECT * FROM v_student_attendance_history
WHERE stud_id = 1
ORDER BY attend_date DESC
LIMIT 10;

-- Test 8.2: View attendance statistics by student
SELECT 
    stud_number,
    student_name,
    COUNT(*) as total_days,
    SUM(CASE WHEN status = 'completed' THEN 1 ELSE 0 END) as completed_days,
    SUM(CASE WHEN status = 'incomplete' THEN 1 ELSE 0 END) as incomplete_days,
    SUM(CASE WHEN forgot_timeout THEN 1 ELSE 0 END) as forgot_timeout_days,
    ROUND(AVG(hours_in_school), 2) as avg_hours_per_day
FROM v_student_attendance_history
GROUP BY stud_number, student_name
ORDER BY total_days DESC;

-- ============================================================
-- TEST SCENARIO 9: Link Attendance Records to Daily Log
-- ============================================================

-- Test 9.1: Create class-specific attendance records linked to daily log
DO $$
DECLARE
    v_daily_attend_id INT;
    v_class_record RECORD;
BEGIN
    -- Get today's daily attendance log ID for student
    SELECT daily_attend_id INTO v_daily_attend_id
    FROM daily_attendance_log
    WHERE stud_id = 1 
    AND attend_date = CURRENT_DATE;
    
    -- If exists, create attendance records for each class
    IF v_daily_attend_id IS NOT NULL THEN
        FOR v_class_record IN (
            SELECT DISTINCT class_id
            FROM v_student_daily_schedule
            WHERE stud_id = 1
            AND sched_day_of_week = TO_CHAR(CURRENT_DATE, 'Day')
        ) LOOP
            INSERT INTO attendance (
                daily_attend_id,
                class_id,
                stud_id,
                attend_date,
                attend_status,
                is_present
            )
            VALUES (
                v_daily_attend_id,
                v_class_record.class_id,
                1,
                CURRENT_DATE,
                'present',
                TRUE
            )
            ON CONFLICT (class_id, stud_id, attend_date) DO NOTHING;
        END LOOP;
    END IF;
END $$;

-- Test 9.2: Verify linked attendance records
SELECT 
    dal.attend_date,
    dal.time_in,
    dal.time_out,
    c.class_id,
    co.course_code,
    co.course_name,
    a.attend_status,
    a.is_present
FROM daily_attendance_log dal
JOIN attendance a ON dal.daily_attend_id = a.daily_attend_id
JOIN classes c ON a.class_id = c.class_id
JOIN courses co ON c.course_id = co.course_id
WHERE dal.stud_id = 1
AND dal.attend_date = CURRENT_DATE;

-- ============================================================
-- TEST SCENARIO 10: Error Handling Tests
-- ============================================================

-- Test 10.1: Try to time in twice on same day (should fail)
SELECT * FROM record_time_in(1, NOW());

-- Test 10.2: Try to time out without time in (should fail)
SELECT * FROM record_time_out(999, NOW());  -- Use non-existent student

-- Test 10.3: Try to time out before time in (should fail)
SELECT * FROM record_time_out(
    1,
    '2025-01-09 06:00:00'::TIMESTAMP  -- Before time in
);

-- Test 10.4: Try to insert duplicate record (should fail due to unique constraint)
INSERT INTO daily_attendance_log (stud_id, attend_date, time_in, status)
VALUES (1, CURRENT_DATE, NOW(), 'active');
-- Expected: ERROR: duplicate key value violates unique constraint

-- ============================================================
-- TEST SCENARIO 11: Performance Testing
-- ============================================================

-- Test 11.1: Bulk insert test (create 100 daily attendance records)
INSERT INTO daily_attendance_log (stud_id, attend_date, time_in, status)
SELECT 
    s.stud_id,
    CURRENT_DATE - (n || ' days')::INTERVAL,
    (CURRENT_DATE - (n || ' days')::INTERVAL + TIME '08:00:00')::TIMESTAMP,
    'completed'
FROM students s
CROSS JOIN generate_series(1, 100) as n
WHERE s.stud_id <= 10
ON CONFLICT (stud_id, attend_date) DO NOTHING;

-- Test 11.2: Query performance test
EXPLAIN ANALYZE
SELECT * FROM v_today_attendance_summary;

EXPLAIN ANALYZE
SELECT * FROM get_daily_attendance_record(1, CURRENT_DATE);

-- ============================================================
-- CLEANUP TEST DATA
-- ============================================================

ROLLBACK;  -- Rollback all test transactions

-- OR manually delete test data:
-- DELETE FROM daily_attendance_log WHERE stud_id IN (1, 2);
-- DELETE FROM attendance WHERE stud_id IN (1, 2) AND attend_date = CURRENT_DATE;

-- ============================================================
-- SAMPLE PRODUCTION USAGE
-- ============================================================

-- Example 1: Time In Workflow (from fingerprint scan)
/*
DO $$
DECLARE
    v_student_id INT := 1;  -- From fingerprint match
    v_result RECORD;
BEGIN
    -- Record time in
    SELECT * INTO v_result
    FROM record_time_in(v_student_id, NOW());
    
    -- Check if successful
    IF v_result.success THEN
        RAISE NOTICE 'Time in successful for student %', v_student_id;
        
        -- If there's an incomplete record, show warning
        IF v_result.incomplete_date IS NOT NULL THEN
            RAISE WARNING 'Student forgot to time out on %', v_result.incomplete_date;
        END IF;
    ELSE
        RAISE EXCEPTION 'Time in failed: %', v_result.message;
    END IF;
END $$;
*/

-- Example 2: Time Out Workflow (from fingerprint scan)
/*
DO $$
DECLARE
    v_student_id INT := 1;  -- From fingerprint match
    v_result RECORD;
BEGIN
    -- Record time out
    SELECT * INTO v_result
    FROM record_time_out(v_student_id, NOW());
    
    -- Check if successful
    IF v_result.success THEN
        RAISE NOTICE 'Time out successful. Duration: % hours', v_result.duration_hours;
    ELSE
        RAISE EXCEPTION 'Time out failed: %', v_result.message;
    END IF;
END $$;
*/

-- Example 3: Check Current Status Before Scan
/*
SELECT 
    CASE 
        WHEN can_time_in THEN 'ALLOW_TIME_IN'
        WHEN can_time_out THEN 'ALLOW_TIME_OUT'
        ELSE 'ALREADY_COMPLETED'
    END as action,
    time_in,
    time_out,
    status
FROM get_daily_attendance_record(1, CURRENT_DATE);
*/

-- ============================================================
SELECT 'Test data script completed. Use BEGIN/COMMIT to test individually.' as status;
