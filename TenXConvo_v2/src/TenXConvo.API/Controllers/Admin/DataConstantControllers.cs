using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TenXConvo.Domain.Entities;
using TenXConvo.Infrastructure.Data;

namespace TenXConvo.API.Controllers.Admin;

// ═══════════════════════════════════════════════════════════════════════════
//  DATA CONSTANT CONTROLLERS — /api/admin/data/**
//  All require Admin Role + AdminPortal CORS
// ═══════════════════════════════════════════════════════════════════════════

// ── CONTROL TYPES ─────────────────────────────────────────────────────────

[ApiController][Route("api/admin/data/control-types")][Authorize(Policy="AdminOnly")][EnableCors("AdminPortal")]
public class ControlTypeController : ControllerBase
{
    private readonly AppDbContext _db;
    public ControlTypeController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? search)
    {
        var q = _db.ControlTypes.Include(x => x.Categories).AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(x => x.ControlTypeName.Contains(search));
        var data = await q.OrderBy(x => x.ControlTypeName).ToListAsync();
        return Ok(new { success = true, data });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var item = await _db.ControlTypes.Include(x => x.Categories).FirstOrDefaultAsync(x => x.Id == id);
        if (item == null) return NotFound(new { success = false, message = "Not found." });
        return Ok(new { success = true, data = item });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ControlTypeRequest req)
    {
        var item = new ControlType { ControlTypeName = req.Name, ControlTypePrefix = req.Prefix };
        _db.ControlTypes.Add(item);
        await _db.SaveChangesAsync();
        return Ok(new { success = true, data = item });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] ControlTypeRequest req)
    {
        var item = await _db.ControlTypes.FindAsync(id);
        if (item == null) return NotFound(new { success = false, message = "Not found." });
        item.ControlTypeName   = req.Name;
        item.ControlTypePrefix = req.Prefix;
        await _db.SaveChangesAsync();
        return Ok(new { success = true, data = item });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var item = await _db.ControlTypes.FindAsync(id);
        if (item == null) return NotFound(new { success = false, message = "Not found." });
        _db.ControlTypes.Remove(item);
        await _db.SaveChangesAsync();
        return Ok(new { success = true, message = "Deleted." });
    }
}
public record ControlTypeRequest(string Name, string Prefix);

// ── CONTROL CATEGORIES ────────────────────────────────────────────────────

[ApiController][Route("api/admin/data/control-categories")][Authorize(Policy="AdminOnly")][EnableCors("AdminPortal")]
public class ControlCategoryController : ControllerBase
{
    private readonly AppDbContext _db;
    public ControlCategoryController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] Guid? controlTypeId, [FromQuery] string? search)
    {
        var q = _db.ControlCategories.Include(x => x.ControlType).AsQueryable();
        if (controlTypeId.HasValue) q = q.Where(x => x.ControlTypeId == controlTypeId);
        if (!string.IsNullOrWhiteSpace(search)) q = q.Where(x => x.ControlCategoryName.Contains(search));
        return Ok(new { success = true, data = await q.OrderBy(x => x.ControlCategoryName).ToListAsync() });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ControlCategoryRequest req)
    {
        var item = new ControlCategory { ControlTypeId = req.ControlTypeId, ControlCategoryName = req.Name, ControlPrefix = req.Prefix };
        _db.ControlCategories.Add(item);
        await _db.SaveChangesAsync();
        return Ok(new { success = true, data = item });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] ControlCategoryRequest req)
    {
        var item = await _db.ControlCategories.FindAsync(id);
        if (item == null) return NotFound(new { success = false, message = "Not found." });
        item.ControlTypeId       = req.ControlTypeId;
        item.ControlCategoryName = req.Name;
        item.ControlPrefix       = req.Prefix;
        await _db.SaveChangesAsync();
        return Ok(new { success = true, data = item });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var item = await _db.ControlCategories.FindAsync(id);
        if (item == null) return NotFound(new { success = false, message = "Not found." });
        _db.ControlCategories.Remove(item);
        await _db.SaveChangesAsync();
        return Ok(new { success = true, message = "Deleted." });
    }
}
public record ControlCategoryRequest(Guid ControlTypeId, string Name, string Prefix);

