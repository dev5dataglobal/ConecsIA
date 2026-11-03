import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useState, type FormEvent } from 'react'
import { Icono } from '../../shared/ui/Icono'
import { Selector } from '../../shared/ui/Selector'
import { useAcciones } from '../modulos/apiModulos'
import { leerOrganizacionActiva } from '../sesion/sesion'
import { consultarSedes, type SedeAdmin } from './apiSedes'
import {
  cambiarEstadoPuntoAcceso,
  consultarPuntosAcceso,
  crearPuntoAcceso,
  editarPuntoAcceso,
  eliminarPuntoAcceso,
  type ModoAcceso,
  type PuntoAcceso,
} from './apiPuntosAcceso'
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
  Interruptor,
  Marco,
} from './componentes'
import { mensajeError, useAviso } from './utilidades'
import type { SeccionProps } from './secciones'

type Pendiente = { operacion: 'estado' | 'eliminar'; registro: PuntoAcceso }

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
 * Puntos de acceso de cada sede (donde se registran entradas y salidas). Si la sede está en una zona empresarial, el punto
 * puede marcarse común de la zona; sigue siendo de la sede. Módulo «sedes»; el servidor valida organización, alcance y unicidad.
 */
export function PuntosAccesoSeccion({ pestanas }: SeccionProps) {
  const acciones = useAcciones('sedes')
  const [buscar, setBuscar] = useState('')
  const [activo, setActivo] = useState('')
  const [idSede, setIdSede] = useState('')
  const [formPunto, setFormPunto] = useState<PuntoAcceso | 'nuevo' | null>(null)
  const [confirmar, setConfirmar] = useState<Pendiente | null>(null)
  const { avisar, aviso } = useAviso()
  const queryClient = useQueryClient()
  const organizacion = leerOrganizacionActiva()

  const sedes = useQuery({ queryKey: ['admin-sedes', organizacion], queryFn: consultarSedes, enabled: acciones.consultar })
  const puntos = useQuery({ queryKey: ['admin-puntos-acceso', organizacion], queryFn: consultarPuntosAcceso, enabled: acciones.consultar })

  const visibles = (puntos.data ?? []).filter((p) => (!activo || String(p.activo) === activo)
    && (!idSede || String(p.idSede) === idSede) && coincide(buscar, p.nombre, p.descripcion, p.sede, p.zonaEmpresarial))

  const operar = useMutation({
    mutationFn: async (p: Pendiente): Promise<string> => {
      if (p.operacion === 'eliminar') {
        await eliminarPuntoAcceso(p.registro.idPuntoAcceso)
        return 'Punto de acceso eliminado.'
      }
      const r = await cambiarEstadoPuntoAcceso(p.registro.idPuntoAcceso, !p.registro.activo)
      return r.activo ? 'Punto de acceso activado.' : 'Punto de acceso inactivado.'
    },
    onSuccess: (mensaje) => {
      void queryClient.invalidateQueries({ queryKey: ['admin-puntos-acceso'] })
      setConfirmar(null)
      avisar(mensaje)
    },
  })

  const filtrando = Boolean(buscar || activo || idSede)
  const sinSedesActivas = !(sedes.data ?? []).some((s) => s.activo)

  function pedir(p: Pendiente) {
    operar.reset()
    setConfirmar(p)
  }

  return (
    <Marco
      titulo="Puntos de acceso"
      descripcion="Lugares de cada sede donde se registran entradas y salidas. Inactivar conserva el historial."
      pestanas={pestanas}
      accion={acciones.anadir && <BotonNuevo texto="Nuevo punto de acceso" onClick={() => setFormPunto('nuevo')} disabled={sinSedesActivas} />}
    >
      <div className="ad-herramientas">
        <Buscador valor={buscar} onChange={setBuscar} etiqueta="Buscar puntos de acceso" placeholder="Buscar por nombre, sede o zona empresarial" />
        <Filtro etiqueta="Filtrar por sede" valor={idSede} onChange={setIdSede}
          opciones={[{ valor: '', texto: 'Todas las sedes' }, ...(sedes.data ?? []).map((s) => ({ valor: String(s.idSede), texto: s.nombre }))]} />
        <Filtro etiqueta="Filtrar por estado" valor={activo} onChange={setActivo} opciones={ESTADOS} />
      </div>

      {!acciones.consultar ? (
        <Estado tipo="permiso" texto="Tu perfil no tiene permiso para consultar puntos de acceso." />
      ) : (
        <EstadoConsulta loading={puntos.isLoading} error={puntos.error} empty={!visibles.length}
          vacio={filtrando
            ? 'Ningún punto de acceso coincide con los filtros.'
            : sinSedesActivas ? 'Crea primero una sede activa para registrar puntos de acceso.' : 'Aún no hay puntos de acceso registrados.'} />
      )}

      {visibles.length > 0 && (
        <div className="ad-zonas">
          {visibles.map((punto) => (
            <article className="ad-zona" key={punto.idPuntoAcceso} data-inactivo={!punto.activo || undefined}>
              <header>
                <span className="ad-perfil-icono" aria-hidden="true"><Icono nombre="accesos" /></span>
                <div>
                  <h2>{punto.nombre}</h2>
                  <small>Sede: <strong>{punto.sede}</strong>{!punto.sedeActiva && ' (inactiva)'}{punto.zonaEmpresarial && ` · ${punto.zonaEmpresarial}`}</small>
                </div>
                <Estatus activo={punto.activo} texto={punto.activo ? 'Activo' : 'Inactivo'} />
              </header>
              <ul className="ad-etiquetas">
                <li className="ad-etiqueta">{MODOS.find((m) => m.valor === punto.modoAcceso)?.texto ?? punto.modoAcceso}</li>
                {punto.esComunZonaEmpresarial && <li className="ad-etiqueta">Común de la zona empresarial</li>}
              </ul>
              {punto.descripcion && <p>{punto.descripcion}</p>}
              <Trazabilidad r={punto} />
              {acciones.editar && (
                <footer>
                  <span />
                  <span className="ad-perfil-acciones">
                    <BotonIcono etiqueta={`Editar ${punto.nombre}`} icono="editar" onClick={() => setFormPunto(punto)} />
                    <BotonIcono etiqueta={`${punto.activo ? 'Inactivar' : 'Activar'} ${punto.nombre}`} icono={punto.activo ? 'cerrar' : 'check'}
                      peligro={punto.activo} onClick={() => pedir({ operacion: 'estado', registro: punto })} />
                    {!punto.tieneHistorial && (
                      <BotonIcono etiqueta={`Eliminar ${punto.nombre}`} icono="eliminar" peligro
                        onClick={() => pedir({ operacion: 'eliminar', registro: punto })} />
                    )}
                  </span>
                </footer>
              )}
            </article>
          ))}
        </div>
      )}

      {formPunto && (
        <PuntoFormulario punto={formPunto === 'nuevo' ? null : formPunto} sedes={sedes.data ?? []} sedeInicial={idSede}
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
  const nombre = p.registro.nombre
  const props = p.operacion === 'eliminar'
    ? { titulo: '¿Eliminar punto de acceso?', texto: `«${nombre}» se eliminará definitivamente. Solo es posible porque no tiene historial asociado.`, accion: 'Eliminar', peligro: true }
    : p.registro.activo
      ? { titulo: '¿Inactivar punto de acceso?', texto: `«${nombre}» no podrá usarse para registrar movimientos. Los movimientos anteriores conservan su referencia.`, accion: 'Inactivar', peligro: true }
      : { titulo: '¿Activar punto de acceso?', texto: `«${nombre}» volverá a estar disponible.`, accion: 'Activar', peligro: false }
  return (
    <Confirmar {...props} pendiente={pendiente} error={error ? mensajeError(error) : null} onConfirmar={onConfirmar} onCancelar={onCancelar} />
  )
}

function PuntoFormulario({ punto, sedes, sedeInicial, onClose, onGuardado }: {
  punto: PuntoAcceso | null
  sedes: SedeAdmin[]
  sedeInicial: string
  onClose: () => void
  onGuardado: (mensaje: string) => void
}) {
  const queryClient = useQueryClient()
  const [nombre, setNombre] = useState(punto?.nombre ?? '')
  const [idSede, setIdSede] = useState(punto ? String(punto.idSede) : sedeInicial)
  const [descripcion, setDescripcion] = useState(punto?.descripcion ?? '')
  const [modo, setModo] = useState<ModoAcceso>(punto?.modoAcceso ?? 'Ambos')
  const [comun, setComun] = useState(punto?.esComunZonaEmpresarial ?? false)
  const [errores, setErrores] = useState<{ nombre?: string; idSede?: string }>({})
  const opcionesSede = sedes.filter((s) => s.activo || s.idSede === punto?.idSede)
  // La marca común solo aplica si la sede pertenece a una zona empresarial (el servidor lo vuelve a validar).
  const sedeConZona = Boolean(sedes.find((s) => String(s.idSede) === idSede)?.idZonaEmpresarial)
  const guardar = useMutation({
    mutationFn: () => {
      const datos = { idSede: Number(idSede), nombre: nombre.trim(), descripcion: descripcion.trim() || null, modoAcceso: modo, esComunZonaEmpresarial: sedeConZona && comun }
      return punto ? editarPuntoAcceso(punto.idPuntoAcceso, datos) : crearPuntoAcceso(datos)
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['admin-puntos-acceso'] })
      onGuardado(punto ? 'Punto de acceso actualizado.' : 'Punto de acceso creado.')
    },
  })

  function enviar(event: FormEvent) {
    event.preventDefault()
    const nuevos: typeof errores = {}
    if (!nombre.trim()) nuevos.nombre = 'El nombre del punto de acceso es obligatorio.'
    if (!idSede) nuevos.idSede = 'Selecciona la sede del punto de acceso.'
    setErrores(nuevos)
    if (Object.keys(nuevos).length === 0 && !guardar.isPending) guardar.mutate()
  }

  return (
    <Dialogo titulo={punto ? 'Editar punto de acceso' : 'Nuevo punto de acceso'} eyebrow="Puntos de acceso" icono="accesos"
      onClose={onClose} onSubmit={enviar} pie={<BotonesFormulario onCancelar={onClose} pendiente={guardar.isPending} />}>
      <div className="ad-rejilla">
        <Campo etiqueta="Sede" error={errores.idSede} ancho="completo">
          <Selector className="sel-campo" etiqueta="Sede" valor={idSede} invalido={Boolean(errores.idSede)} onChange={setIdSede}
            deshabilitado={Boolean(punto)}
            opciones={[{ valor: '', texto: 'Selecciona…', deshabilitada: true }, ...opcionesSede.map((s) => ({ valor: String(s.idSede), texto: s.nombre }))]} />
        </Campo>
        {punto && <p className="ad-ayuda" data-ancho="completo">El punto no cambia de sede: sus movimientos y operadores asignados quedan ligados a ella.</p>}
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
      {sedeConZona && (
        <Interruptor etiqueta="Punto común de la zona empresarial" checked={comun} onChange={setComun}
          ayuda={comun ? 'Acceso compartido de la zona; lo administra tu organización como dueña de la sede.' : 'Acceso propio de la sede.'} />
      )}
      {guardar.error && <p className="ad-mensaje" role="alert">{mensajeError(guardar.error, 'No se pudo guardar el punto de acceso.')}</p>}
    </Dialogo>
  )
}
