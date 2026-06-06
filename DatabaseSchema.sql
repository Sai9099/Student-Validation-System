PRAGMA foreign_keys = ON;

CREATE TABLE IF NOT EXISTS StudentUsers (
    StudentUserId INTEGER PRIMARY KEY AUTOINCREMENT,
    StudentId INTEGER NOT NULL UNIQUE,
    PasswordHash TEXT NOT NULL,
    IsActive INTEGER NOT NULL DEFAULT 1,
    LastLogin TEXT NULL
);

CREATE TABLE IF NOT EXISTS StudentProfile (
    StudentId INTEGER PRIMARY KEY AUTOINCREMENT,
    FullName TEXT NOT NULL,
    RegisterNumber TEXT NOT NULL UNIQUE,
    DateOfBirth TEXT NOT NULL,
    Gender TEXT NOT NULL,
    Email TEXT NOT NULL UNIQUE,
    MobileNumber TEXT NOT NULL,
    Address TEXT NOT NULL,
    ProfilePhotoPath TEXT NOT NULL DEFAULT ''
);

CREATE TABLE IF NOT EXISTS AcademicDetails (
    AcademicId INTEGER PRIMARY KEY AUTOINCREMENT,
    StudentId INTEGER NOT NULL UNIQUE,
    Department TEXT NOT NULL,
    Degree TEXT NOT NULL,
    Year INTEGER NOT NULL,
    CurrentSemester INTEGER NOT NULL,
    Section TEXT NOT NULL,
    Batch TEXT NOT NULL,
    RollNumber TEXT NOT NULL,
    AdmissionNumber TEXT NOT NULL,
    MentorName TEXT NOT NULL,
    CGPA REAL NOT NULL,
    Backlogs INTEGER NOT NULL DEFAULT 0
);

CREATE TABLE IF NOT EXISTS GuardianDetails (
    GuardianId INTEGER PRIMARY KEY AUTOINCREMENT,
    StudentId INTEGER NOT NULL UNIQUE,
    FatherName TEXT NOT NULL,
    MotherName TEXT NOT NULL,
    ParentMobileNumber TEXT NOT NULL,
    ParentEmail TEXT NOT NULL,
    EmergencyContact TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS ExtraCurricularActivities (
    ActivityId INTEGER PRIMARY KEY AUTOINCREMENT,
    StudentId INTEGER NOT NULL,
    ActivityType TEXT NOT NULL,
    ActivityName TEXT NOT NULL,
    Description TEXT NOT NULL,
    DateFrom TEXT NOT NULL,
    DateTo TEXT NULL,
    CertificatePath TEXT NULL
);

CREATE TABLE IF NOT EXISTS Achievements (
    AchievementId INTEGER PRIMARY KEY AUTOINCREMENT,
    StudentId INTEGER NOT NULL,
    AchievementType TEXT NOT NULL,
    Title TEXT NOT NULL,
    Description TEXT NOT NULL,
    AchievementDate TEXT NOT NULL,
    Organization TEXT NULL,
    FilePath TEXT NULL
);

CREATE TABLE IF NOT EXISTS Subjects (
    SubjectId INTEGER PRIMARY KEY AUTOINCREMENT,
    SubjectCode TEXT NOT NULL UNIQUE,
    SubjectName TEXT NOT NULL,
    FacultyName TEXT NOT NULL,
    Credits INTEGER NOT NULL,
    SubjectType TEXT NOT NULL,
    SyllabusPath TEXT NOT NULL,
    CourseOutcomes TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS SemesterSubjects (
    SemesterSubjectId INTEGER PRIMARY KEY AUTOINCREMENT,
    Semester INTEGER NOT NULL,
    SubjectId INTEGER NOT NULL
);

CREATE TABLE IF NOT EXISTS Attendance (
    AttendanceId INTEGER PRIMARY KEY AUTOINCREMENT,
    StudentId INTEGER NOT NULL,
    SubjectId INTEGER NOT NULL,
    Semester INTEGER NOT NULL,
    PresentCount INTEGER NOT NULL,
    AbsentCount INTEGER NOT NULL,
    TotalClassesConducted INTEGER NOT NULL,
    AttendancePercentage REAL NOT NULL,
    MinimumRequired REAL NOT NULL DEFAULT 75
);

CREATE TABLE IF NOT EXISTS Marks (
    MarkId INTEGER PRIMARY KEY AUTOINCREMENT,
    StudentId INTEGER NOT NULL,
    Semester INTEGER NOT NULL,
    GPA REAL NOT NULL,
    CGPA REAL NOT NULL,
    ResultStatus TEXT NOT NULL,
    PublishedDate TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS InternalMarks (
    InternalMarkId INTEGER PRIMARY KEY AUTOINCREMENT,
    StudentId INTEGER NOT NULL,
    SubjectId INTEGER NOT NULL,
    Semester INTEGER NOT NULL,
    AssignmentMarks REAL NOT NULL,
    QuizMarks REAL NOT NULL,
    MidExamMarks REAL NOT NULL,
    LabMarks REAL NOT NULL,
    ExternalMarks REAL NOT NULL,
    TotalMarks REAL NOT NULL,
    Grade TEXT NOT NULL,
    GPA REAL NOT NULL
);

CREATE TABLE IF NOT EXISTS LearningEvidence (
    EvidenceId INTEGER PRIMARY KEY AUTOINCREMENT,
    StudentId INTEGER NOT NULL,
    EvidenceTitle TEXT NOT NULL,
    Category TEXT NOT NULL,
    Semester INTEGER NOT NULL,
    Subject TEXT NOT NULL,
    Description TEXT NOT NULL,
    UploadDate TEXT NOT NULL,
    FilePath TEXT NOT NULL,
    ValidationStatus TEXT NOT NULL DEFAULT 'Pending',
    FacultyComments TEXT NOT NULL DEFAULT ''
);

CREATE TABLE IF NOT EXISTS CampusEvents (
    EventId INTEGER PRIMARY KEY AUTOINCREMENT,
    EventTitle TEXT NOT NULL,
    EventDate TEXT NOT NULL,
    Venue TEXT NOT NULL,
    ConductedBy TEXT NOT NULL,
    EventDescription TEXT NOT NULL,
    RegistrationDeadline TEXT NOT NULL,
    EventType TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS EventRegistrations (
    RegistrationId INTEGER PRIMARY KEY AUTOINCREMENT,
    StudentId INTEGER NOT NULL,
    EventId INTEGER NOT NULL,
    RegistrationDate TEXT NOT NULL,
    Status TEXT NOT NULL,
    UNIQUE(StudentId, EventId)
);

CREATE TABLE IF NOT EXISTS Notifications (
    NotificationId INTEGER PRIMARY KEY AUTOINCREMENT,
    StudentId INTEGER NOT NULL,
    NotificationType TEXT NOT NULL,
    Title TEXT NOT NULL,
    Message TEXT NOT NULL,
    IsRead INTEGER NOT NULL DEFAULT 0,
    CreatedAt TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS StudentDocuments (
    DocumentId INTEGER PRIMARY KEY AUTOINCREMENT,
    StudentId INTEGER NOT NULL,
    DocumentType TEXT NOT NULL,
    DocumentName TEXT NOT NULL,
    FilePath TEXT NOT NULL,
    UploadDate TEXT NOT NULL
);
