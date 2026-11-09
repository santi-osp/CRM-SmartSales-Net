using System.ComponentModel.DataAnnotations;
using System.Text;
using Microsoft.EntityFrameworkCore;
using SmartSales.Contacts.Domain.Entities;
using SmartSales.Contacts.Infrastructure.Persistence;

namespace SmartSales.Contacts.Application.Contacts;

public sealed class ContactService(ContactsDbContext db, TimeProvider clock)
{
    private static readonly HashSet<string> Types = ["Prospect", "Customer"];
    private static readonly HashSet<string> Sources = ["Web", "Referral", "WhatsApp", "Call", "Other"];

    public async Task<PagedResult<ContactResponse>> ListAsync(string? q, string? type, int page, int pageSize,
        CancellationToken cancellationToken)
    {
        if (page < 1 || pageSize is < 1 or > 100 || (long)(page - 1) * pageSize > int.MaxValue)
            throw Invalid("invalid_pagination", "Paginación no válida.");
        if (type is not null && !Types.Contains(type))
            throw Invalid("invalid_type", "Tipo de contacto no válido.");
        q = q?.Trim().Normalize(NormalizationForm.FormC);
        if (q?.Length > 120)
            throw Invalid("invalid_query", "La búsqueda es demasiado larga.");

        IQueryable<Contact> query = db.Contacts.AsNoTracking();
        if (type is not null)
            query = query.Where(contact => contact.Type == type);
        if (!string.IsNullOrEmpty(q))
        {
            var escaped = q.Replace("\\", "\\\\", StringComparison.Ordinal)
                .Replace("%", "\\%", StringComparison.Ordinal)
                .Replace("_", "\\_", StringComparison.Ordinal);
            var pattern = $"%{escaped}%";
            var phoneDigits = new string(q.Where(character => character is >= '0' and <= '9').ToArray());
            query = query.Where(contact =>
                EF.Functions.ILike(contact.Name, pattern, "\\") ||
                (contact.NormalizedEmail != null && EF.Functions.ILike(contact.NormalizedEmail, pattern, "\\")) ||
                (phoneDigits.Length > 0 && contact.NormalizedPhone != null &&
                 contact.NormalizedPhone.Contains(phoneDigits)));
        }

        var count = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(contact => contact.CreatedAt).ThenBy(contact => contact.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(contact => new ContactResponse(contact.Id, contact.Name, contact.Type, contact.Email,
                contact.Phone, contact.Source, contact.CreatedAt, contact.UpdatedAt, contact.Version))
            .ToListAsync(cancellationToken);
        return new PagedResult<ContactResponse>(items, page, pageSize, count);
    }

    public async Task<ContactResponse?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var contact = await db.Contacts.AsNoTracking().SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        return contact is null ? null : ContactResponse.From(contact);
    }

    public async Task<ContactResponse> CreateAsync(CreateContactRequest request, CancellationToken cancellationToken)
    {
        var fields = Validate(new ContactFields(request.Name, request.Type, request.Email, request.Phone, request.Source));
        await CheckDuplicatesAsync(fields, null, request.AcceptPotentialDuplicate, cancellationToken);
        var now = clock.GetUtcNow();
        var contact = new Contact
        {
            Id = Guid.CreateVersion7(), Name = fields.Name, Type = fields.Type,
            Email = fields.Email, NormalizedEmail = fields.NormalizedEmail,
            Phone = fields.Phone, NormalizedPhone = fields.NormalizedPhone,
            Source = fields.Source, CreatedAt = now, UpdatedAt = now, Version = 1
        };
        db.Contacts.Add(contact);
        await db.SaveChangesAsync(cancellationToken);
        return ContactResponse.From(contact);
    }

