import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useState, type FormEvent } from 'react'
import { useLocation, useNavigate } from 'react-router'
import { ApiError } from '../../shared/api/problemDetails'
import { ProblemDetailsAlert } from '../../shared/ui/ProblemDetailsAlert'
import { ingresar } from './apiSesion'

/** Inicio de sesión real (HU-006, ADR-006). La API redacta los errores de credenciales. */
export function IngresarPage() {
  const navigate = useNavigate(); const location = useLocation(); const queryClient = useQueryClient()
  const destino = (location.state as { desde?: string } | null)?.desde ?? '/sedes'
  const [correo, setCorreo] = useState(''); const [contrasena, setContrasena] = useState(''); const [mostrar, setMostrar] = useState(false)
  function continuar() { queryClient.removeQueries({ queryKey: ['me'] }); queryClient.removeQueries({ queryKey: ['sedes'] }); void navigate(destino, { replace: true }) }
  const ingreso = useMutation({ mutationFn: () => ingresar(correo.trim(), contrasena), onSuccess: continuar, onSettled: () => setContrasena('') })
  function enviar(e: FormEvent) { e.preventDefault(); if (!ingreso.isPending) ingreso.mutate() }
  return <main className="login-page">
    <section className="login-hero" aria-label="ConecsIA"><div className="login-orb" aria-hidden="true" />
      <div className="login-brand"><span className="login-brand-mark">✦</span><span><strong>ConecsIA</strong><small>Visitantes y control de accesos</small></span></div>
      <div className="login-hero-copy"><span className="login-kicker">Plataforma multiempresa y multisede</span><h1>Gestiona cada acceso<br />de forma segura y<br />eficiente.</h1><p>Visitantes, contratistas, proveedores y autorizaciones bajo un mismo control, en todas tus sedes y en tiempo real.</p></div>
      <div className="login-benefits"><span><b>01</b><small>Control centralizado</small></span><span><b>24/7</b><small>Visibilidad continua</small></span><span><b>100%</b><small>Trazabilidad</small></span></div>
    </section>
    <section className="login-form-panel"><div className="login-form-wrap"><h1 className="sr-only">Ingresar</h1><div className="login-heading"><h2>Bienvenido</h2><p>Inicia sesión para continuar</p></div>
      <form onSubmit={enviar} className="login-form">{ingreso.error instanceof ApiError && <ProblemDetailsAlert problem={ingreso.error.problem} />}
        <label className="login-field"><span>Correo electrónico o usuario</span><span className="login-input-wrap"><span className="login-input-icon" aria-hidden="true">✉</span><input aria-label="Correo electrónico" type="email" name="correo" autoComplete="username" required value={correo} onChange={(e) => setCorreo(e.target.value)} /></span></label>
        <label className="login-field"><span>Contraseña</span><span className="login-input-wrap"><span className="login-input-icon" aria-hidden="true">♙</span><input aria-label="Contraseña" type={mostrar ? 'text' : 'password'} name="contrasena" autoComplete="current-password" required value={contrasena} onChange={(e) => setContrasena(e.target.value)} /><button type="button" className="login-toggle" onClick={() => setMostrar((v) => !v)} aria-label={mostrar ? 'Ocultar contraseña' : 'Mostrar contraseña'}>{mostrar ? '◉' : '◎'}</button></span></label>
        <div className="login-options"><label><input type="checkbox" disabled title="La política de sesión actual es administrada por el servidor" /> <span>Recordarme</span></label><button type="button" className="login-link" disabled>¿Olvidaste tu contraseña?</button></div>
        <button type="submit" aria-label="Ingresar" disabled={ingreso.isPending} className="login-submit">{ingreso.isPending ? 'Iniciando sesión…' : 'Iniciar sesión'}</button><div className="login-divider"><span>O continúa con</span></div><button type="button" className="login-microsoft" disabled title="Microsoft no está habilitado en el alcance actual"><span className="ms-logo" aria-hidden="true">▦</span>Iniciar sesión con Microsoft</button>
      </form><p className="login-note">La sesión está protegida y administrada por ConecsIA.</p></div></section>
  </main>
}
