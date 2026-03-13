using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Gimnasio.Controllers;

[Route("birthday")]
public class BirthdayController : Controller
{
    private static readonly string DataDir = Path.Combine(
        Directory.GetCurrentDirectory(), "birthday-data");

    private static readonly string UploadsDir = Path.Combine(
        Directory.GetCurrentDirectory(), "wwwroot", "birthday-uploads");

    private const string AdminPassword = "gus0604";

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public BirthdayController()
    {
        if (!Directory.Exists(DataDir)) Directory.CreateDirectory(DataDir);
        if (!Directory.Exists(UploadsDir)) Directory.CreateDirectory(UploadsDir);
        SeedFoods();
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
    public IActionResult GetFoods()
    {
        var foods = ReadJson<BirthdayFood>("foods.json");
        return Json(foods.Select(f => new { f.Id, f.Name, f.Type, f.Photo }));
    }

    [HttpGet("api/gifts")]
    public IActionResult GetGifts()
    {
        var gifts = ReadJson<BirthdayGift>("gifts.json");
        return Json(gifts.Select(g => new { g.Id, g.Name, g.Photo, g.Link, g.Claimed }));
    }

    [HttpPost("api/gifts/claim")]
    public IActionResult ClaimGift([FromBody] ClaimRequest req)
    {
        if (string.IsNullOrWhiteSpace(req?.GiftId))
            return BadRequest(new { error = "giftId is required" });

        var gifts = ReadJson<BirthdayGift>("gifts.json");
        var gift = gifts.FirstOrDefault(g => g.Id == req.GiftId);
        if (gift == null) return NotFound(new { error = "Gift not found" });
        if (gift.Claimed) return Conflict(new { error = "Gift already claimed" });

        gift.Claimed = true;
        WriteJson("gifts.json", gifts);
        return Json(new { success = true });
    }

    [HttpPost("api/rsvp")]
    public IActionResult SaveRsvp([FromBody] RsvpRequest req)
    {
        if (string.IsNullOrWhiteSpace(req?.Name))
            return BadRequest(new { error = "Name is required" });

        var rsvps = ReadJson<BirthdayRsvp>("rsvps.json");
        var rsvp = new BirthdayRsvp
        {
            Id = Guid.NewGuid().ToString(),
            Name = req.Name.Trim(),
            PlusOne = req.PlusOne,
            PlusOneName = req.PlusOneName?.Trim(),
            Foods = req.Foods ?? new List<string>(),
            Extra = req.Extra?.Trim(),
            CreatedAt = DateTime.UtcNow.ToString("o")
        };

        rsvps.Add(rsvp);
        WriteJson("rsvps.json", rsvps);
        return StatusCode(201, rsvp);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ADMIN API
    // ═══════════════════════════════════════════════════════════════════════════

    [HttpGet("api/admin/rsvps")]
    public IActionResult GetAdminRsvps()
    {
        if (!IsAdmin()) return Unauthorized(new { error = "Unauthorized" });
        return Json(ReadJson<BirthdayRsvp>("rsvps.json"));
    }

    [HttpDelete("api/admin/rsvps/{id}")]
    public IActionResult DeleteRsvp(string id)
    {
        if (!IsAdmin()) return Unauthorized(new { error = "Unauthorized" });
        var rsvps = ReadJson<BirthdayRsvp>("rsvps.json");
        var index = rsvps.FindIndex(r => r.Id == id);
        if (index == -1) return NotFound(new { error = "RSVP not found" });

        rsvps.RemoveAt(index);
        WriteJson("rsvps.json", rsvps);
        return Json(new { success = true });
    }

    [HttpGet("api/admin/foods")]
    public IActionResult GetAdminFoods()
    {
        if (!IsAdmin()) return Unauthorized(new { error = "Unauthorized" });
        return Json(ReadJson<BirthdayFood>("foods.json"));
    }

    [HttpPost("api/admin/foods")]
    public async Task<IActionResult> AddFood([FromForm] string name, [FromForm] string? type, IFormFile? photo)
    {
        if (!IsAdmin()) return Unauthorized(new { error = "Unauthorized" });
        if (string.IsNullOrWhiteSpace(name))
            return BadRequest(new { error = "Name is required" });

        string? photoFilename = null;
        if (photo != null)
        {
            photoFilename = $"{Guid.NewGuid()}{Path.GetExtension(photo.FileName)}";
            var filePath = Path.Combine(UploadsDir, photoFilename);
            using var stream = new FileStream(filePath, FileMode.Create);
            await photo.CopyToAsync(stream);
        }

        var foods = ReadJson<BirthdayFood>("foods.json");
        var food = new BirthdayFood
        {
            Id = Guid.NewGuid().ToString(),
            Name = name.Trim(),
            Type = type?.Trim(),
            Photo = photoFilename
        };

        foods.Add(food);
        WriteJson("foods.json", foods);
        return StatusCode(201, food);
    }

    [HttpDelete("api/admin/foods/{id}")]
    public IActionResult DeleteFood(string id)
    {
        if (!IsAdmin()) return Unauthorized(new { error = "Unauthorized" });
        var foods = ReadJson<BirthdayFood>("foods.json");
        var index = foods.FindIndex(f => f.Id == id);
        if (index == -1) return NotFound(new { error = "Food not found" });

        var food = foods[index];
        DeleteUploadedFile(food.Photo);
        foods.RemoveAt(index);
        WriteJson("foods.json", foods);
        return Json(new { success = true });
    }

    [HttpGet("api/admin/gifts")]
    public IActionResult GetAdminGifts()
    {
        if (!IsAdmin()) return Unauthorized(new { error = "Unauthorized" });
        var gifts = ReadJson<BirthdayGift>("gifts.json");
        return Json(gifts.Select(g => new { g.Id, g.Name, g.Photo, g.Link, g.Claimed }));
    }

    [HttpPost("api/admin/gifts")]
    public async Task<IActionResult> AddGift([FromForm] string name, [FromForm] string? link, IFormFile? photo)
    {
        if (!IsAdmin()) return Unauthorized(new { error = "Unauthorized" });
        if (string.IsNullOrWhiteSpace(name))
            return BadRequest(new { error = "Name is required" });

        string? photoFilename = null;
        if (photo != null)
        {
            photoFilename = $"{Guid.NewGuid()}{Path.GetExtension(photo.FileName)}";
            var filePath = Path.Combine(UploadsDir, photoFilename);
            using var stream = new FileStream(filePath, FileMode.Create);
            await photo.CopyToAsync(stream);
        }

        var gifts = ReadJson<BirthdayGift>("gifts.json");
        var gift = new BirthdayGift
        {
            Id = Guid.NewGuid().ToString(),
            Name = name.Trim(),
            Link = link?.Trim(),
            Photo = photoFilename,
            Claimed = false
        };

        gifts.Add(gift);
        WriteJson("gifts.json", gifts);
        return StatusCode(201, gift);
    }

    [HttpDelete("api/admin/gifts/{id}")]
    public IActionResult DeleteGift(string id)
    {
        if (!IsAdmin()) return Unauthorized(new { error = "Unauthorized" });
        var gifts = ReadJson<BirthdayGift>("gifts.json");
        var index = gifts.FindIndex(g => g.Id == id);
        if (index == -1) return NotFound(new { error = "Gift not found" });

        var gift = gifts[index];
        DeleteUploadedFile(gift.Photo);
        gifts.RemoveAt(index);
        WriteJson("gifts.json", gifts);
        return Json(new { success = true });
    }

    [HttpPut("api/admin/gifts/{id}/unclaim")]
    public IActionResult UnclaimGift(string id)
    {
        if (!IsAdmin()) return Unauthorized(new { error = "Unauthorized" });
        var gifts = ReadJson<BirthdayGift>("gifts.json");
        var gift = gifts.FirstOrDefault(g => g.Id == id);
        if (gift == null) return NotFound(new { error = "Gift not found" });

        gift.Claimed = false;
        WriteJson("gifts.json", gifts);
        return Json(new { success = true });
    }

    [HttpGet("api/admin/stats")]
    public IActionResult GetStats()
    {
        if (!IsAdmin()) return Unauthorized(new { error = "Unauthorized" });

        var rsvps = ReadJson<BirthdayRsvp>("rsvps.json");
        var gifts = ReadJson<BirthdayGift>("gifts.json");

        var totalGuests = rsvps.Count;
        var totalPlusOnes = rsvps.Count(r => r.PlusOne);
        var totalAttending = totalGuests + totalPlusOnes;
        var giftsTotal = gifts.Count;
        var giftsClaimed = gifts.Count(g => g.Claimed);

        var foodCounts = new Dictionary<string, int>();
        foreach (var rsvp in rsvps)
        {
            if (rsvp.Foods != null)
            {
                foreach (var food in rsvp.Foods)
                {
                    foodCounts[food] = foodCounts.GetValueOrDefault(food) + 1;
                }
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

    private static List<T> ReadJson<T>(string filename)
    {
        var filePath = Path.Combine(DataDir, filename);
        try
        {
            if (!System.IO.File.Exists(filePath)) return new List<T>();
            var raw = System.IO.File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<List<T>>(raw, JsonOpts) ?? new List<T>();
        }
        catch
        {
            return new List<T>();
        }
    }

    private static void WriteJson<T>(string filename, T data)
    {
        var filePath = Path.Combine(DataDir, filename);
        System.IO.File.WriteAllText(filePath, JsonSerializer.Serialize(data, JsonOpts));
    }

    private static void DeleteUploadedFile(string? filename)
    {
        if (string.IsNullOrEmpty(filename)) return;
        var filePath = Path.Combine(UploadsDir, filename);
        try { if (System.IO.File.Exists(filePath)) System.IO.File.Delete(filePath); } catch { }
    }

    private static void SeedFoods()
    {
        var foods = ReadJson<BirthdayFood>("foods.json");
        if (foods.Count > 0) return;

        var seed = new List<BirthdayFood>
        {
            new() { Id = Guid.NewGuid().ToString(), Name = "Carne Asada Fajitas", Type = "latino" },
            new() { Id = Guid.NewGuid().ToString(), Name = "Pollo Asado Fajitas", Type = "latino" },
            new() { Id = Guid.NewGuid().ToString(), Name = "Chorizo Fajitas", Type = "latino" },
            new() { Id = Guid.NewGuid().ToString(), Name = "Hot Dogs", Type = "gringo" },
            new() { Id = Guid.NewGuid().ToString(), Name = "Burgers", Type = "gringo" },
            new() { Id = Guid.NewGuid().ToString(), Name = "Patacones", Type = "latino" }
        };

        WriteJson("foods.json", seed);
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

    public class BirthdayFood
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string? Type { get; set; }
        public string? Photo { get; set; }
    }

    public class BirthdayGift
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string? Link { get; set; }
        public string? Photo { get; set; }
        public bool Claimed { get; set; }
    }

    public class BirthdayRsvp
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public bool PlusOne { get; set; }
        public string? PlusOneName { get; set; }
        public List<string>? Foods { get; set; }
        public string? Extra { get; set; }
        public string CreatedAt { get; set; } = "";
    }
}
