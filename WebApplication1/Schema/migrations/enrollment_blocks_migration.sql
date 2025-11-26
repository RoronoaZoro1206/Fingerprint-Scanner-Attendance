-- ============================================================
-- ENROLLMENT BLOCKS TABLE
-- Tracks students who have been force-unenrolled by faculty
-- and are blocked from re-enrolling in specific classes
-- ============================================================

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
