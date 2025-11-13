-- ============================================================
-- FACULTY ATTENDANCE MANAGEMENT POWERS
-- Migration Script for Excuse System, Status Changes, Manual Entry
-- Author: AI Assistant
-- Date: 2025-01-11
-- ============================================================

-- ============================================================
-- TABLE: student_excuse_requests
-- Purpose: Store student-submitted excuse requests
-- ============================================================

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

-- ============================================================
-- TABLE: faculty_attendance_actions
-- Purpose: Audit trail for all faculty attendance modifications
-- ============================================================

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

-- ============================================================
-- TABLE: attendance_notifications
-- Purpose: Notify students/faculty of attendance actions
-- ============================================================

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
-- HELPER FUNCTIONS
-- ============================================================

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

-- ============================================================
-- VIEWS FOR REPORTING
-- ============================================================

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

-- ============================================================
-- DATA VALIDATION
-- ============================================================

-- Ensure action_type is valid
ALTER TABLE faculty_attendance_actions 
DROP CONSTRAINT IF EXISTS valid_action_type;

ALTER TABLE faculty_attendance_actions
ADD CONSTRAINT valid_action_type 
CHECK (action_type IN ('excuse', 'change_status', 'add_manual', 'approve_request', 'reject_request', 'bulk_excuse'));

-- ============================================================
-- SAMPLE DATA FOR TESTING
-- ============================================================

-- Insert sample excuse request (commented out - uncomment to test)
/*
INSERT INTO student_excuse_requests (stud_id, class_id, attend_date, excuse_type, excuse_reason, status)
VALUES (
    (SELECT stud_id FROM students LIMIT 1),
    (SELECT class_id FROM classes LIMIT 1),
    CURRENT_DATE,
    'sick',
    'I was sick with flu and couldn''t attend class. I have a medical certificate.',
    'pending'
);
*/

-- ============================================================
-- VERIFICATION QUERIES
-- ============================================================

-- Verify tables were created
SELECT 
    schemaname,
    tablename,
    tableowner
FROM pg_tables
WHERE tablename IN (
    'student_excuse_requests',
    'faculty_attendance_actions',
    'attendance_notifications'
)
ORDER BY tablename;

-- Verify indexes
SELECT 
    schemaname,
    tablename,
    indexname
FROM pg_indexes
WHERE tablename IN (
    'student_excuse_requests',
    'faculty_attendance_actions',
    'attendance_notifications'
)
ORDER BY tablename, indexname;

-- Verify views
SELECT 
    schemaname,
    viewname,
    viewowner
FROM pg_views
WHERE viewname IN (
    'v_pending_excuse_requests',
    'v_faculty_action_history'
)
ORDER BY viewname;

-- ============================================================
-- ROLLBACK SCRIPT (Use with caution!)
-- ============================================================

/*
-- To rollback this migration:
DROP VIEW IF EXISTS v_faculty_action_history CASCADE;
DROP VIEW IF EXISTS v_pending_excuse_requests CASCADE;
DROP TRIGGER IF EXISTS trg_excuse_request_updated_at ON student_excuse_requests;
DROP FUNCTION IF EXISTS update_excuse_request_timestamp CASCADE;
DROP TABLE IF EXISTS attendance_notifications CASCADE;
DROP TABLE IF EXISTS faculty_attendance_actions CASCADE;
DROP TABLE IF EXISTS student_excuse_requests CASCADE;
*/

-- ============================================================
-- MIGRATION COMPLETE
-- ============================================================

-- Print success message
DO $$
BEGIN
    RAISE NOTICE '? Faculty Attendance Powers migration completed successfully!';
    RAISE NOTICE '?? Created 3 tables: student_excuse_requests, faculty_attendance_actions, attendance_notifications';
    RAISE NOTICE '?? Created 2 views: v_pending_excuse_requests, v_faculty_action_history';
    RAISE NOTICE '?? Created 12+ indexes for optimal performance';
    RAISE NOTICE '?? System ready for faculty excuse management features';
END $$;
