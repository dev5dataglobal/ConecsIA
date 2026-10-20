# ConesIA – DG — Product Requirements Document

**Producto:** Plataforma SaaS de gestión de visitantes y control de accesos  
**Nombre de trabajo:** ConesIA – DG; denominación comercial por confirmar  
**Versión:** 0.1 — propuesta funcional y técnica para revisión  
**Fecha:** 17 de octubre de 2026  
**Estado:** Requisitos fuente consolidados; alcance de entregas y decisiones complementarias pendientes de validación  
**Stack confirmado:** backend .NET; frontend React, TypeScript y Tailwind CSS; base de datos PostgreSQL.

## 1. Resumen ejecutivo

La plataforma permitirá gestionar quién puede ingresar a una instalación, a qué destino se dirige, quién autorizó el ingreso, durante qué fechas y horarios, cuándo entró, cuándo salió y cuál es su historial. Operará como un SaaS multi-organización con aislamiento de información y configuración por cliente.

El núcleo será común para empresas, conjuntos residenciales, clubes, instituciones educativas, coworkings, parques industriales y otras organizaciones. La adaptación sectorial se resolverá mediante catálogos, módulos, políticas y terminología configurables, conservando las mismas reglas centrales de autorización, acceso, permanencia y auditoría.

El producto tendrá tres espacios funcionales: administración SaaS, administración y autorización de cada organización, y operación de recepción/portería. Los perfiles de consulta accederán a históricos, indicadores y reportes según sus permisos. Una persona visitante no necesita necesariamente una cuenta de usuario.

El flujo principal será identificar a la persona, recuperar o solicitar su autorización, validar las condiciones de acceso, registrar la entrada, consultar su permanencia y registrar la salida. Vehículos y equipos externos se integrarán a ese flujo cuando el módulo esté habilitado.

Este PRD organiza la implementación; no acredita que exista código, infraestructura, integración ni funcionalidad ya desarrollada.

## 2. Fuentes, alcance y precedencia

### 2.1 Fuentes

| Fuente | Uso en este documento |
|---|---|
| `Texto pegado.txt` | Fuente funcional principal: RF-01 a RF-26, RN-01 a RN-11 y requisitos no funcionales |
| `Markdown pegado (2).md` | Referencia de estructura del PRD de DataTicket; no aporta reglas de negocio a este producto |
| Instrucción actual | Define .NET, React, TypeScript, Tailwind y PostgreSQL |

Las instrucciones explícitas del responsable del producto prevalecen sobre propuestas técnicas. Los requisitos fuente se mantienen identificados para evitar pérdidas de alcance. Las decisiones específicas de DataTicket —chat de tickets, cierre telefónico, responsables nominados, archivos públicos, versiones y calendario de trabajo— no se trasladan a este proyecto.

### 2.2 Clasificación de definiciones

- **Requisito base:** exigencia contenida en los requisitos o en la instrucción actual.
- **Propuesta:** regla complementaria o decisión técnica planteada por este PRD, pendiente de validación.
- **Pendiente:** definición necesaria para cerrar un comportamiento o preparar producción.

Las fases y el MVP son una propuesta de secuenciación: no eliminan requisitos del producto completo. Ninguna estimación de tiempo, volumen, costo o SLA se considera aprobada.

### 2.3 Terminología de trabajo

Se propone usar **Sede** para la ubicación física y **Área/destino** para la unidad receptora. En el documento fuente aparecen como ubicación y unidad. La interfaz podrá mostrar términos sectoriales como campus, apartamento, oficina o dependencia.

| Concepto | Definición |
|---|---|
| Organización | Cliente y frontera de aislamiento de datos |
| Sede | Instalación física perteneciente a una organización |
| Área/destino | Unidad organizacional o destino de la visita dentro de una sede |
| Zona | Sector físico sujeto a permisos; puede representar un punto de acceso |
| Punto de acceso | Zona habilitada para registrar entradas o salidas |
| Persona | Individuo identificado en el ámbito de una organización |
| Usuario | Identidad autenticada con membresías, roles y permisos |
| Responsable | Persona receptora de la visita |
| Autorizador | Persona facultada para decidir el ingreso |
| Autorización | Permiso que establece condiciones para uno o varios ingresos |
| Visita | Episodio operativo asociado a un propósito y destino |
| Permanencia | Intervalo abierto por una entrada y cerrado por su salida |
| Movimiento | Evento de entrada o salida de persona, vehículo o dispositivo |

**Propuesta de modelado:** una sola entidad Zona con `esPuntoAcceso`; cuando sea verdadero podrá configurar entrada, salida o ambas. El destino organizacional y la zona física seguirán siendo conceptos distintos.

## 3. Problema y oportunidad

El registro disperso de visitantes dificulta comprobar autorizaciones vigentes, conocer quién está dentro y reconstruir responsabilidades. Repetir el registro de una misma persona aumenta tiempos de atención y errores. La falta de seguimiento de vehículos y equipos puede generar movimientos sin soporte.

La oportunidad consiste en ofrecer una operación centralizada, trazable y adaptable a varios sectores, evitando construir un sistema independiente para cada organización. El valor inicial será reducir pasos en recepción y asegurar que cada ingreso tenga identidad, contexto, condiciones verificadas y seguimiento hasta su salida.

## 4. Objetivos y señales de éxito

### 4.1 Objetivos

1. Centralizar organizaciones, sedes, destinos, personas, autorizaciones y accesos.
2. Controlar fecha, horario, lugar y condiciones de cada ingreso.
3. Identificar al creador, autorizador, revocador y operador de cada acción.
4. Recuperar personas existentes para reducir duplicados y tiempo de atención.
5. Consultar personas y, cuando aplique, equipos actualmente dentro.
6. Configurar módulos y vocabulario por organización conservando el núcleo común.
7. Exponer una API segura para futuras integraciones.

