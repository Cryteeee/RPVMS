using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BlazorApp1.Server.Data;
using BlazorApp1.Shared.Models;
using BlazorApp1.Shared;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using BlazorApp1.Server.Models;
using System.ComponentModel.DataAnnotations;

namespace BlazorApp1.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "SuperAdmin,Admin,User,Client")]
    public class BillingController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<BillingController> _logger;

        public BillingController(ApplicationDbContext context, ILogger<BillingController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet("user/{userId}")]
        [Authorize(Roles = "SuperAdmin,Admin,User,Client")]
        public async Task<ActionResult<Response<List<BillingDto>>>> GetUserBills(string userId)
        {
            try
            {
                _logger.LogInformation($"Getting bills for user ID: {userId}");
                if (!int.TryParse(userId, out int userIdInt))
                {
                    _logger.LogWarning($"Invalid user ID format: {userId}");
                    return BadRequest(new Response<List<BillingDto>>
                    {
                        IsSuccess = false,
                        Message = $"Invalid user ID format: {userId}"
                    });
                }

                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
                var isAdminOrSuperAdmin = userRole == "SuperAdmin" || userRole == "Admin";
                var isUser = userRole == "User";

                // Allow Users to view bills
                if (!isAdminOrSuperAdmin && !isUser && currentUserId != userId)
                {
                    _logger.LogWarning($"Unauthorized access attempt: User {currentUserId} tried to access bills for user {userId}");
                    return Forbid();
                }

                var user = await _context.Users.FindAsync(userIdInt);
                if (user == null)
                {
                    _logger.LogWarning($"User not found: {userId}");
                    return NotFound(new Response<List<BillingDto>>
                    {
                        IsSuccess = false,
                        Message = $"User not found: {userId}"
                    });
                }

                var bills = await _context.Bills
                    .Where(b => b.UserId == userIdInt)
                    .Select(b => new BillingDto
                    {
                        Id = b.Id,
                        UserId = b.UserId.ToString(),
                        Description = b.Description,
                        Amount = b.Amount,
                        BillDate = b.BillDate,
                        DueDate = b.DueDate,
                        Status = b.Status,
                        IsPaid = b.IsPaid,
                        PaymentDate = b.PaymentDate,
                        UserName = b.User.UserName,
                        BillType = b.BillType,
                        CubicMeter = b.CubicMeter,
                        PreviousCubicMeter = b.PreviousCubicMeter,
                        PricePerCubicMeter = b.PricePerCubicMeter,
                        ORNumber = b.ORNumber
                    })
                    .OrderByDescending(b => b.BillDate)
                    .ToListAsync();

                _logger.LogInformation($"Found {bills.Count} bills for user {userId}");
                return Ok(new Response<List<BillingDto>>
                {
                    Data = bills,
                    IsSuccess = true,
                    Message = $"Retrieved {bills.Count} bills successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving bills for user {userId}");
                return StatusCode(500, new Response<List<BillingDto>>
                {
                    IsSuccess = false,
                    Message = $"An error occurred while retrieving bills: {ex.Message}"
                });
            }
        }

        [HttpPost]
        [Authorize(Roles = "SuperAdmin,Admin,User")]
        public async Task<ActionResult<Response<BillingDto>>> AddBill([FromBody] BillingDto billDto)
        {
            try
            {
                _logger.LogInformation($"Adding bill for user {billDto.UserId}: Amount={billDto.Amount}, Description={billDto.Description}");
                if (!int.TryParse(billDto.UserId, out int userIdInt))
                {
                    _logger.LogWarning($"Invalid user ID format: {billDto.UserId}");
                    return BadRequest(new Response<BillingDto>
                    {
                        IsSuccess = false,
                        Message = $"Invalid user ID format: {billDto.UserId}"
                    });
                }

                var user = await _context.Users.FindAsync(userIdInt);
                if (user == null)
                {
                    _logger.LogWarning($"User not found: {billDto.UserId}");
                    return NotFound(new Response<BillingDto>
                    {
                        IsSuccess = false,
                        Message = $"User not found: {billDto.UserId}"
                    });
                }

                var bill = new Bill
                {
                    UserId = userIdInt,
                    Description = billDto.Description,
                    Amount = billDto.CubicMeter.HasValue && billDto.PreviousCubicMeter.HasValue && billDto.PricePerCubicMeter.HasValue
                        ? (billDto.CubicMeter.Value - billDto.PreviousCubicMeter.Value) * billDto.PricePerCubicMeter.Value
                        : billDto.Amount,
                    BillDate = DateTime.Now,
                    DueDate = billDto.DueDate,
                    Status = "Pending",
                    IsPaid = false,
                    BillType = billDto.BillType,
                    CubicMeter = billDto.CubicMeter,
                    PreviousCubicMeter = billDto.PreviousCubicMeter,
                    PricePerCubicMeter = billDto.PricePerCubicMeter,
                    ORNumber = null
                };

                _context.Bills.Add(bill);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Successfully added bill {bill.Id} for user {billDto.UserId}");

                billDto.Id = bill.Id;
                billDto.Status = bill.Status;
                billDto.BillDate = bill.BillDate;

                return Ok(new Response<BillingDto>
                {
                    Data = billDto,
                    IsSuccess = true,
                    Message = "Bill added successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error adding bill for user {billDto.UserId}");
                return StatusCode(500, new Response<BillingDto>
                {
                    IsSuccess = false,
                    Message = $"An error occurred while adding the bill: {ex.Message}"
                });
            }
        }

        [HttpPut("{id}/pay")]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<ActionResult<Response>> MarkAsPaid(int id, [FromBody] MarkAsPaidRequest request)
        {
            try
            {
                var bill = await _context.Bills.FindAsync(id);
                if (bill == null)
                {
                    return NotFound(new Response
                    {
                        IsSuccess = false,
                        Message = "Bill not found"
                    });
                }

                if (string.IsNullOrWhiteSpace(request.ORNumber))
                {
                    return BadRequest(new Response
                    {
                        IsSuccess = false,
                        Message = "OR Number is required"
                    });
                }

                bill.IsPaid = true;
                bill.Status = "Paid";
                bill.PaymentDate = DateTime.Now;
                bill.ORNumber = request.ORNumber;

                await _context.SaveChangesAsync();

                return Ok(new Response
                {
                    IsSuccess = true,
                    Message = "Bill marked as paid successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking bill {BillId} as paid", id);
                return StatusCode(500, new Response
                {
                    IsSuccess = false,
                    Message = "An error occurred while marking the bill as paid"
                });
            }
        }

        [HttpPost("compress/{userId}")]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<ActionResult<Response>> CompressBills(string userId)
        {
            try
            {
                _logger.LogInformation($"Starting bill compression for user {userId}");
                
                if (!int.TryParse(userId, out int userIdInt))
                {
                    _logger.LogWarning($"Invalid user ID format: {userId}");
                    return BadRequest(new Response
                    {
                        IsSuccess = false,
                        Message = $"Invalid user ID format: {userId}"
                    });
                }

                var user = await _context.Users.FindAsync(userIdInt);
                if (user == null)
                {
                    _logger.LogWarning($"User not found: {userId}");
                    return NotFound(new Response
                    {
                        IsSuccess = false,
                        Message = $"User not found: {userId}"
                    });
                }

                var bills = await _context.Bills
                    .Where(b => b.UserId == userIdInt && !b.IsPaid)
                    .ToListAsync();

                _logger.LogInformation($"Found {bills.Count} unpaid bills for user {userId}");

                if (!bills.Any())
                {
                    _logger.LogInformation($"No unpaid bills found for user {userId}");
                    return Ok(new Response
                    {
                        IsSuccess = true,
                        Message = "No unpaid bills to compress"
                    });
                }

                // Group bills by type and create a single compressed bill
                var groupedBills = bills.GroupBy(b => b.BillType);
                var compressedBills = new List<Bill>();
                var removedBillIds = new List<int>();

                foreach (var group in groupedBills)
                {
                    var totalAmount = group.Sum(b => b.Amount);
                    var description = $"Compressed {group.Key} bills ({group.Count()} bills)";
                    var earliestDueDate = group.Min(b => b.DueDate);
                    
                    var compressedBill = new Bill
                    {
                        UserId = userIdInt,
                        BillType = group.Key,
                        Description = description,
                        Amount = totalAmount,
                        BillDate = DateTime.Now,
                        DueDate = earliestDueDate < DateTime.Now ? DateTime.Now.AddMonths(1) : earliestDueDate,
                        Status = "Pending",
                        IsPaid = false,
                        CubicMeter = group.Any(b => b.CubicMeter.HasValue) ? group.Max(b => b.CubicMeter) : null,
                        PreviousCubicMeter = group.Any(b => b.PreviousCubicMeter.HasValue) ? group.Min(b => b.PreviousCubicMeter) : null,
                        PricePerCubicMeter = group.FirstOrDefault(b => b.PricePerCubicMeter.HasValue)?.PricePerCubicMeter
                    };

                    compressedBills.Add(compressedBill);
                    removedBillIds.AddRange(group.Select(b => b.Id));
                }

                // Remove original bills and add compressed bills
                var billsToRemove = await _context.Bills.Where(b => removedBillIds.Contains(b.Id)).ToListAsync();
                _context.Bills.RemoveRange(billsToRemove);
                await _context.Bills.AddRangeAsync(compressedBills);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Successfully compressed {bills.Count} bills into {compressedBills.Count} consolidated bills for user {userId}");

                return Ok(new Response
                {
                    IsSuccess = true,
                    Message = $"Successfully compressed {bills.Count} bills into {compressedBills.Count} consolidated bills"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error compressing bills for user {UserId}", userId);
                return StatusCode(500, new Response
                {
                    IsSuccess = false,
                    Message = $"An error occurred while compressing the bills: {ex.Message}"
                });
            }
        }

        [HttpPost("setcubicprice")]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<ActionResult<Response>> SetCubicPrice([FromBody] SetCubicPriceRequest request)
        {
            try
            {
                if (!int.TryParse(request.UserId, out int userIdInt))
                {
                    return BadRequest(new Response
                    {
                        IsSuccess = false,
                        Message = $"Invalid user ID format: {request.UserId}"
                    });
                }

                var user = await _context.Users.FindAsync(userIdInt);
                if (user == null)
                {
                    return NotFound(new Response
                    {
                        IsSuccess = false,
                        Message = $"User not found: {request.UserId}"
                    });
                }

                // Update or create user's cubic meter price setting
                var priceSetting = await _context.CubicMeterPriceSettings
                    .FirstOrDefaultAsync(p => p.UserId == userIdInt);

                if (priceSetting == null)
                {
                    priceSetting = new CubicMeterPriceSetting
                    {
                        UserId = userIdInt,
                        PricePerCubicMeter = request.PricePerCubicMeter,
                        LastUpdated = DateTime.Now
                    };
                    _context.CubicMeterPriceSettings.Add(priceSetting);
                }
                else
                {
                    priceSetting.PricePerCubicMeter = request.PricePerCubicMeter;
                    priceSetting.LastUpdated = DateTime.Now;
                }

                await _context.SaveChangesAsync();

                return Ok(new Response
                {
                    IsSuccess = true,
                    Message = "Cubic meter price set successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting cubic meter price for user {UserId}", request.UserId);
                return StatusCode(500, new Response
                {
                    IsSuccess = false,
                    Message = "An error occurred while setting the cubic meter price"
                });
            }
        }

        [HttpPost("setcubicnumber")]
        [Authorize(Roles = "SuperAdmin,Admin,User")]
        public async Task<ActionResult<Response>> SetCubicNumber([FromBody] SetCubicNumberRequest request)
        {
            try
            {
                _logger.LogInformation($"Creating water bill - UserId: {request.UserId}, CubicNumber: {request.CubicNumber}, DueDate: {request.DueDate}");

                if (!int.TryParse(request.UserId, out int userIdInt))
                {
                    _logger.LogWarning($"Invalid user ID format: {request.UserId}");
                    return BadRequest(new Response
                    {
                        IsSuccess = false,
                        Message = $"Invalid user ID format: {request.UserId}"
                    });
                }

                var user = await _context.Users.FindAsync(userIdInt);
                if (user == null)
                {
                    _logger.LogWarning($"User not found: {request.UserId}");
                    return NotFound(new Response
                    {
                        IsSuccess = false,
                        Message = $"User not found: {request.UserId}"
                    });
                }

                // Get the latest cubic meter price setting for the user
                var priceSettings = await _context.CubicMeterPriceSettings
                    .Where(p => p.UserId == userIdInt)
                    .OrderByDescending(p => p.LastUpdated)
                    .FirstOrDefaultAsync();

                if (priceSettings == null)
                {
                    _logger.LogWarning($"No price settings found for user {request.UserId}");
                    return BadRequest(new Response
                    {
                        IsSuccess = false,
                        Message = "Please set the cubic meter price first"
                    });
                }

                // Get the latest reading for comparison
                var latestReading = await _context.Bills
                    .Where(b => b.UserId == userIdInt && b.BillType == "Water")
                    .OrderByDescending(b => b.BillDate)
                    .FirstOrDefaultAsync();

                decimal previousReading = latestReading?.CubicMeter ?? 0;
                decimal consumption = request.CubicNumber - previousReading;

                if (request.CubicNumber < 0)
                {
                    _logger.LogWarning($"Negative cubic number provided: {request.CubicNumber}");
                    return BadRequest(new Response
                    {
                        IsSuccess = false,
                        Message = "Current reading cannot be negative"
                    });
                }

                if (latestReading != null && consumption <= 0)
                {
                    _logger.LogWarning($"Invalid consumption: Current reading ({request.CubicNumber}) must be greater than previous reading ({previousReading})");
                    return BadRequest(new Response
                    {
                        IsSuccess = false,
                        Message = $"Current reading ({request.CubicNumber}) must be greater than the previous reading ({previousReading})"
                    });
                }

                // Create a new water bill
                var bill = new Bill
                {
                    UserId = userIdInt,
                    Description = $"Water Consumption Bill ({previousReading} to {request.CubicNumber} cubic meters)",
                    BillType = "Water",
                    BillDate = DateTime.Now,
                    DueDate = request.DueDate,
                    Status = "Pending",
                    IsPaid = false,
                    CubicMeter = request.CubicNumber,
                    PreviousCubicMeter = previousReading,
                    PricePerCubicMeter = priceSettings.PricePerCubicMeter,
                    Amount = consumption * priceSettings.PricePerCubicMeter,
                    ORNumber = null
                };

                _context.Bills.Add(bill);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Water bill created successfully for user {request.UserId} - Bill ID: {bill.Id}");

                return Ok(new Response
                {
                    IsSuccess = true,
                    Message = $"Water bill created successfully:\n" +
                             $"Previous Reading: {previousReading:N2} m³\n" +
                             $"Current Reading: {request.CubicNumber:N2} m³\n" +
                             $"Consumption: {consumption:N2} m³\n" +
                             $"Rate: ₱{priceSettings.PricePerCubicMeter:N2}/m³\n" +
                             $"Amount Due: ₱{bill.Amount:N2}\n" +
                             $"Due Date: {request.DueDate:MMM dd, yyyy}"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating water bill for user {UserId}: {ErrorMessage}", request.UserId, ex.Message);
                return StatusCode(500, new Response
                {
                    IsSuccess = false,
                    Message = $"An error occurred while creating the water bill: {ex.Message}"
                });
            }
        }

        [HttpDelete("all/{userId}")]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<ActionResult<Response>> DeleteAllBills(string userId)
        {
            try
            {
                if (!int.TryParse(userId, out int userIdInt))
                {
                    return BadRequest(new Response
                    {
                        IsSuccess = false,
                        Message = $"Invalid user ID format: {userId}"
                    });
                }

                var bills = await _context.Bills
                    .Where(b => b.UserId == userIdInt)
                    .ToListAsync();

                if (!bills.Any())
                {
                    return Ok(new Response
                    {
                        IsSuccess = true,
                        Message = "No bills found to delete"
                    });
                }

                _context.Bills.RemoveRange(bills);
                await _context.SaveChangesAsync();

                return Ok(new Response
                {
                    IsSuccess = true,
                    Message = $"Successfully deleted {bills.Count} bills"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting all bills for user {UserId}", userId);
                return StatusCode(500, new Response
                {
                    IsSuccess = false,
                    Message = "An error occurred while deleting the bills"
                });
            }
        }

        [HttpGet("download/{userId}")]
        public async Task<IActionResult> DownloadBills(string userId)
        {
            try
            {
                _logger.LogInformation($"Starting bill download for user {userId}");
                
                if (!int.TryParse(userId, out int userIdInt))
                {
                    _logger.LogWarning($"Invalid user ID format: {userId}");
                    return BadRequest(new Response
                    {
                        IsSuccess = false,
                        Message = $"Invalid user ID format: {userId}"
                    });
                }

                // Check if the user is authorized to download these bills
                var currentUser = User;
                var currentUserId = currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var isAdminOrSuperAdmin = currentUser.IsInRole("Admin") || currentUser.IsInRole("SuperAdmin");

                // Only allow admins to download any bills, or users to download their own bills
                if (!isAdminOrSuperAdmin && currentUserId != userId)
                {
                    _logger.LogWarning($"Unauthorized attempt to download bills. User {currentUserId} tried to access bills for user {userId}");
                    return Forbid();
                }

                var user = await _context.Users.FindAsync(userIdInt);
                if (user == null)
                {
                    _logger.LogWarning($"User not found: {userId}");
                    return NotFound(new Response
                    {
                        IsSuccess = false,
                        Message = $"User not found: {userId}"
                    });
                }

                var bills = await _context.Bills
                    .Where(b => b.UserId == userIdInt)
                    .OrderByDescending(b => b.BillDate)
                    .ToListAsync();

                if (!bills.Any())
                {
                    return NotFound(new Response
                    {
                        IsSuccess = false,
                        Message = "No bills found to download"
                    });
                }

                var memoryStream = new MemoryStream();
                try
                {
                    using (var archive = new System.IO.Compression.ZipArchive(memoryStream, System.IO.Compression.ZipArchiveMode.Create, true))
                    {
                        // Create a CSV file with all bills
                        var csvEntry = archive.CreateEntry($"{user.UserName}_bills.csv");
                        using (var csvStream = new StreamWriter(csvEntry.Open()))
                        {
                            // Write CSV header
                            csvStream.WriteLine("Bill Date,Due Date,Type,Description,Amount,Status,OR Number");
                            
                            // Write bill data
                            foreach (var bill in bills)
                            {
                                csvStream.WriteLine($"{bill.BillDate:yyyy-MM-dd},{bill.DueDate:yyyy-MM-dd},{bill.BillType},{bill.Description},{bill.Amount:N2},{bill.Status},{bill.ORNumber ?? "N/A"}");
                            }
                        }

                        // Create individual text files for each bill
                        foreach (var bill in bills)
                        {
                            var textEntry = archive.CreateEntry($"{user.UserName}_bill_{bill.Id}.txt");
                            using (var textStream = new StreamWriter(textEntry.Open()))
                            {
                                textStream.WriteLine($"BILL DETAILS");
                                textStream.WriteLine($"============");
                                textStream.WriteLine($"Bill ID: {bill.Id}");
                                textStream.WriteLine($"Date: {bill.BillDate:yyyy-MM-dd}");
                                textStream.WriteLine($"Due Date: {bill.DueDate:yyyy-MM-dd}");
                                textStream.WriteLine($"Type: {bill.BillType}");
                                textStream.WriteLine($"Description: {bill.Description}");
                                textStream.WriteLine($"Amount: ₱{bill.Amount:N2}");
                                textStream.WriteLine($"Status: {bill.Status}");
                                if (!string.IsNullOrEmpty(bill.ORNumber))
                                    textStream.WriteLine($"OR Number: {bill.ORNumber}");
                                
                                if (bill.CubicMeter.HasValue)
                                {
                                    textStream.WriteLine($"\nWater Consumption Details:");
                                    textStream.WriteLine($"Previous Reading: {bill.PreviousCubicMeter:N2} m³");
                                    textStream.WriteLine($"Current Reading: {bill.CubicMeter:N2} m³");
                                    textStream.WriteLine($"Rate: ₱{bill.PricePerCubicMeter:N2}/m³");
                                }
                            }
                        }
                    }

                    memoryStream.Position = 0;
                    var fileName = $"{user.UserName}_bills_{DateTime.Now:yyyyMMdd}.zip";
                    return File(memoryStream.ToArray(), "application/zip", fileName);
                }
                finally
                {
                    await memoryStream.DisposeAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading bills for user {UserId}", userId);
                return StatusCode(500, new Response
                {
                    IsSuccess = false,
                    Message = $"An error occurred while downloading the bills: {ex.Message}"
                });
            }
        }

        [HttpGet("downloadall")]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<IActionResult> DownloadAllBills()
        {
            try
            {
                _logger.LogInformation("Starting download of all bills");

                // Check if the user is authorized
                var currentUser = User;
                var isAdminOrSuperAdmin = currentUser.IsInRole("Admin") || currentUser.IsInRole("SuperAdmin");
                if (!isAdminOrSuperAdmin)
                {
                    _logger.LogWarning("Unauthorized attempt to download all bills");
                    return Forbid();
                }

                // Get all bills grouped by user
                var userBills = await _context.Bills
                    .Include(b => b.User)
                    .OrderBy(b => b.User.UserName)
                    .ThenByDescending(b => b.BillDate)
                    .ToListAsync();

                if (!userBills.Any())
                {
                    return NotFound(new Response
                    {
                        IsSuccess = false,
                        Message = "No bills found to download"
                    });
                }

                var memoryStream = new MemoryStream();
                try
                {
                    using (var archive = new System.IO.Compression.ZipArchive(memoryStream, System.IO.Compression.ZipArchiveMode.Create, true))
                    {
                        // Create a summary CSV file with all bills
                        var csvEntry = archive.CreateEntry("all_bills_summary.csv");
                        using (var csvStream = new StreamWriter(csvEntry.Open()))
                        {
                            // Write CSV header
                            csvStream.WriteLine("User,Bill Date,Due Date,Type,Description,Amount,Status,OR Number");
                            
                            // Write bill data
                            foreach (var bill in userBills)
                            {
                                csvStream.WriteLine($"{bill.User.UserName},{bill.BillDate:yyyy-MM-dd},{bill.DueDate:yyyy-MM-dd},{bill.BillType},{bill.Description},{bill.Amount:N2},{bill.Status},{bill.ORNumber ?? "N/A"}");
                            }
                        }

                        // Create individual CSV files for each user
                        foreach (var userGroup in userBills.GroupBy(b => b.User))
                        {
                            var userCsvEntry = archive.CreateEntry($"{userGroup.Key.UserName}_bills.csv");
                            using (var csvStream = new StreamWriter(userCsvEntry.Open()))
                            {
                                // Write CSV header
                                csvStream.WriteLine("Bill Date,Due Date,Type,Description,Amount,Status,OR Number");
                                
                                // Write bill data
                                foreach (var bill in userGroup)
                                {
                                    csvStream.WriteLine($"{bill.BillDate:yyyy-MM-dd},{bill.DueDate:yyyy-MM-dd},{bill.BillType},{bill.Description},{bill.Amount:N2},{bill.Status},{bill.ORNumber ?? "N/A"}");
                                }
                            }
                        }
                    }

                    memoryStream.Position = 0;
                    var currentDate = DateTime.Now.ToString("yyyyMMdd");
                    return File(memoryStream.ToArray(), "application/zip", $"all_bills_{currentDate}.zip");
                }
                finally
                {
                    await memoryStream.DisposeAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading all bills");
                return StatusCode(500, new Response
                {
                    IsSuccess = false,
                    Message = $"An error occurred while downloading the bills: {ex.Message}"
                });
            }
        }

        public class SetCubicPriceRequest
        {
            [Required(ErrorMessage = "User ID is required")]
            public string UserId { get; set; }

            [Required(ErrorMessage = "Price per cubic meter is required")]
            [Range(0.01, double.MaxValue, ErrorMessage = "Price per cubic meter must be greater than 0")]
            public decimal PricePerCubicMeter { get; set; }
        }

        public class SetCubicNumberRequest
        {
            [Required(ErrorMessage = "User ID is required")]
            public string UserId { get; set; }
            
            [Required(ErrorMessage = "Cubic number is required")]
            [Range(0, double.MaxValue, ErrorMessage = "Cubic number must be positive")]
            public decimal CubicNumber { get; set; }

            [Required(ErrorMessage = "Due date is required")]
            [DataType(DataType.Date)]
            [FutureDate(ErrorMessage = "Due date must be in the future")]
            public DateTime DueDate { get; set; }
        }

        public class SetMonthlyPriceRequest
        {
            [Required(ErrorMessage = "User ID is required")]
            public string UserId { get; set; }

            [Required(ErrorMessage = "Monthly price is required")]
            [Range(0.01, double.MaxValue, ErrorMessage = "Monthly price must be greater than 0")]
            public decimal MonthlyPrice { get; set; }
        }

        [HttpPost("setmonthlyprice")]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<ActionResult<Response>> SetMonthlyPrice([FromBody] SetMonthlyPriceRequest request)
        {
            try
            {
                _logger.LogInformation($"Setting monthly price for user {request.UserId}: {request.MonthlyPrice}");

                if (!int.TryParse(request.UserId, out int userIdInt))
                {
                    _logger.LogWarning($"Invalid user ID format: {request.UserId}");
                    return BadRequest(new Response
                    {
                        IsSuccess = false,
                        Message = $"Invalid user ID format: {request.UserId}"
                    });
                }

                var user = await _context.Users.FindAsync(userIdInt);
                if (user == null)
                {
                    _logger.LogWarning($"User not found: {request.UserId}");
                    return NotFound(new Response
                    {
                        IsSuccess = false,
                        Message = $"User not found: {request.UserId}"
                    });
                }

                // Create a new monthly bill
                var bill = new Bill
                {
                    UserId = userIdInt,
                    Description = $"Monthly Payment",
                    Amount = request.MonthlyPrice,
                    BillDate = DateTime.Now,
                    DueDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month)),
                    Status = "Pending",
                    IsPaid = false,
                    BillType = "Monthly"
                };

                _context.Bills.Add(bill);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Successfully set monthly price for user {request.UserId}");

                return Ok(new Response
                {
                    IsSuccess = true,
                    Message = $"Monthly price set successfully: ₱{request.MonthlyPrice:N2}"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting monthly price for user {UserId}", request.UserId);
                return StatusCode(500, new Response
                {
                    IsSuccess = false,
                    Message = $"An error occurred while setting the monthly price: {ex.Message}"
                });
            }
        }

        public class FutureDateAttribute : ValidationAttribute
        {
            public override bool IsValid(object value)
            {
                if (value is DateTime date)
                {
                    return date.Date > DateTime.Now.Date;
                }
                return false;
            }
        }

        public class MarkAsPaidRequest
        {
            [Required(ErrorMessage = "OR Number is required")]
            public string ORNumber { get; set; }
        }
    }
} 