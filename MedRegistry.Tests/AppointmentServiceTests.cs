using Xunit;
using DataLayer.Data;
using DataLayer.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;

namespace MedRegistry.Tests
{
    /// <summary>
    /// Тесты для проверки работы с записями на приём.
    /// Использует In-Memory базу данных для изоляции тестов.
    /// </summary>
    public class AppointmentServiceTests
    {
        /// <summary>
        /// Создаёт контекст базы данных в памяти для тестирования.
        /// </summary>
        private MedRegistryContext GetInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<MedRegistryContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new MedRegistryContext(options);
        }

        /// <summary>
        /// Тест: создание записи на приём с корректными данными.
        /// </summary>
        [Fact]
        public void CreateAppointment_ValidData_ReturnsAppointment()
        {
            // Arrange - подготовка данных
            using var context = GetInMemoryContext();
            
            // Создаём роли
            var patientRole = new Role { RoleId = 4, RoleName = "Пациент" };
            var doctorRole = new Role { RoleId = 3, RoleName = "Врач" };
            context.Roles.AddRange(patientRole, doctorRole);
            
            // Создаём пользователей
            var patientUser = new User { UserId = 1, Username = "patient1", PasswordHash = "hash", FirstName = "Иван", LastName = "Петров", RoleId = 4 };
            var doctorUser = new User { UserId = 2, Username = "doctor1", PasswordHash = "hash", FirstName = "Анна", LastName = "Сидорова", RoleId = 3 };
            context.Users.AddRange(patientUser, doctorUser);
            
            // Создаём специализацию
            var specialization = new Specialization { SpecializationId = 1, Name = "Терапевт" };
            context.Specializations.Add(specialization);
            
            // Создаём пациента и врача
            var patient = new Patient { PatientId = 1, UserId = 1 };
            var doctor = new Doctor { DoctorId = 1, UserId = 2, SpecializationId = 1, CabinetNumber = "101" };
            context.Patients.Add(patient);
            context.Doctors.Add(doctor);
            context.SaveChanges();

            var appointment = new Appointment
            {
                PatientId = 1,
                DoctorId = 1,
                AppointmentStart = DateTime.Now.AddDays(1),
                AppointmentEnd = DateTime.Now.AddDays(1).AddMinutes(30),
                Purpose = "Консультация",
                Status = "Ожидает"
            };

            // Act - выполнение действия
            context.Appointments.Add(appointment);
            context.SaveChanges();

            // Assert - проверка результата
            var created = context.Appointments.FirstOrDefault(a => a.PatientId == 1);
            Assert.NotNull(created);
            Assert.Equal("Ожидает", created.Status);
            Assert.Equal("Консультация", created.Purpose);
        }

        /// <summary>
        /// Тест: получение записи по идентификатору.
        /// </summary>
        [Fact]
        public void GetAppointment_ValidId_ReturnsAppointment()
        {
            // Arrange
            using var context = GetInMemoryContext();
            var appointment = new Appointment
            {
                AppointmentId = 1,
                PatientId = 1,
                DoctorId = 1,
                AppointmentStart = DateTime.Now.AddDays(1),
                AppointmentEnd = DateTime.Now.AddDays(1).AddMinutes(30),
                Purpose = "Первичный осмотр",
                Status = "Ожидает"
            };
            context.Appointments.Add(appointment);
            context.SaveChanges();

            // Act
            var found = context.Appointments.Find(1);

            // Assert
            Assert.NotNull(found);
            Assert.Equal(1, found.AppointmentId);
            Assert.Equal("Первичный осмотр", found.Purpose);
        }

        /// <summary>
        /// Тест: обновление статуса записи на приём.
        /// </summary>
        [Fact]
        public void UpdateAppointment_ChangeStatus_StatusUpdated()
        {
            // Arrange
            using var context = GetInMemoryContext();
            var appointment = new Appointment
            {
                AppointmentId = 1,
                PatientId = 1,
                DoctorId = 1,
                AppointmentStart = DateTime.Now.AddDays(1),
                AppointmentEnd = DateTime.Now.AddDays(1).AddMinutes(30),
                Status = "Ожидает"
            };
            context.Appointments.Add(appointment);
            context.SaveChanges();

            // Act
            var toUpdate = context.Appointments.Find(1);
            toUpdate!.Status = "Выполнено";
            context.SaveChanges();

            // Assert
            var updated = context.Appointments.Find(1);
            Assert.NotNull(updated);
            Assert.Equal("Выполнено", updated.Status);
        }

