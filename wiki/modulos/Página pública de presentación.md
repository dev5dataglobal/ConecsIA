---
tipo: modulo
estado: implementado
fuentes: [PRD v0.1]
relacionados: [D-47, ADR-015]
actualizado: 2026-10-30
tags: [conecsia, frontend, landing]
---

# Página pública de presentación

Landing comercial sin autenticación que explica ConecsIA y recoge contactos de organizaciones interesadas.

## Estado

| Aspecto | Estado |
|---|---|
| Ruta pública `/presentacion` (fuera de `RutaProtegida`) | **Implementado y verificado** en navegador (escritorio 1024 px y móvil 375 px) |
| Secciones: encabezado fijo/compacto con menú móvil, hero, retos, cómo funciona, servicios, sectores, beneficios, vista de actividad, FAQ, contacto, pie | **Implementado** |
| Validación del formulario (obligatorios, correo, teléfono, sedes, longitudes; `aria-invalid`, foco al primer error) | **Implementado y probado** (Vitest) |
| Envío real del formulario → `POST /api/publico/contactos` | **Implementado y verificado** (2026-10-30): prueba de integración con PostgreSQL y envío real desde el navegador (202, fila insertada) |
| Notificación por correo al equipo comercial / bandeja de consulta | **Pendiente** — D-47 |
| Aviso de privacidad / términos | **Pendiente** — el proyecto no tiene; el formulario no lo muestra |

## Archivos

- `frontend/src/features/presentacion/PresentacionPage.tsx` — composición (ambiente, escena del hero, secciones).
- `frontend/src/features/presentacion/CarruselSectores.tsx` — carrusel «Para quién» con tarjeta central (reemplaza a `Carrusel.tsx`, eliminado).
- `frontend/src/features/presentacion/IlustracionSector.tsx` — 7 ilustraciones SVG propias por sector.
- `frontend/src/features/presentacion/ComparativaRetos.tsx` — sección «El reto» (+ prueba).
- `frontend/src/features/presentacion/RecorridoPasos.tsx` — sección «Cómo funciona» (+ prueba).
- `frontend/src/features/presentacion/ServiciosBento.tsx` — sección «Servicios» (+ prueba).
- `frontend/src/features/presentacion/FormularioContacto.tsx` — formulario (extraído sin cambios de lógica).
- `frontend/src/features/presentacion/contenido.ts` — textos (revisión comercial pendiente).
- `frontend/src/features/presentacion/contacto.ts` — validación y envío.
- `frontend/src/features/presentacion/presentacion.css` — estilos `lp-*`, misma paleta que el ingreso.
- `frontend/src/app/rutas.tsx` — ruta pública.
- `frontend/src/features/presentacion/PresentacionPage.test.tsx` — 4 pruebas.
- `backend/ConecsIA.Tests/ContactoTests.cs` — integración: 202 y persistencia, 400 por campo, catálogo, trampa, 429.

## Envío del formulario

**Implementado.** `ContactoController` (`backend/ApiConecsia/Controllers/ContactoController.cs`), anónimo (`[AllowAnonymous]`), responde **202 sin cuerpo**.

| Regla | Detalle |
|---|---|
| Validación servidor | DataAnnotations en `SolicitudContactoEntrada` (`Application/DTOs/ContactoDto.cs`): obligatorios, correo, teléfono, sedes 1–9999, longitudes; tipo de organización contra catálogo cerrado ⇒ 400 `ValidationProblemDetails` |
| Límite | Política `contacto`: 5 envíos por IP y hora (además del global 60/min) ⇒ 429 |
| Anti-bot | Campo trampa `sitioWeb` oculto; si llega con valor ⇒ 202 sin guardar |
| Persistencia | Tabla `solicitud_contacto` (migración `20261030132009_SolicitudesContacto`), **sin `id_organizacion`**: no es dato de ningún tenant; correo normalizado en minúsculas; `estado` = `Nueva` |
| Logs | Solo el id de la solicitud (ADR-018) |
| Frontend | `contacto.ts` usa `${VITE_API_BASE_URL}/api/publico/contactos` (fetch anónimo, sin token). 400/429/red muestran «no se ha enviado» y conservan los datos; éxito solo con 202 |

> [!todo] Pendiente (D-47)
> No hay pantalla ni endpoint para **consultar** las solicitudes ni notificación por correo; hoy solo se leen en la base. Falta definir quién las atiende, retención de los datos y el aviso de privacidad.

## Contenido

