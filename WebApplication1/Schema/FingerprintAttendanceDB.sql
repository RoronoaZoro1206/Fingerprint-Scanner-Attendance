    -- ============================================================
    -- ROLES & USERS
    -- ============================================================
    
    CREATE TABLE user_roles (
                                role_id    SERIAL PRIMARY KEY,
                                role_name  VARCHAR(30) UNIQUE NOT NULL  -- 'admin', 'faculty', 'student'
    );
    
    CREATE TABLE users (
                           user_id       SERIAL PRIMARY KEY,
                           user_profile_image VARCHAR(255) DEFAULT 'default_profile.png';
    user_first_name VARCHAR(50) NOT NULL,
        user_middle_name VARCHAR(50),
        user_last_name  VARCHAR(50) NOT NULL,
        user_email      VARCHAR(100) UNIQUE NOT NULL,
        user_password   TEXT NOT NULL,
        role_id         INT NOT NULL REFERENCES user_roles(role_id) ON DELETE RESTRICT,
        user_created_at TIMESTAMP NOT NULL DEFAULT NOW(),
        user_updated_at TIMESTAMP NOT NULL DEFAULT NOW(),
        is_active       BOOLEAN NOT NULL DEFAULT TRUE
    );
    
    -- ============================================================
    -- ADMINISTRATION (ADMINS)
    -- ============================================================
    
    CREATE TABLE administration (
                                    admin_id      SERIAL PRIMARY KEY,
                                    user_id       INT UNIQUE REFERENCES users(user_id) ON DELETE CASCADE,
                                    admin_code    VARCHAR(20) UNIQUE,
                                    admin_created_at TIMESTAMP NOT NULL DEFAULT NOW()
    );
    
    CREATE TABLE administration_audit (
                                          aud_id       SERIAL PRIMARY KEY,
                                          admin_id     INT NOT NULL REFERENCES administration(admin_id) ON DELETE CASCADE,
                                          aud_action   VARCHAR(100) NOT NULL,
                                          aud_details  TEXT,
                                          aud_action_time TIMESTAMP NOT NULL DEFAULT NOW()
    );
    
    -- ============================================================
    -- FACULTY
    -- ============================================================
    
    CREATE TABLE faculty (
                             fac_id         SERIAL PRIMARY KEY,
                             user_id        INT UNIQUE REFERENCES users(user_id) ON DELETE CASCADE,
                             fac_department VARCHAR(100),
                             fac_rank       VARCHAR(50),
                             fac_created_at TIMESTAMP NOT NULL DEFAULT NOW()
    );
    
    -- ============================================================
    -- PROGRAMS & COURSES
    -- ============================================================
    
    CREATE TABLE programs (
                              program_id    SERIAL PRIMARY KEY,
                              program_code  VARCHAR(20) UNIQUE NOT NULL,   -- e.g. BSIT, BSHM
                              program_name  VARCHAR(100) NOT NULL,         -- full name of program
                              program_desc  TEXT,
                              program_created_at TIMESTAMP NOT NULL DEFAULT NOW()
    );
    
    CREATE TABLE courses (
                             course_id      SERIAL PRIMARY KEY,
                             course_code    VARCHAR(20) UNIQUE NOT NULL,   -- e.g. IT101, MATH001
                             course_name    VARCHAR(100) NOT NULL,
                             course_desc    TEXT,
                             course_units   INT NOT NULL DEFAULT 3,
                             program_id     INT REFERENCES programs(program_id) ON DELETE SET NULL,
                             course_created_at TIMESTAMP NOT NULL DEFAULT NOW()
    );

