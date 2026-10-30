import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useMemo, useState, type FormEvent } from 'react'
import { Icono } from '../../shared/ui/Icono'
import { Selector } from '../../shared/ui/Selector'
import { useAcciones } from '../modulos/apiModulos'
import { leerOrganizacionActiva } from '../sesion/sesion'
import { consultarSedes, type SedeAdmin } from './apiSedes'
import {
  cambiarEstadoPuntoAcceso,
  cambiarEstadoZonaAcceso,
  consultarPuntosAcceso,
  consultarZonasAcceso,
  crearPuntoAcceso,
  crearZonaAcceso,
  editarPuntoAcceso,
  editarZonaAcceso,
  eliminarPuntoAcceso,
  eliminarZonaAcceso,
  type ModoAcceso,
  type PuntoAcceso,
  type ZonaAcceso,
} from './apiZonasAcceso'
import {
  BotonIcono,
  BotonNuevo,
  BotonesFormulario,
  Buscador,
  Campo,
  Confirmar,
  Dialogo,
  Estado,
  EstadoConsulta,
  Estatus,
  Filtro,
  Marco,
} from './componentes'
import { mensajeError, useAviso } from './utilidades'
import type { SeccionProps } from './secciones'

type Vista = 'zonas' | 'puntos'
type Operacion = 'estado' | 'eliminar'
type Pendiente =
  | { tipo: 'zona'; operacion: Operacion; registro: ZonaAcceso }
  | { tipo: 'punto'; operacion: Operacion; registro: PuntoAcceso }

const ESTADOS = [
  { valor: '', texto: 'Todos los estados' },
  { valor: 'true', texto: 'Activos' },
  { valor: 'false', texto: 'Inactivos' },
]

const MODOS: { valor: ModoAcceso; texto: string }[] = [
  { valor: 'Ambos', texto: 'Entradas y salidas' },
  { valor: 'Entrada', texto: 'Solo entradas' },
  { valor: 'Salida', texto: 'Solo salidas' },
]

const coincide = (texto: string, ...campos: (string | null | undefined)[]) => {
  const buscado = texto.trim().toLowerCase()
  return !buscado || campos.some((c) => c?.toLowerCase().includes(buscado))
}

const fecha = (iso: string | null) => (iso ? new Date(iso).toLocaleString('es-CO', { dateStyle: 'medium', timeStyle: 'short' }) : '')

function Trazabilidad({ r }: { r: { creadoEn: string; creadoPor: string | null; actualizadoEn: string | null; actualizadoPor: string | null } }) {
  return (
    <small className="ad-traza">
      Creado {fecha(r.creadoEn)}{r.creadoPor ? ` por ${r.creadoPor}` : ''}
      {r.actualizadoEn && <> · Modificado {fecha(r.actualizadoEn)}{r.actualizadoPor ? ` por ${r.actualizadoPor}` : ''}</>}
    </small>
  )
}

/**
 * Zonas (sectores físicos de una sede, restringibles en una autorización) y puntos de acceso (donde se registran
 * entradas y salidas, dentro de una zona). Módulo «sedes»; el servidor valida organización, alcance y unicidad.
 */
