CREATE DATABASE MedRegistryDb;
GO

USE MedRegistryDb;
GO

CREATE TABLE Roles (
    RoleID INT IDENTITY PRIMARY KEY,
    RoleName NVARCHAR(50) NOT NULL UNIQUE
);

INSERT INTO Roles (RoleName) VALUES
('Администратор'),
('Регистратор'),
('Врач'),
('Пациент');

CREATE TABLE Users (
    UserID INT IDENTITY PRIMARY KEY,
    Username NVARCHAR(50) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(255) NOT NULL,
    FirstName NVARCHAR(50) NOT NULL,
    LastName NVARCHAR(50) NOT NULL,
    MiddleName NVARCHAR(50),
    Phone NVARCHAR(20),
    Email NVARCHAR(100),
    RoleID INT NOT NULL,

    CONSTRAINT FK_Users_Roles FOREIGN KEY (RoleID)
    REFERENCES Roles(RoleID)
);

INSERT INTO Users (Username, PasswordHash, FirstName, LastName, MiddleName, Phone, Email, RoleID)
VALUES
('admin', 'HASH123', 'Иван', 'Иванов', 'Иванович', '79990000001', 'admin@mail.ru', 1),
('reg1', 'HASH123', 'Мария', 'Соколова', 'Петровна', '79990000002', 'reg1@mail.ru', 2),
('doc_smirnov', 'HASH123', 'Алексей', 'Смирнов', 'Павлович', '79990000003', 'smirnov@mail.ru', 3),
('doc_borisova', 'HASH123', 'Ольга', 'Борисова', 'Игоревна', '79990000004', 'borisova@mail.ru', 3),
('pat_ivanova', 'HASH123', 'Елена', 'Иванова', 'Андреевна', '79990000005', 'ivanova@mail.ru', 4),
('pat_petrova', 'HASH123', 'Мария', 'Петрова', 'Олеговна', '79990000006', 'petrova@mail.ru', 4);

CREATE TABLE Patients (
    PatientID INT IDENTITY PRIMARY KEY,
    UserID INT NOT NULL UNIQUE,
    BirthDate DATE,
    Gender NVARCHAR(10),
    Address NVARCHAR(255),

    CONSTRAINT FK_Patients_Users FOREIGN KEY (UserID)
    REFERENCES Users(UserID) ON DELETE CASCADE
);

INSERT INTO Patients (UserID, BirthDate, Gender, Address)
VALUES
(5, '1990-04-10', 'Жен', 'Архангельск, ул. Ломоносова 12'),
(6, '1985-06-20', 'Жен', 'Архангельск, пр. Троицкий 45');

CREATE TABLE Specializations (
    SpecializationID INT IDENTITY PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL UNIQUE
);

INSERT INTO Specializations (Name) VALUES
('Терапевт'),
('Кардиолог'),
('Хирург'),
('Педиатр');

CREATE TABLE Doctors (
    DoctorID INT IDENTITY PRIMARY KEY,
    UserID INT NOT NULL UNIQUE,
    SpecializationID INT NOT NULL,
    LicenseNumber NVARCHAR(50),
    WorkExperienceYears INT,
    CabinetNumber NVARCHAR(20),

    CONSTRAINT FK_Doctors_Users FOREIGN KEY (UserID)
    REFERENCES Users(UserID),

    CONSTRAINT FK_Doctors_Specializations FOREIGN KEY (SpecializationID)
    REFERENCES Specializations(SpecializationID)
);

INSERT INTO Doctors (UserID, SpecializationID, LicenseNumber, WorkExperienceYears, CabinetNumber)
VALUES
(3, 1, 'LIC-001', 8, '101'),
(4, 2, 'LIC-002', 12, '202');

CREATE TABLE Schedule (
    ScheduleID INT IDENTITY PRIMARY KEY,
    DoctorID INT NOT NULL,
    StartTime DATETIME NOT NULL,
    EndTime DATETIME NOT NULL,
    IsAvailable BIT DEFAULT 1,

    CONSTRAINT FK_Schedule_Doctors FOREIGN KEY (DoctorID)
    REFERENCES Doctors(DoctorID)
);

