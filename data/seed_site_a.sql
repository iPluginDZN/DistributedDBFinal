CREATE TABLE Students(student_id INTEGER PRIMARY KEY, name TEXT, department TEXT, year INTEGER);
CREATE TABLE Enrollments(enrollment_id INTEGER PRIMARY KEY, student_id INTEGER, course_id INTEGER, term TEXT, grade TEXT);
CREATE TABLE Courses(course_id INTEGER PRIMARY KEY, title TEXT, credits INTEGER, professor_id INTEGER);

INSERT INTO Students VALUES
(1, 'An Nguyen', 'CS', 2),
(2, 'Binh Tran', 'CS', 3),
(3, 'Chi Le', 'CS', 4),
(4, 'Dung Pham', 'CS', 1);

INSERT INTO Enrollments VALUES
(1, 1, 101, '2026S', 'A'),
(2, 2, 101, '2026S', 'B+'),
(3, 3, 102, '2026S', 'A-'),
(4, 4, 103, '2025F', 'B');

INSERT INTO Courses VALUES
(101, 'Distributed Databases', 3, 201),
(102, 'Query Optimization', 3, 202),
(103, 'Data Structures', 4, 203);
