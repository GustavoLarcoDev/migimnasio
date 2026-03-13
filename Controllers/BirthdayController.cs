using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Gimnasio.Data;
using Gimnasio.Models;
using System.Text.Json;

namespace Gimnasio.Controllers;

[Route("birthday")]
public class BirthdayController : Controller
{
    private readonly ApplicationDbContext _context;
    private const string AdminPassword = "gus0604";

    private static readonly string UploadsDir = Path.Combine(
        Directory.GetCurrentDirectory(), "wwwroot", "birthday-uploads");

    public BirthdayController(ApplicationDbContext context)
    {
        _context = context;
        if (!Directory.Exists(UploadsDir)) Directory.CreateDirectory(UploadsDir);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // PAGES
    // ═══════════════════════════════════════════════════════════════════════════

    [HttpGet("")]
    public IActionResult Index() => View();

    [HttpGet("admin")]
    public IActionResult Admin() => View();

    // ═══════════════════════════════════════════════════════════════════════════
    // PUBLIC API
    // ═══════════════════════════════════════════════════════════════════════════

    [HttpGet("api/foods")]
    public async Task<IActionResult> GetFoods()
    {
        var foods = await _context.BirthdayComidas
            .Where(c => c.IsActive)
            .Select(c => new { id = c.ComidaId, name = c.Nombre, type = c.EsGringo ? "gringo" : "latino", photo = c.FotoUrl })
            .ToListAsync();
        return Json(foods);
    }

    [HttpGet("api/gifts")]
    public async Task<IActionResult> GetGifts()
    {
        var gifts = await _context.BirthdayRegalos
            .Where(r => r.IsActive)
            .Select(r => new { id = r.RegaloId, name = r.Nombre, photo = r.FotoUrl, link = r.Link, claimed = r.Claimed })
            .ToListAsync();
        return Json(gifts);
    }

    [HttpPost("api/gifts/claim")]
    public async Task<IActionResult> ClaimGift([FromBody] ClaimRequest req)
    {
        if (req?.GiftId == null || !Guid.TryParse(req.GiftId, out var giftId))
            return BadRequest(new { error = "giftId is required" });

        var gift = await _context.BirthdayRegalos.FirstOrDefaultAsync(r => r.RegaloId == giftId && r.IsActive);
        if (gift == null) return NotFound(new { error = "Gift not found" });
        if (gift.Claimed) return Conflict(new { error = "Gift already claimed" });

        gift.Claimed = true;
        await _context.SaveChangesAsync();
        return Json(new { success = true });
    }

    [HttpPost("api/rsvp")]
    public async Task<IActionResult> SaveRsvp([FromBody] RsvpRequest req)
    {
        if (string.IsNullOrWhiteSpace(req?.Name))
            return BadRequest(new { error = "Name is required" });

        var rsvp = new BirthdayRsvp
        {
            RsvpId = Guid.NewGuid(),
            Nombre = req.Name.Trim(),
            PlusOne = req.PlusOne,
            PlusOneNombre = req.PlusOneName?.Trim(),
            ComidasJson = req.Foods != null ? JsonSerializer.Serialize(req.Foods) : null,
            Extra = req.Extra?.Trim(),
            FechaCreacion = DateTime.UtcNow
        };

        _context.BirthdayRsvps.Add(rsvp);
        await _context.SaveChangesAsync();

        return StatusCode(201, new
        {
            id = rsvp.RsvpId,
            name = rsvp.Nombre,
            plusOne = rsvp.PlusOne,
            plusOneName = rsvp.PlusOneNombre,
            foods = req.Foods ?? new List<string>(),
            extra = rsvp.Extra,
            createdAt = rsvp.FechaCreacion
        });
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ADMIN API
    // ═══════════════════════════════════════════════════════════════════════════

    [HttpGet("api/admin/rsvps")]
    public async Task<IActionResult> GetAdminRsvps()
    {
        if (!IsAdmin()) return Unauthorized(new { error = "Unauthorized" });
        var rsvps = await _context.BirthdayRsvps
            .OrderByDescending(r => r.FechaCreacion)
            .Select(r => new
            {
                id = r.RsvpId,
                name = r.Nombre,
                plusOne = r.PlusOne,
                plusOneName = r.PlusOneNombre,
                foods = r.ComidasJson,
                extra = r.Extra,
                createdAt = r.FechaCreacion
            })
            .ToListAsync();
        return Json(rsvps);
    }

    [HttpDelete("api/admin/rsvps/{id}")]
    public async Task<IActionResult> DeleteRsvp(Guid id)
    {
        if (!IsAdmin()) return Unauthorized(new { error = "Unauthorized" });
        var rsvp = await _context.BirthdayRsvps.FindAsync(id);
        if (rsvp == null) return NotFound(new { error = "RSVP not found" });

        _context.BirthdayRsvps.Remove(rsvp);
        await _context.SaveChangesAsync();
        return Json(new { success = true });
    }

    [HttpGet("api/admin/foods")]
    public async Task<IActionResult> GetAdminFoods()
    {
        if (!IsAdmin()) return Unauthorized(new { error = "Unauthorized" });
        var foods = await _context.BirthdayComidas
            .Where(c => c.IsActive)
            .Select(c => new { id = c.ComidaId, name = c.Nombre, type = c.EsGringo ? "gringo" : "latino", photo = c.FotoUrl })
            .ToListAsync();
        return Json(foods);
    }

    [HttpPost("api/admin/foods")]
    public async Task<IActionResult> AddFood([FromForm] string name, [FromForm] string? type, IFormFile? photo)
    {
        if (!IsAdmin()) return Unauthorized(new { error = "Unauthorized" });
        if (string.IsNullOrWhiteSpace(name))
            return BadRequest(new { error = "Name is required" });

        string? photoUrl = null;
        if (photo != null)
        {
            var filename = $"{Guid.NewGuid()}{Path.GetExtension(photo.FileName)}";
            var filePath = Path.Combine(UploadsDir, filename);
            using var stream = new FileStream(filePath, FileMode.Create);
            await photo.CopyToAsync(stream);
            photoUrl = $"/birthday-uploads/{filename}";
        }

        var food = new BirthdayComida
        {
            ComidaId = Guid.NewGuid(),
            Nombre = name.Trim(),
            EsGringo = type?.ToLower() == "gringo",
            FotoUrl = photoUrl
        };

        _context.BirthdayComidas.Add(food);
        await _context.SaveChangesAsync();
        return StatusCode(201, new { id = food.ComidaId, name = food.Nombre, type = food.EsGringo ? "gringo" : "latino", photo = food.FotoUrl });
    }

    [HttpDelete("api/admin/foods/{id}")]
    public async Task<IActionResult> DeleteFood(Guid id)
    {
        if (!IsAdmin()) return Unauthorized(new { error = "Unauthorized" });
        var food = await _context.BirthdayComidas.FindAsync(id);
        if (food == null) return NotFound(new { error = "Food not found" });

        food.IsActive = false;
        await _context.SaveChangesAsync();
        return Json(new { success = true });
    }

    [HttpGet("api/admin/gifts")]
    public async Task<IActionResult> GetAdminGifts()
    {
        if (!IsAdmin()) return Unauthorized(new { error = "Unauthorized" });
        var gifts = await _context.BirthdayRegalos
            .Where(r => r.IsActive)
            .Select(r => new { id = r.RegaloId, name = r.Nombre, photo = r.FotoUrl, link = r.Link, claimed = r.Claimed })
            .ToListAsync();
        return Json(gifts);
    }

    [HttpPost("api/admin/gifts")]
    public async Task<IActionResult> AddGift([FromForm] string name, [FromForm] string? link, IFormFile? photo)
    {
        if (!IsAdmin()) return Unauthorized(new { error = "Unauthorized" });
        if (string.IsNullOrWhiteSpace(name))
            return BadRequest(new { error = "Name is required" });

        string? photoUrl = null;
        if (photo != null)
        {
            var filename = $"{Guid.NewGuid()}{Path.GetExtension(photo.FileName)}";
            var filePath = Path.Combine(UploadsDir, filename);
            using var stream = new FileStream(filePath, FileMode.Create);
            await photo.CopyToAsync(stream);
            photoUrl = $"/birthday-uploads/{filename}";
        }

        var gift = new BirthdayRegalo
        {
            RegaloId = Guid.NewGuid(),
            Nombre = name.Trim(),
            Link = link?.Trim(),
            FotoUrl = photoUrl
        };

        _context.BirthdayRegalos.Add(gift);
        await _context.SaveChangesAsync();
        return StatusCode(201, new { id = gift.RegaloId, name = gift.Nombre, photo = gift.FotoUrl, link = gift.Link, claimed = gift.Claimed });
    }

    [HttpDelete("api/admin/gifts/{id}")]
    public async Task<IActionResult> DeleteGift(Guid id)
    {
        if (!IsAdmin()) return Unauthorized(new { error = "Unauthorized" });
        var gift = await _context.BirthdayRegalos.FindAsync(id);
        if (gift == null) return NotFound(new { error = "Gift not found" });

        gift.IsActive = false;
        await _context.SaveChangesAsync();
        return Json(new { success = true });
    }

    [HttpPut("api/admin/gifts/{id}/unclaim")]
    public async Task<IActionResult> UnclaimGift(Guid id)
    {
        if (!IsAdmin()) return Unauthorized(new { error = "Unauthorized" });
        var gift = await _context.BirthdayRegalos.FirstOrDefaultAsync(r => r.RegaloId == id && r.IsActive);
        if (gift == null) return NotFound(new { error = "Gift not found" });

        gift.Claimed = false;
        await _context.SaveChangesAsync();
        return Json(new { success = true });
    }

    [HttpGet("api/admin/stats")]
    public async Task<IActionResult> GetStats()
    {
        if (!IsAdmin()) return Unauthorized(new { error = "Unauthorized" });

        var rsvps = await _context.BirthdayRsvps.ToListAsync();
        var gifts = await _context.BirthdayRegalos.Where(r => r.IsActive).ToListAsync();

        var totalGuests = rsvps.Count;
        var totalPlusOnes = rsvps.Count(r => r.PlusOne);
        var totalAttending = totalGuests + totalPlusOnes;
        var giftsTotal = gifts.Count;
        var giftsClaimed = gifts.Count(g => g.Claimed);

        var foodCounts = new Dictionary<string, int>();
        foreach (var rsvp in rsvps)
        {
            if (!string.IsNullOrEmpty(rsvp.ComidasJson))
            {
                try
                {
                    var foods = JsonSerializer.Deserialize<List<string>>(rsvp.ComidasJson);
                    if (foods != null)
                    {
                        foreach (var food in foods)
                            foodCounts[food] = foodCounts.GetValueOrDefault(food) + 1;
                    }
                }
                catch { }
            }
        }

        return Json(new { totalGuests, totalPlusOnes, totalAttending, giftsTotal, giftsClaimed, foodCounts });
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // HELPERS
    // ═══════════════════════════════════════════════════════════════════════════

    private bool IsAdmin()
    {
        return Request.Headers["x-admin-password"].FirstOrDefault() == AdminPassword;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // DTOs
    // ═══════════════════════════════════════════════════════════════════════════

    public class ClaimRequest { public string? GiftId { get; set; } }

    public class RsvpRequest
    {
        public string? Name { get; set; }
        public bool PlusOne { get; set; }
        public string? PlusOneName { get; set; }
        public List<string>? Foods { get; set; }
        public string? Extra { get; set; }
    }
}
