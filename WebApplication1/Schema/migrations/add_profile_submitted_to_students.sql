-- Migration: Add profile submission tracking to students table
-- Date: 2025-10-16
-- Description: Adds fields to track when a student submits their profile for the first time
--              to prevent future changes to academic information

-- Add profile_submitted column (default false for existing records)
ALTER TABLE students 
ADD COLUMN IF NOT EXISTS profile_submitted BOOLEAN NOT NULL DEFAULT FALSE;

-- Add profile_submitted_at column (nullable, will be set when profile is first submitted)
ALTER TABLE students 
ADD COLUMN IF NOT EXISTS profile_submitted_at TIMESTAMP;

-- Add comment to explain the purpose
COMMENT ON COLUMN students.profile_submitted IS 'Indicates if student has submitted their profile with academic information for the first time';
COMMENT ON COLUMN students.profile_submitted_at IS 'Timestamp when student first submitted their profile with academic information';

-- Optional: Set profile_submitted to true for students who already have program_id set
-- This ensures existing students with academic info are also locked
UPDATE students 
SET profile_submitted = TRUE, 
    profile_submitted_at = stud_created_at 
WHERE program_id IS NOT NULL 
  AND profile_submitted = FALSE;