export function ZonasAccesoSeccion({ pestanas }: SeccionProps) {
  const acciones = useAcciones('sedes')
  const [vista, setVista] = useState<Vista>('zonas')
  const [buscar, setBuscar] = useState('')
  const [activo, setActivo] = useState('')
  const [idSede, setIdSede] = useState('')
  const [idZona, setIdZona] = useState('')
  const [formZona, setFormZona] = useState<ZonaAcceso | 'nueva' | null>(null)
  const [formPunto, setFormPunto] = useState<PuntoAcceso | 'nuevo' | null>(null)
  const [confirmar, setConfirmar] = useState<Pendiente | null>(null)
  const { avisar, aviso } = useAviso()
  const queryClient = useQueryClient()
  const organizacion = leerOrganizacionActiva()

  const sedes = useQuery({ queryKey: ['admin-sedes', organizacion], queryFn: consultarSedes, enabled: acciones.consultar })
  const zonas = useQuery({ queryKey: ['admin-zonas-acceso', organizacion], queryFn: consultarZonasAcceso, enabled: acciones.consultar })
  const puntos = useQuery({ queryKey: ['admin-puntos-acceso', organizacion], queryFn: consultarPuntosAcceso, enabled: acciones.consultar })

  const filtroEstado = (x: { activo: boolean }) => !activo || String(x.activo) === activo
  const zonasVisibles = (zonas.data ?? []).filter((z) => filtroEstado(z)
    && (!idSede || String(z.idSede) === idSede) && coincide(buscar, z.nombre, z.descripcion, z.sede))
  const puntosVisibles = (puntos.data ?? []).filter((p) => filtroEstado(p)
    && (!idSede || String(p.idSede) === idSede) && (!idZona || String(p.idZona) === idZona)
    && coincide(buscar, p.nombre, p.descripcion, p.zona, p.sede))
  const zonasFiltro = useMemo(() => (zonas.data ?? []).filter((z) => !idSede || String(z.idSede) === idSede), [zonas.data, idSede])

  const operar = useMutation({
    mutationFn: async (p: Pendiente): Promise<string> => {
      const nombre = p.tipo === 'zona' ? 'Zona' : 'Punto de acceso'
      if (p.operacion === 'eliminar') {
        await (p.tipo === 'zona' ? eliminarZonaAcceso(p.registro.idZona) : eliminarPuntoAcceso(p.registro.idPuntoAcceso))
        return `${nombre} eliminado.`
      }
      const r = p.tipo === 'zona'
        ? await cambiarEstadoZonaAcceso(p.registro.idZona, !p.registro.activo)
        : await cambiarEstadoPuntoAcceso(p.registro.idPuntoAcceso, !p.registro.activo)
      return r.activo ? `${nombre} activado.` : `${nombre} inactivado.`
    },
    onSuccess: (mensaje) => {
      void queryClient.invalidateQueries({ queryKey: ['admin-zonas-acceso'] })
      void queryClient.invalidateQueries({ queryKey: ['admin-puntos-acceso'] })
      setConfirmar(null)
      avisar(mensaje)
    },
  })

  const consulta = vista === 'zonas' ? zonas : puntos
  const visibles = vista === 'zonas' ? zonasVisibles : puntosVisibles
  const filtrando = Boolean(buscar || activo || idSede || (vista === 'puntos' && idZona))
  const sinSedesActivas = !(sedes.data ?? []).some((s) => s.activo)
  const sinZonasActivas = !(zonas.data ?? []).some((z) => z.activo)

  function cambiarVista(v: Vista) {
    setVista(v)
    setBuscar('')
    setActivo('')
    setIdZona('')
  }

  function pedir(p: Pendiente) {
    operar.reset()
    setConfirmar(p)
  }

  return (
    <Marco
      titulo="Zonas y puntos de acceso"
      descripcion="Sectores de cada sede que una autorización puede restringir y los puntos donde se registran entradas y salidas. Inactivar conserva el historial."
      pestanas={pestanas}
      accion={acciones.anadir && (vista === 'zonas'
        ? <BotonNuevo texto="Nueva zona" onClick={() => setFormZona('nueva')} disabled={sinSedesActivas} />
        : <BotonNuevo texto="Nuevo punto de acceso" onClick={() => setFormPunto('nuevo')} disabled={sinZonasActivas} />)}
    >
      <div className="ad-vista" role="tablist" aria-label="Ver zonas o puntos de acceso">
        {(['zonas', 'puntos'] as const).map((v) => (
          <button key={v} type="button" role="tab" aria-selected={vista === v} onClick={() => cambiarVista(v)}>
            {v === 'zonas' ? 'Zonas' : 'Puntos de acceso'}
            <span>{(v === 'zonas' ? zonas.data : puntos.data)?.filter((x) => x.activo).length ?? 0}</span>
          </button>
        ))}
      </div>

      <div className="ad-herramientas">
        <Buscador valor={buscar} onChange={setBuscar} etiqueta={vista === 'zonas' ? 'Buscar zonas' : 'Buscar puntos de acceso'}
          placeholder={vista === 'zonas' ? 'Buscar por nombre, descripción o sede' : 'Buscar por nombre, zona o sede'} />
        <Filtro etiqueta="Filtrar por sede" valor={idSede} onChange={(v) => { setIdSede(v); setIdZona('') }}
          opciones={[{ valor: '', texto: 'Todas las sedes' }, ...(sedes.data ?? []).map((s) => ({ valor: String(s.idSede), texto: s.nombre }))]} />
        {vista === 'puntos' && (
          <Filtro etiqueta="Filtrar por zona" valor={idZona} onChange={setIdZona}
            opciones={[{ valor: '', texto: 'Todas las zonas' }, ...zonasFiltro.map((z) => ({ valor: String(z.idZona), texto: idSede ? z.nombre : `${z.nombre} · ${z.sede}` }))]} />
        )}
        <Filtro etiqueta="Filtrar por estado" valor={activo} onChange={setActivo} opciones={ESTADOS} />
      </div>

      {!acciones.consultar ? (
        <Estado tipo="permiso" texto="Tu perfil no tiene permiso para consultar zonas y puntos de acceso." />
      ) : (
        <EstadoConsulta loading={consulta.isLoading} error={consulta.error} empty={!visibles.length}
          vacio={filtrando
            ? `Ningún registro coincide con los filtros.`
            : vista === 'zonas'
              ? sinSedesActivas ? 'Crea primero una sede activa para registrar zonas.' : 'Aún no hay zonas registradas.'
              : sinZonasActivas ? 'Crea primero una zona activa para registrar puntos de acceso.' : 'Aún no hay puntos de acceso registrados.'} />
      )}

      {vista === 'zonas' && zonasVisibles.length > 0 && (
        <div className="ad-zonas">
          {zonasVisibles.map((zona) => (
            <article className="ad-zona" key={zona.idZona} data-inactivo={!zona.activo || undefined}>
              <header>
                <span className="ad-perfil-icono" aria-hidden="true"><Icono nombre="sede" /></span>
                <div>
                  <h2>{zona.nombre}</h2>
                  <small>Sede: <strong>{zona.sede}</strong>{!zona.sedeActiva && ' (inactiva)'}</small>
                </div>
                <Estatus activo={zona.activo} texto={zona.activo ? 'Activa' : 'Inactiva'} />
              </header>
              {zona.descripcion && <p>{zona.descripcion}</p>}
              <Trazabilidad r={zona} />
              <footer>
                <span>{zona.puntosAcceso} {zona.puntosAcceso === 1 ? 'punto de acceso' : 'puntos de acceso'}</span>
                {acciones.editar && (
                  <span className="ad-perfil-acciones">
                    <BotonIcono etiqueta={`Editar ${zona.nombre}`} icono="editar" onClick={() => setFormZona(zona)} />
                    <BotonIcono etiqueta={`${zona.activo ? 'Inactivar' : 'Activar'} ${zona.nombre}`} icono={zona.activo ? 'cerrar' : 'check'}
                      peligro={zona.activo} onClick={() => pedir({ tipo: 'zona', operacion: 'estado', registro: zona })} />
                    {!zona.tieneHistorial && (
                      <BotonIcono etiqueta={`Eliminar ${zona.nombre}`} icono="eliminar" peligro
                        onClick={() => pedir({ tipo: 'zona', operacion: 'eliminar', registro: zona })} />
                    )}
                  </span>
                )}
              </footer>
            </article>
          ))}
        </div>
      )}

      {vista === 'puntos' && puntosVisibles.length > 0 && (
        <div className="ad-zonas">
          {puntosVisibles.map((punto) => (
            <article className="ad-zona" key={punto.idPuntoAcceso} data-inactivo={!punto.activo || undefined}>
              <header>
                <span className="ad-perfil-icono" aria-hidden="true"><Icono nombre="accesos" /></span>
                <div>
                  <h2>{punto.nombre}</h2>
                  <small>Zona: <strong>{punto.zona}</strong>{!punto.zonaActiva && ' (inactiva)'} · {punto.sede}</small>
                </div>
                <Estatus activo={punto.activo} texto={punto.activo ? 'Activo' : 'Inactivo'} />
              </header>
              <ul className="ad-etiquetas">
                <li className="ad-etiqueta">{MODOS.find((m) => m.valor === punto.modoAcceso)?.texto ?? punto.modoAcceso}</li>
              </ul>
              {punto.descripcion && <p>{punto.descripcion}</p>}
              <Trazabilidad r={punto} />
              {acciones.editar && (
                <footer>
                  <span />
                  <span className="ad-perfil-acciones">
                    <BotonIcono etiqueta={`Editar ${punto.nombre}`} icono="editar" onClick={() => setFormPunto(punto)} />
                    <BotonIcono etiqueta={`${punto.activo ? 'Inactivar' : 'Activar'} ${punto.nombre}`} icono={punto.activo ? 'cerrar' : 'check'}
                      peligro={punto.activo} onClick={() => pedir({ tipo: 'punto', operacion: 'estado', registro: punto })} />
                    {!punto.tieneHistorial && (
                      <BotonIcono etiqueta={`Eliminar ${punto.nombre}`} icono="eliminar" peligro
                        onClick={() => pedir({ tipo: 'punto', operacion: 'eliminar', registro: punto })} />
                    )}
                  </span>
                </footer>
              )}
            </article>
          ))}
        </div>
      )}

      {formZona && (
        <ZonaFormulario zona={formZona === 'nueva' ? null : formZona} sedes={sedes.data ?? []}
          onClose={() => setFormZona(null)} onGuardado={(m) => { setFormZona(null); avisar(m) }} />
      )}
      {formPunto && (
        <PuntoFormulario punto={formPunto === 'nuevo' ? null : formPunto} zonas={zonas.data ?? []}
          onClose={() => setFormPunto(null)} onGuardado={(m) => { setFormPunto(null); avisar(m) }} />
      )}
      {confirmar && <ConfirmarOperacion p={confirmar} pendiente={operar.isPending} error={operar.error}
        onConfirmar={() => operar.mutate(confirmar)} onCancelar={() => setConfirmar(null)} />}
      {aviso}
    </Marco>
  )
}