        /// <summary>
        /// Тест: удаление записи на приём.
        /// </summary>
        [Fact]
        public void DeleteAppointment_ValidId_AppointmentDeleted()
        {
            // Arrange
            using var context = GetInMemoryContext();
            var appointment = new Appointment
            {
                AppointmentId = 1,
                PatientId = 1,
                DoctorId = 1,
                AppointmentStart = DateTime.Now.AddDays(1),
                AppointmentEnd = DateTime.Now.AddDays(1).AddMinutes(30),
                Status = "Ожидает"
            };
            context.Appointments.Add(appointment);
            context.SaveChanges();

            // Act
            var toDelete = context.Appointments.Find(1);
            context.Appointments.Remove(toDelete!);
            context.SaveChanges();

            // Assert
            var deleted = context.Appointments.Find(1);
            Assert.Null(deleted);
        }

        /// <summary>
        /// Тест: получение записей пациента.
        /// </summary>
        [Fact]
        public void GetAppointmentsByPatient_ValidPatientId_ReturnsAppointments()
        {
            // Arrange
            using var context = GetInMemoryContext();
            
            // Добавляем несколько записей для одного пациента
            context.Appointments.AddRange(
                new Appointment { PatientId = 1, DoctorId = 1, AppointmentStart = DateTime.Now.AddDays(1), AppointmentEnd = DateTime.Now.AddDays(1).AddMinutes(30), Status = "Ожидает" },
                new Appointment { PatientId = 1, DoctorId = 2, AppointmentStart = DateTime.Now.AddDays(2), AppointmentEnd = DateTime.Now.AddDays(2).AddMinutes(30), Status = "Ожидает" },
                new Appointment { PatientId = 2, DoctorId = 1, AppointmentStart = DateTime.Now.AddDays(1), AppointmentEnd = DateTime.Now.AddDays(1).AddMinutes(30), Status = "Ожидает" }
            );
            context.SaveChanges();

            // Act
            var appointments = context.Appointments
                .Where(a => a.PatientId == 1)
                .ToList();

            // Assert
            Assert.Equal(2, appointments.Count);
            Assert.All(appointments, a => Assert.Equal(1, a.PatientId));
        }