### 4.2 Métricas a instrumentar

| Métrica | Definición |
|---|---|
| Tiempo de atención de ingreso | Desde inicio de identificación en recepción hasta confirmación de entrada; distinguir captura manual y QR |
| Tiempo de decisión | Desde solicitud pendiente hasta aprobación/rechazo |
| Entradas y salidas | Movimientos confirmados en el periodo local seleccionado |
| Personas dentro | Permanencias abiertas por sede al momento de consulta |
| Permanencia | Salida menos entrada; para abiertas, hora actual menos entrada |
| Intentos denegados | Cantidad por motivo, sede y punto de acceso |
| Pendientes de autorización | Solicitudes que aún no tienen decisión |
| Reutilización de registro | Visitas atendidas recuperando una persona existente |
| Notificaciones fallidas | Entregas fallidas o agotadas tras reintentos |
| Latencia operativa | p50/p95 de consulta por documento, validación QR, entrada y salida |

Las metas se fijarán con el piloto. La fuente no define concurrencia, latencias máximas, disponibilidad ni objetivos de recuperación. Las fechas de indicadores se interpretarán en la zona horaria de la sede; se propone guardar instantes en UTC y conservar la zona horaria aplicable.

## 5. Usuarios, organizaciones y permisos

### 5.1 Perfiles iniciales

| Perfil | Responsabilidad y alcance |
|---|---|
| Superadministrador SaaS | Organizaciones, tipos, planes, módulos y configuración global |
| Administrador de organización | Configuración, sedes, destinos, usuarios, roles y parámetros del cliente |
| Administrador de sede | Gestión de las sedes asignadas según permisos |
| Operador de acceso | Identificación, solicitudes y entradas/salidas en puntos autorizados |
| Autorizador | Creación y decisión de autorizaciones sobre destinos asignados |
| Auditor | Consulta de movimientos, autorizaciones, reportes y auditoría habilitados |

Estos perfiles serán plantillas iniciales. Cada organización podrá crear roles adicionales. Un rol no sustituye el alcance territorial ni la relación con el destino.

### 5.2 Reglas de autorización

1. Validar en backend identidad autenticada, organización activa, membresía vigente, permiso y sede/destino autorizado en cada operación.
2. Derivar el contexto de organización de una membresía verificada; no confiar en un identificador enviado libremente por el navegador.
3. Aplicar el mismo aislamiento a API, búsquedas, archivos, exportaciones, eventos y procesos en segundo plano.
4. Separar permisos de consultar, crear, modificar, inactivar, decidir, revocar, registrar entrada/salida, exportar y administrar.
5. Conservar la autoría aunque el usuario o su membresía se inactiven.
6. **Propuesta:** el superadministrador gestiona el plano SaaS sin acceso ordinario al detalle personal de los clientes. Cualquier acceso de soporte deberá tener alcance explícito y auditoría.
7. **Pendiente:** definir si una identidad puede pertenecer a varias organizaciones. El modelo propuesto lo permite con selección de contexto y roles independientes.
8. **Propuesta:** alta de usuarios por invitación administrativa, sin registro público inicial. Restablecimiento seguro de credenciales y desactivación de sesiones al retirar acceso.

### 5.3 Configuración organizacional

RF-01 a RF-06 y RF-26 incluyen alta e inactivación de organizaciones, tipo, información general, plan, módulos, parámetros, sedes, áreas, responsables, zonas, usuarios y permisos.

El tipo de organización precargará catálogos y vocabulario, que el cliente podrá ajustar. Deshabilitar un módulo impedirá nuevas operaciones de ese módulo; se propone conservar consulta histórica a usuarios autorizados. Cambiar una plantilla no deberá sobrescribir configuraciones ya personalizadas.

Los planes contemplan asociación y habilitación de módulos. Cobro, facturación, pasarelas, límites comerciales y suspensión por impago no están definidos y se difieren.

## 6. Flujo funcional de visitas y accesos

### 6.1 Personas y tipos — RF-07 y RF-08

El registro de personas incluirá tipo y número de documento, nombres, apellidos, teléfono, correo, empresa de procedencia cuando aplique, tipo de persona, fotografía opcional, observaciones y estado.

La búsqueda por documento recuperará el registro existente dentro de la organización. Se propone normalizar espacios externos y usar una clave única por organización, tipo y documento normalizado; se deberá definir país emisor u otra dimensión cuando existan documentos potencialmente coincidentes. No se eliminarán ceros iniciales ni caracteres significativos sin una regla explícita.

“Centralizado” se interpreta como un catálogo compartido entre sedes de una organización. No supone compartir personas, fotografías ni históricos entre clientes SaaS. La identidad de usuario y la persona visitante serán entidades diferentes, vinculables cuando corresponda.

Los tipos serán configurables e inactivables sin borrar históricos: visitante, proveedor, contratista, cliente, técnico, domiciliario, residente, socio, familiar y otros. Frecuencia de visita y tipo de persona podrán distinguirse para evitar duplicar categorías innecesariamente.

### 6.2 Creación de autorizaciones — RF-09, RF-10 y RF-20

Cada autorización definirá persona, organización, sede, área/destino, zonas permitidas si aplica, fechas, horario, días, motivo, responsable, autorizador y estado. Conservará creador, decisión, fecha/hora, observaciones y revocación posterior.

| Modalidad | Regla base | Complemento propuesto o pendiente |
|---|---|---|
| Única | Un ingreso | Consumo atómico al confirmar la entrada; lectura QR sin ingreso no consume |
| Diaria | Válida un día | Definir si permite varios reingresos dentro del día |
| Temporal | Intervalo de fechas | Definir cantidad de ingresos y horarios aplicables |
| Recurrente | Días y horarios determinados | Múltiples accesos; validar en cada entrada |
| Permanente | Sin fecha final si la política lo permite | Mantener revisión de estado, sede y demás restricciones |

