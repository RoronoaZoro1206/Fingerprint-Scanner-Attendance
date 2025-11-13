-- ============================================================
-- DATABASE FIX SCRIPT - Multi-Issue Resolution
-- Run these queries in pgAdmin or your PostgreSQL client
-- ============================================================

-- ============================================================
-- DIAGNOSTIC SECTION - Run these first to understand the issues
-- ============================================================

PRINT '====================';
PRINT 'DIAGNOSTIC QUERIES';
PRINT '====================';

-- Query 1: Check all users and their student records
SELECT 
    u.user_id,
    u.user_email,
    u.user_first_name || ' ' || u.user_last_name as full_name,
    r.role_name,
    CASE 
        WHEN s.stud_id IS NOT NULL THEN 'Has Student Record (ID: ' || s.stud_id || ')'
        ELSE '? MISSING STUDENT RECORD'
    END as student_status,
    s.stud_number,
    p.program_name
FROM users u
JOIN user_roles r ON u.role_id = r.role_id
LEFT JOIN students s ON u.user_id = s.user_id
LEFT JOIN programs p ON s.program_id = p.program_id
WHERE r.role_name = 'student'
ORDER BY u.user_email;

PRINT '';
PRINT 'Expected: All students should have student records';
PRINT '---';

-- Query 2: Check classes and their enrollments
SELECT 
    c.class_id,
    co.course_code,
    co.course_name,
    r.room_code,
    u.user_first_name || ' ' || u.user_last_name as faculty_name,
    t.term_name,
    COUNT(e.enroll_id) as enrolled_students,
    CASE 
        WHEN COUNT(e.enroll_id) = 0 THEN '? NO STUDENTS ENROLLED'
        ELSE '? ' || COUNT(e.enroll_id) || ' students enrolled'
    END as enrollment_status
FROM classes c
LEFT JOIN courses co ON c.course_id = co.course_id
LEFT JOIN rooms r ON c.room_id = r.room_id
LEFT JOIN faculty f ON c.fac_id = f.fac_id
LEFT JOIN users u ON f.user_id = u.user_id
LEFT JOIN academic_terms t ON c.term_id = t.term_id
LEFT JOIN enrollment e ON c.class_id = e.class_id
GROUP BY c.class_id, co.course_code, co.course_name, r.room_code, 
         u.user_first_name, u.user_last_name, t.term_name
ORDER BY c.class_id;

PRINT '';
PRINT 'Expected: Each class should have at least 1 enrolled student';
PRINT '---';

-- Query 3: Check faculty assignments
SELECT 
    f.fac_id,
    u.user_first_name || ' ' || u.user_last_name as faculty_name,
    u.user_email,
    COUNT(c.class_id) as assigned_classes,
    CASE 
        WHEN COUNT(c.class_id) = 0 THEN '? NO CLASSES ASSIGNED'
        ELSE '? ' || COUNT(c.class_id) || ' classes assigned'
    END as assignment_status
FROM faculty f
JOIN users u ON f.user_id = u.user_id
LEFT JOIN classes c ON f.fac_id = c.fac_id
GROUP BY f.fac_id, u.user_first_name, u.user_last_name, u.user_email
ORDER BY f.fac_id;

PRINT '';
PRINT 'Expected: Each faculty should have at least 1 class assigned';
PRINT '---';

-- ============================================================
-- FIX SECTION - Run these to resolve the issues
-- ============================================================

PRINT '';
PRINT '====================';
PRINT 'APPLYING FIXES';
PRINT '====================';

-- FIX 1: Auto-create missing student records for users with role=student
PRINT '';
PRINT 'FIX 1: Creating missing student records...';

DO $$
DECLARE
    v_user_record RECORD;
    v_temp_stud_number VARCHAR(50);
    v_created_count INT := 0;
BEGIN
    FOR v_user_record IN 
        SELECT u.user_id, u.user_email, u.user_first_name, u.user_last_name
        FROM users u
        LEFT JOIN students s ON u.user_id = s.user_id
        WHERE u.role_id = 3  -- Student role
        AND s.stud_id IS NULL
    LOOP
        -- ? FIXED: Generate shorter temporary student number to fit 20 char limit
        -- Format: TEMP-{user_id}-{seconds} (max 20 chars)
        v_temp_stud_number := 'TEMP-' || v_user_record.user_id || '-' || TO_CHAR(NOW(), 'SSSSS');
        
        -- Insert student record
        INSERT INTO students (user_id, stud_number, stud_created_at)
        VALUES (v_user_record.user_id, v_temp_stud_number, NOW());
        
        v_created_count := v_created_count + 1;
        
        RAISE NOTICE 'Created student record for: % % (Email: %, Temp ID: %)', 
            v_user_record.user_first_name, 
            v_user_record.user_last_name,
            v_user_record.user_email,
            v_temp_stud_number;
    END LOOP;
    
    RAISE NOTICE '? Created % student record(s)', v_created_count;
