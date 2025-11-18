-- ============================================================
-- DATA REPAIR SCRIPT: Fix Student ID Pool Inconsistency
-- ============================================================
-- Purpose: Mark student IDs as "Used" when they are assigned to students
-- Date: 2025-01-18
-- Issue: student_id_pool.is_used is FALSE even when IDs are assigned
--
-- INSTRUCTIONS:
-- 1. Run this script ONCE to fix existing data
-- 2. The database triggers will handle future changes automatically
-- ============================================================

-- Step 1: Backup current state (for safety)
CREATE TABLE IF NOT EXISTS student_id_pool_backup_20250118 AS
SELECT * FROM student_id_pool;

-- Step 2: Display BEFORE state (for verification)
SELECT 
    'BEFORE REPAIR' as status,
    COUNT(*) as total_ids,
    SUM(CASE WHEN is_used = TRUE THEN 1 ELSE 0 END) as used_ids,
    SUM(CASE WHEN is_used = FALSE THEN 1 ELSE 0 END) as available_ids
FROM student_id_pool;

-- Step 3: Show which IDs are incorrectly marked as "Available"
SELECT 
    'INCONSISTENT DATA' as status,
    sip.student_id_code,
    sip.is_used as pool_status,
    s.stud_number as assigned_to_student,
    s.stud_id,
    u.user_first_name || ' ' || u.user_last_name as student_name
FROM student_id_pool sip
INNER JOIN students s ON sip.student_id_code = s.stud_number
LEFT JOIN users u ON s.user_id = u.user_id
WHERE sip.is_used = FALSE;  -- Should be TRUE but is FALSE

-- Step 4: FIX THE DATA - Mark student IDs as used for existing students
UPDATE student_id_pool sip
SET 
    is_used = TRUE,
    used_at = COALESCE(s.stud_created_at, NOW()),  -- Use student creation time if available
    remarks = COALESCE(sip.remarks, '') || 
              ' | Data repair: Marked as used for student ' || 
              (SELECT user_first_name || ' ' || user_last_name 
               FROM users u 
               WHERE u.user_id = s.user_id) || 
              ' on ' || NOW()::TEXT
FROM students s
WHERE sip.student_id_code = s.stud_number
  AND sip.is_used = FALSE;  -- Only update incorrectly marked entries

-- Step 5: Display AFTER state (for verification)
SELECT 
    'AFTER REPAIR' as status,
    COUNT(*) as total_ids,
    SUM(CASE WHEN is_used = TRUE THEN 1 ELSE 0 END) as used_ids,
    SUM(CASE WHEN is_used = FALSE THEN 1 ELSE 0 END) as available_ids
FROM student_id_pool;

-- Step 6: Verify repair by showing all used IDs with student names
SELECT 
    'VERIFICATION' as status,
    sip.student_id_code,
    sip.is_used,
    sip.used_at,
    s.stud_number,
    u.user_first_name || ' ' || u.user_last_name as student_name,
    u.user_email
FROM student_id_pool sip
LEFT JOIN students s ON sip.student_id_code = s.stud_number
LEFT JOIN users u ON s.user_id = u.user_id
WHERE sip.is_used = TRUE
ORDER BY sip.student_id_code;

-- Step 7: Show summary report
SELECT 
    '=== DATA REPAIR SUMMARY ===' as report;

SELECT 
    'Student IDs repaired' as metric,
    COUNT(*) as count
FROM student_id_pool sip
INNER JOIN students s ON sip.student_id_code = s.stud_number
WHERE sip.remarks LIKE '%Data repair%';

-- ============================================================
-- EXPECTED RESULTS:
-- ============================================================
-- Before Repair:
--   - Total IDs: 50
--   - Used: 0 (INCORRECT)
--   - Available: 50 (INCORRECT)
--
-- After Repair:
--   - Total IDs: 50
--   - Used: 1 (2025-0001 for Cristen Reyes)
--   - Available: 49
--
-- Verification:
--   - 2025-0001 should show:
--     * is_used = TRUE
--     * used_at = (timestamp when student was created)
--     * Student: Cristen Reyes
--     * Email: reyescristen9@gmail.com
-- ============================================================

-- ROLLBACK (if needed - run this to undo changes):
-- UPDATE student_id_pool sip
-- SET is_used = b.is_used,
--     used_at = b.used_at,
--     remarks = b.remarks
-- FROM student_id_pool_backup_20250118 b
-- WHERE sip.pool_id = b.pool_id;
