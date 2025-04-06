using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using BlazorApp1.Server.Data;
using BlazorApp1.Shared.Models;
using BlazorApp1.Shared.Constants;
using BlazorApp1.Shared;
using BlazorApp1.Shared.Enums;

namespace BlazorApp1.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "SuperAdmin,Admin,User")]
    public class AdminController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AdminController> _logger;

        public AdminController(ApplicationDbContext context, ILogger<AdminController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet("GetClientCount")]
        public async Task<ActionResult<int>> GetClientCount([FromQuery] int year, [FromQuery] string viewType, [FromQuery] int? month = null)
        {
            try
            {
                var query = _context.Users.Where(u => u.Role == "Client");

                // For client count, we'll use the CreatedAt date for filtering
                if (viewType.ToLower() == "yearly")
                {
                    query = query.Where(u => u.CreatedAt.Year == year);
                }
                else if (viewType.ToLower() == "monthly" && month.HasValue)
                {
                    // Filter by both year and month exactly
                    query = query.Where(u => u.CreatedAt.Year == year && u.CreatedAt.Month == month.Value);
                }

                var count = await query.CountAsync();
                return Ok(count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting client count");
                return StatusCode(500, 0);
            }
        }

        [HttpGet("GetRequestCount")]
        public async Task<ActionResult<int>> GetRequestCount()
        {
            try
            {
                var submissionsCount = await _context.Submissions
                    .Where(s => s.Status != SubmissionStatus.Archived && s.Status != SubmissionStatus.Resolved)
                    .CountAsync();

                var contactFormsCount = await _context.ContactForms
                    .Where(c => c.Status != SubmissionStatus.Archived && c.Status != SubmissionStatus.Resolved)
                    .CountAsync();

                return Ok(submissionsCount + contactFormsCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting request count");
                return StatusCode(500, 0);
            }
        }

        [HttpGet("GetDuePaymentsCount")]
        public async Task<ActionResult<int>> GetDuePaymentsCount()
        {
            try
            {
                var count = await _context.Bills
                    .Where(b => !b.IsPaid && b.DueDate <= DateTime.UtcNow)
                    .CountAsync();

                return Ok(count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting due payments count");
                return StatusCode(500, 0);
            }
        }

        [HttpGet("GetArchivedCount")]
        public async Task<ActionResult<int>> GetArchivedCount()
        {
            try
            {
                var submissionsCount = await _context.Submissions
                    .Where(s => s.Status == SubmissionStatus.Archived)
                    .CountAsync();

                var contactFormsCount = await _context.ContactForms
                    .Where(c => c.Status == SubmissionStatus.Archived)
                    .CountAsync();

                return Ok(submissionsCount + contactFormsCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting archived count");
                return StatusCode(500, 0);
            }
        }

        [HttpGet("GetTotalPendingWaterBills")]
        public async Task<ActionResult<decimal>> GetTotalPendingWaterBills([FromQuery] int year, [FromQuery] string viewType, [FromQuery] int? month = null)
        {
            try
            {
                var query = _context.Bills.Where(b => b.BillType == "Water" && !b.IsPaid);

                // Apply date filtering
                if (viewType.ToLower() == "yearly")
                {
                    query = query.Where(b => b.BillDate.Year == year);
                }
                else if (viewType.ToLower() == "monthly" && month.HasValue)
                {
                    // Filter by both year and month exactly
                    query = query.Where(b => b.BillDate.Year == year && b.BillDate.Month == month.Value);
                }

                var totalAmount = await query.SumAsync(b => b.Amount);
                return Ok(totalAmount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting total pending water bills amount");
                return StatusCode(500, 0m);
            }
        }

        [HttpGet("GetTotalPaidWaterBills")]
        public async Task<ActionResult<decimal>> GetTotalPaidWaterBills([FromQuery] int year, [FromQuery] string viewType, [FromQuery] int? month = null)
        {
            try
            {
                var query = _context.Bills.Where(b => b.BillType == "Water" && b.IsPaid);

                // Apply date filtering
                if (viewType.ToLower() == "yearly")
                {
                    query = query.Where(b => b.BillDate.Year == year);
                }
                else if (viewType.ToLower() == "monthly" && month.HasValue)
                {
                    // Filter by both year and month exactly
                    query = query.Where(b => b.BillDate.Year == year && b.BillDate.Month == month.Value);
                }

                var totalAmount = await query.SumAsync(b => b.Amount);
                return Ok(totalAmount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting total paid water bills amount");
                return StatusCode(500, 0m);
            }
        }

        [HttpGet("GetTotalPendingOtherBills")]
        public async Task<ActionResult<decimal>> GetTotalPendingOtherBills([FromQuery] int year, [FromQuery] string viewType, [FromQuery] int? month = null)
        {
            try
            {
                var query = _context.Bills.Where(b => b.BillType != "Water" && !b.IsPaid);

                // Apply date filtering
                if (viewType.ToLower() == "yearly")
                {
                    query = query.Where(b => b.BillDate.Year == year);
                }
                else if (viewType.ToLower() == "monthly" && month.HasValue)
                {
                    // Filter by both year and month exactly
                    query = query.Where(b => b.BillDate.Year == year && b.BillDate.Month == month.Value);
                }

                var totalAmount = await query.SumAsync(b => b.Amount);
                return Ok(totalAmount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting total pending other bills amount");
                return StatusCode(500, 0m);
            }
        }

        [HttpGet("GetTotalPaidOtherBills")]
        public async Task<ActionResult<decimal>> GetTotalPaidOtherBills([FromQuery] int year, [FromQuery] string viewType, [FromQuery] int? month = null)
        {
            try
            {
                var query = _context.Bills.Where(b => b.BillType != "Water" && b.IsPaid);

                // Apply date filtering
                if (viewType.ToLower() == "yearly")
                {
                    query = query.Where(b => b.BillDate.Year == year);
                }
                else if (viewType.ToLower() == "monthly" && month.HasValue)
                {
                    // Filter by both year and month exactly
                    query = query.Where(b => b.BillDate.Year == year && b.BillDate.Month == month.Value);
                }

                var totalAmount = await query.SumAsync(b => b.Amount);
                return Ok(totalAmount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting total paid other bills amount");
                return StatusCode(500, 0m);
            }
        }

        [HttpGet("GetTotalAllPendingBills")]
        public async Task<ActionResult<decimal>> GetTotalAllPendingBills([FromQuery] int year, [FromQuery] string viewType, [FromQuery] int? month = null)
        {
            try
            {
                var query = _context.Bills.Where(b => !b.IsPaid);

                // Apply date filtering
                if (viewType.ToLower() == "yearly")
                {
                    query = query.Where(b => b.BillDate.Year == year);
                }
                else if (viewType.ToLower() == "monthly" && month.HasValue)
                {
                    // Filter by both year and month exactly
                    query = query.Where(b => b.BillDate.Year == year && b.BillDate.Month == month.Value);
                }

                var totalAmount = await query.SumAsync(b => b.Amount);
                return Ok(totalAmount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting total all pending bills amount");
                return StatusCode(500, 0m);
            }
        }

        [HttpGet("GetTotalAllPaidBills")]
        public async Task<ActionResult<decimal>> GetTotalAllPaidBills([FromQuery] int year, [FromQuery] string viewType, [FromQuery] int? month = null)
        {
            try
            {
                var query = _context.Bills.Where(b => b.IsPaid);

                // Apply date filtering
                if (viewType.ToLower() == "yearly")
                {
                    query = query.Where(b => b.BillDate.Year == year);
                }
                else if (viewType.ToLower() == "monthly" && month.HasValue)
                {
                    // Filter by both year and month exactly
                    query = query.Where(b => b.BillDate.Year == year && b.BillDate.Month == month.Value);
                }

                var totalAmount = await query.SumAsync(b => b.Amount);
                return Ok(totalAmount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting total all paid bills amount");
                return StatusCode(500, 0m);
            }
        }

        [HttpGet("GetTotalAllWaterBills")]
        public async Task<ActionResult<decimal>> GetTotalAllWaterBills([FromQuery] int year, [FromQuery] string viewType, [FromQuery] int? month = null)
        {
            try
            {
                var query = _context.Bills.Where(b => b.BillType == "Water");

                if (viewType.ToLower() == "yearly")
                {
                    query = query.Where(b => b.BillDate.Year == year);
                }
                else if (viewType.ToLower() == "monthly" && month.HasValue)
                {
                    query = query.Where(b => b.BillDate.Year == year && b.BillDate.Month == month.Value);
                }

                var totalAmount = await query.SumAsync(b => b.Amount);
                return Ok(totalAmount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting total water bills amount");
                return StatusCode(500, 0m);
            }
        }

        [HttpGet("GetTotalAllOtherBills")]
        public async Task<ActionResult<decimal>> GetTotalAllOtherBills([FromQuery] int year, [FromQuery] string viewType, [FromQuery] int? month = null)
        {
            try
            {
                var query = _context.Bills.Where(b => b.BillType != "Water");

                if (viewType.ToLower() == "yearly")
                {
                    query = query.Where(b => b.BillDate.Year == year);
                }
                else if (viewType.ToLower() == "monthly" && month.HasValue)
                {
                    query = query.Where(b => b.BillDate.Year == year && b.BillDate.Month == month.Value);
                }

                var totalAmount = await query.SumAsync(b => b.Amount);
                return Ok(totalAmount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting total other bills amount");
                return StatusCode(500, 0m);
            }
        }
    }
} 