// ── CLIENT AREAS ──────────────────────────────────────────────────────────

[ApiController][Route("api/admin/data/client-areas")][Authorize(Policy="AdminOnly")][EnableCors("AdminPortal")]
public class ClientAreaController : ControllerBase
{
    private readonly AppDbContext _db;
    public ClientAreaController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? search)
    {
        var q = _db.ClientAreas.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search)) q = q.Where(x => x.ControlAreaName.Contains(search));
        return Ok(new { success = true, data = await q.OrderBy(x => x.ControlAreaName).ToListAsync() });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ClientAreaRequest req)
    {
        var item = new ClientArea { ControlAreaName = req.Name, ControlAreaPrefix = req.Prefix };
        _db.ClientAreas.Add(item);
        await _db.SaveChangesAsync();
        return Ok(new { success = true, data = item });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] ClientAreaRequest req)
    {
        var item = await _db.ClientAreas.FindAsync(id);
        if (item == null) return NotFound(new { success = false, message = "Not found." });
        item.ControlAreaName   = req.Name;
        item.ControlAreaPrefix = req.Prefix;
        await _db.SaveChangesAsync();
        return Ok(new { success = true, data = item });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var item = await _db.ClientAreas.FindAsync(id);
        if (item == null) return NotFound(new { success = false, message = "Not found." });
        _db.ClientAreas.Remove(item);
        await _db.SaveChangesAsync();
        return Ok(new { success = true, message = "Deleted." });
    }
}
public record ClientAreaRequest(string Name, string Prefix);

// ── CLIENT CATEGORIES ─────────────────────────────────────────────────────

[ApiController][Route("api/admin/data/client-categories")][Authorize(Policy="AdminOnly")][EnableCors("AdminPortal")]
public class ClientCategoryController : ControllerBase
{
    private readonly AppDbContext _db;
    public ClientCategoryController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? search)
    {
        var q = _db.ClientCategories.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search)) q = q.Where(x => x.ControlCategoryName.Contains(search));
        return Ok(new { success = true, data = await q.OrderBy(x => x.ControlCategoryName).ToListAsync() });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ClientCategoryRequest req)
    {
        var item = new ClientCategory { ControlCategoryName = req.Name, ControlCategoryPrefix = req.Prefix };
        _db.ClientCategories.Add(item);
        await _db.SaveChangesAsync();
        return Ok(new { success = true, data = item });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] ClientCategoryRequest req)
    {
        var item = await _db.ClientCategories.FindAsync(id);
        if (item == null) return NotFound(new { success = false, message = "Not found." });
        item.ControlCategoryName   = req.Name;
        item.ControlCategoryPrefix = req.Prefix;
        await _db.SaveChangesAsync();
        return Ok(new { success = true, data = item });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var item = await _db.ClientCategories.FindAsync(id);
        if (item == null) return NotFound(new { success = false, message = "Not found." });
        _db.ClientCategories.Remove(item);
        await _db.SaveChangesAsync();
        return Ok(new { success = true, message = "Deleted." });
    }
}
public record ClientCategoryRequest(string Name, string Prefix);

// ── CRITERIA TYPES + SUBTYPES ─────────────────────────────────────────────

[ApiController][Route("api/admin/data/criteria-types")][Authorize(Policy="AdminOnly")][EnableCors("AdminPortal")]
public class CriteriaTypeController : ControllerBase
{
    private readonly AppDbContext _db;
    public CriteriaTypeController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? search)
    {
        var q = _db.CriteriaTypes.Include(x => x.SubTypes).AsQueryable();
        if (!string.IsNullOrWhiteSpace(search)) q = q.Where(x => x.CriteriaTypeName.Contains(search));
        return Ok(new { success = true, data = await q.OrderBy(x => x.CriteriaTypeName).ToListAsync() });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CriteriaTypeRequest req)
    {
        var item = new CriteriaType { CriteriaTypeName = req.Name, CriteriaTypePrefix = req.Prefix };
        _db.CriteriaTypes.Add(item);
        await _db.SaveChangesAsync();
        return Ok(new { success = true, data = item });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] CriteriaTypeRequest req)
    {
        var item = await _db.CriteriaTypes.FindAsync(id);
        if (item == null) return NotFound(new { success = false, message = "Not found." });
        item.CriteriaTypeName   = req.Name;
        item.CriteriaTypePrefix = req.Prefix;
        await _db.SaveChangesAsync();
        return Ok(new { success = true, data = item });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var item = await _db.CriteriaTypes.FindAsync(id);
        if (item == null) return NotFound(new { success = false, message = "Not found." });
        _db.CriteriaTypes.Remove(item);
        await _db.SaveChangesAsync();
        return Ok(new { success = true, message = "Deleted." });
    }
}
public record CriteriaTypeRequest(string Name, string Prefix);