function ConfirmarOperacion({ p, pendiente, error, onConfirmar, onCancelar }: {
  p: Pendiente
  pendiente: boolean
  error: unknown
  onConfirmar: () => void
  onCancelar: () => void
}) {
  const sustantivo = p.tipo === 'zona' ? 'zona' : 'punto de acceso'
  const nombre = p.registro.nombre
  const props = p.operacion === 'eliminar'
    ? { titulo: `¿Eliminar ${sustantivo}?`, texto: `«${nombre}» se eliminará definitivamente. Solo es posible porque no tiene historial asociado.`, accion: 'Eliminar', peligro: true }
    : p.registro.activo
      ? {
          titulo: `¿Inactivar ${sustantivo}?`,
          texto: p.tipo === 'zona'
            ? `«${nombre}» no podrá usarse en nuevas autorizaciones y sus puntos no registrarán movimientos. Se conservan su historial y sus autorizaciones.`
            : `«${nombre}» no podrá usarse para registrar movimientos. Los movimientos anteriores conservan su referencia.`,
          accion: 'Inactivar', peligro: true,
        }
      : { titulo: `¿Activar ${sustantivo}?`, texto: `«${nombre}» volverá a estar disponible.`, accion: 'Activar', peligro: false }
  return (
    <Confirmar {...props} pendiente={pendiente} error={error ? mensajeError(error) : null} onConfirmar={onConfirmar} onCancelar={onCancelar} />
  )
}

