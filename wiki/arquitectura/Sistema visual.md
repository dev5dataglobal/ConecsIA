---
tipo: arquitectura
estado: implementado
fuentes: [Paleta de colores 2026-10-17, Mockup Figma ConecsIA]
relacionados: [ADR-015, HU-002, HU-013, D-01]
actualizado: 2026-10-17
tags: [conecsia, frontend, diseno]
---

# Sistema visual

Fuentes entregadas por el usuario el 2026-10-17: la paleta oficial (imagen) y el mockup de Figma `ConecsIA` (frame de la pantalla "Personas", 1920 px, generado con Lovable). Aplicado al frontend el mismo día (**Implementado**).

## Paleta y tokens (Tailwind v4, `frontend/src/styles.css`)
| Token | Hex | Uso |
|---|---|---|
| `marca` | `#6953DA` | Botón primario, ítem activo del menú, avatar, página actual |
| `marca-suave` | `#9C8BE7` | Anillo de foco de los campos |
| `marca-tenue` | `#EFECFB` | Fondo de íconos en tarjetas y filas (derivado, no está en la paleta) |
| `marca-oscura` | `#5641C4` | Hover del primario (derivado) |
| `acento` | `#D5F379` | Estado "Activa" (con texto `tinta`) |
| `tinta` | `#131315` | Barra lateral y texto principal |
| `gris` | `#838383` | Bordes e íconos; **no** para texto pequeño (contraste 3,8:1) |
| `tinta-suave` | `#5C5C60` | Texto secundario (derivado para cumplir AA) |
| `superficie` | `#EEEEEC` | Fondo de la aplicación |
| `fondo` | `#FDFDFD` | Tarjetas, cabecera, tabla |
| `linea` | `#E4E4E1` | Divisores (derivado) |

Tipografía: Inter (Google Fonts), igual que el mockup.

## Patrones tomados del mockup
- Barra lateral oscura con marca "ConecsIA / Control de acceso", ítem activo en píldora `marca` y botón "Colapsar menú".
- Cabecera clara con estado de conexión, tarjeta de organización y avatar con iniciales del usuario.
- Página: migas, título y subtítulo, acción primaria a la derecha, tarjetas de resumen con ícono, tarjeta con filtros y tabla (encabezados en mayúsculas pequeñas, avatar por fila, estado en píldora, paginación con la página actual en `marca`).

## Desviaciones deliberadas (no inventar funcionalidad)
> [!note] Solo se muestra lo que existe
> - El menú lista únicamente módulos implementados (Inicio, Sedes); el mockup muestra Dashboard, Personas, Autorizaciones, Visitas, etc.
> - Sin buscador global, selector de sede ni notificaciones: no hay backend. El selector de organización es informativo (multi-membresía: D-03).
> - El estado "Activa" usa el lima de la paleta en vez del verde del mockup; "Restringida" (ámbar) no aplica a sedes.
> - Acciones por fila como botones de ícono (editar, inactivar) en lugar del menú "⋮".
> - Nombre: el mockup y la paleta usan **ConecsIA**; el proyecto figuraba como "ConesIA – DG". Se usa ConecsIA en la UI; confirmar en **D-01**.