Se propone separar la modalidad del cupo de ingresos para evitar interpretar “diaria” como permiso ilimitado. Mientras se valida, no se habilitarán reingresos por inferencia.

Una autorización emitida directamente por un usuario facultado podrá quedar aprobada; una solicitud que requiera decisión permanecerá pendiente. El operador no tendrá permiso de aprobación por el solo hecho de registrar personas.

### 6.3 Visita ocasional — RF-11

1. El operador identifica o registra a la persona.
2. Selecciona sede, destino, responsable y motivo.
3. El sistema registra la visita y solicita autorización al responsable facultado.
4. Una decisión válida habilita el proceso de entrada; rechazo, cancelación o vencimiento impiden usar esa solicitud.
5. El operador confirma el ingreso únicamente después de validar las condiciones vigentes.
6. La salida finaliza la visita y cierra la permanencia.

Cuando una política permita ingreso sin autorización, se registrará explícitamente la política aplicada, persona, operador y destino. No se fabricará una aprobación atribuida al responsable.

### 6.4 Preautorización e invitación — RF-12

Un usuario facultado registrará visitante, fecha/horario esperado, sede, destino, responsable y motivo. El sistema generará la invitación y, cuando corresponda, QR. La entrega de invitación no equivale a entrada ni sustituye la verificación de identidad.

Al llegar se reutilizará la visita esperada cuando coincida con la autorización. Las modalidades recurrentes podrán generar episodios de visita independientes para cada permanencia.

### 6.5 Entrada — RF-13

El servidor comprobará identidad, estado de persona, política de autorización, vigencia, fecha, horario, sede, destino/zonas y estado de autorización. También verificará el permiso y el punto asignado al operador.

Al confirmar almacenará persona, instante, sede, punto de acceso, autorización cuando corresponda y operador. Se propone registrar visita y permanencia como parte de la misma transacción. La respuesta indicará éxito confirmado o un motivo concreto de denegación.

**Propuesta de integridad:** impedir dos permanencias abiertas para la misma persona en la misma sede. Dos operadores que intenten ingresar simultáneamente a la persona no podrán generar entradas duplicadas. La política para presencia simultánea en sedes distintas queda pendiente.

### 6.6 Salida — RF-14

La salida identificará persona, permanencia abierta, instante, sede, punto de salida y operador. Cerrará únicamente el ingreso correspondiente. Un reintento no generará otra salida ni duración negativa.

**Propuesta:** el vencimiento o revocación posterior de la autorización no impedirá registrar la salida de alguien que ya se encuentra dentro. Se conservará el estado histórico de las condiciones del ingreso.

Si no existe entrada abierta, se mostrará una inconsistencia. Un eventual mecanismo de regularización requerirá permiso específico, motivo y auditoría, sin reescribir silenciosamente los eventos originales.

### 6.7 Estados y transiciones

Se propone separar tres ciclos para que una autorización recurrente no se cierre al terminar una sola visita.

| Entidad | Estados propuestos | Transiciones y restricciones |
|---|---|---|
| Solicitud/autorización | Pendiente, Aprobada, Rechazada, Cancelada, Revocada, Vencida, Agotada | Pendiente se aprueba/rechaza/cancela o vence. Aprobada se revoca/cancela, vence o agota su cupo. Los estados terminales no habilitan nuevos ingresos. |
| Visita | Pendiente, Autorizada, Ingresó, Finalizada, Rechazada, Cancelada, Vencida | Flujo fuente: Pendiente → Autorizada → Ingresó → Finalizada. Rechazo/cancelación/vencimiento aplican antes del ingreso; después se conserva la permanencia hasta su cierre. |
| Permanencia | Abierta, Cerrada | Entrada confirmada abre; salida confirmada cierra. |
| Alerta | Nueva, Revisada, Cerrada | Conserva actor y fecha de cada transición. |

“Vigente” será una evaluación de estado aprobado, fecha, horario, días, lugar y cupo; no será un sinónimo de aprobado. Una autorización aprobada para mañana todavía no permite entrar hoy. Toda transición se validará en backend.

**Pendiente:** matriz exacta de quién puede cancelar, revocar o corregir y si existen flujos de aprobación de varios niveles. No se presupone aprobación múltiple.

## 7. Identificación QR y actualización operativa

### 7.1 QR — RF-15

El QR identificará la autorización sin contener directamente datos personales sensibles. Se propone utilizar un token opaco de alta entropía asociado al registro; el servidor validará existencia, estado, vigencia, fecha, horario, sede, destino, restricciones y cupo en cada intento.

La lectura ofrecerá validación y contexto autorizado al operador. La confirmación de entrada será una operación explícita. Un QR cancelado, vencido o revocado se rechazará aunque ya se hubiera mostrado en una pantalla o correo.

La autorización única deberá resistir dos escaneos simultáneos: solo una entrada podrá confirmarse. No se asumirán validez offline ni QR como prueba suficiente de identidad. Generación, rotación, caducidad y revocación del token se definirán en el diseño de seguridad.

### 7.2 Actualización de presencia — RF-19 y RF-23

La fuente exige información operativa en tiempo real. Se propone API para consultas persistidas y SignalR para comunicar cambios confirmados de entradas/salidas, pendientes y alertas. Este canal no es un chat.

Los eventos se publicarán después de confirmar la transacción. Cada conexión y suscripción respetará organización, sedes y permisos vigentes. Al reconectar se recuperará el estado desde la API; no se dependerá de haber recibido todos los eventos.