        /// <summary>
        /// Тест: отмена записи на приём.
        /// </summary>
        [Fact]
        public void CancelAppointment_ValidId_StatusCancelled()
        {
            // Arrange
            using var context = GetInMemoryContext();
            var appointment = new Appointment
            {
                AppointmentId = 1,
                PatientId = 1,
                DoctorId = 1,
                AppointmentStart = DateTime.Now.AddDays(1),
                AppointmentEnd = DateTime.Now.AddDays(1).AddMinutes(30),
                Status = "Ожидает"
            };
            context.Appointments.Add(appointment);
            context.SaveChanges();

            // Act
            var toCancel = context.Appointments.Find(1);
            toCancel!.Status = "Отменено";
            context.SaveChanges();

            // Assert
            var cancelled = context.Appointments.Find(1);
            Assert.NotNull(cancelled);
            Assert.Equal("Отменено", cancelled.Status);
        }
    }

    /// <summary>
    /// Тесты для проверки работы с пользователями.
    /// </summary>
    public class UserServiceTests
    {
        private MedRegistryContext GetInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<MedRegistryContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new MedRegistryContext(options);
        }

        /// <summary>
        /// Тест: создание нового пользователя.
        /// </summary>
        [Fact]
        public void CreateUser_ValidData_UserCreated()
        {
            // Arrange
            using var context = GetInMemoryContext();
            var role = new Role { RoleId = 4, RoleName = "Пациент" };
            context.Roles.Add(role);
            context.SaveChanges();

            var user = new User
            {
                Username = "testuser",
                PasswordHash = "hashedpassword",
                FirstName = "Тест",
                LastName = "Пользователь",
                RoleId = 4
            };

            // Act
            context.Users.Add(user);
            context.SaveChanges();

            // Assert
            var created = context.Users.FirstOrDefault(u => u.Username == "testuser");
            Assert.NotNull(created);
            Assert.Equal("Тест", created.FirstName);
            Assert.Equal("Пользователь", created.LastName);
        }

        /// <summary>
        /// Тест: проверка уникальности логина.
        /// </summary>
        [Fact]
        public void CheckUsernameExists_ExistingUsername_ReturnsTrue()
        {
            // Arrange
            using var context = GetInMemoryContext();
            var role = new Role { RoleId = 4, RoleName = "Пациент" };
            context.Roles.Add(role);
            
            var user = new User
            {
                Username = "existinguser",
                PasswordHash = "hash",
                FirstName = "Имя",
                LastName = "Фамилия",
                RoleId = 4
            };
            context.Users.Add(user);
            context.SaveChanges();

            // Act
            bool exists = context.Users.Any(u => u.Username == "existinguser");

            // Assert
            Assert.True(exists);
        }

        /// <summary>
        /// Тест: авторизация пользователя с корректными данными.
        /// </summary>
        [Fact]
        public void AuthenticateUser_ValidCredentials_ReturnsUser()
        {
            // Arrange
            using var context = GetInMemoryContext();
            var role = new Role { RoleId = 4, RoleName = "Пациент" };
            context.Roles.Add(role);
            
            var user = new User
            {
                Username = "authuser",
                PasswordHash = "correctpassword",
                FirstName = "Авторизация",
                LastName = "Тест",
                RoleId = 4
            };
            context.Users.Add(user);
            context.SaveChanges();

            // Act
            var authenticated = context.Users
                .FirstOrDefault(u => u.Username == "authuser" && u.PasswordHash == "correctpassword");

            // Assert
            Assert.NotNull(authenticated);
            Assert.Equal("authuser", authenticated.Username);
        }

        /// <summary>
        /// Тест: авторизация с неверным паролем.
        /// </summary>
        [Fact]
        public void AuthenticateUser_WrongPassword_ReturnsNull()
        {
            // Arrange
            using var context = GetInMemoryContext();
            var role = new Role { RoleId = 4, RoleName = "Пациент" };
            context.Roles.Add(role);
            
            var user = new User
            {
                Username = "authuser2",
                PasswordHash = "correctpassword",
                FirstName = "Авторизация",
                LastName = "Тест",
                RoleId = 4
            };
            context.Users.Add(user);
            context.SaveChanges();

            // Act
            var authenticated = context.Users
                .FirstOrDefault(u => u.Username == "authuser2" && u.PasswordHash == "wrongpassword");

            // Assert
            Assert.Null(authenticated);
        }
    }

    /// <summary>
    /// Тесты для проверки работы с расписанием.
    /// </summary>
    public class ScheduleServiceTests
    {
        private MedRegistryContext GetInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<MedRegistryContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new MedRegistryContext(options);
        }

        /// <summary>
        /// Тест: создание расписания врача.
        /// </summary>
        [Fact]
        public void CreateSchedule_ValidData_ScheduleCreated()
        {
            // Arrange
            using var context = GetInMemoryContext();
            var workDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1));
            var schedule = new Schedule
            {
                DoctorId = 1,
                WorkDate = workDate,
                StartTime = DateTime.Today.AddDays(1).AddHours(9),
                EndTime = DateTime.Today.AddDays(1).AddHours(17),
                IsAvailable = true
            };

            // Act
            context.Schedules.Add(schedule);
            context.SaveChanges();

            // Assert
            var created = context.Schedules.FirstOrDefault(s => s.DoctorId == 1);
            Assert.NotNull(created);
            Assert.Equal(workDate, created.WorkDate);
            Assert.True(created.IsAvailable);
        }

        /// <summary>
        /// Тест: получение расписания врача на дату.
        /// </summary>
        [Fact]
        public void GetScheduleByDoctorAndDate_ValidData_ReturnsSchedule()
        {
            // Arrange
            using var context = GetInMemoryContext();
            var workDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1));
            var schedule = new Schedule
            {
                DoctorId = 1,
                WorkDate = workDate,
                StartTime = DateTime.Today.AddDays(1).AddHours(9),
                EndTime = DateTime.Today.AddDays(1).AddHours(17),
                IsAvailable = true
            };
            context.Schedules.Add(schedule);
            context.SaveChanges();

            // Act
            var found = context.Schedules
                .FirstOrDefault(s => s.DoctorId == 1 && s.WorkDate == workDate);

            // Assert
            Assert.NotNull(found);
            Assert.Equal(1, found.DoctorId);
        }
    }
}