END $$;

-- FIX 2: Enroll all students in the existing class (Class ID 2 - Programming 1)
PRINT '';
PRINT 'FIX 2: Enrolling students in existing classes...';

DO $$
DECLARE
    v_student_record RECORD;
    v_enrolled_count INT := 0;
    v_class_id INT := 2;  -- Programming 1 class
BEGIN
    FOR v_student_record IN 
        SELECT s.stud_id, s.stud_number, u.user_first_name, u.user_last_name
        FROM students s
        JOIN users u ON s.user_id = u.user_id
        WHERE s.stud_id NOT IN (
            SELECT stud_id FROM enrollment WHERE class_id = v_class_id
        )
    LOOP
        INSERT INTO enrollment (stud_id, class_id, enroll_at)
        VALUES (v_student_record.stud_id, v_class_id, NOW())
        ON CONFLICT DO NOTHING;
        
        v_enrolled_count := v_enrolled_count + 1;
        
        RAISE NOTICE 'Enrolled: % % (ID: %) in Class ID %', 
            v_student_record.user_first_name,
            v_student_record.user_last_name,
            v_student_record.stud_number,
            v_class_id;
    END LOOP;
    
    RAISE NOTICE '? Enrolled % student(s) in Class ID %', v_enrolled_count, v_class_id;
END $$;

-- FIX 3: Create additional sample classes
PRINT '';
PRINT 'FIX 3: Creating additional sample classes...';

-- ? FIXED: Remove window function from OFFSET, use simpler room rotation
DO $$
DECLARE
    v_term_id INT;
    v_fac_id INT;
    v_created_count INT := 0;
    v_room_count INT;
BEGIN
    -- Get active term
    SELECT term_id INTO v_term_id
    FROM academic_terms
    WHERE is_active = TRUE
    LIMIT 1;
    
    -- Get faculty ID
    SELECT fac_id INTO v_fac_id
    FROM faculty
    LIMIT 1;
    
    -- Get total room count
    SELECT COUNT(*) INTO v_room_count FROM rooms;
    
    -- Create classes for available courses (simple round-robin room assignment)
    WITH numbered_courses AS (
        SELECT 
            course_id,
            ROW_NUMBER() OVER() - 1 AS rn
        FROM courses c
        WHERE c.course_id NOT IN (SELECT course_id FROM classes WHERE course_id IS NOT NULL)
    ),
    room_assignment AS (
        SELECT 
            nc.course_id,
            (SELECT room_id FROM rooms ORDER BY room_id LIMIT 1 OFFSET (nc.rn % v_room_count)) as room_id
        FROM numbered_courses nc
    )
    INSERT INTO classes (course_id, room_id, fac_id, term_id, class_created_at, class_updated_at)
    SELECT 
        ra.course_id,
        ra.room_id,
        v_fac_id,
        v_term_id,
        NOW(),
        NOW()
    FROM room_assignment ra
    ON CONFLICT DO NOTHING;
    
    GET DIAGNOSTICS v_created_count = ROW_COUNT;
    
    RAISE NOTICE '? Created % new class(es)', v_created_count;
END $$;

-- FIX 4: Enroll all students in all classes
PRINT '';
PRINT 'FIX 4: Enrolling all students in all classes...';

DO $$
DECLARE
    v_enrollment_count INT := 0;
BEGIN
    INSERT INTO enrollment (stud_id, class_id, enroll_at)
    SELECT s.stud_id, c.class_id, NOW()
    FROM students s
    CROSS JOIN classes c
    WHERE NOT EXISTS (
        SELECT 1 FROM enrollment e 
        WHERE e.stud_id = s.stud_id AND e.class_id = c.class_id
    )
    ON CONFLICT DO NOTHING;
    
    GET DIAGNOSTICS v_enrollment_count = ROW_COUNT;
    
    RAISE NOTICE '? Created % new enrollment(s)', v_enrollment_count;
END $$;