- Solo capacidades presentes en el código: personas, sedes/áreas/zonas/puntos, autorizaciones (única, diaria, temporal, recurrente, permanente según política), visitas, entradas/salidas, personas dentro, historial, multi-organización y roles.
- Vehículos y dispositivos se marcan como **módulo opcional**.
- QR: solo aparece un «pase de ingreso · ejemplo» ilustrativo; no se anuncia lectura de QR (solo hay CRUD de `TokenQr`).
- Toda actividad mostrada usa nombres «de ejemplo» y la leyenda «Datos de ejemplo».
- Sin cifras, clientes, certificaciones ni integraciones.

## Limitaciones

- `restaurarSesion()` llama a la API al cargar cualquier ruta; sin API aparece un 502 en consola, sin efecto en la página.
- `/` muestra la presentación a visitantes sin sesión y redirige a `/dashboard` con sesión (2026-10-30). `/presentacion` sigue disponible siempre.

## Rediseño visual (2026-10-30)

**Implementado y verificado** en navegador (800 px, tableta 768 px, móvil 375 px; sin scroll horizontal, sin errores de consola).

| Elemento | Detalle |
|---|---|
| Fondo | Capa fija con 3 orbes difuminados (transform), trama de puntos con máscara y red SVG de 10 nodos que titilan |
| Bandas | Cada sección con gradiente radial distinto (`lp-banda-a/b/c`) y separador luminoso |
| Hero | Escena en 3 capas con parallax por puntero (solo ratón): flujo Solicitada → Aprobada → Vigente → Entrada registrada que avanza cada 2,2 s (estado por icono y peso, no solo color), invitación con QR decorativo y actividad; trazos animados entre capas; sello «Datos de ejemplo» y `role="img"` con descripción |
| Reto | Comparativa «Hoy → Con ConecsIA» (ver «Sección El reto v2») |
| Cómo funciona | Recorrido conectado (ver «Cómo funciona v2») |
| Servicios | Bento asimétrico (ver «Servicios v2») |
| Mensaje destacado | «Aprobada no siempre significa vigente» |
| Para quién | **Carrusel con tarjeta central** (ver abajo) |
| Beneficios | Lista en dos columnas con beneficio principal destacado |
| Producto | Ventana de aplicación ficticia: persona, destino, zonas, vigencia, autorizador y actividad |
| Movimiento reducido | `prefers-reduced-motion`: sin animaciones ni parallax; el flujo se muestra completo |

> [!note] Sin skill de UI
> El pedido mencionaba «la skill de UI del proyecto»; no existe ninguna en `.claude/`. Se siguió la identidad del ingreso.

> [!todo] Pendiente
> - Las pruebas del carrusel son manuales (jsdom no desplaza); falta una prueba e2e.
> - Revisión comercial de los ejemplos por sector y del mensaje destacado.

## Carrusel «Para quién» v2 (2026-10-30)

**Implementado y verificado** (navegador 800 px y 375 px; 2 pruebas Vitest en `CarruselSectores.test.tsx`).

| Aspecto | Detalle |
|---|---|
| Composición | Tarjeta activa al centro (borde y halo del color de acento, insignia «Seleccionado», tamaño mayor); vecinas ±1 inclinadas y atenuadas, ±2 tenues; resto ocultas |
| Ilustraciones | SVG propios por sector (escenas nocturnas: torres, conjunto con portería, campus, club con piscina y cancha, coworking, bodegas con camión genérico, edificio con torniquete). Sin fotos, marcas ni datos reales; sin descargas |
| Acentos | Violeta, lima y cian por sector (`acento` en `SECTORES`) |
| Fondo | Halos de color, dos arcos finos y trama de puntos (`lp-banda-sectores`) |
| Navegación | Ciclo continuo (anterior desde el primero ⇒ último); botones circulares, barra de progreso por indicadores, contador `n / 7`, flechas/Inicio/Fin en el escenario enfocable; clic en tarjeta lateral la activa |
| Táctil | Deslizar horizontal > 40 px cambia; gesto vertical se ignora (`touch-action: pan-y`) |
| Accesibilidad | Solo la tarjeta activa expuesta (`aria-hidden` en el resto); `aria-live` con «Nombre, n de 7»; activo indicado por insignia de texto, tamaño y posición, no solo color |
| Movimiento | Transiciones `transform`/`opacity`; sin avance automático; `prefers-reduced-motion` anula transiciones (regla global, no emulada en navegador) |

> [!todo] Antes de publicar
> Si comercial prefiere fotografías (como el mockup), reemplazar las ilustraciones SVG por imágenes con licencia, optimizadas (WebP/AVIF) y alojadas localmente.

## Sección «El reto» v2 (2026-10-30)

**Implementado y verificado** (navegador 800 px y 375 px; `ComparativaRetos.test.tsx`).

