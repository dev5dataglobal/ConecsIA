import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import { ApiError } from '../../shared/api/problemDetails'
import { Selector } from '../../shared/ui/Selector'
import { BotonIcono, Campo, Confirmar, EstadoConsulta, Estatus } from '../administracion/componentes'
import { mensajeError } from '../administracion/utilidades'
import {
  agregarDispositivo, cambiarEstadoDispositivo, consultarDispositivos, consultarTiposDispositivo, crearTipoDispositivo, editarDispositivo,
  type Dispositivo, type DispositivoEntrada,
} from './apiPersonas'

/*
 * Dispositivos (equipos externos) de una persona. La persona y la organización salen de la ruta y del servidor;
 * inactivar conserva el dispositivo, sus autorizaciones y movimientos.
 */

const vacio = (): DispositivoEntrada => ({ idTipoDispositivo: 0, codigoIdentificador: '', serial: null, marca: null, modelo: null, color: null, descripcion: null })
type ErroresDispositivo = Partial<Record<'idTipoDispositivo' | 'codigoIdentificador', string>>

export function validarDispositivo(datos: DispositivoEntrada): ErroresDispositivo {
  const errores: ErroresDispositivo = {}
  if (!datos.idTipoDispositivo) errores.idTipoDispositivo = 'Selecciona el tipo de dispositivo.'
  if (!datos.codigoIdentificador.trim()) errores.codigoIdentificador = 'El identificador es obligatorio.'
  else if (datos.codigoIdentificador.trim().length > 60) errores.codigoIdentificador = 'Máximo 60 caracteres.'
  return errores
}