La pérdida de conexión deberá ser visible y no mostrará una entrada como guardada sin confirmación del servidor. El presupuesto máximo de actualización de pantalla se fijará antes del piloto.

## 8. Vehículos, equipos y archivos

### 8.1 Vehículos — RF-16

El módulo configurable registrará placa, tipo, marca, modelo opcional, color y propietario/responsable. Permitirá asociarlos con personas y autorizaciones, y registrar qué vehículo entró y salió.

Cada movimiento vehicular se vinculará a una visita y a su responsable. Se deberá definir placa normalizada y ámbito de unicidad. **Pendiente:** acompañantes, cambio de conductor, cupos de parqueadero y reglas de salida independiente; no forman parte de una lógica aprobada.

### 8.2 Dispositivos y equipos externos — RF-17

Permitirá propietario/responsable, tipo parametrizable, código/serial/identificador, marca, modelo, color, descripción/observaciones y estado. Una persona y una autorización admitirán múltiples dispositivos.

Cada movimiento conservará dispositivo, instante, entrada/salida, sede, zona como punto de acceso, responsable, visita y usuario operador. El historial permitirá identificar qué equipos siguen dentro. La salida de la persona no cerrará automáticamente los equipos sin verificación.

**Propuesta:** advertir al operador cuando una persona salga con equipos pendientes y permitir registrar la novedad con permiso y motivo. Si la organización desea bloqueo, salida con otro responsable o transferencia de custodia, deberá definir y aprobar la política; no se impone ese comportamiento desde este PRD.

### 8.3 Fotografías y documentación

La fotografía de persona es opcional. La documentación de contratistas aparece como posible capacidad de la vertical empresa; el documento no define tipos, vigencia, revisión o su efecto en el acceso. Se conserva como capacidad a especificar antes de su implementación.

**Propuesta:** almacenamiento de binarios en un servicio privado detrás de una interfaz de archivos; PostgreSQL conservará metadatos y referencias. Descargas y cargas comprobarán organización y permisos. No se hereda el uso de URLs públicas del PRD de referencia.

Proveedor, tipos permitidos, tamaños, controles de contenido, conservación y eliminación quedan pendientes. La carga de una fotografía no habilita reconocimiento facial ni biometría.

## 9. Formularios, campos y experiencia de uso

### 9.1 Formularios principales

| Formulario | Datos y validaciones esenciales |
|---|---|
| Organización | Nombre, tipo, estado, plan, módulos, parámetros y datos generales |
| Sede | Nombre, dirección, ciudad, contacto, horarios, responsable y estado; zona horaria propuesta |
| Área/destino | Sede, nombre, responsables/autorizadores, estado y jerarquía opcional por definir |
| Zona/punto | Sede, nombre, estado y condición de punto de acceso |
| Persona | Documento, nombres, apellidos, contacto, procedencia, tipo, foto opcional y estado |
| Autorización | Persona, sede, destino, zonas opcionales, modalidad, fechas, días, horarios, motivo, responsable y decisión |
| Visita ocasional | Persona, sede, destino, responsable, motivo y solicitud |
| Entrada/salida | Documento o QR, visita/permanencia, punto, operador derivado y resultado |
| Vehículo/dispositivo | Campos de RF-16/RF-17 y vínculo a responsable/autorización/visita |
| Configuración | Roles, permisos, tipos de persona/dispositivo, plantillas y políticas |

Obligatoriedad de teléfono, correo, foto y campos adicionales se definirá por finalidad y política. Los campos de identificación y trazabilidad requeridos por cada operación no serán omitibles por la interfaz.

### 9.2 Criterios de interfaz

- Interfaz responsiva para computador, tablet y móvil con React, TypeScript y Tailwind.
- Vista de recepción centrada en buscar documento/escanear QR, ver resultado y confirmar acción.
- Separación visible entre autorización, entrada registrada y persona dentro.
- Acciones principales accesibles con teclado, etiquetas claras, contraste y mensajes que no dependan solo del color.
- Listados con filtros, paginación y ordenamiento en servidor; cards compactas o tablas adaptadas según pantalla.
- Confirmación de acciones críticas, bloqueo de doble envío en interfaz y protección equivalente en servidor.
- Selección visible de organización y sede operativa, evitando registrar accesos en un contexto equivocado.
- Estado de conexión, última actualización y errores recuperables visibles; sin presentar una operación fallida como exitosa.

**Propuesta:** empezar con formularios comunes y vocabulario configurable. Un constructor genérico de formularios no está solicitado y no es requisito del núcleo.

## 10. Bandejas, históricos, dashboard y reportes

### 10.1 Vistas operativas

1. Recepción: identificación, visitas esperadas, solicitudes pendientes y registro de movimientos.
2. Autorizador: solicitudes sobre sus destinos, visitas esperadas y decisiones.
3. Personas dentro: documento, tipo, hora de entrada, tiempo, destino, responsable y vehículo cuando aplique; filtros por sede, destino y tipo.
4. Histórico por persona: visitas, autorizaciones, entradas/salidas, duración, sedes, destinos, motivos, responsables, autorizadores, vehículos y observaciones.
5. Dispositivos: movimientos y condición dentro/fuera.
6. Administración: configuración organizacional y permisos.
7. Auditoría: acciones críticas con filtros y detalle permitido.

### 10.2 Dashboard — RF-23

Mostrará personas dentro, entradas/salidas del día, visitas pendientes, autorizadas y rechazadas, autorizaciones próximas a vencer, personas sin salida, visitas por tipo/sede y franjas de mayor ingreso.

Una visita rechazada no contará como entrada. “Personas dentro” no se confundirá con “entradas del día”, ya que puede incluir ingresos anteriores. “Personas sin salida” requerirá una definición de alerta: una permanencia abierta reciente no implica una novedad.