INSERT INTO Schedule (DoctorID, StartTime, EndTime)
VALUES
(1, '2025-11-12 09:00', '2025-11-12 09:30'),
(1, '2025-11-12 09:30', '2025-11-12 10:00'),
(2, '2025-11-12 10:00', '2025-11-12 10:30'),
(2, '2025-11-12 10:30', '2025-11-12 11:00');

CREATE TABLE Appointments (
    AppointmentID INT IDENTITY PRIMARY KEY,
    PatientID INT NOT NULL,
    DoctorID INT NOT NULL,
    AppointmentStart DATETIME NOT NULL,
    AppointmentEnd DATETIME NOT NULL,
    Status NVARCHAR(50),
    Purpose NVARCHAR(255),

    CONSTRAINT FK_Appointments_Patients FOREIGN KEY (PatientID)
    REFERENCES Patients(PatientID),

    CONSTRAINT FK_Appointments_Doctors FOREIGN KEY (DoctorID)
    REFERENCES Doctors(DoctorID)
);

INSERT INTO Appointments (PatientID, DoctorID, AppointmentStart, AppointmentEnd, Status, Purpose)
VALUES
(1, 1, '2025-11-12 09:00', '2025-11-12 09:30', 'Запланировано', 'Консультация'),
(2, 2, '2025-11-12 10:00', '2025-11-12 10:30', 'Запланировано', 'Осмотр');

CREATE TABLE MedicalRecords (
    RecordID INT IDENTITY PRIMARY KEY,
    PatientID INT NOT NULL,
    DoctorID INT NOT NULL,
    RecordDate DATETIME DEFAULT GETDATE(),
    Diagnosis NVARCHAR(255),
    Treatment NVARCHAR(MAX),
    Notes NVARCHAR(MAX),

    CONSTRAINT FK_MR_Patients FOREIGN KEY (PatientID)
    REFERENCES Patients(PatientID),

    CONSTRAINT FK_MR_Doctors FOREIGN KEY (DoctorID)
    REFERENCES Doctors(DoctorID)
);

INSERT INTO MedicalRecords (PatientID, DoctorID, Diagnosis, Treatment, Notes)
VALUES
(1, 1, 'ОРВИ', 'Покой, питьевой режим, парацетамол', 'Температура 37.8'),
(2, 2, 'Гипертония', 'Ингибиторы АПФ', 'Требуется наблюдение');


CREATE TABLE ActionLogs (
    LogID INT IDENTITY PRIMARY KEY,
    UserID INT,
    ActionTime DATETIME DEFAULT GETDATE(),
    ActionType NVARCHAR(100),
    Description NVARCHAR(500),

    CONSTRAINT FK_Logs_Users FOREIGN KEY (UserID)
    REFERENCES Users(UserID)
);

INSERT INTO ActionLogs (UserID, ActionType, Description)
VALUES
(1, 'Авторизация', 'Вход администратора'),
(2, 'Создание записи', 'Регистратор создал запись пациенту');

PRINT '✅ База данных MedRegistryDb успешно создана и заполнена начальными данными.';

<<<<<<< HEAD
-------------------------------------------------------
-- 1. Приведение всех существующих значений к допустимым
-------------------------------------------------------

UPDATE Appointments
SET Status = 'Ожидает'
WHERE Status NOT IN ('Ожидает', 'Выполнено', 'Отменено')
   OR Status IS NULL;

-------------------------------------------------------
-- 2. Удаляем старый CHECK, если он существует
-------------------------------------------------------

IF EXISTS (
    SELECT * FROM sys.check_constraints 
    WHERE name = 'CHK_Appointment_Status'
)
BEGIN
    ALTER TABLE Appointments
    DROP CONSTRAINT CHK_Appointment_Status;
END