[ApiController][Route("api/admin/data/criteria-subtypes")][Authorize(Policy="AdminOnly")][EnableCors("AdminPortal")]
public class CriteriaSubTypeController : ControllerBase
{
    private readonly AppDbContext _db;
    public CriteriaSubTypeController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] Guid? criteriaTypeId, [FromQuery] string? search)
    {
        var q = _db.CriteriaSubTypes.Include(x => x.CriteriaType).AsQueryable();
        if (criteriaTypeId.HasValue) q = q.Where(x => x.CriteriaTypeId == criteriaTypeId);
        if (!string.IsNullOrWhiteSpace(search)) q = q.Where(x => x.SubCriteriaName.Contains(search));
        return Ok(new { success = true, data = await q.OrderBy(x => x.SubCriteriaName).ToListAsync() });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CriteriaSubTypeRequest req)
    {
        var item = new CriteriaSubType { CriteriaTypeId = req.CriteriaTypeId, SubCriteriaName = req.Name, Prefix = req.Prefix };
        _db.CriteriaSubTypes.Add(item);
        await _db.SaveChangesAsync();
        return Ok(new { success = true, data = item });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] CriteriaSubTypeRequest req)
    {
        var item = await _db.CriteriaSubTypes.FindAsync(id);
        if (item == null) return NotFound(new { success = false, message = "Not found." });
        item.CriteriaTypeId = req.CriteriaTypeId;
        item.SubCriteriaName = req.Name;
        item.Prefix          = req.Prefix;
        await _db.SaveChangesAsync();
        return Ok(new { success = true, data = item });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var item = await _db.CriteriaSubTypes.FindAsync(id);
        if (item == null) return NotFound(new { success = false, message = "Not found." });
        _db.CriteriaSubTypes.Remove(item);
        await _db.SaveChangesAsync();
        return Ok(new { success = true, message = "Deleted." });
    }
}
public record CriteriaSubTypeRequest(Guid CriteriaTypeId, string Name, string Prefix);

// ── CURRENCIES ────────────────────────────────────────────────────────────

[ApiController][Route("api/admin/data/currencies")][Authorize(Policy="AdminOnly")][EnableCors("AdminPortal")]
public class CurrencyController : ControllerBase
{
    private readonly AppDbContext _db;
    public CurrencyController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? search)
    {
        var q = _db.Currencies.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(x => x.CountryName.Contains(search) || x.CurrencyName.Contains(search));
        return Ok(new { success = true, data = await q.OrderBy(x => x.CountryName).ToListAsync() });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CurrencyRequest req)
    {
        var item = new Currency { CountryName = req.CountryName, CurrencyName = req.CurrencyName, Symbol = req.Symbol };
        _db.Currencies.Add(item);
        await _db.SaveChangesAsync();
        return Ok(new { success = true, data = item });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] CurrencyRequest req)
    {
        var item = await _db.Currencies.FindAsync(id);
        if (item == null) return NotFound(new { success = false, message = "Not found." });
        item.CountryName  = req.CountryName;
        item.CurrencyName = req.CurrencyName;
        item.Symbol       = req.Symbol;
        await _db.SaveChangesAsync();
        return Ok(new { success = true, data = item });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var item = await _db.Currencies.FindAsync(id);
        if (item == null) return NotFound(new { success = false, message = "Not found." });
        _db.Currencies.Remove(item);
        await _db.SaveChangesAsync();
        return Ok(new { success = true, message = "Deleted." });
    }
}
public record CurrencyRequest(string CountryName, string CurrencyName, string Symbol);

// ── GEOGRAPHY: COUNTRY ────────────────────────────────────────────────────

