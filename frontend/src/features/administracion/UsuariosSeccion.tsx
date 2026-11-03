import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useState, type FormEvent } from 'react'
import { useAcciones } from '../modulos/apiModulos'
import { leerOrganizacionActiva } from '../sesion/sesion'
import {
  cambiarEstadoUsuario,
  consultarPerfilesAsignables,
  consultarUsuarios,
  crearUsuario,
  editarUsuario,
  type UsuarioAdmin,
} from './apiAdministracion'
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
import { ESTADOS, iniciales, mensajeError, useAviso, useDiferido } from './utilidades'
import type { SeccionProps } from './secciones'

const CORREO = /^[^\s@]+@[^\s@]+\.[^\s@]+$/

export function UsuariosSeccion({ pestanas }: SeccionProps) {
  const acciones = useAcciones('usuarios')
  const org = leerOrganizacionActiva()
  const [buscar, setBuscar] = useState('')
  const [estado, setEstado] = useState('')
  const [formulario, setFormulario] = useState<UsuarioAdmin | 'nuevo' | null>(null)
  const [confirmar, setConfirmar] = useState<UsuarioAdmin | null>(null)
  const { avisar, aviso } = useAviso()
  const queryClient = useQueryClient()
  const texto = useDiferido(buscar)
  const usuarios = useQuery({
    queryKey: ['admin-usuarios', org, texto, estado],
    queryFn: () => consultarUsuarios({ buscar: texto, estado }),
    enabled: acciones.consultar,
    placeholderData: keepPreviousData,
  })
  const estadoMutation = useMutation({
    mutationFn: (u: UsuarioAdmin) => cambiarEstadoUsuario(u.idMembresia, !u.activo),
    onSuccess: (u) => {
      void queryClient.invalidateQueries({ queryKey: ['admin-usuarios'] })
      setConfirmar(null)
      avisar(u.activo ? 'Usuario activado.' : 'Usuario inactivado. Sus sesiones en esta organización se cerraron.')
    },
  })

  return (
    <Marco
      titulo="Usuarios"
      descripcion="Cuentas con acceso a la organización activa y sus perfiles."
      pestanas={pestanas}
      accion={acciones.anadir && <BotonNuevo texto="Nuevo usuario" onClick={() => setFormulario('nuevo')} />}
    >
      <div className="ad-herramientas">
        <Buscador valor={buscar} onChange={setBuscar} etiqueta="Buscar usuarios" placeholder="Buscar por nombre, correo o documento" />
        <Filtro etiqueta="Filtrar por estado" valor={estado} onChange={setEstado} opciones={ESTADOS} />
      </div>
      {!acciones.consultar ? (
        <Estado tipo="permiso" texto="Tu perfil puede gestionar usuarios, pero no consultarlos." />
      ) : (
        <EstadoConsulta
          loading={usuarios.isLoading}
          error={usuarios.error}
          empty={!usuarios.data?.length}
          vacio={buscar || estado ? 'Ningún usuario coincide con los filtros.' : 'Aún no hay usuarios registrados.'}
        />
      )}
      {usuarios.data && usuarios.data.length > 0 && (
        <div className="ad-tabla-wrap">
          <table className="ad-tabla">
            <thead>
              <tr>
                <th scope="col">Usuario</th>
                <th scope="col">Perfiles</th>
                <th scope="col">Estado</th>
                {acciones.editar && <th scope="col"><span className="sr-only">Acciones</span></th>}
              </tr>
            </thead>
            <tbody>
              {usuarios.data.map((usuario) => (
                <tr key={usuario.idMembresia}>
                  <td>
                    <span className="ad-persona">
                      <span className="ad-avatar" aria-hidden="true">{iniciales(usuario.nombre ?? usuario.correo)}</span>
                      <span>
                        <strong>{usuario.nombre ?? usuario.correo}</strong>
                        <small>{usuario.nombre ? usuario.correo : 'Sin persona vinculada'}{usuario.documento ? ` · ${usuario.documento}` : ''}</small>
                      </span>
                    </span>
                  </td>
                  <td>
                    {usuario.perfiles.length ? (
                      <span className="ad-etiquetas">{usuario.perfiles.map((p) => <span key={p} className="ad-etiqueta">{p}</span>)}</span>
                    ) : (
                      <span className="ad-tenue">Sin perfiles</span>
                    )}
                  </td>
                  <td><Estatus activo={usuario.activo} /></td>
                  {acciones.editar && (
                    <td className="ad-acciones">
                      <BotonIcono etiqueta={`Editar ${usuario.correo}`} icono="editar" onClick={() => setFormulario(usuario)} />
                      <BotonIcono
                        etiqueta={`${usuario.activo ? 'Inactivar' : 'Activar'} ${usuario.correo}`}
                        icono={usuario.activo ? 'cerrar' : 'check'}
                        peligro={usuario.activo}
                        onClick={() => { estadoMutation.reset(); setConfirmar(usuario) }}
                      />
                    </td>
                  )}
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {formulario && (
        <UsuarioFormulario
          usuario={formulario === 'nuevo' ? null : formulario}
          onClose={() => setFormulario(null)}
          onGuardado={(mensaje) => { setFormulario(null); avisar(mensaje) }}
        />
      )}
      {confirmar && (
        <Confirmar
          titulo={confirmar.activo ? '¿Inactivar usuario?' : '¿Activar usuario?'}
          texto={confirmar.activo
            ? `${confirmar.correo} no podrá ingresar a esta organización y sus sesiones abiertas se cerrarán. Sus perfiles e historial se conservan.`
            : `${confirmar.correo} podrá volver a ingresar con sus perfiles actuales.`}
          accion={confirmar.activo ? 'Inactivar' : 'Activar'}
          peligro={confirmar.activo}
          pendiente={estadoMutation.isPending}
          error={estadoMutation.error ? mensajeError(estadoMutation.error) : null}
          onConfirmar={() => estadoMutation.mutate(confirmar)}
          onCancelar={() => setConfirmar(null)}
        />
      )}
      {aviso}
    </Marco>
  )
}

function UsuarioFormulario({ usuario, onClose, onGuardado }: { usuario: UsuarioAdmin | null; onClose: () => void; onGuardado: (mensaje: string) => void }) {
  const queryClient = useQueryClient()
  const [correo, setCorreo] = useState(usuario?.correo ?? '')
  const [contrasena, setContrasena] = useState('')
  const [roles, setRoles] = useState<number[]>(usuario?.idRoles ?? [])
  const [activo, setActivo] = useState(usuario?.activo ?? true)
  const [errores, setErrores] = useState<Record<string, string>>({})
  const perfiles = useQuery({ queryKey: ['perfiles-asignables', leerOrganizacionActiva()], queryFn: consultarPerfilesAsignables })
  const guardar = useMutation({
    mutationFn: () => usuario
      ? editarUsuario(usuario.idMembresia, { correo: correo.trim(), idPersona: usuario.idPersona, idRoles: roles, activo })
      : crearUsuario({ correo: correo.trim(), contrasena, idRoles: roles, activo }),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['admin-usuarios'] })
      void queryClient.invalidateQueries({ queryKey: ['admin-perfiles'] })
      onGuardado(usuario ? 'Usuario actualizado.' : 'Usuario creado.')
    },
  })

  // Perfiles ya asignados que el usuario actual no puede asignar (p. ej. inactivos) se muestran igual para no perderlos.
  const opciones = [
    ...(perfiles.data ?? []),
    ...(usuario?.idRoles ?? [])
      .filter((id) => !perfiles.data?.some((p) => p.id === id))
      .map((id, i) => ({ id, nombre: usuario?.perfiles[usuario.idRoles.indexOf(id)] ?? `Perfil ${i + 1}`, bloqueado: true })),
  ] as { id: number; nombre: string; bloqueado?: boolean }[]

  function enviar(event: FormEvent) {
    event.preventDefault()
    const nuevos: Record<string, string> = {}
    if (!CORREO.test(correo.trim())) nuevos.correo = 'Ingresa un correo válido.'
    if (!usuario && contrasena.length < 8) nuevos.contrasena = 'La contraseña debe tener al menos 8 caracteres.'
    setErrores(nuevos)
    if (Object.keys(nuevos).length === 0 && !guardar.isPending) guardar.mutate()
  }

  const alternar = (id: number) => setRoles((actual) => (actual.includes(id) ? actual.filter((x) => x !== id) : [...actual, id]))

  return (
    <Dialogo
      titulo={usuario ? 'Editar usuario' : 'Nuevo usuario'}
      eyebrow="Usuarios"
      icono="personas"
      onClose={onClose}
      onSubmit={enviar}
      pie={<BotonesFormulario onCancelar={onClose} pendiente={guardar.isPending} />}
    >
      <Campo etiqueta="Correo electrónico" error={errores.correo}>
        <input type="email" required autoFocus autoComplete="off" value={correo} aria-invalid={Boolean(errores.correo)} onChange={(e) => setCorreo(e.target.value)} />
      </Campo>
      {!usuario && (
        <Campo etiqueta="Contraseña inicial" error={errores.contrasena}>
          <input type="password" required minLength={8} autoComplete="new-password" value={contrasena} aria-invalid={Boolean(errores.contrasena)} onChange={(e) => setContrasena(e.target.value)} />
        </Campo>
      )}
      <fieldset className="ad-grupo">
        <legend>Perfiles</legend>
        {perfiles.isLoading && <div className="ad-cargando" aria-busy="true" aria-label="Cargando perfiles"><div /></div>}
        {perfiles.error && <p className="ad-mensaje" role="alert">{mensajeError(perfiles.error, 'No se pudieron cargar los perfiles.')}</p>}
        {!perfiles.isLoading && opciones.length === 0 && <p className="ad-ayuda">La organización no tiene perfiles activos que puedas asignar.</p>}
        <div className="ad-opciones">
          {opciones.map((perfil) => (
            <label key={perfil.id} className="ad-opcion">
              <input type="checkbox" checked={roles.includes(perfil.id)} disabled={perfil.bloqueado} onChange={() => alternar(perfil.id)} />
              <span>{perfil.nombre}{perfil.bloqueado && <small> · no editable</small>}</span>
            </label>
          ))}
        </div>
        {roles.length === 0 && opciones.length > 0 && <p className="ad-ayuda">Sin perfiles, el usuario podrá ingresar pero no tendrá acceso a ningún módulo.</p>}
      </fieldset>
      <Interruptor
        etiqueta="Usuario activo"
        ayuda={activo ? 'Puede ingresar a esta organización.' : 'No podrá ingresar; sus sesiones se cerrarán.'}
        checked={activo}
        onChange={setActivo}
      />
      {guardar.error && <p className="ad-mensaje" role="alert">{mensajeError(guardar.error, 'No se pudo guardar el usuario.')}</p>}
    </Dialogo>
  )
}