- Mensaje a la izquierda (fijo al desplazarse en escritorio): píldora «El reto», título con «memoria de la portería» en violeta, subtítulo e ilustración de fichas (documento, mensaje, persona) sobre una plataforma con trazos.
- Comparativa a la derecha: encabezados «Hoy» / «Con ConecsIA» con línea; problemas en bloques sobrios con icono; flecha degradada; soluciones con check lima, fondo iluminado y hover (elevación + halo).
- Aparición escalonada por fila: problema → flecha que se traza → solución. Sin movimiento continuo.
- Móvil: cada par apilado con flecha hacia abajo y etiquetas visibles «Hoy» / «Con ConecsIA» por bloque; sin desbordamiento.
- Accesibilidad: cada par es un `li`; las etiquetas por bloque se leen siempre (ocultas visualmente en escritorio). `prefers-reduced-motion` muestra todo sin transiciones.
- `Icono.tsx`: nuevos trazos `documento` y `mensaje` (compartidos).

## «Cómo funciona» v2 (2026-10-30)

**Implementado y verificado**: geometría y pruebas automáticas; revisión visual confirmada por el usuario (2026-10-30).

- Título centrado con «cinco pasos» en lima; fondo con halos violeta/lima/cian y trama.
- Escritorio (> 960 px): trayectoria SVG ondulada con degradado violeta → cian → lima que se dibuja al entrar; nodos 01–05 con halo sobre la onda (medido: centro del nodo = y% de la altura del trayecto); chevrones entre nodos; tarjetas alineadas, cada una con composición de iconos (personas+sede, autorización+documento, documento+check, acceso+perfil, reportes+reloj) y barra de color.
- ≤ 960 px: secuencia vertical con nodo a la izquierda y conector degradado entre pasos; ≤ 480 px la ilustración pasa encima del texto. Medido a 375 px: sin desbordamiento.
- Aparición: trayectoria → nodos escalonados → tarjetas; hover con elevación, borde iluminado e iconos que se desplazan. Movimiento reducido: todo visible sin transiciones.
- Accesibilidad: lista ordenada; cada título lleva «Paso n de 5» oculto; decoración con `aria-hidden`. Sin elementos interactivos nuevos.
- `Icono.tsx`: nuevo trazo `reloj`.


## «Servicios» v2 (2026-10-30)

**Implementado y verificado** (capturas a 1280 px y 800 px; medición a 768 y 375 px sin desbordamiento; `ServiciosBento.test.tsx`).

- Cuadrícula de 12 columnas: fila 1 personas (3) · sedes (5) · autorizaciones (4); fila 2 visitas (3) · **entradas y salidas** (6, borde y halo lima, sello «Ejemplo ilustrativo») · quién está dentro (3); fila 3 historial · multi-organización · vehículos (4 c/u). Tableta: 2 columnas con la tarjeta principal a ancho completo; móvil: 1 columna.
- Vistas ilustrativas con etiquetas genéricas y barras neutras en lugar de valores: buscador por documento con filas «Registrada», jerarquía Sede → Área → Zona → Punto de acceso, eventos «Entrada/Salida registrada» con operador genérico y torniquete esquemático, permanencias por sede sin cifras, línea de tiempo Solicitud creada → Autorización aprobada → Entrada registrada, organización actual + permisos del rol (Consultar/Añadir/Editar, como el sistema de acciones), vehículo y dispositivo autorizados. Todas `aria-hidden`, más una nota visible «Las vistas… son ilustrativas».
- Se descartaron del mockup nombres, documentos, fechas, placas, operadores y conteos.
- Hover: elevación, borde e icono con halo; aparición escalonada; `prefers-reduced-motion` respetado por la regla global.

## Contacto y pie v2 (2026-10-30)

**Implementado y verificado** (capturas 1280 px y 375 px; medición sin desbordamiento; 11 pruebas Vitest).

- Contacto: píldora «Contacto» con punto cian, título con «gestionar los accesos» en degradado violeta→cian, pasos 1–3 con anillos violeta/cian/lima unidos por conector, ilustración decorativa (ficha de persona y burbuja de mensaje en órbita; oculta en móvil). Fondo con halos y arcos violetas y trama.
- Formulario: **fondo blanco conservado**, borde y halo violeta, selector con flecha propia, botón violeta. Placeholders neutros (sin datos ficticios). Lógica, validación, endpoint `POST /api/publico/contactos` y estados (enviando, éxito solo con 202, errores 400/429/red) **sin cambios**.
- Pie: `Marca` con «IA» en violeta (prop `iaResaltada`), descripción, enlaces de sección, separador e «Iniciar sesión» en violeta; línea legal «© año ConecsIA | Elaborado por DataGlobal» (DataGlobal en cian, sin logo).
- El usuario retiró la nota «vistas ilustrativas» de Servicios; la prueba ahora verifica el sello «Ejemplo ilustrativo».
