namespace SmartService.API.Controllers;

using Microsoft.AspNetCore.Mvc;
using SmartService.Application.Interfaces;
using SmartService.Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

[ApiController]
[Route("health")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok("Service running");
}