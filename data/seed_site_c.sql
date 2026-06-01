CREATE TABLE Courses(course_id INTEGER PRIMARY KEY, title TEXT, credits INTEGER, professor_id INTEGER);
CREATE TABLE Professors(professor_id INTEGER PRIMARY KEY, name TEXT, department TEXT);

INSERT INTO Courses VALUES
(101, 'Distributed Databases', 3, 201),
(102, 'Query Optimization', 3, 202),
(103, 'Data Structures', 4, 203),
(104, 'Business Analytics', 3, 204),
(105, 'Modern Physics', 4, 205),
(106, 'Linear Algebra', 4, 206);

INSERT INTO Professors VALUES
(201, 'Prof. Mai Nguyen', 'CS'),
(202, 'Prof. Quang Tran', 'CS'),
(203, 'Prof. Sara Pham', 'CS'),
(204, 'Prof. Nam Le', 'Business'),
(205, 'Prof. Yen Vo', 'Physics'),
(206, 'Prof. Long Do', 'Math');