-- ============================================================
-- VERIFICATION SECTION - Run these to confirm fixes worked
-- ============================================================

PRINT '';
PRINT '====================';
PRINT 'VERIFICATION';
PRINT '====================';

-- Verify 1: All students now have records
PRINT '';
PRINT 'Verification 1: Student Records';
SELECT 
    COUNT(*) as total_users_with_student_role,
    COUNT(s.stud_id) as users_with_student_records,
    CASE 
        WHEN COUNT(*) = COUNT(s.stud_id) THEN '? ALL STUDENTS HAVE RECORDS'
        ELSE '? ' || (COUNT(*) - COUNT(s.stud_id)) || ' STUDENTS MISSING RECORDS'
    END as status
FROM users u
LEFT JOIN students s ON u.user_id = s.user_id
WHERE u.role_id = 3;

-- Verify 2: All classes have enrollments
PRINT '';
PRINT 'Verification 2: Class Enrollments';
SELECT 
    c.class_id,
    co.course_code || ' - ' || co.course_name as class_name,
    COUNT(e.enroll_id) as enrolled_students,
    CASE 
        WHEN COUNT(e.enroll_id) > 0 THEN '? HAS STUDENTS'
        ELSE '? NO STUDENTS'
    END as status
FROM classes c
LEFT JOIN courses co ON c.course_id = co.course_id
LEFT JOIN enrollment e ON c.class_id = e.class_id
GROUP BY c.class_id, co.course_code, co.course_name
ORDER BY c.class_id;

-- Verify 3: Total system stats
PRINT '';
PRINT 'Verification 3: System Stats';
SELECT 
    (SELECT COUNT(*) FROM users WHERE role_id = 3) as total_students,
    (SELECT COUNT(*) FROM students) as student_records,
    (SELECT COUNT(*) FROM classes) as total_classes,
    (SELECT COUNT(*) FROM enrollment) as total_enrollments,
    (SELECT COUNT(*) FROM faculty) as total_faculty;

PRINT '';
PRINT '====================';
PRINT 'FIXES COMPLETE!';
PRINT '====================';
PRINT 'You can now test:';
PRINT '1. Student Profile (Cristen Reyes)';
PRINT '2. Faculty Attendance (Lawrence Ramirez)';
PRINT '3. Admin Classes (Super Admin)';

-- ============================================================
-- MANUAL TESTING QUERIES (Optional)
-- ============================================================

-- Test Query 1: Check specific student
PRINT '';
PRINT 'Test: Check Cristen Reyes profile';
SELECT 
    u.user_email,
    u.user_first_name || ' ' || u.user_last_name as full_name,
    s.stud_number,
    s.profile_submitted,
    p.program_name,
    s.stud_year_level,
    sec.section_name
FROM users u
LEFT JOIN students s ON u.user_id = s.user_id
LEFT JOIN programs p ON s.program_id = p.program_id
LEFT JOIN sections sec ON s.section_id = sec.section_id
WHERE u.user_email = 'cristenreyes@example.com';

-- Test Query 2: Check class enrollments for faculty
PRINT '';
PRINT 'Test: Check Lawrence Ramirez classes';
SELECT 
    c.class_id,
    co.course_code || ' - ' || co.course_name as class_name,
    COUNT(e.enroll_id) as enrolled_students,
    STRING_AGG(s.stud_number, ', ') as student_numbers
FROM classes c
JOIN courses co ON c.course_id = co.course_id
JOIN faculty f ON c.fac_id = f.fac_id
JOIN users u ON f.user_id = u.user_id
LEFT JOIN enrollment e ON c.class_id = e.class_id
LEFT JOIN students s ON e.stud_id = s.stud_id
WHERE u.user_email = 'lawrenceramirez@example.com'
GROUP BY c.class_id, co.course_code, co.course_name
ORDER BY c.class_id;

-- Test Query 3: Check all classes visible to admin
PRINT '';
PRINT 'Test: Check all classes for admin view';
SELECT 
    c.class_id,
    co.course_name,
    r.room_code,
    u.user_first_name || ' ' || u.user_last_name as faculty_name,
    t.term_name
FROM classes c
LEFT JOIN courses co ON c.course_id = co.course_id
LEFT JOIN rooms r ON c.room_id = r.room_id
LEFT JOIN faculty f ON c.fac_id = f.fac_id
LEFT JOIN users u ON f.user_id = u.user_id
LEFT JOIN academic_terms t ON c.term_id = t.term_id
ORDER BY c.class_id;