### 10.3 Reportes — RF-24

| Reporte | Contenido y filtros relevantes |
|---|---|
| Historial de visitas | Fecha, persona, tipo, estado, sede, destino y responsable |
| Entradas y salidas | Movimientos y operadores por periodo/sede/punto |
| Autorizaciones | Modalidad, estado, persona, responsable, vigencia y destino |
| Autorizaciones vencidas | Vigencia finalizada y estado aplicable |
| Personas dentro | Corte temporal explícito de permanencias abiertas |
| Permanencia | Duración de visitas cerradas y tiempo transcurrido de abiertas |
| Visitas por sede/destino | Agrupaciones sin doble conteo |
| Movimientos vehiculares | Placa, responsable, entrada/salida y visita |
| Auditoría | Actor, acción, objeto, resultado y fecha |
| Dispositivos, propuesto | Historial y equipos dentro; complementa la consulta obligatoria de RF-17 |

La fuente exige Excel y PDF, con filtros por fecha, sede, persona, tipo, estado y destino cuando sean aplicables. Las exportaciones conservarán filtros, zona horaria y permisos; un archivo generado no podrá descargarse desde otra organización. Se definirán límites y procesamiento en segundo plano para resultados extensos.

## 11. Alertas y notificaciones

### 11.1 Alertas — RF-21

Cubrirán autorización próxima a vencer/vencida, intento sin autorización, fuera de horario, sede o zona no autorizada, permanencia prolongada, persona sin salida, visita pendiente, autorización revocada y reutilización no permitida de QR.

Cada alerta tendrá organización, sede, tipo, entidad vinculada, instante, estado y seguimiento. La transición Nueva → Revisada → Cerrada conservará usuario y fecha. Se propone deduplicar por evento y política para evitar alertas repetidas por cada actualización de pantalla.

Los umbrales de próximo vencimiento y permanencia prolongada serán parámetros por organización. Cerrar una alerta no cambiará por sí solo una autorización ni cerrará una permanencia.

### 11.2 Correo — RF-22

| Evento | Destinatario propuesto; debe respetar configuración |
|---|---|
| Solicitud de autorización | Autorizador/responsable facultado |
| Aprobación o rechazo | Solicitante y operador involucrado según política |
| Invitación | Visitante con correo disponible |
| Llegada | Responsable de la visita |
| Próximo vencimiento | Responsable/autorizador configurado |
| Revocación | Persona y responsables definidos por política |

Las plantillas serán configurables por organización. Se distinguirá evento de negocio de entrega de correo; una falla del proveedor no revertirá una entrada ya confirmada. Se propone una cola durable con reintentos, deduplicación y registro de resultado, sin afirmar entrega garantizada en la bandeja del destinatario.

SMS, WhatsApp y push son canales futuros. No se incorporan al MVP propuesto. Falta definir proveedor, remitente, política de destinatarios y qué hacer cuando el visitante no tiene correo.

## 12. Seguridad, privacidad, auditoría y arquitectura

### 12.1 Arquitectura propuesta

Se propone un **monolito modular con separación de dominio, aplicación, infraestructura y API**, organizado por capacidades: SaaS/organizaciones, identidad/permisos, estructura física, personas, autorizaciones, visitas/accesos, vehículos, dispositivos, alertas/notificaciones, consultas/reportes y auditoría.

Los casos de uso no dependerán directamente de correo o almacenamiento. Esas integraciones se expondrán mediante interfaces. React consumirá una API segura; no decidirá por sí mismo la validez del ingreso. No se requieren microservicios para comenzar.

| Componente | Decisión o propuesta |
|---|---|
| Backend | .NET confirmado; propuesta C# y ASP.NET Core Web API |
| Frontend | React, TypeScript y Tailwind CSS confirmados |
| Base de datos | PostgreSQL confirmado |
| Persistencia | Propuesta Entity Framework Core con proveedor Npgsql y migraciones versionadas |
| Tiempo real | Propuesta ASP.NET Core SignalR para eventos operativos |
| Identidad | Solución estándar; proveedor y estrategia de sesión pendientes |
| Archivos | Adaptador a almacenamiento privado; proveedor pendiente |
| Correo | Adaptador a proveedor por seleccionar |
| Procesos durables | Propuesta outbox/cola persistida para correo y alertas programadas |
| Desarrollo local | Propuesta contenedores para PostgreSQL y servicios auxiliares |

Versiones mayores y parches se fijarán al iniciar la implementación tras verificar soporte y compatibilidad; no se copian las versiones del PRD de referencia.

### 12.2 Aislamiento multi-organización

Se propone comenzar con base compartida y `organization_id` en las entidades del cliente. Las claves y relaciones impedirán asociar objetos de organizaciones distintas. Las consultas tendrán filtros obligatorios y pruebas negativas entre tenants.

La seguridad por filas de PostgreSQL es una defensa adicional propuesta que deberá evaluarse y validarse con los roles reales de conexión, tareas de fondo y pool de conexiones. No sustituye la autorización por usuario y sede. Toda caché, trabajo diferido, exportación y canal de eventos incluirá contexto de organización verificado.

### 12.3 Modelo conceptual mínimo