[ApiController][Route("api/admin/data/countries")][Authorize(Policy="AdminOnly")][EnableCors("AdminPortal")]
public class CountryController : ControllerBase
{
    private readonly AppDbContext _db;
    public CountryController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? search)
    {
        var q = _db.Countries.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search)) q = q.Where(x => x.CountryName.Contains(search));
        return Ok(new { success = true, data = await q.OrderBy(x => x.CountryName).ToListAsync() });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CountryRequest req)
    {
        var item = new Country { CountryName = req.Name, Code = req.Code, Prefix = req.Prefix };
        _db.Countries.Add(item);
        await _db.SaveChangesAsync();
        return Ok(new { success = true, data = item });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] CountryRequest req)
    {
        var item = await _db.Countries.FindAsync(id);
        if (item == null) return NotFound(new { success = false, message = "Not found." });
        item.CountryName = req.Name; item.Code = req.Code; item.Prefix = req.Prefix;
        await _db.SaveChangesAsync();
        return Ok(new { success = true, data = item });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var item = await _db.Countries.FindAsync(id);
        if (item == null) return NotFound(new { success = false, message = "Not found." });
        _db.Countries.Remove(item);
        await _db.SaveChangesAsync();
        return Ok(new { success = true, message = "Deleted." });
    }
}
public record CountryRequest(string Name, string Code, string Prefix);

// ── GEOGRAPHY: PROVINCE ───────────────────────────────────────────────────

[ApiController][Route("api/admin/data/provinces")][Authorize(Policy="AdminOnly")][EnableCors("AdminPortal")]
public class ProvinceController : ControllerBase
{
    private readonly AppDbContext _db;
    public ProvinceController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] Guid? countryId, [FromQuery] string? search)
    {
        var q = _db.Provinces.Include(x => x.Country).AsQueryable();
        if (countryId.HasValue) q = q.Where(x => x.CountryId == countryId);
        if (!string.IsNullOrWhiteSpace(search)) q = q.Where(x => x.ProvinceName.Contains(search));
        return Ok(new { success = true, data = await q.OrderBy(x => x.ProvinceName).ToListAsync() });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ProvinceRequest req)
    {
        var item = new Province { CountryId = req.CountryId, ProvinceName = req.Name, Prefix = req.Prefix };
        _db.Provinces.Add(item);
        await _db.SaveChangesAsync();
        return Ok(new { success = true, data = item });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] ProvinceRequest req)
    {
        var item = await _db.Provinces.FindAsync(id);
        if (item == null) return NotFound(new { success = false, message = "Not found." });
        item.CountryId = req.CountryId; item.ProvinceName = req.Name; item.Prefix = req.Prefix;
        await _db.SaveChangesAsync();
        return Ok(new { success = true, data = item });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var item = await _db.Provinces.FindAsync(id);
        if (item == null) return NotFound(new { success = false, message = "Not found." });
        _db.Provinces.Remove(item);
        await _db.SaveChangesAsync();
        return Ok(new { success = true, message = "Deleted." });
    }
}
public record ProvinceRequest(Guid CountryId, string Name, string Prefix);

// ── GEOGRAPHY: CITY ───────────────────────────────────────────────────────

[ApiController][Route("api/admin/data/cities")][Authorize(Policy="AdminOnly")][EnableCors("AdminPortal")]
public class CityController : ControllerBase
{
    private readonly AppDbContext _db;
    public CityController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] Guid? provinceId, [FromQuery] string? search)
    {
        var q = _db.Cities.Include(x => x.Province).AsQueryable();
        if (provinceId.HasValue) q = q.Where(x => x.ProvinceId == provinceId);
        if (!string.IsNullOrWhiteSpace(search)) q = q.Where(x => x.CityName.Contains(search));
        return Ok(new { success = true, data = await q.OrderBy(x => x.CityName).ToListAsync() });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CityRequest req)
    {
        var item = new City { ProvinceId = req.ProvinceId, CityName = req.Name };
        _db.Cities.Add(item);
        await _db.SaveChangesAsync();
        return Ok(new { success = true, data = item });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] CityRequest req)
    {
        var item = await _db.Cities.FindAsync(id);
        if (item == null) return NotFound(new { success = false, message = "Not found." });
        item.ProvinceId = req.ProvinceId; item.CityName = req.Name;
        await _db.SaveChangesAsync();
        return Ok(new { success = true, data = item });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var item = await _db.Cities.FindAsync(id);
        if (item == null) return NotFound(new { success = false, message = "Not found." });
        _db.Cities.Remove(item);
        await _db.SaveChangesAsync();
        return Ok(new { success = true, message = "Deleted." });
    }
}
public record CityRequest(Guid ProvinceId, string Name);

