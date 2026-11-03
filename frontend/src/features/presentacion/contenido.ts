import type { NombreIcono } from '../../shared/ui/Icono'
import type { EscenaSector } from './IlustracionSector'

/**
 * Textos de la página pública. Solo describen capacidades presentes en el producto;
 * las que dependen de la habilitación por organización se indican como tales.
 * Pendiente de revisión comercial antes de publicar (ver wiki: Página pública de presentación).
 */

export const SECCIONES_NAV = [
  { id: 'como-funciona', texto: 'Cómo funciona' },
  { id: 'servicios', texto: 'Servicios' },
  { id: 'beneficios', texto: 'Beneficios' },
  { id: 'contacto', texto: 'Contacto' },
] as const

export const RETOS: { icono: NombreIcono; reto: string; respuesta: string }[] = [
  {
    icono: 'documento',
    reto: 'Accesos repartidos entre planillas, mensajes y cuadernos.',
    respuesta: 'Autorizaciones, visitas y movimientos en un solo lugar.',
  },
  {
    icono: 'autorizaciones',
    reto: 'No es claro quién está autorizado ni hasta cuándo.',
    respuesta: 'Cada autorización con sede, zonas, periodo y estado.',
  },
  {
    icono: 'editar',
    reto: 'Entradas y salidas anotadas a mano en la portería.',
    respuesta: 'Registro en el punto de acceso, ligado al operador.',
  },
  {
    icono: 'perfil',
    reto: 'Sin rastro de quién solicitó o aprobó un ingreso.',
    respuesta: 'Creador, autorizador y decisión quedan registrados.',
  },
  {
    icono: 'sede',
    reto: 'Difícil saber quién está dentro en este momento.',
    respuesta: 'Permanencias abiertas consultables por sede.',
  },
]

export const PASOS = [
  {
    titulo: 'Registra personas y organizaciones',
    texto:
      'Un registro reutilizable por organización: buscar por documento recupera a la persona existente.',
  },
  {
    titulo: 'Define autorizaciones',
    texto: 'Sede, zonas, periodo, días, horarios y condiciones de ingreso.',
  },
  {
    titulo: 'Gestiona solicitudes y aprobaciones',
    texto: 'Cada autorización conserva quién la creó, quién la aprobó y cuándo.',
  },
  {
    titulo: 'Registra entradas y salidas',
    texto: 'El operador valida y registra el movimiento en el punto de acceso.',
  },
  {
    titulo: 'Consulta actividad e historial',
    texto: 'Quién está dentro, qué movimientos hubo y quién los registró.',
  },
]

export const SERVICIOS: {
  icono: NombreIcono
  titulo: string
  texto: string
  opcional?: boolean
}[] = [
  {
    icono: 'personas',
    titulo: 'Personas y organizaciones',
    texto: 'Registro de personas por organización, con búsqueda por documento y sin duplicados.',
  },
  {
    icono: 'sede',
    titulo: 'Sedes, áreas, zonas y puntos de acceso',
    texto: 'Modela tus instalaciones y define dónde se registra cada movimiento.',
  },
  {
    icono: 'autorizaciones',
    titulo: 'Autorizaciones flexibles',
    texto:
      'Única, diaria, temporal, recurrente o permanente, según la política de tu organización.',
  },
  {
    icono: 'visitas',
    titulo: 'Planificación de visitas',
    texto: 'Programa la llegada de visitantes externos y su destino dentro de la sede.',
  },
  {
    icono: 'accesos',
    titulo: 'Control de entradas y salidas',
    texto: 'Registro en el punto de acceso vinculado a persona, sede, instante y operador.',
  },
  {
    icono: 'dashboard',
    titulo: 'Quién está dentro',
    texto: 'Consulta las permanencias abiertas en cada instalación.',
  },
  {
    icono: 'reportes',
    titulo: 'Historial y trazabilidad',
    texto: 'Solicitudes, aprobaciones y movimientos con su responsable y fecha.',
  },
  {
    icono: 'administracion',
    titulo: 'Multi-organización y roles',
    texto: 'Cada organización aislada, con roles y permisos configurables por sede.',
  },
  {
    icono: 'vehiculos',
    titulo: 'Vehículos y dispositivos',
    texto: 'Autorizaciones para vehículos y equipos asociados a personas.',
    opcional: true,
  },
]

