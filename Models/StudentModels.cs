namespace StudentValidationSystem.Models;

public class StudentUser
{
    public int StudentUserId { get; set; }
    public int StudentId { get; set; }
    public string RegisterNumber { get; set; } = "";
    public string Email { get; set; } = "";
    public string FullName { get; set; } = "";
}

public class StudentProfile
{
    public int StudentId { get; set; }
    public string FullName { get; set; } = "";
    public string RegisterNumber { get; set; } = "";
    public string DateOfBirth { get; set; } = "";
    public string Gender { get; set; } = "";
    public string Email { get; set; } = "";
    public string MobileNumber { get; set; } = "";
    public string Address { get; set; } = "";
    public string ProfilePhotoPath { get; set; } = "";
}

public class AcademicDetails
{
    public int StudentId { get; set; }
    public string Department { get; set; } = "";
    public string Degree { get; set; } = "";
    public int Year { get; set; }
    public int CurrentSemester { get; set; }
    public string Section { get; set; } = "";
    public string Batch { get; set; } = "";
    public string RollNumber { get; set; } = "";
    public string AdmissionNumber { get; set; } = "";
    public string MentorName { get; set; } = "";
    public double CGPA { get; set; }
    public int Backlogs { get; set; }
}

public class GuardianDetails
{
    public int StudentId { get; set; }
    public string FatherName { get; set; } = "";
    public string MotherName { get; set; } = "";
    public string ParentMobileNumber { get; set; } = "";
    public string ParentEmail { get; set; } = "";
    public string EmergencyContact { get; set; } = "";
}

public class DashboardSummary
{
    public int CurrentSemester { get; set; }
    public double AttendancePercentage { get; set; }
    public double CGPA { get; set; }
    public int TotalSubjects { get; set; }
    public int PendingEvidence { get; set; }
    public int ApprovedEvidence { get; set; }
    public int UpcomingEvents { get; set; }
    public double LatestInternalMarks { get; set; }
}

public class StudentSession
{
    public StudentUser User { get; set; } = new();
}

public class Faculty
{
    public int FacultyId { get; set; }
    public string FacultyCode { get; set; } = "";
    public string Name { get; set; } = "";
    public string Department { get; set; } = "";
    public string Designation { get; set; } = "";
    public string Email { get; set; } = "";
    public string MobileNumber { get; set; } = "";
    public string ProfileImagePath { get; set; } = "";
    public string Qualification { get; set; } = "";
    public string Experience { get; set; } = "";
}

public class FacultySession
{
    public int FacultyId { get; set; }
    public string Username { get; set; } = "";
    public string Name { get; set; } = "";
    public string Department { get; set; } = "";
    public string Designation { get; set; } = "";
    public string Email { get; set; } = "";
    public string MobileNumber { get; set; } = "";
    public string ProfileImagePath { get; set; } = "";
    public string Qualification { get; set; } = "";
    public string Experience { get; set; } = "";
}

public class Section
{
    public int SectionId { get; set; }
    public string Department { get; set; } = "";
    public int Year { get; set; }
    public int Semester { get; set; }
    public string SectionName { get; set; } = "";
    public int TotalStudents { get; set; }
    public int StudentCount { get; set; }
    public double AverageAttendance { get; set; }
    public double AverageInternalMarks { get; set; }
    public int PendingEvidenceCount { get; set; }
    public string SubjectsHandled { get; set; } = "";
}

public class StudentWithDetails
{
    public int StudentId { get; set; }
    public string RegisterNumber { get; set; } = "";
    public string StudentName { get; set; } = "";
    public string Department { get; set; } = "";
    public int Year { get; set; }
    public int Semester { get; set; }
    public string Section { get; set; } = "";
    public string Email { get; set; } = "";
    public string MobileNumber { get; set; } = "";
    public double AttendancePercentage { get; set; }
    public double InternalMarks { get; set; }
    public string EvidenceStatus { get; set; } = "";
    public string ProfileImagePath { get; set; } = "";
}

public class AttendanceRecord
{
    public int AttendanceRecordId { get; set; }
    public int FacultyId { get; set; }
    public int StudentId { get; set; }
    public int SubjectId { get; set; }
    public int SectionId { get; set; }
    public string AttendanceDate { get; set; } = "";
    public string Status { get; set; } = ""; // Present, Absent, Leave
}

public class MarksRecord
{
    public int FacultyMarkId { get; set; }
    public int FacultyId { get; set; }
    public int StudentId { get; set; }
    public int SubjectId { get; set; }
    public int SectionId { get; set; }
    public string ExamType { get; set; } = ""; // Internal, External, Quiz, etc.
    public int MaxMarks { get; set; }
    public double MarksObtained { get; set; }
    public string Grade { get; set; } = "";
    public int Semester { get; set; }
}