// ── GEOGRAPHY: DISTRICT ───────────────────────────────────────────────────

[ApiController][Route("api/admin/data/districts")][Authorize(Policy="AdminOnly")][EnableCors("AdminPortal")]
public class DistrictController : ControllerBase
{
    private readonly AppDbContext _db;
    public DistrictController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] Guid? cityId, [FromQuery] string? search)
    {
        var q = _db.Districts.Include(x => x.City).AsQueryable();
        if (cityId.HasValue) q = q.Where(x => x.CityId == cityId);
        if (!string.IsNullOrWhiteSpace(search)) q = q.Where(x => x.DistrictName.Contains(search));
        return Ok(new { success = true, data = await q.OrderBy(x => x.DistrictName).ToListAsync() });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] DistrictRequest req)
    {
        var item = new District { CityId = req.CityId, DistrictName = req.Name };
        _db.Districts.Add(item);
        await _db.SaveChangesAsync();
        return Ok(new { success = true, data = item });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] DistrictRequest req)
    {
        var item = await _db.Districts.FindAsync(id);
        if (item == null) return NotFound(new { success = false, message = "Not found." });
        item.CityId = req.CityId; item.DistrictName = req.Name;
        await _db.SaveChangesAsync();
        return Ok(new { success = true, data = item });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var item = await _db.Districts.FindAsync(id);
        if (item == null) return NotFound(new { success = false, message = "Not found." });
        _db.Districts.Remove(item);
        await _db.SaveChangesAsync();
        return Ok(new { success = true, message = "Deleted." });
    }
}
public record DistrictRequest(Guid CityId, string Name);

// ── GEOGRAPHY: TEHSIL ─────────────────────────────────────────────────────

[ApiController][Route("api/admin/data/tehsils")][Authorize(Policy="AdminOnly")][EnableCors("AdminPortal")]
public class TehsilController : ControllerBase
{
    private readonly AppDbContext _db;
    public TehsilController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] Guid? districtId, [FromQuery] string? search)
    {
        var q = _db.Tehsils.Include(x => x.District).AsQueryable();
        if (districtId.HasValue) q = q.Where(x => x.DistrictId == districtId);
        if (!string.IsNullOrWhiteSpace(search)) q = q.Where(x => x.TehsilName.Contains(search));
        return Ok(new { success = true, data = await q.OrderBy(x => x.TehsilName).ToListAsync() });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] TehsilRequest req)
    {
        var item = new Tehsil { DistrictId = req.DistrictId, TehsilName = req.Name };
        _db.Tehsils.Add(item);
        await _db.SaveChangesAsync();
        return Ok(new { success = true, data = item });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] TehsilRequest req)
    {
        var item = await _db.Tehsils.FindAsync(id);
        if (item == null) return NotFound(new { success = false, message = "Not found." });
        item.DistrictId = req.DistrictId; item.TehsilName = req.Name;
        await _db.SaveChangesAsync();
        return Ok(new { success = true, data = item });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var item = await _db.Tehsils.FindAsync(id);
        if (item == null) return NotFound(new { success = false, message = "Not found." });
        _db.Tehsils.Remove(item);
        await _db.SaveChangesAsync();
        return Ok(new { success = true, message = "Deleted." });
    }
}
public record TehsilRequest(Guid DistrictId, string Name);

// ── GEOGRAPHY: AREA ───────────────────────────────────────────────────────

