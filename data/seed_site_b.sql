CREATE TABLE Students(student_id INTEGER PRIMARY KEY, name TEXT, department TEXT, year INTEGER);
CREATE TABLE Enrollments(enrollment_id INTEGER PRIMARY KEY, student_id INTEGER, course_id INTEGER, term TEXT, grade TEXT);
CREATE TABLE Professors(professor_id INTEGER PRIMARY KEY, name TEXT, department TEXT);

INSERT INTO Students VALUES
(31, 'Hoa Vu', 'Business', 2),
(32, 'Khanh Do', 'Math', 3),
(33, 'Linh Ho', 'Physics', 4),
(34, 'Minh Bui', 'Literature', 1);

INSERT INTO Enrollments VALUES
(31, 31, 104, '2026S', 'A'),
(32, 32, 102, '2026S', 'B'),
(33, 33, 105, '2025F', 'C+'),
(34, 34, 101, '2026S', 'B+');

INSERT INTO Professors VALUES
(201, 'Prof. Mai Nguyen', 'CS'),
(202, 'Prof. Quang Tran', 'CS'),
(203, 'Prof. Sara Pham', 'CS'),
(204, 'Prof. Nam Le', 'Business');
