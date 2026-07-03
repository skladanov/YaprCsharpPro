using Domain.Models;

namespace Application.Dtos;

public record RegisterRequest(string Login, string Password, string? Role);