[ApiController][Route("api/admin/data/areas")][Authorize(Policy="AdminOnly")][EnableCors("AdminPortal")]
public class AreaController : ControllerBase
{
    private readonly AppDbContext _db;
    public AreaController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] Guid? cityId, [FromQuery] string? search)
    {
        var q = _db.Areas.Include(x => x.City).AsQueryable();
        if (cityId.HasValue) q = q.Where(x => x.CityId == cityId);
        if (!string.IsNullOrWhiteSpace(search)) q = q.Where(x => x.AreaName.Contains(search));
        return Ok(new { success = true, data = await q.OrderBy(x => x.AreaName).ToListAsync() });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] AreaRequest req)
    {
        var item = new Area { CityId = req.CityId, AreaName = req.Name };
        _db.Areas.Add(item);
        await _db.SaveChangesAsync();
        return Ok(new { success = true, data = item });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] AreaRequest req)
    {
        var item = await _db.Areas.FindAsync(id);
        if (item == null) return NotFound(new { success = false, message = "Not found." });
        item.CityId = req.CityId; item.AreaName = req.Name;
        await _db.SaveChangesAsync();
        return Ok(new { success = true, data = item });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var item = await _db.Areas.FindAsync(id);
        if (item == null) return NotFound(new { success = false, message = "Not found." });
        _db.Areas.Remove(item);
        await _db.SaveChangesAsync();
        return Ok(new { success = true, message = "Deleted." });
    }
}
public record AreaRequest(Guid CityId, string Name);

// ── DOCUMENT TYPES ────────────────────────────────────────────────────────

[ApiController][Route("api/admin/data/document-types")][Authorize(Policy="AdminOnly")][EnableCors("AdminPortal")]
public class DocumentTypeController : ControllerBase
{
    private readonly AppDbContext _db;
    public DocumentTypeController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? search)
    {
        var q = _db.DocumentTypes.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search)) q = q.Where(x => x.DocumentTypeName.Contains(search));
        return Ok(new { success = true, data = await q.OrderBy(x => x.DocumentTypeName).ToListAsync() });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] DocumentTypeRequest req)
    {
        var item = new DocumentType { DocumentTypeName = req.Name, ShortName = req.ShortName };
        _db.DocumentTypes.Add(item);
        await _db.SaveChangesAsync();
        return Ok(new { success = true, data = item });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] DocumentTypeRequest req)
    {
        var item = await _db.DocumentTypes.FindAsync(id);
        if (item == null) return NotFound(new { success = false, message = "Not found." });
        item.DocumentTypeName = req.Name; item.ShortName = req.ShortName;
        await _db.SaveChangesAsync();
        return Ok(new { success = true, data = item });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var item = await _db.DocumentTypes.FindAsync(id);
        if (item == null) return NotFound(new { success = false, message = "Not found." });
        _db.DocumentTypes.Remove(item);
        await _db.SaveChangesAsync();
        return Ok(new { success = true, message = "Deleted." });
    }
}
public record DocumentTypeRequest(string Name, string ShortName);

// ── LOCATION TYPES ────────────────────────────────────────────────────────

[ApiController][Route("api/admin/data/location-types")][Authorize(Policy="AdminOnly")][EnableCors("AdminPortal")]
public class LocationTypeController : ControllerBase
{
    private readonly AppDbContext _db;
    public LocationTypeController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? search)
    {
        var q = _db.LocationTypes.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search)) q = q.Where(x => x.LocationTypeName.Contains(search));
        return Ok(new { success = true, data = await q.OrderBy(x => x.LocationTypeName).ToListAsync() });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] LocationTypeRequest req)
    {
        var item = new LocationType { LocationTypeName = req.Name, ShortName = req.ShortName };
        _db.LocationTypes.Add(item);
        await _db.SaveChangesAsync();
        return Ok(new { success = true, data = item });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] LocationTypeRequest req)
    {
        var item = await _db.LocationTypes.FindAsync(id);
        if (item == null) return NotFound(new { success = false, message = "Not found." });
        item.LocationTypeName = req.Name; item.ShortName = req.ShortName;
        await _db.SaveChangesAsync();
        return Ok(new { success = true, data = item });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var item = await _db.LocationTypes.FindAsync(id);
        if (item == null) return NotFound(new { success = false, message = "Not found." });
        _db.LocationTypes.Remove(item);
        await _db.SaveChangesAsync();
        return Ok(new { success = true, message = "Deleted." });
    }
}
public record LocationTypeRequest(string Name, string ShortName);
