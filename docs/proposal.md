# Distributed Database Project Proposal

Due Date: Week 3  
Project ID & Category: Academic Records Algebraic Query Tree Visualizer

## 1. Project Identity

Team Name: Algebra Architects  
Team Members: To be filled by team  
Project Title: Algebraic Query Tree Visualizer: Academic Records

## 2. Objective & Problem Statement

The project demonstrates how a distributed database coordinator decomposes a SQL query into relational algebra and then optimizes the query before site execution.

The main distributed database challenge is reducing intermediate results and communication cost across sites through query optimization. The system visualizes the initial algebraic query tree, the optimized tree, and the transformation rules used.

Core logic:

- SQL query decomposition
- relational algebra tree construction
- selection pushdown
- join commutativity
- projection pruning
- distributed fragment planning
- site failure detection

## 3. Dataset Specification

Source: Synthetic dataset generated for this project.  
Size: Approximately 100-300 logical rows in the intended dataset; seed scripts include representative rows for reproducible demos.

Schema:

- `Students(student_id, name, department, year)`
- `Courses(course_id, title, credits, professor_id)`
- `Enrollments(enrollment_id, student_id, course_id, term, grade)`
- `Professors(professor_id, name, department)`

Fragmentation Strategy:

- Site A stores CS students, CS enrollments, and a Courses replica.
- Site B stores non-CS students, non-CS enrollments, and a Professors replica.
- Site C stores Courses and Professors replicas plus backup/reference metadata.

## 4. System Architecture

Nodes: 3 simulated sites: Site A, Site B, Site C.  
Communication Layer: HTTP/REST.  
Storage: One SQLite database file per simulated site.

Architecture:

```text
WPF App -> Coordinator API -> Site A / Site B / Site C
```

## 5. Tech Stack & Implementation Plan

Programming Language: C#/.NET.  
Deployment: Docker Compose for backend services; WPF runs natively on Windows.  
Libraries/Frameworks:

- WPF for native Windows UI
- ASP.NET Core for coordinator and site APIs
- SQLite-backed site storage model
- Docker Compose for deployment

## 6. Success Metrics & Analysis

Quantitative Metric:

- Number of transformation steps shown
- Site health status accuracy
- Number of affected/rerouted fragments after Site B failure

Failure Scenario:

Stop Site B during the demo using:

```powershell
docker compose stop site-b
```

The coordinator must detect Site B as offline and the UI must show the affected distributed plan.

## 7. Project Milestones

Milestone 1: Solution structure, dataset, and site APIs complete.  
Milestone 2: Coordinator query analysis and optimizer complete.  
Milestone 3: WPF visualizer, Docker deployment, failure demo, and documentation complete.
