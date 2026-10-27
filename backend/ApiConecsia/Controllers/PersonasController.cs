using ApiConecsia.Helpers;
using ApiConecsia.Services;
using ConecsIA.Application.DTOs;
using ConecsIA.Application.Interfaces;
using ConecsIA.Application.Seguridad;
using ConecsIA.Domain.Entities;
using ConecsIA.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApiConecsia.Controllers;

/// <summary>
/// Personas de la organización activa, con sus teléfonos (1:N) y vehículos (N:M por persona_vehiculo).
/// La organización sale siempre del contexto validado en servidor; los filtros globales de EF Core y la
/// validación de escritura del DbContext impiden leer o escribir filas de otra organización (⇒ 404).
/// No hay borrado físico: personas, teléfonos y vínculos con vehículos se inactivan para conservar la historia.
/// Los vehículos son un módulo opcional: sus endpoints exigen también el módulo Vehículos.
/// </summary>
[Route("api/personas")]
public sealed class PersonasController(
    ConecsiaDbContext db,
    IOrganizationContext organizationContext,
    IModuloAccesoService accesos,
    AlmacenamientoArchivos almacenamiento) : AdministracionControllerBase(db, organizationContext, accesos)
{
    private const string Entidad = "Persona";

    // ---------- Personas ----------

    [HttpGet]
    [RequiereModulo(CodigosModulo.Personas, AccionModulo.Consultar)]
    public async Task<ActionResult<IEnumerable<PersonaListaDto>>> Listar([FromQuery] string? buscar = null, [FromQuery] string? estado = null, [FromQuery] int? idTipoPersona = null, CancellationToken ct = default)
    {
        var query = Db.Personas.AsQueryable();
        if (estado is "Activo" or "Inactivo") query = query.Where(x => x.Activo == (estado == "Activo"));
        if (idTipoPersona is int tipo) query = query.Where(x => x.IdTipoPersona == tipo);
        if (!string.IsNullOrWhiteSpace(buscar))
        {
            var texto = $"%{buscar.Trim()}%";
            query = query.Where(x => EF.Functions.ILike(x.Nombres + " " + x.Apellidos, texto) || EF.Functions.ILike(x.NumeroDocumento, texto)
                || (x.Correo != null && EF.Functions.ILike(x.Correo, texto)));
        }
        return Ok(await Proyectar(query.OrderBy(x => x.Apellidos).ThenBy(x => x.Nombres)).Take(500).ToListAsync(ct));
    }

    /// <summary>Búsqueda exacta por identificación (flujo «buscar primero»). 404 si no existe en la organización.</summary>
    [HttpGet("buscar-documento")]
    [RequiereModulo(CodigosModulo.Personas, AccionModulo.Consultar)]
    public async Task<ActionResult<PersonaListaDto>> BuscarDocumento([FromQuery] int idTipoDocumento, [FromQuery] string? numero, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(numero)) return Invalido("Indique el número de documento.");
        var normalizado = ReglasPersona.NormalizarDocumento(numero);
        var dto = await Proyectar(Db.Personas.Where(x => x.IdTipoDocumento == idTipoDocumento && x.NumeroDocumentoNormalizado == normalizado)).SingleOrDefaultAsync(ct);
        return dto is null ? NoEncontrado("No hay una persona registrada con ese documento.") : Ok(dto);
    }

    [HttpGet("{idPersona:int}")]
    [RequiereModulo(CodigosModulo.Personas, AccionModulo.Consultar)]
    public async Task<ActionResult<PersonaDetalleDto>> Obtener(int idPersona, CancellationToken ct)
    {
        var detalle = await DetalleAsync(idPersona, ct);
        return detalle is null ? NoEncontrado("La persona no existe o no está en tu alcance.") : Ok(detalle);
    }

    /// <summary>Catálogos activos del formulario (tipos de persona de la organización y tipos de documento globales).</summary>
    [HttpGet("catalogos")]
    [RequiereModulo(CodigosModulo.Personas, AccionModulo.Consultar)]
    public async Task<ActionResult<CatalogosPersonaDto>> Catalogos(CancellationToken ct) => Ok(new CatalogosPersonaDto
    {
        TiposPersona = await Db.TiposPersona.Where(x => x.Activo).OrderBy(x => x.Nombre).Select(x => new OpcionCatalogoDto { Id = x.IdTipoPersona, Nombre = x.Nombre }).ToListAsync(ct),
        TiposDocumento = await Db.TiposDocumento.Where(x => x.Activo).OrderBy(x => x.IdTipoDocumento)
            .Select(x => new CatalogoTipoDocumentoDto { Id = x.IdTipoDocumento, Codigo = x.Codigo, Nombre = x.Nombre, PatronValidacion = x.PatronValidacion }).ToListAsync(ct),
        TiposTelefono = ReglasPersona.TiposTelefono,
        TiposVehiculo = ReglasPersona.TiposVehiculo,
        RelacionesVehiculo = ReglasPersona.RelacionesVehiculo,
    });

    [HttpPost]
    [RequiereModulo(CodigosModulo.Personas, AccionModulo.Anadir)]
    public async Task<ActionResult<PersonaDetalleDto>> Crear([FromBody] PersonaEntrada request, CancellationToken ct)
    {
        var (normalizado, error) = await ValidarAsync(request, null, ct);
        if (error is not null) return error;

        var persona = new Persona { IdOrganizacion = OrganizacionActiva, CreadoEn = DateTime.UtcNow, IdUsuarioCrea = User.IdUsuario()!.Value, Activo = true };
        Aplicar(persona, request, normalizado!);
        await using var transaccion = await Db.Database.BeginTransactionAsync(ct);
        Db.Personas.Add(persona);
        // Doble envío o alta concurrente: el índice único (organización, tipo, documento) rechaza la segunda.
        if (!await GuardarAsync(ct)) return Conflicto("Ya existe una persona con ese tipo y número de documento.");
        Auditar(OrganizacionActiva, "Crear", Entidad, persona.IdPersona.ToString(), null, Resumen(persona));
        await Db.SaveChangesAsync(ct);
        await transaccion.CommitAsync(ct);
        return CreatedAtAction(nameof(Obtener), new { idPersona = persona.IdPersona }, await DetalleAsync(persona.IdPersona, ct));
    }

    [HttpPut("{idPersona:int}")]
    [RequiereModulo(CodigosModulo.Personas, AccionModulo.Editar)]
    public async Task<ActionResult<PersonaDetalleDto>> Editar(int idPersona, [FromBody] PersonaEntrada request, CancellationToken ct)
    {
        var persona = await Db.Personas.SingleOrDefaultAsync(x => x.IdPersona == idPersona, ct);
        if (persona is null) return NoEncontrado("La persona no existe o no está en tu alcance.");
        var (normalizado, error) = await ValidarAsync(request, persona, ct);
        if (error is not null) return error;

        var anterior = Resumen(persona);
        Aplicar(persona, request, normalizado!);
        Auditar(OrganizacionActiva, "Modificar", Entidad, idPersona.ToString(), anterior, Resumen(persona));
        if (!await GuardarAsync(ct)) return Conflicto("Ya existe una persona con ese tipo y número de documento.");
        return Ok(await DetalleAsync(idPersona, ct));
    }

    /// <summary>Activa o inactiva. Inactivar no borra ni revoca: autorizaciones, visitas y movimientos se conservan.</summary>
    [HttpPatch("{idPersona:int}/estado")]
    [RequiereModulo(CodigosModulo.Personas, AccionModulo.Editar)]
    public async Task<ActionResult<PersonaDetalleDto>> CambiarEstado(int idPersona, [FromBody] EstadoAdministracionEntrada request, CancellationToken ct)
    {
        var persona = await Db.Personas.SingleOrDefaultAsync(x => x.IdPersona == idPersona, ct);
        if (persona is null) return NoEncontrado("La persona no existe o no está en tu alcance.");
        if (persona.Activo != request.Activo!.Value)
        {
            persona.Activo = request.Activo.Value;
            Auditar(OrganizacionActiva, persona.Activo ? "Activar" : "Inactivar", Entidad, idPersona.ToString(), new { Activo = !persona.Activo }, new { persona.Activo });
            await Db.SaveChangesAsync(ct);
        }
        return Ok(await DetalleAsync(idPersona, ct));
    }

    // ---------- Foto ----------

    [HttpGet("{idPersona:int}/foto")]
    [RequiereModulo(CodigosModulo.Personas, AccionModulo.Consultar)]
    public async Task<IActionResult> Foto(int idPersona, CancellationToken ct)
    {
        var idArchivo = await Db.Personas.Where(x => x.IdPersona == idPersona).Select(x => x.IdArchivoFoto).SingleOrDefaultAsync(ct);
        if (idArchivo is not Guid id || await almacenamiento.AbrirAsync(id, ct) is not { } archivo)
            return NoEncontrado("La persona no tiene foto o no está en tu alcance.");
        Response.Headers.CacheControl = "private, max-age=300";
        return File(archivo.Contenido, archivo.Archivo.TipoContenido);
    }

    /// <summary>Carga o reemplaza la foto (PNG, JPEG o WebP, máximo 2 MB, tipo por firma). La anterior se conserva (D-14).</summary>
    [HttpPost("{idPersona:int}/foto")]
    [RequiereModulo(CodigosModulo.Personas, AccionModulo.Editar)]
    [RequestSizeLimit(AlmacenamientoArchivos.TamanoMaximoImagen + 64 * 1024)]
    public async Task<ActionResult<PersonaDetalleDto>> CargarFoto(int idPersona, IFormFile? archivo, CancellationToken ct)
    {
        var persona = await Db.Personas.SingleOrDefaultAsync(x => x.IdPersona == idPersona, ct);
        if (persona is null) return NoEncontrado("La persona no existe o no está en tu alcance.");
        if (archivo is null || archivo.Length == 0) return Invalido("Seleccione una imagen.");
        if (archivo.Length > AlmacenamientoArchivos.TamanoMaximoImagen) return Invalido("La foto no puede superar 2 MB.");
        using var memoria = new MemoryStream();
        await archivo.CopyToAsync(memoria, ct);
        var contenido = memoria.ToArray();
        var tipo = AlmacenamientoArchivos.TipoImagen(contenido);
        if (tipo is null) return Invalido("La foto debe ser una imagen PNG, JPEG o WebP.");

        var guardado = await almacenamiento.GuardarAsync(contenido, tipo, OrganizacionActiva, User.IdUsuario()!.Value, ct);
        var anterior = persona.IdArchivoFoto;
        persona.IdArchivoFoto = guardado.IdArchivo;
        Auditar(OrganizacionActiva, "CargarFoto", Entidad, idPersona.ToString(), new { IdArchivoFoto = anterior }, new { persona.IdArchivoFoto });
        await Db.SaveChangesAsync(ct);
        return Ok(await DetalleAsync(idPersona, ct));
    }

    [HttpDelete("{idPersona:int}/foto")]
    [RequiereModulo(CodigosModulo.Personas, AccionModulo.Editar)]
    public async Task<ActionResult<PersonaDetalleDto>> QuitarFoto(int idPersona, CancellationToken ct)
    {
        var persona = await Db.Personas.SingleOrDefaultAsync(x => x.IdPersona == idPersona, ct);
        if (persona is null) return NoEncontrado("La persona no existe o no está en tu alcance.");
        if (persona.IdArchivoFoto is not null)
        {
            Auditar(OrganizacionActiva, "QuitarFoto", Entidad, idPersona.ToString(), new { persona.IdArchivoFoto }, null);
            persona.IdArchivoFoto = null;
            await Db.SaveChangesAsync(ct);
        }
        return Ok(await DetalleAsync(idPersona, ct));
    }

    // ---------- Teléfonos ----------

    [HttpPost("{idPersona:int}/telefonos")]
    [RequiereModulo(CodigosModulo.Personas, AccionModulo.Editar)]
    public async Task<ActionResult<TelefonoPersonaDto>> AgregarTelefono(int idPersona, [FromBody] TelefonoEntrada request, CancellationToken ct)
    {
        if (!await Db.Personas.AnyAsync(x => x.IdPersona == idPersona, ct)) return NoEncontrado("La persona no existe o no está en tu alcance.");
        var numero = ReglasPersona.NormalizarTelefono(request.Numero);
        if (ReglasPersona.ValidarTelefono(request.CodigoPais, numero, request.Tipo) is { } invalido) return Invalido(invalido);
        if (await Db.PersonasTelefonos.AnyAsync(x => x.IdPersona == idPersona && x.Activo && x.CodigoPais == request.CodigoPais && x.Numero == numero, ct))
            return Conflicto("La persona ya tiene registrado ese teléfono.");

        await using var transaccion = await Db.Database.BeginTransactionAsync(ct);
        if (request.Principal) await QuitarPrincipalAsync(idPersona, null, ct);
        var telefono = new PersonaTelefono { IdOrganizacion = OrganizacionActiva, IdPersona = idPersona, CodigoPais = request.CodigoPais, Numero = numero, Tipo = request.Tipo, Principal = request.Principal, Activo = true };
        Db.PersonasTelefonos.Add(telefono);
        if (!await GuardarAsync(ct)) return Conflicto("La persona ya tiene registrado ese teléfono.");
        Auditar(OrganizacionActiva, "AgregarTelefono", Entidad, idPersona.ToString(), null, ATelefonoDto(telefono));
        await Db.SaveChangesAsync(ct);
        await transaccion.CommitAsync(ct);
        return Created($"/api/personas/{idPersona}/telefonos/{telefono.IdTelefono}", ATelefonoDto(telefono));
    }

    [HttpPut("{idPersona:int}/telefonos/{idTelefono:int}")]
    [RequiereModulo(CodigosModulo.Personas, AccionModulo.Editar)]
    public async Task<ActionResult<TelefonoPersonaDto>> EditarTelefono(int idPersona, int idTelefono, [FromBody] TelefonoEntrada request, CancellationToken ct)
    {
        var telefono = await Db.PersonasTelefonos.SingleOrDefaultAsync(x => x.IdTelefono == idTelefono && x.IdPersona == idPersona, ct);
        if (telefono is null) return NoEncontrado("El teléfono no existe para esta persona.");
        var numero = ReglasPersona.NormalizarTelefono(request.Numero);
        if (ReglasPersona.ValidarTelefono(request.CodigoPais, numero, request.Tipo) is { } invalido) return Invalido(invalido);
        if (telefono.Activo && await Db.PersonasTelefonos.AnyAsync(x => x.IdPersona == idPersona && x.IdTelefono != idTelefono && x.Activo && x.CodigoPais == request.CodigoPais && x.Numero == numero, ct))
            return Conflicto("La persona ya tiene registrado ese teléfono.");

        await using var transaccion = await Db.Database.BeginTransactionAsync(ct);
        var anterior = ATelefonoDto(telefono);
        if (request.Principal && telefono.Activo) await QuitarPrincipalAsync(idPersona, idTelefono, ct);
        telefono.CodigoPais = request.CodigoPais;
        telefono.Numero = numero;
        telefono.Tipo = request.Tipo;
        telefono.Principal = request.Principal && telefono.Activo;
        Auditar(OrganizacionActiva, "ModificarTelefono", Entidad, idPersona.ToString(), anterior, ATelefonoDto(telefono));
        if (!await GuardarAsync(ct)) return Conflicto("La persona ya tiene registrado ese teléfono.");
        await transaccion.CommitAsync(ct);
        return Ok(ATelefonoDto(telefono));
    }

    /// <summary>Quitar un teléfono lo inactiva (deja de ser principal); reactivarlo valida que no esté duplicado.</summary>
    [HttpPatch("{idPersona:int}/telefonos/{idTelefono:int}/estado")]
    [RequiereModulo(CodigosModulo.Personas, AccionModulo.Editar)]
    public async Task<ActionResult<TelefonoPersonaDto>> CambiarEstadoTelefono(int idPersona, int idTelefono, [FromBody] EstadoAdministracionEntrada request, CancellationToken ct)
    {
        var telefono = await Db.PersonasTelefonos.SingleOrDefaultAsync(x => x.IdTelefono == idTelefono && x.IdPersona == idPersona, ct);
        if (telefono is null) return NoEncontrado("El teléfono no existe para esta persona.");
        if (telefono.Activo == request.Activo!.Value) return Ok(ATelefonoDto(telefono));
        var anterior = ATelefonoDto(telefono);
        telefono.Activo = request.Activo.Value;
        if (!telefono.Activo) telefono.Principal = false;
        Auditar(OrganizacionActiva, telefono.Activo ? "ActivarTelefono" : "InactivarTelefono", Entidad, idPersona.ToString(), anterior, ATelefonoDto(telefono));
        if (!await GuardarAsync(ct)) return Conflicto("La persona ya tiene activo ese teléfono.");
        return Ok(ATelefonoDto(telefono));
    }

    // ---------- Vehículos ----------

    /// <summary>
    /// Asocia un vehículo a la persona. La placa es única por organización: si ya existe se vincula el mismo vehículo
    /// (sin sobrescribir sus datos); si no, se crea. Un vínculo inactivo se reactiva.
    /// </summary>
    [HttpPost("{idPersona:int}/vehiculos")]
    [RequiereModulo(CodigosModulo.Personas, AccionModulo.Editar)]
    [RequiereModulo(CodigosModulo.Vehiculos, AccionModulo.Anadir)]
    public async Task<ActionResult<VehiculoPersonaDto>> AgregarVehiculo(int idPersona, [FromBody] VehiculoPersonaEntrada request, CancellationToken ct)
    {
        if (!await Db.Personas.AnyAsync(x => x.IdPersona == idPersona, ct)) return NoEncontrado("La persona no existe o no está en tu alcance.");
        var placa = ReglasPersona.NormalizarPlaca(request.Placa);
        var relacion = request.Relacion ?? "Responsable";
        if (ReglasPersona.ValidarVehiculo(placa, request.Tipo, relacion) is { } invalido) return Invalido(invalido);

        await using var transaccion = await Db.Database.BeginTransactionAsync(ct);
        var vehiculo = await Db.Vehiculos.SingleOrDefaultAsync(x => x.PlacaNormalizada == placa, ct);
        if (vehiculo is null)
        {
            vehiculo = new Vehiculo { IdOrganizacion = OrganizacionActiva, Placa = placa, PlacaNormalizada = placa, Activo = true };
            AplicarVehiculo(vehiculo, request);
            Db.Vehiculos.Add(vehiculo);
            if (!await GuardarAsync(ct)) return Conflicto("Otro usuario registró esa placa al mismo tiempo. Inténtalo de nuevo.");
        }
        var vinculo = await Db.PersonasVehiculos.SingleOrDefaultAsync(x => x.IdPersona == idPersona && x.IdVehiculo == vehiculo.IdVehiculo, ct);
        if (vinculo is { Activo: true }) return Conflicto("El vehículo ya está asociado a esta persona.");
        if (vinculo is null)
        {
            vinculo = new PersonaVehiculo { IdOrganizacion = OrganizacionActiva, IdPersona = idPersona, IdVehiculo = vehiculo.IdVehiculo };
            Db.PersonasVehiculos.Add(vinculo);
        }
        vinculo.Relacion = relacion;
        vinculo.Activo = true;
        Auditar(OrganizacionActiva, "AsociarVehiculo", Entidad, idPersona.ToString(), null, new { vehiculo.IdVehiculo, vehiculo.Placa, vinculo.Relacion });
        if (!await GuardarAsync(ct)) return Conflicto("El vehículo ya está asociado a esta persona.");
        await transaccion.CommitAsync(ct);
        return Created($"/api/personas/{idPersona}/vehiculos/{vehiculo.IdVehiculo}", await VehiculoAsync(idPersona, vehiculo.IdVehiculo, ct));
    }

    /// <summary>Edita los datos del vehículo (compartidos con las demás personas asociadas) y la relación con esta persona.</summary>
    [HttpPut("{idPersona:int}/vehiculos/{idVehiculo:int}")]
    [RequiereModulo(CodigosModulo.Personas, AccionModulo.Editar)]
    [RequiereModulo(CodigosModulo.Vehiculos, AccionModulo.Editar)]
    public async Task<ActionResult<VehiculoPersonaDto>> EditarVehiculo(int idPersona, int idVehiculo, [FromBody] VehiculoPersonaEntrada request, CancellationToken ct)
    {
        var vinculo = await Db.PersonasVehiculos.SingleOrDefaultAsync(x => x.IdPersona == idPersona && x.IdVehiculo == idVehiculo, ct);
        var vehiculo = vinculo is null ? null : await Db.Vehiculos.SingleOrDefaultAsync(x => x.IdVehiculo == idVehiculo, ct);
        if (vinculo is null || vehiculo is null) return NoEncontrado("El vehículo no está asociado a esta persona.");
        var placa = ReglasPersona.NormalizarPlaca(request.Placa);
        var relacion = request.Relacion ?? vinculo.Relacion;
        if (ReglasPersona.ValidarVehiculo(placa, request.Tipo, relacion) is { } invalido) return Invalido(invalido);
        if (placa != vehiculo.PlacaNormalizada && await Db.Vehiculos.AnyAsync(x => x.PlacaNormalizada == placa, ct))
            return Conflicto("Ya existe otro vehículo con esa placa en la organización.");

        var anterior = new { vehiculo.Placa, vehiculo.Tipo, vehiculo.Marca, vehiculo.Modelo, vehiculo.Color, vinculo.Relacion };
        vehiculo.Placa = placa;
        vehiculo.PlacaNormalizada = placa;
        AplicarVehiculo(vehiculo, request);
        vinculo.Relacion = relacion;
        Auditar(OrganizacionActiva, "ModificarVehiculo", Entidad, idPersona.ToString(), anterior, new { idVehiculo, vehiculo.Placa, vehiculo.Tipo, vehiculo.Marca, vehiculo.Modelo, vehiculo.Color, vinculo.Relacion });
        if (!await GuardarAsync(ct)) return Conflicto("Ya existe otro vehículo con esa placa en la organización.");
        return Ok(await VehiculoAsync(idPersona, idVehiculo, ct));
    }

    /// <summary>Activa o inactiva el vínculo con la persona; el vehículo y sus otros vínculos no cambian.</summary>
    [HttpPatch("{idPersona:int}/vehiculos/{idVehiculo:int}/estado")]
    [RequiereModulo(CodigosModulo.Personas, AccionModulo.Editar)]
    [RequiereModulo(CodigosModulo.Vehiculos, AccionModulo.Editar)]
    public async Task<ActionResult<VehiculoPersonaDto>> CambiarEstadoVehiculo(int idPersona, int idVehiculo, [FromBody] EstadoAdministracionEntrada request, CancellationToken ct)
    {
        var vinculo = await Db.PersonasVehiculos.SingleOrDefaultAsync(x => x.IdPersona == idPersona && x.IdVehiculo == idVehiculo, ct);
        if (vinculo is null) return NoEncontrado("El vehículo no está asociado a esta persona.");
        if (vinculo.Activo != request.Activo!.Value)
        {
            vinculo.Activo = request.Activo.Value;
            Auditar(OrganizacionActiva, vinculo.Activo ? "ActivarVehiculo" : "DesasociarVehiculo", Entidad, idPersona.ToString(), new { idVehiculo, Activo = !vinculo.Activo }, new { idVehiculo, vinculo.Activo });
            await Db.SaveChangesAsync(ct);
        }
        return Ok(await VehiculoAsync(idPersona, idVehiculo, ct));
    }

    // ---------- Apoyo ----------

    private async Task<(string? Normalizado, ActionResult? Error)> ValidarAsync(PersonaEntrada request, Persona? actual, CancellationToken ct)
    {
        // Un tipo inactivo solo se admite si la persona ya lo tenía (conserva la referencia histórica).
        var (idPersona, tipoActual, documentoActual) = actual is null ? (0, 0, 0) : (actual.IdPersona, actual.IdTipoPersona, actual.IdTipoDocumento);
        if (!await Db.TiposPersona.AnyAsync(x => x.IdTipoPersona == request.IdTipoPersona && (x.Activo || x.IdTipoPersona == tipoActual), ct))
            return (null, Invalido("El tipo de persona no existe en tu organización o está inactivo."));
        var tipoDocumento = await Db.TiposDocumento.SingleOrDefaultAsync(x => x.IdTipoDocumento == request.IdTipoDocumento && (x.Activo || x.IdTipoDocumento == documentoActual), ct);
        if (tipoDocumento is null) return (null, Invalido("El tipo de documento no existe o está inactivo."));
        var normalizado = ReglasPersona.NormalizarDocumento(request.NumeroDocumento);
        if (ReglasPersona.ValidarDocumento(tipoDocumento, normalizado) is { } formato) return (null, Invalido(formato));
        if (await Db.Personas.AnyAsync(x => x.IdPersona != idPersona && x.IdTipoDocumento == request.IdTipoDocumento && x.NumeroDocumentoNormalizado == normalizado, ct))
            return (null, Conflicto("Ya existe una persona con ese tipo y número de documento."));
        return (normalizado, null);
    }

    private static void Aplicar(Persona persona, PersonaEntrada request, string normalizado)
    {
        persona.IdTipoPersona = request.IdTipoPersona;
        persona.IdTipoDocumento = request.IdTipoDocumento;
        persona.NumeroDocumento = normalizado;
        persona.NumeroDocumentoNormalizado = normalizado;
        persona.Nombres = request.Nombres.Trim();
        persona.Apellidos = request.Apellidos.Trim();
        persona.Correo = request.Correo.Trim();
        persona.EmpresaProcedencia = Limpiar(request.EmpresaProcedencia);
        persona.Observacion = Limpiar(request.Observacion);
    }

    private static void AplicarVehiculo(Vehiculo vehiculo, VehiculoPersonaEntrada request)
    {
        vehiculo.Tipo = request.Tipo;
        vehiculo.Marca = Limpiar(request.Marca);
        vehiculo.Modelo = Limpiar(request.Modelo);
        vehiculo.Color = Limpiar(request.Color);
    }

    /// <summary>Datos auditados de la persona (sin foto ni observación completa).</summary>
    private static object Resumen(Persona x) => new { x.IdTipoPersona, x.IdTipoDocumento, x.NumeroDocumento, x.Nombres, x.Apellidos, x.Correo, x.EmpresaProcedencia, x.Activo };

    private async Task QuitarPrincipalAsync(int idPersona, int? excepto, CancellationToken ct)
    {
        var principales = await Db.PersonasTelefonos.Where(x => x.IdPersona == idPersona && x.Principal && x.IdTelefono != excepto).ToListAsync(ct);
        if (principales.Count == 0) return;
        foreach (var telefono in principales) telefono.Principal = false;
        await Db.SaveChangesAsync(ct);
    }

    private static TelefonoPersonaDto ATelefonoDto(PersonaTelefono x) => new()
    {
        IdTelefono = x.IdTelefono, IdPersona = x.IdPersona, CodigoPais = x.CodigoPais, Numero = x.Numero, Tipo = x.Tipo, Principal = x.Principal, Activo = x.Activo,
    };

    private IQueryable<PersonaListaDto> Proyectar(IQueryable<Persona> origen) =>
        from p in origen
        select new PersonaListaDto
        {
            IdPersona = p.IdPersona, IdOrganizacion = p.IdOrganizacion,
            IdTipoPersona = p.IdTipoPersona, TipoPersona = Db.TiposPersona.Where(t => t.IdTipoPersona == p.IdTipoPersona).Select(t => t.Nombre).FirstOrDefault(),
            IdTipoDocumento = p.IdTipoDocumento, TipoDocumento = Db.TiposDocumento.Where(t => t.IdTipoDocumento == p.IdTipoDocumento).Select(t => t.Codigo).FirstOrDefault(),
            NumeroDocumento = p.NumeroDocumento, Nombres = p.Nombres, Apellidos = p.Apellidos, Correo = p.Correo,
            EmpresaProcedencia = p.EmpresaProcedencia, Observacion = p.Observacion,
            TelefonoPrincipal = Db.PersonasTelefonos.Where(t => t.IdPersona == p.IdPersona && t.Activo).OrderByDescending(t => t.Principal).ThenBy(t => t.IdTelefono)
                .Select(t => t.CodigoPais + " " + t.Numero).FirstOrDefault(),
            TieneFoto = p.IdArchivoFoto != null, Activo = p.Activo, CreadoEn = p.CreadoEn,
        };

    private IQueryable<VehiculoPersonaDto> ProyectarVehiculos(int idPersona) =>
        from pv in Db.PersonasVehiculos
        join v in Db.Vehiculos on pv.IdVehiculo equals v.IdVehiculo
        where pv.IdPersona == idPersona
        orderby pv.Activo descending, v.Placa
        select new VehiculoPersonaDto
        {
            IdVehiculo = v.IdVehiculo, IdPersona = pv.IdPersona, Placa = v.Placa, Tipo = v.Tipo, Marca = v.Marca, Modelo = v.Modelo, Color = v.Color,
            Relacion = pv.Relacion, Activo = pv.Activo,
            OtrasPersonas = Db.PersonasVehiculos.Count(o => o.IdVehiculo == v.IdVehiculo && o.IdPersona != idPersona && o.Activo),
        };

    private Task<VehiculoPersonaDto> VehiculoAsync(int idPersona, int idVehiculo, CancellationToken ct) =>
        ProyectarVehiculos(idPersona).SingleAsync(x => x.IdVehiculo == idVehiculo, ct);

    private async Task<PersonaDetalleDto?> DetalleAsync(int idPersona, CancellationToken ct)
    {
        var p = await Proyectar(Db.Personas.Where(x => x.IdPersona == idPersona)).AsNoTracking().SingleOrDefaultAsync(ct);
        if (p is null) return null;
        return new PersonaDetalleDto
        {
            IdPersona = p.IdPersona, IdOrganizacion = p.IdOrganizacion, IdTipoPersona = p.IdTipoPersona, TipoPersona = p.TipoPersona,
            IdTipoDocumento = p.IdTipoDocumento, TipoDocumento = p.TipoDocumento, NumeroDocumento = p.NumeroDocumento,
            Nombres = p.Nombres, Apellidos = p.Apellidos, Correo = p.Correo, EmpresaProcedencia = p.EmpresaProcedencia, Observacion = p.Observacion,
            TieneFoto = p.TieneFoto, Activo = p.Activo, CreadoEn = p.CreadoEn,
            RegistradoPor = await (from x in Db.Personas.AsNoTracking()
                                   join u in Db.Usuarios on x.IdUsuarioCrea equals u.IdUsuario
                                   where x.IdPersona == idPersona
                                   select u.Correo).SingleOrDefaultAsync(ct),
            Telefonos = await Db.PersonasTelefonos.AsNoTracking().Where(x => x.IdPersona == idPersona).OrderByDescending(x => x.Activo).ThenByDescending(x => x.Principal).ThenBy(x => x.IdTelefono)
                .Select(x => new TelefonoPersonaDto { IdTelefono = x.IdTelefono, IdPersona = x.IdPersona, CodigoPais = x.CodigoPais, Numero = x.Numero, Tipo = x.Tipo, Principal = x.Principal, Activo = x.Activo }).ToListAsync(ct),
            Vehiculos = await ProyectarVehiculos(idPersona).AsNoTracking().ToListAsync(ct),
        };
    }
}
