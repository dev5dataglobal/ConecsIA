import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useState, type FormEvent } from 'react'
import { Selector } from '../../shared/ui/Selector'
import { BotonesFormulario, Campo, Dialogo } from '../administracion/componentes'
import { mensajeError, useDiferido } from '../administracion/utilidades'
import { useAcciones } from '../modulos/apiModulos'
import { consultarCatalogosPersona, consultarDispositivos, consultarPersona, consultarPersonas } from '../personas/apiPersonas'
import { PersonaFormulario } from '../personas/PersonasPage'
import {
  DIAS, MODALIDADES, crearAcceso,
  type CategoriaAcceso, type Franja, type Modalidad, type OpcionesAcceso, type RegistroAcceso, type TipoObjeto,
} from './apiControlAccesos'
import { hoyIso } from './formato'
import { cuerpoAcceso, validarRegistroAcceso, type DatosAcceso, type ErroresAcceso } from './reglasAcceso'

/*
 * Único punto de creación del registro de acceso: persona (se busca o se registra sin duplicarla), categoría, destino,
 * anfitrión, motivo, tipo de permanencia, horario y vehículos/dispositivos. La validación aquí es de cortesía; el
 * servidor vuelve a validar todo.
 */

export function NuevoAcceso({ opciones, idPersonaInicial, onClose, onCreado }: {
  opciones: OpcionesAcceso
  idPersonaInicial: number | null
  onClose: () => void
  onCreado: (a: RegistroAcceso) => void
}) {
  const personasMod = useAcciones('personas')
  const vehiculosMod = useAcciones('vehiculos')
  const dispositivosMod = useAcciones('dispositivos')
  const [idPersona, setIdPersona] = useState(idPersonaInicial ?? 0)
  const categorias = opciones.categorias
  const [datos, setDatos] = useState<DatosAcceso>(() => ({
    idCategoriaAcceso: categorias.length === 1 ? categorias[0]!.idCategoriaAcceso : 0,
    idSede: opciones.sedes.length === 1 ? opciones.sedes[0]!.id : 0, idArea: 0, idMembresiaResponsable: 0,
    modalidad: categorias.length === 1 ? categorias[0]!.modalidadesPermitidas[0] ?? 'Unica' : 'Unica',
    fechaInicio: hoyIso(), fechaFin: null, horaInicio: null, horaFin: null, franjas: [], usaHorarioBase: false, motivo: '', observacion: null, objetos: [],
  }))
  const [buscar, setBuscar] = useState('')
  const [registrar, setRegistrar] = useState(false)
  const [errores, setErrores] = useState<ErroresAcceso>({})
  const texto = useDiferido(buscar)
  const personas = useQuery({ queryKey: ['personas', 'selector', texto], queryFn: () => consultarPersonas({ buscar: texto, estado: 'Activo' }), enabled: texto.trim().length >= 2 && !idPersona })
  const persona = useQuery({ queryKey: ['persona', idPersona], queryFn: () => consultarPersona(idPersona), enabled: idPersona > 0 })
  const dispositivos = useQuery({ queryKey: ['dispositivos', idPersona], queryFn: () => consultarDispositivos(idPersona), enabled: idPersona > 0 && dispositivosMod.consultar })
  const catalogos = useQuery({ queryKey: ['personas', 'catalogos'], queryFn: consultarCatalogosPersona, enabled: registrar })
  const queryClient = useQueryClient()
  const crear = useMutation({
    mutationFn: () => crearAcceso(cuerpoAcceso(idPersona, datos)),
    onSuccess: (creado) => {
      void queryClient.invalidateQueries({ queryKey: ['accesos'] })
      onCreado(creado)
    },
  })
  const cambiar = (parcial: Partial<DatosAcceso>) => setDatos((d) => ({ ...d, ...parcial }))
  const categoria = categorias.find((c) => c.idCategoriaAcceso === datos.idCategoriaAcceso)
  const areas = opciones.areas.filter((x) => x.idSede === datos.idSede)
  // Con responsables designados en el destino, solo ellos pueden ser anfitriones (D-36); el servidor lo vuelve a validar.
  const designados = opciones.areas.find((x) => x.id === datos.idArea)?.responsables ?? []
  const anfitriones = designados.length ? opciones.anfitriones.filter((r) => designados.includes(r.id)) : opciones.anfitriones
  function elegirArea(idArea: number) {
    const responsables = opciones.areas.find((x) => x.id === idArea)?.responsables ?? []
    cambiar({ idArea, ...(responsables.length && !responsables.includes(datos.idMembresiaResponsable) ? { idMembresiaResponsable: responsables.length === 1 ? responsables[0]! : 0 } : {}) })
  }

  function elegirModalidad(m: Modalidad) {
    setErrores({})
    // Al cambiar de tipo se limpian los campos que no le aplican para no enviar condiciones ocultas.
    cambiar({ modalidad: m, fechaFin: null, horaInicio: null, horaFin: null, usaHorarioBase: false,
      franjas: m === 'Recurrente' ? [1, 2, 3, 4, 5].map((dia) => ({ diaSemana: dia, horaInicio: '07:00', horaFin: '17:00' })) : [] })
  }

  function elegirCategoria(c: CategoriaAcceso) {
    const objetos = datos.objetos.filter((o) => (o.tipoObjeto === 'Vehiculo' ? c.permiteVehiculos : c.permiteDispositivos))
    cambiar({ idCategoriaAcceso: c.idCategoriaAcceso, objetos })
    if (!c.modalidadesPermitidas.includes(datos.modalidad)) elegirModalidad(c.modalidadesPermitidas[0] ?? 'Unica')
  }

  function alternarObjeto(tipoObjeto: TipoObjeto, idObjeto: number, marcado: boolean) {
    cambiar({ objetos: marcado ? [...datos.objetos, { tipoObjeto, idObjeto }] : datos.objetos.filter((o) => !(o.tipoObjeto === tipoObjeto && o.idObjeto === idObjeto)) })
  }

  function enviar(event: FormEvent) {
    event.preventDefault()
    const nuevos = validarRegistroAcceso(idPersona, datos, categoria, opciones.permitePermanente)
    setErrores(nuevos)
    if (Object.keys(nuevos).length === 0 && !crear.isPending) crear.mutate()
  }

  const p = persona.data
  const unDia = datos.modalidad === 'Unica' || datos.modalidad === 'Diaria'
  const conFranjas = datos.modalidad === 'Recurrente' || datos.modalidad === 'Permanente'
  const vehiculos = vehiculosMod.consultar && categoria?.permiteVehiculos !== false ? (p?.vehiculos ?? []).filter((v) => v.activo) : []
  const equipos = dispositivosMod.consultar && categoria?.permiteDispositivos !== false ? (dispositivos.data ?? []).filter((d) => d.activo) : []
  const automatico = categoria?.flujoAprobacion === 'Automatico'

  return (
    <Dialogo titulo="Nuevo registro de acceso" eyebrow="Control de accesos" icono="accesos" ancho="amplio" onClose={onClose} onSubmit={enviar}
      pie={<BotonesFormulario onCancelar={onClose} pendiente={crear.isPending} texto="Registrar acceso" />}>
      <Campo etiqueta="Persona" error={errores.persona} ancho="completo">
        {idPersona && p ? (
          <div className="pe-existente">
            <strong>{p.nombres} {p.apellidos}</strong>
            <span className="ad-tenue">{p.tipoDocumento} {p.numeroDocumento}{p.tipoPersona ? ` · ${p.tipoPersona}` : ''}</span>
            {!p.activo && <span className="ad-mensaje" role="alert">Persona inactiva: no se puede registrar su acceso.</span>}
            <button type="button" className="ad-enlace" onClick={() => { setIdPersona(0); setBuscar(''); cambiar({ objetos: [] }) }}>Cambiar</button>
          </div>
        ) : idPersona && persona.isLoading ? (
          <span className="ad-tenue">Cargando persona…</span>
        ) : (
          <div className="au-buscador-persona">
            <input value={buscar} onChange={(e) => setBuscar(e.target.value)} placeholder="Nombre o documento (mínimo 2 caracteres)" aria-label="Buscar persona" aria-invalid={Boolean(errores.persona)} />
            {personas.data && (
              <ul role="listbox" aria-label="Personas encontradas">
                {personas.data.length === 0 && <li className="ad-tenue">Sin coincidencias entre personas activas.</li>}
                {personas.data.slice(0, 8).map((x) => (
                  <li key={x.idPersona}>
                    <button type="button" role="option" aria-selected={false} onClick={() => setIdPersona(x.idPersona)}>
                      {x.nombres} {x.apellidos} <small>{x.tipoDocumento} {x.numeroDocumento}</small>
                    </button>
                  </li>
                ))}
              </ul>
            )}
            {personasMod.anadir && <button type="button" className="ad-enlace" onClick={() => setRegistrar(true)}>Registrar persona nueva</button>}
          </div>
        )}
      </Campo>

      <fieldset className="ca-modalidades">
        <legend>Categoría de acceso</legend>
        {categorias.length === 0 ? <p className="ad-mensaje" role="alert">La organización no tiene categorías de acceso activas.</p> : (
          <div className="ca-modalidades-opciones">
            {categorias.map((c) => (
              <label key={c.idCategoriaAcceso} className="ca-modalidad">
                <input type="radio" name="categoria" checked={datos.idCategoriaAcceso === c.idCategoriaAcceso} onChange={() => elegirCategoria(c)} />
                <strong>{c.nombre}</strong>
                <small>{c.flujoAprobacion === 'Automatico' ? 'Aprobación automática' : c.flujoAprobacion === 'Anfitrion' ? 'La aprueba el anfitrión' : 'La aprueba seguridad o administración'}</small>
              </label>
            ))}
          </div>
        )}
        {errores.categoria && <p className="ad-mensaje" role="alert">{errores.categoria}</p>}
      </fieldset>

      <fieldset className="ca-modalidades">
        <legend>Tipo de permanencia</legend>
        <div className="ca-modalidades-opciones">
          {MODALIDADES.filter((m) => !categoria || categoria.modalidadesPermitidas.includes(m.valor)).map((m) => {
            const bloqueada = m.valor === 'Permanente' && !opciones.permitePermanente
            return (
              <label key={m.valor} className="ca-modalidad" data-bloqueada={bloqueada || undefined}>
                <input type="radio" name="modalidad" value={m.valor} checked={datos.modalidad === m.valor} disabled={bloqueada} onChange={() => elegirModalidad(m.valor)} />
                <strong>{m.texto}</strong>
                <small>{bloqueada ? 'No disponible: la política de la organización no la permite.' : `${m.grupo}. ${m.descripcion}`}</small>
              </label>
            )
          })}
        </div>
        {errores.modalidad && <p className="ad-mensaje" role="alert">{errores.modalidad}</p>}
      </fieldset>

      <div className="ad-rejilla">
        <Campo etiqueta="Sede" error={errores.sede}>
          <Selector className="sel-campo" etiqueta="Sede" valor={String(datos.idSede || '')} invalido={Boolean(errores.sede)} onChange={(v) => cambiar({ idSede: Number(v), idArea: 0 })}
            opciones={[{ valor: '', texto: opciones.sedes.length ? 'Selecciona…' : 'No hay sedes en tu alcance', deshabilitada: true }, ...opciones.sedes.map((s) => ({ valor: String(s.id), texto: s.nombre }))]} />
        </Campo>
        <Campo etiqueta="Destino" error={errores.area}>
          <Selector className="sel-campo" etiqueta="Destino" valor={String(datos.idArea || '')} invalido={Boolean(errores.area)} onChange={(v) => elegirArea(Number(v))}
            opciones={[{ valor: '', texto: !datos.idSede ? 'Primero elige la sede' : areas.length ? 'Selecciona…' : 'La sede no tiene destinos activos', deshabilitada: true }, ...areas.map((x) => ({ valor: String(x.id), texto: x.nombre }))]} />
        </Campo>
        <Campo etiqueta="Anfitrión o responsable" error={errores.anfitrion} ancho="completo">
          <Selector className="sel-campo" etiqueta="Anfitrión o responsable" valor={String(datos.idMembresiaResponsable || '')} invalido={Boolean(errores.anfitrion)} buscable
            onChange={(v) => cambiar({ idMembresiaResponsable: Number(v) })}
            opciones={[{ valor: '', texto: 'Selecciona…', deshabilitada: true }, ...anfitriones.map((r) => ({ valor: String(r.id), texto: r.nombre }))]} />
          {designados.length > 0 && <small className="ad-ayuda">Solo los responsables designados para este destino.</small>}
        </Campo>

        {unDia && (
          <>
            <Campo etiqueta="Día" error={errores.fechaInicio} ancho="completo">
              <input type="date" value={datos.fechaInicio} min={hoyIso()} aria-invalid={Boolean(errores.fechaInicio)} onChange={(e) => cambiar({ fechaInicio: e.target.value })} />
            </Campo>
            <Campo etiqueta="Desde la hora (opcional)" error={errores.horas}>
              <input type="time" value={datos.horaInicio ?? ''} aria-invalid={Boolean(errores.horas)} onChange={(e) => cambiar({ horaInicio: e.target.value || null })} />
            </Campo>
            <Campo etiqueta="Hasta la hora (opcional)">
              <input type="time" value={datos.horaFin ?? ''} onChange={(e) => cambiar({ horaFin: e.target.value || null })} />
            </Campo>
          </>
        )}
        {datos.modalidad === 'Temporal' && (
          <>
            <Campo etiqueta="Fecha de inicio" error={errores.fechaInicio}>
              <input type="date" value={datos.fechaInicio} min={hoyIso()} aria-invalid={Boolean(errores.fechaInicio)} onChange={(e) => cambiar({ fechaInicio: e.target.value })} />
            </Campo>
            <Campo etiqueta="Hora de inicio" error={errores.horas}>
              <input type="time" value={datos.horaInicio ?? ''} aria-invalid={Boolean(errores.horas)} onChange={(e) => cambiar({ horaInicio: e.target.value || null })} />
            </Campo>
            <Campo etiqueta="Fecha de finalización" error={errores.fechaFin}>
              <input type="date" value={datos.fechaFin ?? ''} min={datos.fechaInicio || hoyIso()} aria-invalid={Boolean(errores.fechaFin)} onChange={(e) => cambiar({ fechaFin: e.target.value || null })} />
            </Campo>
            <Campo etiqueta="Hora de finalización">
              <input type="time" value={datos.horaFin ?? ''} onChange={(e) => cambiar({ horaFin: e.target.value || null })} />
            </Campo>
          </>
        )}
        {conFranjas && (
          <>
            <Campo etiqueta={datos.modalidad === 'Recurrente' ? 'Se repite desde' : 'Vigente desde'} error={errores.fechaInicio}>
              <input type="date" value={datos.fechaInicio} aria-invalid={Boolean(errores.fechaInicio)} onChange={(e) => cambiar({ fechaInicio: e.target.value })} />
            </Campo>
            {datos.modalidad === 'Recurrente' ? (
              <Campo etiqueta="Hasta (opcional)" error={errores.fechaFin}>
                <input type="date" value={datos.fechaFin ?? ''} min={datos.fechaInicio || hoyIso()} aria-invalid={Boolean(errores.fechaFin)} onChange={(e) => cambiar({ fechaFin: e.target.value || null })} />
              </Campo>
            ) : <span />}
            <EditorFranjas franjas={datos.franjas} usaHorarioBase={datos.usaHorarioBase} error={errores.franjas} opcional={datos.modalidad === 'Permanente'}
              onChange={(franjas, usaHorarioBase) => cambiar({ franjas, usaHorarioBase })} />
          </>
        )}

        <Campo etiqueta={categoria?.exigeMotivo === false ? 'Motivo (opcional)' : 'Motivo'} error={errores.motivo} ancho="completo">
          <textarea value={datos.motivo ?? ''} maxLength={500} aria-invalid={Boolean(errores.motivo)} onChange={(e) => cambiar({ motivo: e.target.value })} />
        </Campo>
        <Campo etiqueta="Observación (opcional)" ancho="completo">
          <textarea value={datos.observacion ?? ''} maxLength={1000} onChange={(e) => cambiar({ observacion: e.target.value || null })} />
        </Campo>
      </div>

      {idPersona > 0 && (vehiculos.length > 0 || equipos.length > 0) && (
        <fieldset className="ca-objetos">
          <legend>Vehículos y dispositivos que ingresan</legend>
          <p className="ad-tenue pe-nota">Cada uno se aprueba o rechaza por separado de la persona.</p>
          {vehiculos.map((v) => (
            <label key={`v-${v.idVehiculo}`} className="ca-objeto">
              <input type="checkbox" checked={datos.objetos.some((o) => o.tipoObjeto === 'Vehiculo' && o.idObjeto === v.idVehiculo)}
                onChange={(e) => alternarObjeto('Vehiculo', v.idVehiculo, e.target.checked)} />
              <span><strong>{v.placa}</strong> <small>{['Vehículo', v.tipo, v.marca, v.color].filter(Boolean).join(' · ')}</small></span>
            </label>
          ))}
          {equipos.map((d) => (
            <label key={`d-${d.idDispositivo}`} className="ca-objeto">
              <input type="checkbox" checked={datos.objetos.some((o) => o.tipoObjeto === 'Dispositivo' && o.idObjeto === d.idDispositivo)}
                onChange={(e) => alternarObjeto('Dispositivo', d.idDispositivo, e.target.checked)} />
              <span><strong>{d.codigoIdentificador}</strong> <small>{['Dispositivo', d.tipoDispositivo, d.marca, d.serial].filter(Boolean).join(' · ')}</small></span>
            </label>
          ))}
        </fieldset>
      )}

      <p className="ad-tenue pe-nota">
        {automatico
          ? <>Esta categoría se <strong>aprueba automáticamente</strong>; la entrada valida igualmente vigencia y horario.</>
          : <>El registro queda <strong>pendiente de aprobación</strong> y no habilita el ingreso hasta que se apruebe.</>}
      </p>
      {crear.error && <p className="ad-mensaje" role="alert">{mensajeError(crear.error, 'No se pudo registrar el acceso.')}</p>}
      {registrar && (
        <PersonaFormulario persona={null} catalogos={catalogos.data} onClose={() => setRegistrar(false)}
          onVerExistente={(id) => { setRegistrar(false); setIdPersona(id) }}
          onGuardado={(id) => { setRegistrar(false); if (id) setIdPersona(id) }} />
      )}
    </Dialogo>
  )
}