function ZonaFormulario({ zona, sedes, onClose, onGuardado }: {
  zona: ZonaAcceso | null
  sedes: SedeAdmin[]
  onClose: () => void
  onGuardado: (mensaje: string) => void
}) {
  const queryClient = useQueryClient()
  const [nombre, setNombre] = useState(zona?.nombre ?? '')
  const [idSede, setIdSede] = useState(zona ? String(zona.idSede) : '')
  const [descripcion, setDescripcion] = useState(zona?.descripcion ?? '')
  const [errores, setErrores] = useState<{ nombre?: string; idSede?: string }>({})
  const opcionesSede = sedes.filter((s) => s.activo || s.idSede === zona?.idSede)
  const guardar = useMutation({
    mutationFn: () => {
      const datos = { idSede: Number(idSede), nombre: nombre.trim(), descripcion: descripcion.trim() || null }
      return zona ? editarZonaAcceso(zona.idZona, datos) : crearZonaAcceso(datos)
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['admin-zonas-acceso'] })
      void queryClient.invalidateQueries({ queryKey: ['admin-puntos-acceso'] })
      onGuardado(zona ? 'Zona actualizada.' : 'Zona creada.')
    },
  })

  function enviar(event: FormEvent) {
    event.preventDefault()
    const nuevos: typeof errores = {}
    if (!nombre.trim()) nuevos.nombre = 'El nombre de la zona es obligatorio.'
    if (!idSede) nuevos.idSede = 'Selecciona la sede de la zona.'
    setErrores(nuevos)
    if (Object.keys(nuevos).length === 0 && !guardar.isPending) guardar.mutate()
  }

  return (
    <Dialogo titulo={zona ? 'Editar zona' : 'Nueva zona'} eyebrow="Zonas y puntos de acceso" icono="sede"
      onClose={onClose} onSubmit={enviar} pie={<BotonesFormulario onCancelar={onClose} pendiente={guardar.isPending} />}>
      <div className="ad-rejilla">
        <Campo etiqueta="Sede" error={errores.idSede} ancho="completo">
          <Selector className="sel-campo" etiqueta="Sede" valor={idSede} invalido={Boolean(errores.idSede)} onChange={setIdSede}
            deshabilitado={Boolean(zona?.tieneHistorial)}
            opciones={[{ valor: '', texto: 'Selecciona…', deshabilitada: true }, ...opcionesSede.map((s) => ({ valor: String(s.idSede), texto: s.nombre }))]} />
        </Campo>
        {zona?.tieneHistorial && <p className="ad-ayuda" data-ancho="completo">La sede no se puede cambiar: la zona ya tiene puntos, autorizaciones o movimientos.</p>}
        <Campo etiqueta="Nombre de la zona" error={errores.nombre} ancho="completo">
          <input required autoFocus maxLength={150} value={nombre} placeholder="Ej.: Parqueadero, Planta, Bodega" aria-invalid={Boolean(errores.nombre)}
            onChange={(e) => setNombre(e.target.value)} />
        </Campo>
        <Campo etiqueta="Descripción (opcional)" ancho="completo">
          <textarea rows={2} maxLength={500} value={descripcion} onChange={(e) => setDescripcion(e.target.value)} />
        </Campo>
      </div>
      {guardar.error && <p className="ad-mensaje" role="alert">{mensajeError(guardar.error, 'No se pudo guardar la zona.')}</p>}
    </Dialogo>
  )
}