| Grupo | Entidades y relaciones propuestas |
|---|---|
| SaaS | TipoOrganizacion, Organizacion, Plan, Modulo, OrganizacionModulo y Configuracion |
| Identidad | Usuario, MembresiaOrganizacion, Rol, Permiso, RolPermiso, UsuarioRol y AlcanceSede |
| Estructura | Organizacion → Sede → AreaDestino; Sede → Zona; vínculos de responsables/autorizadores |
| Personas | Persona, TipoDocumento, TipoPersona; vínculo opcional PersonaUsuario |
| Autorización | Autorizacion, Decision, Horario/DiaPermitido, AutorizacionZona y TokenQR |
| Operación | Visita → Permanencia → MovimientoAcceso; Visita vincula autorización opcional por política |
| Vehículos | Vehiculo, PersonaVehiculo, AutorizacionVehiculo y MovimientoVehiculo |
| Equipos | Dispositivo, TipoDispositivo, AutorizacionDispositivo y MovimientoDispositivo |
| Soporte | Archivo, Alerta, TransicionAlerta, Notificacion, Plantilla, Outbox y Auditoria |

Las relaciones operativas conservarán identificadores y datos históricos necesarios aunque cambien nombres, tipos o responsables. El esquema físico, índices y contratos API se producirán durante diseño técnico; esta tabla no constituye una migración lista para ejecutar.

### 12.4 Integridad y concurrencia

- Entrada, consumo de autorización y apertura de permanencia serán atómicos.
- Salida y cierre de permanencia se confirmarán juntos.
- Reintentos tendrán identificadores de idempotencia o protección equivalente; una falla de red no deberá duplicar movimientos.
- Revocación y entrada concurrentes tendrán un orden transaccional definido: una entrada confirmada antes de revocar permanece histórica; después de revocar no se admitirán nuevos accesos.
- Los cambios sobre decisiones y cupos usarán control de concurrencia.
- Un horario nocturno que cruce medianoche no se interpretará silenciosamente: la regla del día de inicio y límites se cerrará antes de habilitarlo.
- Los instantes usarán `timestamptz`; horarios recurrentes conservarán hora local y zona horaria. La inclusión exacta de límites y días festivos se definirá en políticas.

### 12.5 Seguridad y protección de datos

HTTPS, gestión estándar de contraseñas con derivación segura, sesiones protegidas, validación en servidor, protección de secretos, límites de intentos y permisos mínimos son requisitos de implementación. No se almacenarán contraseñas, tokens QR o credenciales en logs.

Se deberán definir finalidad, avisos, acceso a datos personales, retención, eliminación y tratamiento de fotografías antes de operar con datos reales. No se fija un plazo de retención ni se afirma cumplimiento normativo automático. SSO, MFA y federación requieren decisión de alcance; no se presuponen implementados ni descartados para producción.

### 12.6 Auditoría — RF-25

La auditoría será inalterable desde las operaciones ordinarias. Registrará organización, usuario o actor de sistema, instante, acción, objeto, resultado, valores anterior/nuevo cuando corresponda e IP si está disponible.

Cubrirá creación, modificación, inactivación, decisión, rechazo, revocación, entrada, salida y cambios de permisos/configuración. Se propone añadir intentos denegados, regularizaciones, exportaciones y acceso de soporte. Los eventos no expondrán secretos ni copiarán innecesariamente datos personales completos.

### 12.7 Requisitos no funcionales y verificación

| Área | Exigencia | Evidencia prevista |
|---|---|---|
| Rendimiento | Priorizar consulta por documento, QR, validación, entrada y salida | Medición p95 con volumen/concurrencia acordados |
| Escalabilidad | Aumentar organizaciones, sedes, usuarios y movimientos sin cambiar el núcleo | Prueba de carga y revisión de índices/paginación |
| Continuidad | Recuperación ante fallos y respaldo de datos/archivos | Restauración verificada; RPO/RTO por acordar |
| Usabilidad | Operación en computador, tablet y móvil | Recorridos sobre pantallas y lectores seleccionados |
| Consistencia | Sin entradas/salidas duplicadas ante reintentos y concurrencia | Pruebas transaccionales con PostgreSQL real |
| Seguridad | Aislamiento, permisos y archivos privados | Matriz de pruebas positivas y negativas |
| Observabilidad | Diagnóstico sin exponer datos sensibles | Logs estructurados, métricas y correlación |
| Interoperabilidad | Funcionalidades centrales mediante API segura | Contratos documentados y pruebas de integración |

## 13. Fases y prioridades propuestas

### 13.1 Secuencia

| Fase | Resultado verificable | Alcance |
|---|---|---|
| 0. Definiciones y base técnica | Decisiones críticas, proyectos, migraciones, autenticación, aislamiento y auditoría base | Fundamento |
| 1. Administración y personas | Organizaciones, tipo, sedes, destinos, zonas, roles, usuarios, tipos y personas | RF-01 a RF-08; plantilla básica RF-26 |
| 2. Autorizaciones y visitas | Modalidades, solicitudes, decisiones, revocación, invitación y QR | RF-09 a RF-12, RF-15 y RF-20 |
| 3. Operación de accesos | Validación, entrada/salida, concurrencia, historial y presencia actualizada | RF-13, RF-14, RF-18 y RF-19 |
| 4. Operación medible | Alertas, correo, dashboard y reportes núcleo Excel/PDF | RF-21 a RF-24; RF-25 transversal |
| 5. Vehículos y equipos | Configuración, autorizaciones, movimientos e históricos de activos | RF-16 y RF-17; reportes asociados |
| 6. Adaptación por vertical | Plantillas completas; especificar documentación de contratistas y límites de invitados | RF-26 completo según reglas validadas |
| 7. Preparación productiva | Seguridad, recuperación, carga, capacitación y aceptación | Requisitos no funcionales y cierre |

**MVP propuesto:** fases 0 a 4, con RF-25 desde el primer flujo. El piloto usará el núcleo común en organizaciones de prueba. Vehículos, equipos y capacidades sectoriales avanzadas siguen comprometidos en el alcance completo, pero quedan fuera de este recorte inicial propuesto. Si el primer cliente necesita RF-16/RF-17, se incorporarán antes de su piloto. La priorización requiere validación del responsable del producto.

