---
tipo: implementacion
estado: verificado
fuentes: [solicitud del usuario 2026-11-03]
relacionados: [ADR-029, D-49]
actualizado: 2026-11-11
tags: [conecsia, migracion, control-accesos]
---

# Guía de migración al control de accesos unificado

Cómo llevar una base existente al modelo de [[ADR-029 Registro de acceso unificado]] sin perder historial.

## Migraciones (en orden)

| Migración | Qué hace | Reversible |
|---|---|---|
| `20261103163234_RegistroAccesoUnificado` | Esquema aditivo (categorías, plantillas, horario base, zona horaria de la organización, origen del registro, FK movimiento → registro, índices de auditoría) y **traslado de datos** | Sí (`Down` desvincula y borra lo creado; la auditoría inmutable se conserva) |
| `20261104204214_RestriccionesRegistroAccesoUnificado` | Categoriza lo que falte, crea restricciones `NOT VALID` y quita la ruta de menú de Visitas y Autorizaciones | Sí |
| `20261111201544_MovimientosObjetoYResponsablesArea` | Reemplaza `movimiento_vehiculo`/`movimiento_dispositivo` (vacías) por `movimiento_objeto` y añade FK y trazabilidad a `area_responsable` ([[ADR-030 Movimientos de vehículos y dispositivos]]); se detiene si las tablas retiradas tienen filas | Sí (`Down` recrea las tablas vacías) |

## Qué se traslada
1. Tipos de organización faltantes (Empresa, Conjunto residencial, Club, Universidad, Coworking, Parque industrial) y 19 plantillas de categorías.
2. Categorías por organización (plantillas de su tipo + `VISITANTE` y `RECURRENTE`). Si la organización tenía `visitas.permite_sin_autorizacion = true`, `VISITANTE` queda con flujo **Automatico**.
3. Autorizaciones existentes ⇒ categoría por modalidad (Recurrente/Permanente ⇒ `RECURRENTE`; resto ⇒ `VISITANTE`).
4. Cada **visita sin autorización** ⇒ un registro **Único** (`origen = Migracion`, `id_visita_origen`) con su sede, destino, anfitrión, motivo y fecha (esperada o local de creación). Estado: Pendiente ⇒ Pendiente; Autorizada/Ingreso/Finalizada (política) ⇒ Aprobada **sin decisor humano**; Rechazada/Cancelada/Vencida se conservan con motivo. Cupo 1 y consumo según sus entradas confirmadas.
5. Se vinculan la visita y sus movimientos al registro; se agrega una transición inicial y una auditoría `Migrar` (actor `Sistema`).
6. `accesos.permite_permanente` copia el valor de `autorizaciones.permite_permanente`.
7. Bitácora `public.migracion_accesos_adr029`: `Migrado` (visitas trasladadas), `Historico` (objetos sin registro de la persona), `Pendiente` (visitas que no pudieron trasladarse).

## Procedimiento recomendado
1. **Respaldo** de la base (p. ej. copia por `TEMPLATE` o `pg_dump`).
2. Detener la API anterior (sus endpoints de visitas ya no existen en el código nuevo).
3. Aplicar: `dotnet ef database update --project Infrastructure --startup-project ApiConecsia --connection "<cadena>"`.
4. Verificar:
   - `SELECT count(*) FROM visita WHERE id_autorizacion IS NULL;` ⇒ 0
   - `SELECT resultado, count(*) FROM migracion_accesos_adr029 GROUP BY 1;` (revisar cualquier `Pendiente`)
   - Conteo de `visita`, `movimiento_acceso` y `permanencia` igual al de antes.
   - `SELECT count(*) FROM autorizacion WHERE id_categoria_acceso IS NULL;` ⇒ 0
5. Revisar categorías y flujos de cada organización y ajustar parámetros ([[Configuración del control de accesos por tipo de organización]]).
6. Publicar API y frontend nuevos juntos (el frontend ya no llama a los endpoints retirados).

## Verificación realizada
- Copia de la base Docker (2026-11-03): Up → Down → Up sin diferencias; 0 visitas sin registro.
- `MigracionAccesosTests` (datos del esquema anterior): sin pérdida de visitas ni movimientos, política ⇒ Automatico, bitácora y auditoría.
- `conecsia_verificacion` (2026-11-04): migrada; registros visibles en la pantalla unificada.

> [!warning] No aplicado aún
> Ninguna de las dos migraciones se ha aplicado a la base de trabajo `conecsia` ni a la base de Docker. Requiere decisión del usuario.

> [!warning] Contradicción 2026-11-11
> La nota anterior ya no es cierta para `conecsia`: el 2026-11-11 se aplicaron por error ADR-028, ADR-029, sus restricciones y ADR-030, sin respaldo previo. La base Docker sigue sin ellas. Detalle y comprobaciones en [[Estado de implementación]] (incidente del 2026-11-11). **Aplica siempre con `--connection`**: `DesignTimeDbContextFactory` prioriza `DefaultConnection` de `appsettings.Local.json` sobre `CONECSIA_CONNECTION_STRING`.
> *Actualización 2026-11-13:* `conecsia` se revirtió a `20261030132009` (ver [[Estado de implementación]]). La nota original vuelve a ser cierta: ninguna de estas migraciones está aplicada a `conecsia` ni a Docker.

> [!note] Semillas de verificación
> `backend/scripts/seed-verificacion-accesos.sql` reemplaza a `seed-verificacion-visitas.sql` (obsoleto desde ADR-028).