function PuntoFormulario({ punto, zonas, onClose, onGuardado }: {
  punto: PuntoAcceso | null
  zonas: ZonaAcceso[]
  onClose: () => void
  onGuardado: (mensaje: string) => void
}) {
  const queryClient = useQueryClient()
  const [nombre, setNombre] = useState(punto?.nombre ?? '')
  const [idZona, setIdZona] = useState(punto ? String(punto.idZona) : '')
  const [descripcion, setDescripcion] = useState(punto?.descripcion ?? '')
  const [modo, setModo] = useState<ModoAcceso>(punto?.modoAcceso ?? 'Ambos')
  const [errores, setErrores] = useState<{ nombre?: string; idZona?: string }>({})
  // Un punto existente solo cambia a otra zona de su misma sede.
  const opcionesZona = zonas.filter((z) => (z.activo || z.idZona === punto?.idZona) && (!punto || z.idSede === punto.idSede))
  const guardar = useMutation({
    mutationFn: () => {
      const datos = { idZona: Number(idZona), nombre: nombre.trim(), descripcion: descripcion.trim() || null, modoAcceso: modo }
      return punto ? editarPuntoAcceso(punto.idPuntoAcceso, datos) : crearPuntoAcceso(datos)
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['admin-puntos-acceso'] })
      void queryClient.invalidateQueries({ queryKey: ['admin-zonas-acceso'] })
      onGuardado(punto ? 'Punto de acceso actualizado.' : 'Punto de acceso creado.')
    },
  })

  function enviar(event: FormEvent) {
    event.preventDefault()
    const nuevos: typeof errores = {}
    if (!nombre.trim()) nuevos.nombre = 'El nombre del punto de acceso es obligatorio.'
    if (!idZona) nuevos.idZona = 'Selecciona la zona del punto de acceso.'
    setErrores(nuevos)
    if (Object.keys(nuevos).length === 0 && !guardar.isPending) guardar.mutate()
  }

  return (
    <Dialogo titulo={punto ? 'Editar punto de acceso' : 'Nuevo punto de acceso'} eyebrow="Zonas y puntos de acceso" icono="accesos"
      onClose={onClose} onSubmit={enviar} pie={<BotonesFormulario onCancelar={onClose} pendiente={guardar.isPending} />}>
      <div className="ad-rejilla">
        <Campo etiqueta="Zona" error={errores.idZona} ancho="completo">
          <Selector className="sel-campo" etiqueta="Zona" valor={idZona} invalido={Boolean(errores.idZona)} onChange={setIdZona} buscable
            placeholderBusqueda="Buscar zona o sede"
            opciones={[{ valor: '', texto: 'Selecciona…', deshabilitada: true },
              ...opcionesZona.map((z) => ({ valor: String(z.idZona), texto: `${z.nombre} · ${z.sede}` }))]} />
        </Campo>
        <Campo etiqueta="Nombre del punto de acceso" error={errores.nombre} ancho="completo">
          <input required autoFocus maxLength={150} value={nombre} placeholder="Ej.: Portería principal, Recepción" aria-invalid={Boolean(errores.nombre)}
            onChange={(e) => setNombre(e.target.value)} />
        </Campo>
        <Campo etiqueta="Uso" ancho="completo">
          <Selector className="sel-campo" etiqueta="Uso" valor={modo} onChange={(v) => setModo(v as ModoAcceso)}
            opciones={MODOS.map((m) => ({ valor: m.valor, texto: m.texto }))} />
        </Campo>
        <Campo etiqueta="Descripción (opcional)" ancho="completo">
          <textarea rows={2} maxLength={500} value={descripcion} onChange={(e) => setDescripcion(e.target.value)} />
        </Campo>
      </div>
      {guardar.error && <p className="ad-mensaje" role="alert">{mensajeError(guardar.error, 'No se pudo guardar el punto de acceso.')}</p>}
    </Dialogo>
  )
}
