-- ============================================================
-- FIX MISSING STUDENT DATA
-- Run this script to identify and fix students with missing data
-- ============================================================

-- ============================================================
-- DIAGNOSTIC: Find students with missing data
-- ============================================================

-- Query 1: Find students with missing user names
SELECT 
    e.enroll_id,
    e.class_id,
    s.stud_id,
    s.stud_number,
    u.user_id,
    u.user_first_name,
    u.user_last_name,
    u.user_email,
    p.program_name,
    s.stud_year_level,
    CASE 
        WHEN TRIM(COALESCE(u.user_first_name, '')) = '' AND TRIM(COALESCE(u.user_last_name, '')) = '' 
        THEN '❌ MISSING NAME'
        ELSE '✅ Has Name'
    END as name_status,
    CASE 
        WHEN TRIM(COALESCE(s.stud_number, '')) = '' OR s.stud_number LIKE 'TEMP-%'
        THEN '❌ MISSING/TEMP ID'
        ELSE '✅ Has ID'
    END as id_status,
    CASE 
        WHEN s.program_id IS NULL 
        THEN '❌ NO PROGRAM'
        ELSE '✅ Has Program'
    END as program_status
FROM enrollment e
INNER JOIN students s ON e.stud_id = s.stud_id
INNER JOIN users u ON s.user_id = u.user_id
LEFT JOIN programs p ON s.program_id = p.program_id
ORDER BY e.class_id, s.stud_id;

-- ============================================================
-- FIX 1: Update users with missing names (set from email)
-- This extracts the name from the email address as a fallback
-- ============================================================

-- Preview what will be updated
SELECT 
    u.user_id,
    u.user_email,
    u.user_first_name as current_first_name,
    u.user_last_name as current_last_name,
    INITCAP(SPLIT_PART(SPLIT_PART(u.user_email, '@', 1), '.', 1)) as suggested_first_name,
    INITCAP(COALESCE(NULLIF(SPLIT_PART(SPLIT_PART(u.user_email, '@', 1), '.', 2), ''), 'User')) as suggested_last_name
FROM users u
WHERE (TRIM(COALESCE(u.user_first_name, '')) = '' OR TRIM(COALESCE(u.user_last_name, '')) = '')
AND u.role_id = 3;  -- Students only

-- Uncomment to apply the fix:
/*
UPDATE users u
SET 
    user_first_name = INITCAP(SPLIT_PART(SPLIT_PART(u.user_email, '@', 1), '.', 1)),
    user_last_name = INITCAP(COALESCE(NULLIF(SPLIT_PART(SPLIT_PART(u.user_email, '@', 1), '.', 2), ''), 'User')),
    user_updated_at = NOW()
WHERE (TRIM(COALESCE(u.user_first_name, '')) = '' OR TRIM(COALESCE(u.user_last_name, '')) = '')
AND u.role_id = 3;
*/

-- ============================================================
-- FIX 2: Update students with temporary student numbers
-- This assigns proper student numbers from the pool
-- ============================================================

-- Preview students with temporary IDs
SELECT 
    s.stud_id,
    s.stud_number as current_stud_number,
    u.user_email,
    u.user_first_name || ' ' || u.user_last_name as student_name
FROM students s
INNER JOIN users u ON s.user_id = u.user_id
WHERE s.stud_number LIKE 'TEMP-%' OR TRIM(COALESCE(s.stud_number, '')) = '';

-- Preview available student IDs in pool
SELECT 
    pool_id,
    student_id_code,
    is_used
FROM student_id_pool
WHERE is_used = FALSE
ORDER BY pool_id
LIMIT 10;

-- ============================================================
-- FIX 3: Assign programs to students without programs
-- ============================================================

-- Preview students without programs
SELECT 
    s.stud_id,
    s.stud_number,
    u.user_first_name || ' ' || u.user_last_name as student_name,
    s.program_id
FROM students s
INNER JOIN users u ON s.user_id = u.user_id
WHERE s.program_id IS NULL;

-- Preview available programs
SELECT program_id, program_code, program_name FROM programs ORDER BY program_id;

-- Uncomment to assign a default program (e.g., BSIT = program_id 1):
/*
UPDATE students
SET program_id = 1  -- Replace with actual program_id
WHERE program_id IS NULL;
*/

-- ============================================================
-- VERIFICATION: Check the data after fixes
-- ============================================================

-- Verify enrolled students data for a specific class
SELECT 
    e.enroll_id,
    s.stud_id,
    COALESCE(NULLIF(TRIM(s.stud_number), ''), 'N/A') as stud_number,
    CASE 
        WHEN TRIM(COALESCE(u.user_first_name, '')) = '' AND TRIM(COALESCE(u.user_last_name, '')) = '' 
        THEN 'Unknown Student'
        WHEN TRIM(COALESCE(u.user_first_name, '')) = '' 
        THEN TRIM(u.user_last_name)
        WHEN TRIM(COALESCE(u.user_last_name, '')) = '' 
        THEN TRIM(u.user_first_name)
        ELSE TRIM(u.user_first_name) || ' ' || TRIM(u.user_last_name)
    END as full_name,
    COALESCE(NULLIF(TRIM(u.user_email), ''), 'N/A') as user_email,
    COALESCE(NULLIF(TRIM(p.program_name), ''), 'N/A') as program_name,
    COALESCE(s.stud_year_level, 0) as year_level,
    e.enroll_at
FROM enrollment e
INNER JOIN students s ON e.stud_id = s.stud_id
INNER JOIN users u ON s.user_id = u.user_id
LEFT JOIN programs p ON s.program_id = p.program_id
WHERE e.class_id = 7  -- Replace with your class_id
ORDER BY u.user_last_name, u.user_first_name;
