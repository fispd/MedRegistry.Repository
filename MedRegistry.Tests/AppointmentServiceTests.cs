using Xunit;
using DataLayer.Data;
using DataLayer.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MedRegistry.Tests
{
    public class AppointmentServiceTests
    {
        private MedRegistryContext GetInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<MedRegistryContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new MedRegistryContext(options);
        }

        [Fact]
        public void CreateAppointment_ValidData_ReturnsAppointment()
        {
            // Arrange
            using var context = GetInMemoryContext();
            var patient = new Patient { PatientId = 1, UserId = 1 };
            var doctor = new Doctor { DoctorId = 1, UserId = 2 };
            context.Patients.Add(patient);
            context.Doctors.Add(doctor);
            context.SaveChanges();

            var appointment = new Appointment
            {
                PatientId = 1,
                DoctorId = 1,
                AppointmentStart = DateTime.Now.AddDays(1),
                AppointmentEnd = DateTime.Now.AddDays(1).AddHours(1),
                Status = "Запланировано"
            };

            // Act
            context.Appointments.Add(appointment);
            context.SaveChanges();

            // Assert
            var created = context.Appointments.FirstOrDefault(a => a.PatientId == 1);
            Assert.NotNull(created);
            Assert.Equal("Запланировано", created.Status);
        }

        [Fact]
        public void GetAppointmentsByPatient_ValidPatientId_ReturnsAppointments()
        {
            // Arrange
            using var context = GetInMemoryContext();
            var appointment = new Appointment
            {
                PatientId = 1,
                DoctorId = 1,
                AppointmentStart = DateTime.Now.AddDays(1),
                AppointmentEnd = DateTime.Now.AddDays(1).AddHours(1),
                Status = "Запланировано"
            };
            context.Appointments.Add(appointment);
            context.SaveChanges();

            // Act
            var appointments = context.Appointments
                .Where(a => a.PatientId == 1)
                .ToList();

            // Assert
            Assert.Single(appointments);
            Assert.Equal(1, appointments[0].PatientId);
        }
    }
}

