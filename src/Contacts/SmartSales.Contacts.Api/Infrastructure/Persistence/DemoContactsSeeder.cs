using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using SmartSales.Contacts.Domain.Entities;

namespace SmartSales.Contacts.Infrastructure.Persistence;

public static class DemoContactsSeeder
{
    private static readonly (string Name, string Type, string Email, string? Phone, string Source)[] OriginalEight =
    [
        ("Café Aurora", "Customer", "aurora@demo.example", "+57 300 111 2233", "Referral"),
        ("Montaña Clara", "Prospect", "montana@demo.example", "+57 310 222 3344", "Web"),
        ("Taller Andino", "Prospect", "taller@demo.example", "+57 320 333 4455", "Call"),
        ("Casa Nube", "Customer", "nube@demo.example", "+57 315 444 5566", "Referral"),
        ("Boreal Studio", "Prospect", "boreal@demo.example", "+57 301 555 6677", "Web"),
        ("Verde Lienzo", "Customer", "verde@demo.example", "+57 312 666 7788", "Other"),
        ("Ruta Nova", "Prospect", "ruta@demo.example", "+57 318 777 8899", "Web"),
        ("Lumen Co", "Customer", "lumen@demo.example", "+57 305 888 9900", "Call")
    ];

    private static readonly (string Name, string Type, string Email, string? Phone, string Source)[] AdditionalFiftyTwo =
    [
        ("Logística del Café SAS", "Customer", "logistica.cafe@demo.example", "+57 300 201 0001", "Referral"),
        ("Soluciones Textiles Medellín", "Prospect", "textiles.medellin@demo.example", "+57 310 201 0002", "Web"),
        ("Agroindustrias del Valle", "Customer", "agro.valle@demo.example", "+57 320 201 0003", "Referral"),
        ("Construcciones Andinas", "Prospect", "construcciones.andinas@demo.example", "+57 315 201 0004", "Call"),
        ("Alimentos Sabana Real", "Customer", "alimentos.sabana@demo.example", "+57 301 201 0005", "WhatsApp"),
        ("Distribuidora Caribeña", "Prospect", "distribuidora.caribe@demo.example", "+57 312 201 0006", "Web"),
        ("Ingeniería Hidráulica del Sur", "Customer", "hidraulica.sur@demo.example", "+57 318 201 0007", "Other"),
        ("Papeles y Empaques Nacionales", "Prospect", "papeles.nacionales@demo.example", "+57 305 201 0008", "Call"),
        ("Servicios Petroleros del Llano", "Customer", "servicios.llano@demo.example", "+57 300 201 0009", "Referral"),
        ("Ferretería Central de Occidente", "Prospect", "ferreteria.central@demo.example", "+57 310 201 0010", "Web"),
        ("Biotecnología Colombiana", "Customer", "biotec.colombia@demo.example", "+57 320 201 0011", "WhatsApp"),
        ("Transportes Rápidos Boyacá", "Prospect", "transportes.boyaca@demo.example", "+57 315 201 0012", "Call"),
        ("Clínica Veterinaria San Roque", "Customer", "vet.sanroque@demo.example", "+57 301 201 0013", "Web"),
        ("Muebles & Diseños del Eje", "Prospect", "muebles.eje@demo.example", "+57 312 201 0014", "Referral"),
        ("Tecnología Satelital SAS", "Customer", "tecno.satelital@demo.example", "+57 318 201 0015", "Web"),
        ("Seguridad Integral Andina", "Prospect", "seguridad.andina@demo.example", "+57 305 201 0016", "Call"),
        ("Plásticos del Oriente", "Customer", "plasticos.oriente@demo.example", "+57 300 201 0017", "Other"),
        ("Editorial Siglo Nuevo", "Prospect", "editorial.siglonuevo@demo.example", "+57 310 201 0018", "Web"),
        ("Energías Limpias del Norte", "Customer", "energias.norte@demo.example", "+57 320 201 0019", "Referral"),
        ("Consultoría Contable & Tributaria", "Prospect", "consultoria.tributaria@demo.example", "+57 315 201 0020", "Call"),
        ("Laboratorios Farmacéuticos del Sol", "Customer", "laboratorios.sol@demo.example", "+57 301 201 0021", "WhatsApp"),
        ("Inmobiliaria Metropolitana", "Prospect", "inmobiliaria.metro@demo.example", "+57 312 201 0022", "Web"),
        ("Lácteos de la Sabana", "Customer", "lacteos.sabana@demo.example", "+57 318 201 0023", "Referral"),
        ("Arquitectura Urbana SAS", "Prospect", "arquitectura.urbana@demo.example", "+57 305 201 0024", "Call"),
        ("Vidrios & Aluminios Caldas", "Customer", "vidrios.caldas@demo.example", "+57 300 201 0025", "Other"),
        ("Químicos Industriales del Tolima", "Prospect", "quimicos.tolima@demo.example", "+57 310 201 0026", "Web"),
        ("Asesorías Jurídicas Bolívar", "Customer", "asesorias.bolivar@demo.example", "+57 320 201 0027", "Referral"),
        ("Suministros Médicos del País", "Prospect", "suministros.medicos@demo.example", "+57 315 201 0028", "WhatsApp"),
        ("Embalajes del Pacífico", "Customer", "embalajes.pacifico@demo.example", "+57 301 201 0029", "Call"),
        ("Calzado & Cueros Santander", "Prospect", "calzado.santander@demo.example", "+57 312 201 0030", "Web"),
        ("Metalmecánica del Cauca", "Customer", "metalmecanica.cauca@demo.example", "+57 318 201 0031", "Other"),
        ("Harinas & Cereales San Pedro", "Prospect", "cereales.sanpedro@demo.example", "+57 305 201 0032", "Call"),
        ("Redes & Fibra Óptica Colombia", "Customer", "fibra.colombia@demo.example", "+57 300 201 0033", "Web"),
        ("Dotaciones Empresariales SAS", "Prospect", "dotaciones.empresas@demo.example", "+57 310 201 0034", "Referral"),
        ("Empaques Flexibles del Valle", "Customer", "empaques.flexibles@demo.example", "+57 320 201 0035", "WhatsApp"),
        ("Refrigeración Comercial Andina", "Prospect", "refrigeracion.andina@demo.example", "+57 315 201 0036", "Call"),
        ("Cosméticos Naturales Selva", "Customer", "cosmeticos.selva@demo.example", "+57 301 201 0037", "Web"),
        ("Imprenta Nacional de Gráficas", "Prospect", "imprenta.graficas@demo.example", "+57 312 201 0038", "Other"),
        ("Cervecería Artesanal Montañera", "Customer", "cerveceria.artesanal@demo.example", "+57 318 201 0039", "Referral"),
        ("Software Financiero Ágil", "Prospect", "software.financiero@demo.example", "+57 305 201 0040", "Web"),
        ("Publicidad Digital Creadores", "Customer", "publicidad.digital@demo.example", "+57 300 201 0041", "Call"),
        ("Repuestos Automotrices Líder", "Prospect", "repuestos.lider@demo.example", "+57 310 201 0042", "WhatsApp"),
        ("Pinturas & Revestimientos SAS", "Customer", "pinturas.sas@demo.example", "+57 320 201 0043", "Other"),
        ("Confecciones El Cóndor", "Prospect", "confecciones.condor@demo.example", "+57 315 201 0044", "Web"),
        ("Equipos de Medición de Precisión", "Customer", "equipos.medicion@demo.example", "+57 301 201 0045", "Referral"),
        ("Vivero & Paisajismo Los Cerros", "Prospect", "vivero.cerros@demo.example", "+57 312 201 0046", "Call"),
        ("Cables Eléctricos Especiales", "Customer", "cables.especiales@demo.example", "+57 318 201 0047", "Web"),
        ("Madera Inmunizada del Darién", "Prospect", "madera.darien@demo.example", "+57 305 201 0048", "Other"),
        ("Distribuidora Avícola Campesina", "Customer", "avicola.campesina@demo.example", "+57 300 201 0049", "Referral"),
        ("Panadería & Repostería Francesa", "Prospect", "panaderia.francesa@demo.example", "+57 310 201 0050", "WhatsApp"),
        ("Servicios de Limpieza Institucional", "Customer", "limpieza.institucional@demo.example", "+57 320 201 0051", "Call"),
        ("Cerrajería de Alta Seguridad", "Prospect", "cerrajeria.segura@demo.example", "+57 315 201 0052", "Web")
    ];

