---
tipo: adr
estado: propuesta
area: seguridad
origen: PRD §7.1
fuentes: [PRD v0.1]
relacionados: [RF-15, CA-08]
actualizado: 2026-10-17
tags: [conecsia, adr, seguridad]
---

# ADR-010 Token QR opaco

> [!warning] Estado: **Propuesta** — no aprobada. No implementar como decisión firme hasta su aprobación.
> Área: seguridad · Origen: PRD §7.1

## Contexto
El QR identifica una autorización sin datos personales y no sustituye la validación en servidor.

## Decisión propuesta
- Token aleatorio opaco de alta entropía (≥128 bits, CSPRNG) codificado en el QR; sin datos personales ni IDs secuenciales.
- En base de datos se guarda solo un hash del token, asociado a la autorización y organización.
- Cada lectura valida en servidor existencia, estado, vigencia, lugar y cupo; la confirmación de entrada es un comando aparte.
- Revocar/cancelar la autorización invalida el token; se permite rotarlo (emitir nuevo).
- Lecturas y reutilizaciones no permitidas se auditan y pueden generar alerta.
- Tokens nunca en logs.

## Alternativas consideradas
Token firmado autocontenido (JWT): permite validación offline, que no se requiere, y complica la revocación.

## Consecuencias
Resolver el token requiere conocer la organización a partir del hash; índice único global sobre el hash.

## Pendientes
Caducidad y rotación del token; lectores compatibles (D-13).


## Revisión 2026-10-17
- [x] Cumple RF-15 y CA-08: sin datos personales y con validación en servidor.
- [ ] **Precisión:** la búsqueda por hash es global, pero si la organización del token no coincide con el contexto verificado del operador, la respuesta es "no encontrado" y se audita el intento.
- [ ] **Precisión:** el hash es SHA-256 del token. Basta porque el token es aleatorio de ≥128 bits; no hace falta un hash lento como el de las contraseñas.
- [ ] **Por decidir:** contenido del QR. Recomendación: solo el token, que el lector de recepción interpreta. Alternativa: una URL, que abriría una página web si el visitante escanea con su celular.
- [ ] **Precisión:** el token deja de ser útil cuando la autorización deja de estar vigente. Se rota al reenviar una invitación o si se sospecha filtración. El consumo de autorizaciones únicas lo protege [[ADR-007 Concurrencia e idempotencia en accesos]].
- Sin bloqueos de negocio.
