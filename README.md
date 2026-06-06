# Student Learning Evidence & Progress Validation System

This project now contains the **Student Module only** for a C# Windows Forms desktop application. It lets a student log in, view and edit their own profile, track attendance, subjects, marks, learning evidence, campus events, progress, notifications, and documents.

## Tech Stack

- C# .NET 10
- Windows Forms
- SQLite with `Microsoft.Data.Sqlite`
- VS Code compatible `.csproj` and `.sln`

## Sample Login

Choose **Student Login** on the role selection screen. Use either the register number or email.

```text
Register number: RA2311033010096
Email: sai.kumar@student.edu
Password: student123
```

Choose **Faculty Login** for the faculty module.

```text
Faculty ID: faculty1
Email-style username: faculty1@college.edu
Password: 1234
Role: Faculty
```

## Implemented Student Features

- Student login using register number/email and password.
- Student can view and edit only their own profile.
- Dashboard cards for semester, attendance, CGPA, subjects, pending/approved evidence, events, and marks.
- Personal, academic, guardian, extra-curricular, and achievement profile sections.
- Attendance module with subject-wise counts, shortage alert, minimum required percentage, and semester filter.
- Semester subjects module with subject code, faculty, credits, type, syllabus path, and outcomes.
- Marks module with internal marks, external marks, grade, GPA, CGPA, and semester history.
- Learning evidence upload and status tracking.
- Notes sharing module for handwritten/scanned/PDF notes with upload, approval status, search, bookmarks, likes, reports, and separate student/faculty note views.
- Campus events list, interest registration, and registered events list.
- Progress summary with progress bars, simple chart, evidence status, timeline, strengths, and improvement areas.
- Notifications with filters and mark-read action.
- Documents upload/view with file path preview.
- Dark mode toggle from the main shell.
- SQLite database is created and seeded automatically.

## Project Structure

```text
StudentValidationSystem/
  Assets/
  Database/
    StudentDatabase.cs
  Helpers/
    StudentSessionManager.cs
    UiFactory.cs
  Models/
    StudentModels.cs
  Services/
  Utils/
    Theme.cs
  Views/
    LoginForm.cs
    MainForm.cs
    StudentDashboardControl.cs
    StudentProfileControl.cs
    StudentAttendanceControl.cs
    StudentSubjectsControl.cs
    StudentMarksControl.cs
    StudentEvidenceControl.cs
    StudentNotesControl.cs
    StudentEventsControl.cs
    StudentProgressControl.cs
    StudentNotificationsControl.cs
    StudentDocumentsControl.cs
  DatabaseSchema.sql
  Program.cs
  StudentValidationSystem.csproj
```

## Run in VS Code

```powershell
dotnet restore
dotnet build
dotnet run
```

## Login Troubleshooting

If the sample password is changed during testing, reset it with:

```powershell
dotnet run -- --reset-sample-login
```

To verify authentication from the terminal before opening the desktop UI:

```powershell
dotnet run -- --login-check --login RA2311033010096 --password student123
```

The database is created at:

```text
bin/Debug/net10.0-windows/Data/student_module.db
```

## Database Tables

- StudentUsers
- StudentProfile
- AcademicDetails
- GuardianDetails
- ExtraCurricularActivities
- Achievements
- Subjects
- SemesterSubjects
- Attendance
- Marks
- InternalMarks
- LearningEvidence
- CampusEvents
- EventRegistrations
- Notifications
- StudentDocuments

## Notes

- Uploaded evidence and documents store file paths in SQLite.
- Student cannot approve or reject evidence.
- Passwords are SHA-256 hashed for a demo project. For production, use a salted password hashing algorithm such as BCrypt or Argon2.
