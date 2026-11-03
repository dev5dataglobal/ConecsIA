---
tipo: modulo
estado: obsoleto
fuentes: [PRD v0.1]
relacionados: [RF-05, HU-015, ADR-027, D-34, D-43, D-44, D-45]
actualizado: 2026-10-31
tags: [conecsia]
---

# Zonas y puntos de acceso

> [!warning] Contradicción / cambio 2026-10-31 — [[ADR-028 Zona empresarial por sede y retiro de zonas de acceso]]
> Antes (ADR-027, 2026-10-28): Sede → Zona → Punto de acceso; autorizaciones restringibles por zona (`autorizacion_zona`, `autorizaciones.exige_zonas`).
> Ahora (solicitud del usuario 2026-10-31, **Implementado**): se retiran las zonas de acceso. El punto de acceso pertenece directamente a la sede y puede marcarse común de la zona empresarial; la autorización vale en toda su sede. El contenido siguiente se conserva como historia.
> Página vigente: [[Puntos de acceso]].

Estructura: Organización → [[Sede]] (ubicación) → [[Zona y punto de acceso|Zona]] → Punto de acceso. Modelo en [[ADR-027 Punto de acceso como entidad dentro de la zona]].

## Administración — **Implementado 2026-10-28**
- Pestaña **Administración › Zonas y puntos de acceso** (`/administracion/zonas-acceso`), vistas Zonas / Puntos de acceso.
- Consultar, crear, editar, activar/inactivar y **eliminar solo sin historial**. Zona con puntos, autorizaciones o movimientos ⇒ 409, se inactiva. Punto con movimientos u operadores asignados ⇒ 409.
- Filtros: búsqueda, sede, zona (puntos) y estado. La organización es siempre la activa (el administrador de plataforma la elige en el selector del shell); el cliente nunca la envía.
- Las tarjetas muestran quién creó y modificó cada registro y cuándo; la auditoría guarda valores anterior y nuevo (`zona.*`, `punto_acceso.*`).
- Reglas: zona solo en sede activa; punto solo en zona activa; un punto cambia de zona solo dentro de su sede; una zona con historial no cambia de sede; activar exige el padre activo.
- API: `GET/POST /api/zonas`, `GET/PUT/DELETE /api/zonas/{id}`, `POST /api/zonas/{id}/activar|inactivar`; ídem `/api/puntos-acceso`.

## Permisos — **Propuesta (D-43)**
Módulo `sedes` (estructura física): Consultar = ver; Añadir = crear; Editar = editar, activar/inactivar y eliminar. Alcance por sede de la membresía ([[Decisiones pendientes]] D-34). Se retiró el `ZonaController` genérico, que aceptaba la entidad sin validar.

## Integración con autorizaciones — **Implementado**
- La solicitud ([[Control de accesos]]) ofrece solo zonas activas de la sede; el servidor valida organización, sede y estado.
- Sin zonas = sin restricción. Parámetro de organización `autorizaciones.exige_zonas = true` ⇒ al menos una zona obligatoria (Propuesta, D-44; sin pantalla para configurarlo).
- `PUT /api/autorizaciones-acceso/{id}/zonas` (Control de accesos · Editar) reemplaza las zonas **solo mientras está Pendiente**; decidida ⇒ 409. En la UI: «Editar zonas» en el detalle.
- Pendiente o rechazada nunca habilitan ingreso (regla previa de [[Visitas]], verificada de nuevo).

## Registro de movimientos — **Implementado**
- Entrada y salida de [[Visitas]] exigen un punto **de la sede de la visita**, activo y con zona activa; los operadores con `membresia_punto_acceso` solo usan sus puntos.
- **Entrada:** si la autorización tiene zonas, la zona del punto debe estar entre ellas (409 «zonas autorizadas»).
- **Salida:** no se valida la zona (D-44): no se retiene a una persona dentro.
- El movimiento guarda `id_punto_acceso` y `id_zona`; el detalle de la visita muestra «Punto (Zona)» aunque luego se mueva o inactive.

## Verificación
- Integración (PostgreSQL real): `ZonasPuntosAccesoTests` (6) + suites existentes adaptadas (Visitas, Autorizaciones de acceso): 100/100 en verde el 2026-10-28.
- Frontend: `ZonasAccesoSeccion.test.tsx` (3) y suites existentes: 41/41.
- **Migración aplicada 2026-10-29** a `conecsia` y a la base desechable `conecsia_verificacion` (seeds de verificación).
- **Verificado en navegador 2026-10-29** (API Release en :5192 contra `conecsia_verificacion`, frontend modo verificación en :5174, usuario `admin.alfa@verificacion.test`): listado de zonas y puntos con sede/zona/modo; duplicado « PARQUEADERO » rechazado (409); zona nueva creada con «Creado … por»; eliminar visible solo sin historial. No se probaron en navegador la solicitud con zonas ni la entrada por punto (cubiertas por integración).

> [!todo] Pendiente
> D-43 (permisos), D-44 (política y salida), D-45 (punto multi-zona y dispositivos); pantalla para asignar operadores a puntos (HU-018).