export function DispositivosPersona({ idPersona, nombre, puedeAnadir, puedeEditar }: { idPersona: number; nombre: string; puedeAnadir: boolean; puedeEditar: boolean }) {
  const queryClient = useQueryClient()
  const [edicion, setEdicion] = useState<Dispositivo | 'nuevo' | null>(null)
  const [datos, setDatos] = useState<DispositivoEntrada>(vacio())
  const [errores, setErrores] = useState<ErroresDispositivo>({})
  const [confirmar, setConfirmar] = useState<Dispositivo | null>(null)
  const [nuevoTipo, setNuevoTipo] = useState('')
  const lista = useQuery({ queryKey: ['dispositivos', idPersona], queryFn: () => consultarDispositivos(idPersona) })
  const tipos = useQuery({ queryKey: ['tipos-dispositivo'], queryFn: consultarTiposDispositivo, enabled: edicion !== null })
  const refrescar = () => void queryClient.invalidateQueries({ queryKey: ['dispositivos', idPersona] })

  const guardar = useMutation({
    mutationFn: () => {
      const entrada = { ...datos, codigoIdentificador: datos.codigoIdentificador.trim() }
      return edicion && edicion !== 'nuevo' ? editarDispositivo(idPersona, edicion.idDispositivo, entrada) : agregarDispositivo(idPersona, entrada)
    },
    onSuccess: () => { refrescar(); setEdicion(null) },
  })
  const estado = useMutation({
    mutationFn: (d: Dispositivo) => cambiarEstadoDispositivo(idPersona, d.idDispositivo, !d.activo),
    onSuccess: () => { refrescar(); setConfirmar(null) },
  })
  const tipo = useMutation({
    mutationFn: () => crearTipoDispositivo({ nombre: nuevoTipo.trim(), descripcion: null }),
    onSuccess: (creado) => {
      void queryClient.invalidateQueries({ queryKey: ['tipos-dispositivo'] })
      setDatos((d) => ({ ...d, idTipoDispositivo: creado.idTipoDispositivo }))
      setNuevoTipo('')
    },
  })

  function abrir(d: Dispositivo | 'nuevo') {
    guardar.reset()
    setErrores({})
    setDatos(d === 'nuevo' ? vacio() : {
      idTipoDispositivo: d.idTipoDispositivo, codigoIdentificador: d.codigoIdentificador, serial: d.serial, marca: d.marca, modelo: d.modelo, color: d.color, descripcion: d.descripcion,
    })
    setEdicion(d)
  }
  function enviar() {
    const nuevos = validarDispositivo(datos)
    setErrores(nuevos)
    if (Object.keys(nuevos).length === 0 && !guardar.isPending) guardar.mutate()
  }
  const texto = (campo: 'serial' | 'marca' | 'modelo' | 'color' | 'descripcion', valor: string) => setDatos({ ...datos, [campo]: valor || null })

  // Un tipo inactivo que el dispositivo ya tiene se sigue mostrando para no perder la referencia.
  const activos = (tipos.data ?? []).filter((t) => t.activo || (edicion !== 'nuevo' && edicion?.idTipoDispositivo === t.idTipoDispositivo))
  const dispositivos = lista.data ?? []

  return (
    <section className="pe-seccion" aria-label={`Dispositivos de ${nombre}`}>
      <header>
        <h3>Dispositivos <small>de {nombre}</small></h3>
        {puedeAnadir && edicion === null && <button type="button" className="ad-enlace" onClick={() => abrir('nuevo')}>+ Agregar dispositivo</button>}
      </header>
      <EstadoConsulta loading={lista.isLoading} error={lista.error} empty={false} vacio="" />
      {lista.isSuccess && dispositivos.length === 0 && edicion === null && <p className="ad-tenue">Sin dispositivos registrados.</p>}
      {dispositivos.length > 0 && (
        <ul className="pe-lista">
          {dispositivos.map((d) => (
            <li key={d.idDispositivo} data-inactivo={!d.activo || undefined}>
              <span>
                <strong>{d.tipoDispositivo ?? 'Dispositivo'} · <span className="pe-placa">{d.codigoIdentificador}</span></strong>
                <small>{[d.marca, d.modelo, d.color, d.serial && `Serial ${d.serial}`].filter(Boolean).join(' · ') || 'Sin datos adicionales'}</small>
                {d.descripcion && <small>{d.descripcion}</small>}
              </span>
              {!d.activo && <Estatus activo={false} />}
              {puedeEditar && (
                <span>
                  {d.activo && <BotonIcono etiqueta={`Editar dispositivo ${d.codigoIdentificador}`} icono="editar" onClick={() => abrir(d)} />}
                  <BotonIcono etiqueta={`${d.activo ? 'Inactivar' : 'Activar'} dispositivo ${d.codigoIdentificador}`} icono={d.activo ? 'cerrar' : 'check'} peligro={d.activo}
                    onClick={() => { estado.reset(); setConfirmar(d) }} />
                </span>
              )}
            </li>
          ))}
        </ul>
      )}
      {edicion !== null && (
        <div className="pe-subformulario" role="group" aria-label={edicion === 'nuevo' ? 'Nuevo dispositivo' : 'Editar dispositivo'}
          onKeyDown={(e) => { if (e.key === 'Enter' && (e.target as HTMLElement).tagName === 'INPUT') { e.preventDefault(); enviar() } }}>
          <div className="ad-rejilla">
            <Campo etiqueta="Tipo de dispositivo" error={errores.idTipoDispositivo}>
              <Selector className="sel-campo" etiqueta="Tipo de dispositivo" valor={String(datos.idTipoDispositivo || '')} invalido={Boolean(errores.idTipoDispositivo)}
                onChange={(v) => setDatos({ ...datos, idTipoDispositivo: Number(v) })}
                opciones={[{ valor: '', texto: tipos.isLoading ? 'Cargando…' : 'Selecciona…', deshabilitada: true },
                  ...activos.map((t) => ({ valor: String(t.idTipoDispositivo), texto: t.activo ? t.nombre : `${t.nombre} (inactivo)` }))]} />
            </Campo>
            <Campo etiqueta="Identificador" error={errores.codigoIdentificador}>
              <input value={datos.codigoIdentificador} maxLength={60} autoFocus autoComplete="off" placeholder="Serial, código de inventario…"
                aria-invalid={Boolean(errores.codigoIdentificador)} onChange={(e) => setDatos({ ...datos, codigoIdentificador: e.target.value })} />
            </Campo>
            <Campo etiqueta="Serial (opcional)"><input value={datos.serial ?? ''} maxLength={60} onChange={(e) => texto('serial', e.target.value)} /></Campo>
            <Campo etiqueta="Marca (opcional)"><input value={datos.marca ?? ''} maxLength={60} onChange={(e) => texto('marca', e.target.value)} /></Campo>
            <Campo etiqueta="Modelo (opcional)"><input value={datos.modelo ?? ''} maxLength={60} onChange={(e) => texto('modelo', e.target.value)} /></Campo>
            <Campo etiqueta="Color (opcional)"><input value={datos.color ?? ''} maxLength={40} onChange={(e) => texto('color', e.target.value)} /></Campo>
            <Campo etiqueta="Descripción (opcional)" ancho="completo"><textarea value={datos.descripcion ?? ''} maxLength={500} onChange={(e) => texto('descripcion', e.target.value)} /></Campo>
          </div>
          {puedeAnadir && tipos.isSuccess && (
            <div className="pe-existente">
              <input aria-label="Nombre del nuevo tipo de dispositivo" placeholder={activos.length ? '¿Falta un tipo? Escríbelo aquí' : 'No hay tipos: crea el primero (p. ej. Portátil)'}
                value={nuevoTipo} maxLength={80} onChange={(e) => setNuevoTipo(e.target.value)} />
              <button type="button" className="ad-enlace" disabled={!nuevoTipo.trim() || tipo.isPending} onClick={() => tipo.mutate()}>+ Crear tipo</button>
              {tipo.error && <small className="ad-campo-error" role="alert">{tipo.error instanceof ApiError && tipo.error.status === 409 ? 'Ese tipo ya existe.' : mensajeError(tipo.error)}</small>}
            </div>
          )}
          {guardar.error && <p className="ad-mensaje" role="alert">{mensajeError(guardar.error, 'No se pudo guardar el dispositivo.')}</p>}
          <div className="pe-subacciones">
            <button type="button" className="ad-boton-sec" onClick={() => setEdicion(null)}>Cancelar</button>
            <button type="button" className="ad-boton" onClick={enviar} disabled={guardar.isPending}>{guardar.isPending ? 'Guardando…' : 'Guardar dispositivo'}</button>
          </div>
        </div>
      )}
      {confirmar && (
        <Confirmar titulo={confirmar.activo ? '¿Inactivar dispositivo?' : '¿Activar dispositivo?'}
          texto={confirmar.activo
            ? `El dispositivo ${confirmar.codigoIdentificador} quedará inactivo: no podrá recibir nuevas autorizaciones. Su historial de autorizaciones y movimientos se conserva.`
            : `El dispositivo ${confirmar.codigoIdentificador} volverá a estar activo para ${nombre}.`}
          accion={confirmar.activo ? 'Inactivar' : 'Activar'} peligro={confirmar.activo} pendiente={estado.isPending}
          error={estado.error ? mensajeError(estado.error) : null} onConfirmar={() => estado.mutate(confirmar)} onCancelar={() => setConfirmar(null)} />
      )}
    </section>
  )
}