/** Ejemplos de uso descriptivos: el núcleo es común y se ajusta por configuración. */
export const SECTORES: {
  icono: NombreIcono
  nombre: string
  ejemplo: string
  escena: EscenaSector
  acento: 'violeta' | 'lima' | 'cian'
}[] = [
  {
    icono: 'administracion',
    nombre: 'Empresas y centros empresariales',
    escena: 'empresas',
    acento: 'violeta',
    ejemplo: 'Visitantes, proveedores y contratistas hacia áreas y pisos.',
  },
  {
    icono: 'sede',
    nombre: 'Conjuntos residenciales',
    escena: 'residencial',
    acento: 'lima',
    ejemplo: 'Visitas a una unidad, registradas en portería.',
  },
  {
    icono: 'visitas',
    nombre: 'Universidades e instituciones educativas',
    escena: 'educacion',
    acento: 'cian',
    ejemplo: 'Ingresos por varias sedes y puntos de acceso.',
  },
  {
    icono: 'personas',
    nombre: 'Clubes',
    escena: 'club',
    acento: 'violeta',
    ejemplo: 'Invitados de miembros con fecha y horario definidos.',
  },
  {
    icono: 'dispositivos',
    nombre: 'Coworkings',
    escena: 'coworking',
    acento: 'violeta',
    ejemplo: 'Invitados recurrentes o de un solo día.',
  },
  {
    icono: 'vehiculos',
    nombre: 'Parques industriales',
    escena: 'industrial',
    acento: 'lima',
    ejemplo: 'Contratistas y proveedores por zonas restringidas.',
  },
  {
    icono: 'accesos',
    nombre: 'Otras organizaciones',
    escena: 'otras',
    acento: 'cian',
    ejemplo: 'Cualquier instalación que necesite controlar quién entra.',
  },
]

export const BENEFICIOS = [
  {
    titulo: 'Información centralizada',
    texto: 'Autorizaciones, visitas y movimientos en un solo lugar.',
  },
  {
    titulo: 'Claridad de autorización',
    texto: 'Quién está autorizado, dónde y durante qué periodo.',
  },
  {
    titulo: 'Registro ordenado',
    texto: 'Cada entrada y salida queda asociada a su punto de acceso y operador.',
  },
  {
    titulo: 'Seguimiento de aprobaciones',
    texto: 'Conoce quién solicitó y quién aprobó cada autorización.',
  },
  { titulo: 'Historial consultable', texto: 'Revisa los movimientos pasados cuando lo necesites.' },
  { titulo: 'Adaptable', texto: 'Varias organizaciones, sedes y zonas sobre una base común.' },
  { titulo: 'Menos papel', texto: 'Menor dependencia de registros manuales y dispersos.' },
]

/** Datos ficticios, solo ilustrativos. */
export const ACTIVIDAD_DEMO = [
  {
    iniciales: 'VI',
    nombre: 'Visitante de ejemplo',
    lugar: 'Recepción · Sede demo',
    estado: 'Entrada',
    tono: 'entrada',
  },
  {
    iniciales: 'PR',
    nombre: 'Proveedor de ejemplo',
    lugar: 'Bodega · Sede demo',
    estado: 'Pendiente',
    tono: 'pendiente',
  },
  {
    iniciales: 'CO',
    nombre: 'Contratista de ejemplo',
    lugar: 'Torre B · Sede demo',
    estado: 'Salida',
    tono: 'salida',
  },
  {
    iniciales: 'EM',
    nombre: 'Colaborador de ejemplo',
    lugar: 'Portería norte · Sede demo',
    estado: 'Aprobada',
    tono: 'aprobada',
  },
] as const

export const PREGUNTAS = [
  {
    p: '¿Qué tipo de organizaciones pueden usar ConecsIA?',
    r: 'Empresas, conjuntos residenciales, clubes, instituciones educativas, coworkings, parques industriales y otras organizaciones que necesiten gestionar accesos. Todas comparten una base común que se ajusta mediante configuración.',
  },
  {
    p: '¿Se pueden gestionar varias sedes?',
    r: 'Sí. Cada organización puede registrar varias sedes, con sus áreas, zonas y puntos de acceso, y asignar a cada usuario las sedes que le corresponden.',
  },
  {
    p: '¿Qué tipos de autorización se pueden configurar?',
    r: 'Única, diaria, temporal, recurrente y permanente, con fechas, días, horarios y zonas. Las modalidades disponibles dependen de la política de cada organización.',
  },
  {
    p: '¿La plataforma registra entradas y salidas?',
    r: 'Sí. El operador registra cada entrada y salida en el punto de acceso; la salida cierra la permanencia y ambas quedan en el historial.',
  },
  {
    p: '¿Cómo puedo solicitar una presentación o más información?',
    r: 'Escríbenos mediante el formulario de contacto de esta página y te responderemos para coordinar una presentación.',
  },
]

export const TIPOS_ORGANIZACION = [
  'Empresa o centro empresarial',
  'Conjunto residencial',
  'Club',
  'Universidad o institución educativa',
  'Coworking',
  'Parque industrial',
  'Otra',
]
