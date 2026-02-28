using Gimnasio.Data;
using Gimnasio.Models;
using Microsoft.EntityFrameworkCore;

namespace Gimnasio.Services;

public class PropagandaService : IPropagandaService
{
    private readonly ApplicationDbContext _db;

    public PropagandaService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<List<object>> GetDisenosAsync(Guid negocioId)
    {
        return await _db.DisenosMarketing
            .Where(d => d.NegocioId == negocioId && d.IsActive)
            .OrderByDescending(d => d.FechaModificacion)
            .Select(d => new
            {
                d.DisenoId,
                d.Nombre,
                d.ThumbnailDataUri,
                d.Ancho,
                d.Alto,
                d.FechaCreacion,
                d.FechaModificacion
            } as object)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<DisenoMarketing> GetDisenoAsync(Guid disenoId, Guid negocioId)
    {
        return await _db.DisenosMarketing
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.DisenoId == disenoId && d.NegocioId == negocioId && d.IsActive);
    }

    public async Task<(bool success, string message, Guid? dataId)> CrearDisenoAsync(
        Guid negocioId, string nombre, string canvasJson, string thumbnailDataUri, int ancho, int alto)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            return (false, "El nombre es requerido", null);

        var diseno = new DisenoMarketing
        {
            DisenoId = Guid.NewGuid(),
            NegocioId = negocioId,
            Nombre = nombre.Trim(),
            CanvasJson = canvasJson ?? "{}",
            ThumbnailDataUri = thumbnailDataUri ?? "",
            Ancho = ancho > 0 ? ancho : 1080,
            Alto = alto > 0 ? alto : 1080,
            FechaCreacion = TimeHelper.Now,
            FechaModificacion = TimeHelper.Now
        };

        _db.DisenosMarketing.Add(diseno);
        await _db.SaveChangesAsync();

        return (true, "Diseno creado exitosamente", diseno.DisenoId);
    }

    public async Task<(bool success, string message)> GuardarDisenoAsync(
        Guid disenoId, Guid negocioId, string nombre, string canvasJson, string thumbnailDataUri)
    {
        var diseno = await _db.DisenosMarketing
            .FirstOrDefaultAsync(d => d.DisenoId == disenoId && d.NegocioId == negocioId && d.IsActive);

        if (diseno == null)
            return (false, "Diseno no encontrado");

        if (!string.IsNullOrWhiteSpace(nombre))
            diseno.Nombre = nombre.Trim();

        if (!string.IsNullOrWhiteSpace(canvasJson))
            diseno.CanvasJson = canvasJson;

        if (!string.IsNullOrWhiteSpace(thumbnailDataUri))
            diseno.ThumbnailDataUri = thumbnailDataUri;

        diseno.FechaModificacion = TimeHelper.Now;
        await _db.SaveChangesAsync();

        return (true, "Diseno guardado exitosamente");
    }

    public async Task<(bool success, string message)> EliminarDisenoAsync(Guid disenoId, Guid negocioId)
    {
        var diseno = await _db.DisenosMarketing
            .FirstOrDefaultAsync(d => d.DisenoId == disenoId && d.NegocioId == negocioId && d.IsActive);

        if (diseno == null)
            return (false, "Diseno no encontrado");

        diseno.IsActive = false;
        await _db.SaveChangesAsync();

        return (true, "Diseno eliminado exitosamente");
    }

    public async Task<(bool success, string message)> DuplicarDisenoAsync(Guid disenoId, Guid negocioId)
    {
        var original = await _db.DisenosMarketing
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.DisenoId == disenoId && d.NegocioId == negocioId && d.IsActive);

        if (original == null)
            return (false, "Diseno no encontrado");

        var copia = new DisenoMarketing
        {
            DisenoId = Guid.NewGuid(),
            NegocioId = negocioId,
            Nombre = original.Nombre + " (copia)",
            CanvasJson = original.CanvasJson,
            ThumbnailDataUri = original.ThumbnailDataUri,
            Ancho = original.Ancho,
            Alto = original.Alto,
            FechaCreacion = TimeHelper.Now,
            FechaModificacion = TimeHelper.Now
        };

        _db.DisenosMarketing.Add(copia);
        await _db.SaveChangesAsync();

        return (true, "Diseno duplicado exitosamente");
    }
}
