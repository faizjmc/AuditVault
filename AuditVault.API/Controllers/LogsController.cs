using AirMark.AuditVault.Application.DTOs;
using AirMark.AuditVault.Application.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AirMark.AuditVault.API.Controllers;

[ApiController]
[Route("logs")]
[Authorize]
public class LogsController : ControllerBase
{
    private readonly ILogService _logService;
    private readonly IValidator<CreateLogRequest> _validator;
    private readonly ILogger<LogsController> _logger;

    public LogsController(
        ILogService logService,
        IValidator<CreateLogRequest> validator,
        ILogger<LogsController> logger)
    {
        _logService = logService;
        _validator = validator;
        _logger = logger;
    }

    /// <summary>
    /// Upload a new audit log. Auditors only.
    /// TenantId is sourced from the authenticated JWT — never from the request body.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Auditor")]
    [ProducesResponseType(typeof(LogResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateLog([FromBody] CreateLogRequest request)
    {
        var validation = await _validator.ValidateAsync(request);
        if (!validation.IsValid)
            return BadRequest(validation.Errors.Select(e => e.ErrorMessage));

        var result = await _logService.CreateLogAsync(request);
        return CreatedAtAction(nameof(GetLogById), new { id = result.Id }, result);
    }

    /// <summary>
    /// Get paginated audit logs for the authenticated user's tenant.
    /// Admins and Auditors may access.
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin,Auditor")]
    [ProducesResponseType(typeof(PagedResult<LogResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLogs(
        [FromQuery] int page = 1,
        [FromQuery] int limit = 20)
    {
        if (page < 1 || limit < 1 || limit > 100)
            return BadRequest(new { error = "page must be >= 1 and limit must be between 1 and 100." });

        var result = await _logService.GetLogsAsync(page, limit);
        return Ok(result);
    }

    /// <summary>
    /// Get a specific audit log by ID.
    /// Only returns the log if it belongs to the authenticated user's tenant.
    /// </summary>
    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Admin,Auditor")]
    [ProducesResponseType(typeof(LogResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetLogById(Guid id)
    {
        var result = await _logService.GetLogByIdAsync(id);
        if (result is null)
            return NotFound(new { error = $"Log {id} not found." });

        return Ok(result);
    }
}