/** Franjas por día (varias por día) o el horario base de la sede/organización. */
export function EditorFranjas({ franjas, usaHorarioBase, error, opcional, onChange }: {
  franjas: Franja[]
  usaHorarioBase: boolean
  error?: string
  opcional?: boolean
  onChange: (franjas: Franja[], usaHorarioBase: boolean) => void
}) {
  const actualizar = (i: number, parcial: Partial<Franja>) => onChange(franjas.map((f, j) => (j === i ? { ...f, ...parcial } : f)), false)
  return (
    <fieldset className="ca-franjas" data-ancho="completo" aria-invalid={Boolean(error)}>
      <legend>Horario de ingreso{opcional ? ' (opcional: sin franjas, a cualquier hora)' : ''}</legend>
      <label className="ca-objeto">
        <input type="checkbox" checked={usaHorarioBase} onChange={(e) => onChange(e.target.checked ? [] : franjas, e.target.checked)} />
        <span>Usar el horario base de la sede u organización</span>
      </label>
      {!usaHorarioBase && (
        <>
          <ul className="ca-franjas-lista">
            {franjas.map((f, i) => (
              <li key={i}>
                <select aria-label={`Día de la franja ${i + 1}`} value={f.diaSemana} onChange={(e) => actualizar(i, { diaSemana: Number(e.target.value) })}>
                  {DIAS.map((d) => <option key={d.valor} value={d.valor}>{d.texto}</option>)}
                </select>
                <input type="time" aria-label={`Inicio de la franja ${i + 1}`} value={f.horaInicio.slice(0, 5)} onChange={(e) => actualizar(i, { horaInicio: e.target.value })} />
                <input type="time" aria-label={`Fin de la franja ${i + 1}`} value={f.horaFin.slice(0, 5)} onChange={(e) => actualizar(i, { horaFin: e.target.value })} />
                <button type="button" className="ad-enlace" onClick={() => onChange(franjas.filter((_, j) => j !== i), false)}>Quitar</button>
              </li>
            ))}
          </ul>
          <button type="button" className="ad-boton-sec" onClick={() => onChange([...franjas, { diaSemana: 1, horaInicio: '08:00', horaFin: '12:00' }], false)}>Agregar franja</button>
        </>
      )}
      {error && <small className="ad-campo-error" role="alert">{error}</small>}
    </fieldset>
  )
}
