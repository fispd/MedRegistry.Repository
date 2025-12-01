using DataLayer.Data;
using DataLayer.Models;
using MedRegistry.webapi.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MedRegistry.webapi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly MedRegistryContext _context;

    public AuthController(MedRegistryContext context)
    {
        _context = context;
    }

    // POST: api/auth/login
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponseDto>> Login(LoginDto dto)
    {
        var user = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Username == dto.Username);

        if (user == null)
        {
            return Ok(new LoginResponseDto
            {
                Success = false,
                Message = "Пользователь не найден"
            });
        }

        if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
        {
            return Ok(new LoginResponseDto
            {
                Success = false,
                Message = "Неверный пароль"
            });
        }

        return Ok(new LoginResponseDto
        {
            Success = true,
            User = new UserDto
            {
                UserId = user.UserId,
                Username = user.Username,
                FirstName = user.FirstName,
                LastName = user.LastName,
                MiddleName = user.MiddleName,
                Phone = user.Phone,
                Email = user.Email,
                Address = user.Address,
                MedicalPolicy = user.MedicalPolicy,
                RoleName = user.Role?.RoleName ?? ""
            }
        });
    }

    // POST: api/auth/register
    [HttpPost("register")]
    public async Task<ActionResult<LoginResponseDto>> Register(RegisterDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Username))
        {
            return Ok(new LoginResponseDto
            {
                Success = false,
                Message = "Логин не может быть пустым"
            });
        }

        if (!System.Text.RegularExpressions.Regex.IsMatch(dto.Username, @"^[a-zA-Z0-9_]{3,20}$"))
        {
            return Ok(new LoginResponseDto
            {
                Success = false,
                Message = "Логин должен содержать только латинские буквы, цифры и подчеркивания, от 3 до 20 символов"
            });
        }

        var exists = await _context.Users.AnyAsync(u => u.Username == dto.Username);
        if (exists)
        {
            return Ok(new LoginResponseDto
            {
                Success = false,
                Message = "Пользователь с таким логином уже существует"
            });
        }

        if (!string.IsNullOrWhiteSpace(dto.Phone))
        {
            var phoneExists = await _context.Users.AnyAsync(u => u.Phone == dto.Phone);
            if (phoneExists)
            {
                return Ok(new LoginResponseDto
                {
                    Success = false,
                    Message = "Пользователь с таким номером телефона уже зарегистрирован"
                });
            }
        }

        if (!string.IsNullOrWhiteSpace(dto.Email))
        {
            var emailExists = await _context.Users.AnyAsync(u => u.Email == dto.Email);
            if (emailExists)
            {
                return Ok(new LoginResponseDto
                {
                    Success = false,
                    Message = "Пользователь с таким email уже зарегистрирован"
                });
            }
        }

        // Создаём пользователя
        var user = new User
        {
            Username = dto.Username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            MiddleName = dto.MiddleName,
            Phone = dto.Phone,
            Email = dto.Email,
            Address = dto.Address,
            MedicalPolicy = dto.MedicalPolicy,
            RoleId = 5 // Пациент по умолчанию
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Создаём запись пациента
        var patient = new Patient
        {
            UserId = user.UserId,
            BirthDate = dto.BirthDate,
            Gender = dto.Gender,
            Address = dto.Address
        };

        _context.Patients.Add(patient);
        await _context.SaveChangesAsync();

        return Ok(new LoginResponseDto
        {
            Success = true,
            Message = "Регистрация успешна",
            User = new UserDto
            {
                UserId = user.UserId,
                Username = user.Username,
                FirstName = user.FirstName,
                LastName = user.LastName,
                RoleName = "Пациент"
            }
        });
    }

    // GET: api/auth/user/5
    [HttpGet("user/{id}")]
    public async Task<ActionResult<UserDto>> GetUser(int id)
    {
        var user = await _context.Users
            .Include(u => u.Role)
            .Where(u => u.UserId == id)
            .Select(u => new UserDto
            {
                UserId = u.UserId,
                Username = u.Username,
                FirstName = u.FirstName,
                LastName = u.LastName,
                MiddleName = u.MiddleName,
                Phone = u.Phone,
                Email = u.Email,
                Address = u.Address,
                MedicalPolicy = u.MedicalPolicy,
                RoleName = u.Role.RoleName
            })
            .FirstOrDefaultAsync();

        if (user == null)
            return NotFound();

        return Ok(user);
    }
}