### 13.2 Fuera del MVP propuesto

Aplicación móvil nativa; biometría/reconocimiento facial; apertura automática de torniquetes o barreras; funcionamiento offline con sincronización; integración con directorios/ERP; SMS, WhatsApp y push; facturación SaaS; IA; constructor de formularios; jerarquías sectoriales avanzadas todavía no especificadas.

La arquitectura deberá permitir incorporar integraciones futuras sin convertirlas en funcionalidades ya aprobadas. Los lectores QR que operen como teclado o cámara web se evaluarán dentro del flujo de recepción.

## 14. Criterios de aceptación

### 14.1 Núcleo y MVP propuesto

| ID | Escenario verificable | Requisitos |
|---|---|---|
| CA-01 | Un usuario de A no puede listar, leer, modificar, exportar, descargar archivos ni recibir eventos de B aunque conozca sus IDs | RF-01, RF-06; RN-01 |
| CA-02 | Una organización administra varias sedes y limita un operador a las asignadas; el backend rechaza el acceso a otra sede | RF-03, RF-06 |
| CA-03 | Tipos, destinos, responsables y zonas pueden configurarse e inactivarse conservando referencias históricas | RF-02, RF-04, RF-05, RF-08, RF-26 |
| CA-04 | Buscar un documento existente recupera la persona; dos altas concurrentes no duplican la clave de identificación definida | RF-07 |
| CA-05 | Persona visitante y usuario autenticado se administran de forma independiente | RF-06, RF-07 |
| CA-06 | Crear una autorización conserva persona, destino, modalidad, condiciones, creador y decisión atribuible | RF-09, RF-10, RF-20 |
| CA-07 | Una visita ocasional pendiente no ingresa cuando la política exige aprobación; un usuario sin permiso no puede aprobarla por API | RF-11, RF-13 |
| CA-08 | Una invitación permite recuperar la autorización por QR sin exponer datos personales directamente en el código | RF-12, RF-15 |
| CA-09 | Entradas con persona inactiva, autorización no vigente, sede/zona incorrecta o fuera de horario se deniegan con causa y sin abrir permanencia | RF-13, RF-15, RF-21 |
| CA-10 | Dos intentos simultáneos de consumir una autorización única producen una sola entrada confirmada | RF-10, RF-13 |
| CA-11 | Una autorización recurrente permite visitas distintas en sus ventanas válidas sin cerrarse tras la primera salida | RF-10; RN-09 |
| CA-12 | Reenviar la misma solicitud de entrada o salida no crea movimientos duplicados | RF-13, RF-14 |
| CA-13 | La salida cierra el ingreso correcto y calcula duración; una autorización revocada después del ingreso no impide esa salida según la regla propuesta | RF-14 |
| CA-14 | La vista de personas dentro coincide con permanencias abiertas; tras reconectar recupera estado persistido sin depender de eventos perdidos | RF-19 |
| CA-15 | El historial conserva autorizadores, responsables, duración y movimientos aun después de inactivar catálogos | RF-18, RF-20; RN-08 |
| CA-16 | Los escenarios de alerta de RF-21 se generan con su motivo y transitan Nueva → Revisada → Cerrada con seguimiento | RF-21 |
| CA-17 | Los eventos de correo usan plantillas del tenant y destinatarios configurados; una falla genera reintento sin duplicar la operación de acceso | RF-22 |
| CA-18 | El dashboard respeta sedes/permisos y distingue entradas del día, personas dentro y visitas pendientes | RF-23 |
| CA-19 | Excel/PDF reflejan filtros, ámbito y corte temporal; no se descarga una exportación de otro tenant | RF-24 |
| CA-20 | Las operaciones críticas registran actor, organización, instante, acción y objeto; los usuarios ordinarios no alteran la auditoría | RF-25 |
| CA-21 | Los flujos de buscar, solicitar, decidir, entrar y salir funcionan en computador, tablet y móvil con validaciones visibles | Requisitos no funcionales |
| CA-22 | Una restauración recupera datos coherentes y archivos vinculados según los objetivos acordados antes de producción | Requisitos no funcionales |

### 14.2 Alcance completo

| ID | Escenario verificable | Requisitos |
|---|---|---|
| CA-23 | Con vehículos habilitados se registra placa, responsable, autorización y movimientos vinculados; al deshabilitar no se crean nuevos movimientos del módulo | RF-16 |
| CA-24 | Una persona/autorización admite varios dispositivos y se conoce qué equipos tienen entrada sin salida | RF-17 |
| CA-25 | Cada movimiento de equipo identifica sede, zona/punto, responsable, visita y operador; el histórico sigue accesible con permiso | RF-17 |
| CA-26 | Cambiar la plantilla sectorial no modifica permisos existentes ni elimina personalizaciones o históricos; los catálogos quedan ajustables | RF-26 |

### 14.3 Trazabilidad de reglas originales

| Regla | Conservación en el PRD |
|---|---|
| RN-01 — Aislamiento | Secciones 5 y 12; CA-01 |
| RN-02 — Persona identificada en toda entrada | Sección 6.5; CA-09 |
| RN-03 — Autorización vigente cuando la política lo exige | Secciones 6.3 y 6.5; CA-07 y CA-09 |
| RN-04 — Autor de autorización | Sección 6.2; CA-06 |
| RN-05 — Fechas, horarios y lugares | Secciones 6.2, 6.5 y 7; CA-09 y CA-11 |
| RN-06 — Vencida/cancelada/revocada no habilita nuevos ingresos | Secciones 6.7 y 7; CA-09 |
| RN-07 — Entrada con salida o cierre pendiente visible | Secciones 6.6 y 10; CA-13 y CA-14 |
| RN-08 — Inactivación conserva históricos | Secciones 5, 6.1 y 12; CA-15 |
| RN-09 — Recurrente con múltiples accesos | Sección 6.2; CA-11 |
| RN-10 — Permisos por rol | Sección 5; CA-01, CA-02 y CA-07 |
| RN-11 — Verticales preservan núcleo | Secciones 1, 5 y 13; CA-26 |