    public async Task<ContactResponse?> UpdateAsync(Guid id, UpdateContactRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Version is null or < 1)
            throw Invalid("invalid_version", "La versión es obligatoria.");
        var fields = Validate(new ContactFields(request.Name, request.Type, request.Email, request.Phone, request.Source));
        var contact = await db.Contacts.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (contact is null) return null;
        if (contact.Version != request.Version)
            throw new ContactRequestException(409, "concurrency_conflict", "La ficha cambió. Recarga antes de guardar.");
        await CheckDuplicatesAsync(fields, id, request.AcceptPotentialDuplicate, cancellationToken);
        contact.Name = fields.Name;
        contact.Type = fields.Type;
        contact.Email = fields.Email;
        contact.NormalizedEmail = fields.NormalizedEmail;
        contact.Phone = fields.Phone;
        contact.NormalizedPhone = fields.NormalizedPhone;
        contact.Source = fields.Source;
        contact.UpdatedAt = clock.GetUtcNow();
        contact.Version++;
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ContactRequestException(409, "concurrency_conflict", "La ficha cambió. Recarga antes de guardar.");
        }
        return ContactResponse.From(contact);
    }

    private async Task CheckDuplicatesAsync(ValidatedFields fields, Guid? currentId, bool accepted,
        CancellationToken cancellationToken)
    {
        if (accepted) return;
        var ids = await db.Contacts.AsNoTracking()
            .Where(contact => contact.Id != currentId &&
                ((fields.NormalizedEmail != null && contact.NormalizedEmail == fields.NormalizedEmail) ||
                 (fields.NormalizedPhone != null && contact.NormalizedPhone == fields.NormalizedPhone)))
            .OrderBy(contact => contact.Id).Select(contact => contact.Id).Take(100)
            .ToListAsync(cancellationToken);
        if (ids.Count > 0)
            throw new ContactRequestException(409, "potential_duplicate",
                "Posible contacto duplicado. Revisa y confirma si deseas continuar.", ids);
    }

    private static ValidatedFields Validate(ContactFields raw)
    {
        var name = raw.Name?.Trim().Normalize(NormalizationForm.FormC);
        var email = NormalizeOptional(raw.Email);
        var phone = NormalizeOptional(raw.Phone);
        if (name is null or { Length: 0 or > 160 })
            throw Invalid("invalid_name", "Nombre obligatorio de máximo 160 caracteres.");
        if (raw.Type is null || !Types.Contains(raw.Type))
            throw Invalid("invalid_type", "Tipo de contacto no válido.");
        if (raw.Source is null || !Sources.Contains(raw.Source))
            throw Invalid("invalid_source", "Origen de contacto no válido.");
        if (email is { Length: > 254 } ||
            (email is not null && !new EmailAddressAttribute().IsValid(email)))
            throw Invalid("invalid_email", "Correo no válido.");
        if (phone is { Length: > 40 })
            throw Invalid("invalid_phone", "Teléfono no válido.");
        if (phone is not null && phone.Any(character =>
            character is not (>= '0' and <= '9' or '+' or ' ' or '-' or '(' or ')')))
            throw Invalid("invalid_phone", "Teléfono no válido.");
        var digits = phone is null ? null : new string(phone.Where(character => character is >= '0' and <= '9').ToArray());
        if (phone is not null && digits!.Length is < 7 or > 15)
            throw Invalid("invalid_phone", "El teléfono debe tener entre 7 y 15 dígitos.");
        if (email is null && phone is null)
            throw Invalid("contact_method_required", "Indica correo o teléfono.");
        return new ValidatedFields(name, raw.Type, email, email?.ToLowerInvariant(), phone, digits, raw.Source);
    }

    private static string? NormalizeOptional(string? value)
    {
        var normalized = value?.Trim().Normalize(NormalizationForm.FormC);
        return string.IsNullOrEmpty(normalized) ? null : normalized;
    }

    private static ContactRequestException Invalid(string code, string title) => new(400, code, title);

    private sealed record ValidatedFields(string Name, string Type, string? Email, string? NormalizedEmail,
        string? Phone, string? NormalizedPhone, string Source);
}