-- ============================================================
-- STUDENT ID POOL (Pre-generated Student IDs by Admin)
-- ============================================================

    CREATE TABLE student_id_pool (
                                     pool_id         SERIAL PRIMARY KEY,
                                     student_id_code VARCHAR(20) UNIQUE NOT NULL,  -- e.g. 2025-0156
                                     is_used         BOOLEAN NOT NULL DEFAULT FALSE,
                                     generated_by    INT REFERENCES administration(admin_id) ON DELETE SET NULL,
                                     generated_at    TIMESTAMP NOT NULL DEFAULT NOW(),
                                     used_at         TIMESTAMP,
                                     remarks         TEXT
    );
    
    -- ============================================================
    -- STUDENTS
    -- ============================================================
    
    CREATE TABLE students (
                              stud_id      SERIAL PRIMARY KEY,
                              user_id      INT UNIQUE REFERENCES users(user_id) ON DELETE CASCADE,
                              stud_number  VARCHAR(20) UNIQUE NOT NULL,
                              program_id INT REFERENCES programs(program_id),
                              stud_year_level INT,
                              stud_section VARCHAR(20),
                              stud_created_at TIMESTAMP NOT NULL DEFAULT NOW()
    );
    
    -- ============================================================
    -- ROOMS & CLASSES
    -- ============================================================
    
    CREATE TABLE rooms (
                           room_id     SERIAL PRIMARY KEY,
                           room_code   VARCHAR(20) UNIQUE NOT NULL,
                           room_description VARCHAR(100)
    );
    
    CREATE TABLE classes (
                             class_id     SERIAL PRIMARY KEY,
                             course_id INT REFERENCES courses(course_id) ON DELETE CASCADE,
                             room_id      INT REFERENCES rooms(room_id) ON DELETE CASCADE,
                             fac_id       INT REFERENCES faculty(fac_id) ON DELETE SET NULL,
                             class_created_at TIMESTAMP NOT NULL DEFAULT NOW(),
                             class_updated_at TIMESTAMP NOT NULL DEFAULT NOW()
    );
    
    CREATE TABLE class_schedules (
                                     sched_id     SERIAL PRIMARY KEY,
                                     class_id     INT REFERENCES classes(class_id) ON DELETE CASCADE,
                                     sched_day_of_week VARCHAR(10) NOT NULL,
                                     sched_start_time TIME NOT NULL,
                                     sched_end_time   TIME NOT NULL,
                                     UNIQUE (class_id, sched_day_of_week, sched_start_time, sched_end_time)
    );
    
    -- ============================================================
    -- ENROLLMENT
    -- ============================================================
    
    CREATE TABLE enrollment (
                                enroll_id   SERIAL PRIMARY KEY,
                                stud_id     INT REFERENCES students(stud_id) ON DELETE CASCADE,
                                class_id    INT REFERENCES classes(class_id) ON DELETE CASCADE,
                                enroll_at   TIMESTAMP NOT NULL DEFAULT NOW(),
                                UNIQUE (stud_id, class_id)
    );
    
    -- ============================================================
    -- BIOMETRIC & ATTENDANCE
    -- ============================================================
    
    DO $$ BEGIN
    CREATE TYPE attendance_status AS ENUM ('pending','present','late','absent','excused');
    EXCEPTION WHEN duplicate_object THEN null; END $$;
    
    DO $$ BEGIN
    CREATE TYPE scan_type_enum AS ENUM ('in','out');
    EXCEPTION WHEN duplicate_object THEN null; END $$;
    
    CREATE TABLE fingerprint_templates (
                                           ftemp_id      SERIAL PRIMARY KEY,
                                           user_id       INT UNIQUE REFERENCES students(stud_id) ON DELETE CASCADE,
                                           ftemp_data    BYTEA NOT NULL,
                                           ftemp_name    VARCHAR(20),
                                           ftemp_created_at TIMESTAMP NOT NULL DEFAULT NOW()
    );
    
    CREATE TABLE attendance (
                                attend_id     SERIAL PRIMARY KEY,
                                class_id      INT REFERENCES classes(class_id) ON DELETE CASCADE,
                                stud_id       INT REFERENCES students(stud_id) ON DELETE CASCADE,
                                attend_scan_time TIMESTAMP NOT NULL DEFAULT NOW(),
                                attend_type   scan_type_enum NOT NULL,
                                attend_status attendance_status NOT NULL DEFAULT 'pending',
                                attend_remark TEXT,
                                attend_created_at TIMESTAMP NOT NULL DEFAULT NOW(),
                                UNIQUE (class_id, stud_id, attend_scan_time, attend_type)
    );
    
    CREATE TABLE scan_logs (
                               scan_id     SERIAL PRIMARY KEY,
                               user_id     INT REFERENCES users(user_id) ON DELETE SET NULL,
                               scan_time   TIMESTAMP NOT NULL DEFAULT NOW(),
                               scan_success BOOLEAN,
                               scan_log_message TEXT,
                               scan_created_at TIMESTAMP NOT NULL DEFAULT NOW()
    );
    
    -- ============================================================
    -- DEFAULT ROLES
    -- ============================================================
    
    INSERT INTO user_roles (role_name)
    VALUES ('admin'), ('faculty'), ('student')
        ON CONFLICT (role_name) DO NOTHING;
    
    -- ============================================================
    -- INDEXES
    -- ============================================================
    
    CREATE INDEX idx_users_role_id           ON users(role_id);
    CREATE INDEX idx_students_stud_number    ON students(stud_number);
    CREATE INDEX idx_enroll_stud_id          ON enrollment(stud_id);
    CREATE INDEX idx_enroll_class_id         ON enrollment(class_id);
    CREATE INDEX idx_attend_class_id         ON attendance(class_id);
    CREATE INDEX idx_attend_stud_id          ON attendance(stud_id);
    CREATE INDEX idx_attend_scan_time        ON attendance(attend_scan_time);
    CREATE INDEX idx_class_fac_id            ON classes(fac_id);
    CREATE INDEX idx_class_room_id           ON classes(room_id);
    CREATE INDEX idx_scan_logs_time          ON scan_logs(scan_time);
    
    -- ============================================================
    -- TRIGGERS FOR UPDATED_AT
    -- ============================================================

    CREATE OR REPLACE FUNCTION mark_student_id_used()
    RETURNS TRIGGER AS $$
    BEGIN
    UPDATE student_id_pool
    SET is_used = TRUE,
        used_at = NOW()
    WHERE student_id_code = NEW.stud_number;
    RETURN NEW;
    END;
$$ LANGUAGE plpgsql;

    CREATE TRIGGER trg_mark_student_id_used
        AFTER INSERT ON students
        FOR EACH ROW
        EXECUTE FUNCTION mark_student_id_used();


    CREATE OR REPLACE FUNCTION update_updated_at_column()
    RETURNS TRIGGER AS $$
    BEGIN
       NEW.class_updated_at = NOW();
    RETURN NEW;
    END;
    $$ LANGUAGE 'plpgsql';
    
    CREATE TRIGGER trigger_update_classes_updated_at
        BEFORE UPDATE ON classes
        FOR EACH ROW
        EXECUTE FUNCTION update_updated_at_column();