-------------------------------------------------------
-- 3. Добавляем новый CHECK
-------------------------------------------------------

ALTER TABLE Appointments
ADD CONSTRAINT CHK_Appointment_Status
CHECK (Status IN ('Ожидает', 'Выполнено', 'Отменено'));
=======
-------------------------------------------------------
-- 1. Приведение всех существующих значений к допустимым
-------------------------------------------------------

UPDATE Appointments
SET Status = 'Ожидает'
WHERE Status NOT IN ('Ожидает', 'Выполнено', 'Отменено')
   OR Status IS NULL;

-------------------------------------------------------
-- 2. Удаляем старый CHECK, если он существует
-------------------------------------------------------

IF EXISTS (
    SELECT * FROM sys.check_constraints 
    WHERE name = 'CHK_Appointment_Status'
)
BEGIN
    ALTER TABLE Appointments
    DROP CONSTRAINT CHK_Appointment_Status;
END

-------------------------------------------------------
-- 3. Добавляем новый CHECK
-------------------------------------------------------

ALTER TABLE Appointments
ADD CONSTRAINT CHK_Appointment_Status
CHECK (Status IN ('Ожидает', 'Выполнено', 'Отменено'));

use MedRegistryDb

ALTER TABLE Schedule
ADD WorkDate date NOT NULL DEFAULT ('2025-01-01');

use MedRegistryDb

ALTER TABLE Users
ADD Address NVARCHAR(255) NULL;

ALTER TABLE Users
ADD MedicalPolicy NVARCHAR(30) NULL;

USE MedRegistryDb;
GO

CREATE TABLE Reports (
    ReportID INT IDENTITY PRIMARY KEY,

    AppointmentID INT NOT NULL,
    DoctorID INT NOT NULL,
    PatientID INT NOT NULL,

    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
    FilePath NVARCHAR(500) NOT NULL,

    Diagnosis NVARCHAR(MAX),
    Recommendations NVARCHAR(MAX),
    Description NVARCHAR(MAX),

    CONSTRAINT FK_Reports_Appointments FOREIGN KEY (AppointmentID)
        REFERENCES Appointments(AppointmentID),

    CONSTRAINT FK_Reports_Doctors FOREIGN KEY (DoctorID)
        REFERENCES Doctors(DoctorID),

    CONSTRAINT FK_Reports_Patients FOREIGN KEY (PatientID)
        REFERENCES Patients(PatientID)
);

------------------------------------------------------------
-- 1. Удаляем старую таблицу, если она существует
------------------------------------------------------------
IF OBJECT_ID('dbo.MedicalRecords', 'U') IS NOT NULL
    DROP TABLE dbo.MedicalRecords;
GO

------------------------------------------------------------
-- 2. Создаём новую "чистую" таблицу MedicalRecords
------------------------------------------------------------
CREATE TABLE dbo.MedicalRecords (
    RecordID INT IDENTITY PRIMARY KEY,

    PatientID INT NOT NULL,
    DoctorID INT NOT NULL,
    AppointmentID INT NULL,

    RecordDate DATETIME NOT NULL DEFAULT GETDATE(),

    Diagnosis NVARCHAR(255),
    Treatment NVARCHAR(MAX),
    Notes NVARCHAR(MAX)
);
GO

------------------------------------------------------------
-- 3. Внешние ключи
------------------------------------------------------------
ALTER TABLE dbo.MedicalRecords
ADD CONSTRAINT FK_MR_Patients FOREIGN KEY (PatientID)
    REFERENCES dbo.Patients (PatientID);
GO

ALTER TABLE dbo.MedicalRecords
ADD CONSTRAINT FK_MR_Doctors FOREIGN KEY (DoctorID)
    REFERENCES dbo.Doctors (DoctorID);
GO

ALTER TABLE dbo.MedicalRecords
ADD CONSTRAINT FK_MR_Appointments FOREIGN KEY (AppointmentID)
    REFERENCES dbo.Appointments (AppointmentID);
GO