    public static (string Name, string Type, string Email, string? Phone, string Source)[] GetCatalog(bool isShowcase)
    {
        if (!isShowcase) return OriginalEight;
        return [.. OriginalEight, .. AdditionalFiftyTwo];
    }

    public static async Task SeedAsync(IServiceProvider services, IConfiguration configuration,
        CancellationToken cancellationToken)
    {
        var env = services.GetService<IHostEnvironment>();
        var isShowcase = env is not null
            ? DemoSeedProfile.IsShowcase(configuration, env)
            : configuration.GetValue<bool>("Demo:Enabled") && (configuration["Demo:Profile"] ?? "Basic").Equals("Showcase", StringComparison.OrdinalIgnoreCase);

        if (!configuration.GetValue<bool>("Demo:Enabled"))
            throw new InvalidOperationException("Demo:Enabled must be true to seed demonstration data.");

        var db = services.GetRequiredService<ContactsDbContext>();
        var clock = services.GetRequiredService<TimeProvider>();
        var now = DemoSeedProfile.Reference(configuration, clock);

        var rows = GetCatalog(isShowcase);

        for (var index = 0; index < rows.Length; index++)
        {
            var id = Guid.Parse($"019a6020-0000-7000-8000-{index + 1:000000000000}");
            var existing = await db.Contacts.AsNoTracking()
                .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
            if (existing is not null)
            {
                // Idempotente: se conserva la fila existente sin sobrescribir ediciones del usuario
                continue;
            }

            var row = rows[index];
            var normalizedPhone = row.Phone is null ? null : new string(row.Phone.Where(char.IsDigit).ToArray());

            db.Contacts.Add(new Contact
            {
                Id = id,
                Name = row.Name,
                Type = row.Type,
                Email = row.Email,
                NormalizedEmail = row.Email.ToUpperInvariant(),
                Phone = row.Phone,
                NormalizedPhone = normalizedPhone,
                Source = row.Source,
                CreatedAt = now.AddMinutes(-rows.Length + index),
                UpdatedAt = now,
                Version = 1
            });
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