## 15. Entorno local de desarrollo y preparación de implementación

Se propone un repositorio con API .NET, frontend React/TypeScript, migraciones PostgreSQL, pruebas y documentación. La organización concreta de carpetas se fijará al crear la solución.

El entorno local deberá documentar requisitos de runtime, instalación, variables, migraciones, datos sintéticos, arranque y ejecución de pruebas. Se propone PostgreSQL en contenedor con volumen persistente y un servicio local de captura de correo. Secretos reales no se incluirán en código ni en datos de ejemplo.

Antes de programar el primer flujo se prepararán:

1. Registro de decisiones de arquitectura: módulos, tenancy, identidad, sesión y concurrencia.
2. Modelo físico inicial con claves de organización, restricciones y estrategia de migración.
3. Contratos API de personas, autorizaciones, visitas, entradas y salidas; errores de validación y conflicto consistentes.
4. Matriz de permisos por rol y alcance de sede/destino.
5. Pruebas de aislamiento entre dos organizaciones y de duplicidad concurrente de accesos.
6. Semillas sintéticas con varias sedes, personas, modalidades de autorización y estados de visita.

La entrega técnica de cada fase incluirá código, migraciones, pruebas relevantes y evidencia de sus criterios de aceptación. No se declara una fase terminada solo por disponer de pantallas o datos simulados.

## 16. Decisiones pendientes y riesgos de implementación

| ID | Definición pendiente | Momento de cierre |
|---|---|---|
| D-01 | Confirmar nombre comercial y términos internos Sede/Área; etiquetas por vertical | Antes de consolidar UI y contratos |
| D-02 | Aprobar MVP y necesidad de vehículos/equipos en el primer cliente | Antes de planificar entregas |
| D-03 | Elegir identidad, sesión, múltiples membresías, invitación, SSO y MFA | Antes de autenticación y producción según alcance |
| D-04 | Definir cuándo se exige autorización, quién decide/revoca/cancela y destinos de cada autorizador | Antes del flujo de autorización |
| D-05 | Definir reingresos, cupos por modalidad y modificación de autorizaciones aprobadas | Antes de validar entradas |
| D-06 | Fijar zona horaria por sede, horarios nocturnos, límites inclusivos/exclusivos y festivos | Antes de reglas de calendario |
| D-07 | Confirmar duplicados de presencia por sede y simultaneidad entre sedes | Antes de restricciones transaccionales |
| D-08 | Definir regularización de entradas/salidas omitidas, permisos y evidencia | Antes del piloto operativo |
| D-09 | Definir identificación documental, países emisores y atención de personas sin documento estándar | Antes de modelo de persona |
| D-10 | Precisar custodia y salida de equipos, cambios de responsable y movimientos vehiculares | Antes de RF-16/RF-17 |
| D-11 | Especificar documentación de contratistas, jerarquías residenciales y límites de invitados | Antes de adaptación sectorial completa |
| D-12 | Seleccionar correo, remitentes, destinatarios, umbrales y política de reintentos | Antes de notificaciones/alertas |
| D-13 | Elegir infraestructura, archivos privados, formatos, tamaños y lectores compatibles | Antes de integrar servicios |
| D-14 | Definir privacidad, retención, eliminación, respaldos, RPO/RTO y responsables operativos | Antes de datos reales/producción |
| D-15 | Acordar volumen, concurrencia, p95, frescura de pantalla y disponibilidad | Antes de pruebas de carga/aceptación |
| D-16 | Confirmar plan SaaS, módulos y efectos operativos de inactivar una organización | Antes de activar clientes |

**Riesgos principales:** alcance sectorial sin detalle, confusión entre visita y autorización, duplicación por concurrencia, filtros de tenant incompletos, horarios ambiguos, presencia desactualizada, fallos de correo y retención indefinida de datos personales. Cada riesgo se aborda con una decisión identificada, controles en servidor y criterios de aceptación; no se declara resuelto hasta verificarlos.

Las decisiones comerciales y de operación corresponden al responsable del producto; arquitectura y desarrollo propondrán soluciones técnicas; infraestructura y el responsable de datos cerrarán despliegue, recuperación y conservación. Se deberán asignar responsables nominales al planificar.

## 17. Stack y fuentes técnicas

El stack confirmado proviene de la instrucción del proyecto, no del PRD de DataTicket. Las recomendaciones arquitectónicas de este documento son propuestas para concretar los requisitos.

Referencias oficiales consultadas como apoyo técnico el 17 de octubre de 2026:

- [ASP.NET Core: introducción a autorización](https://learn.microsoft.com/en-us/aspnet/core/security/authorization/introduction): marco de autorización en servidor; la matriz del producto se define en la sección 5.
- [PostgreSQL: políticas de seguridad por filas](https://www.postgresql.org/docs/current/ddl-rowsecurity.html): fundamento para evaluar defensa adicional por tenant; requiere diseño y pruebas con roles reales.
- [Tailwind CSS: diseño responsivo](https://tailwindcss.com/docs/responsive-design): mecanismos de adaptación por tamaño de pantalla; la experiencia de recepción requiere validación propia.

Estas fuentes respaldan capacidades técnicas, no confirman decisiones de negocio, versiones de runtime ni resultados de rendimiento. La selección y fijación de versiones formará parte de la fase 0.
