import type { OpcionDocumento, PersonaEntrada, TelefonoEntrada, VehiculoEntrada } from './apiPersonas'

/*
 * Validación en el cliente solo para orientar al usuario antes de enviar; el servidor repite cada regla
 * (formato por tipo de documento, unicidad, organización) y es quien decide.
 */

const CORREO = /^[^\s@]+@[^\s@]+\.[^\s@]+$/

export type Errores<T> = Partial<Record<keyof T, string>>

export const normalizarDocumento = (numero: string) => numero.trim().toUpperCase()
export const normalizarTelefono = (numero: string) => numero.replace(/[\s\-().]/g, '')
export const normalizarPlaca = (placa: string) => placa.replace(/[\s-]/g, '').toUpperCase()

export function validarPersona(datos: PersonaEntrada, tiposDocumento: OpcionDocumento[]): Errores<PersonaEntrada> {
  const errores: Errores<PersonaEntrada> = {}
  if (!datos.idTipoPersona) errores.idTipoPersona = 'Selecciona el tipo de persona.'
  if (!datos.idTipoDocumento) errores.idTipoDocumento = 'Selecciona el tipo de documento.'
  const documento = normalizarDocumento(datos.numeroDocumento)
  const tipo = tiposDocumento.find((t) => t.id === datos.idTipoDocumento)
  if (!documento) errores.numeroDocumento = 'El número de documento es obligatorio.'
  else if (tipo?.patronValidacion && !new RegExp(tipo.patronValidacion).test(documento))
    errores.numeroDocumento = `El número no tiene un formato válido para ${tipo.nombre}.`
  if (!datos.nombres.trim()) errores.nombres = 'Los nombres son obligatorios.'
  if (!datos.apellidos.trim()) errores.apellidos = 'Los apellidos son obligatorios.'
  if (!datos.correo.trim()) errores.correo = 'El correo es obligatorio.'
  else if (!CORREO.test(datos.correo.trim())) errores.correo = 'El correo no es válido.'
  return errores
}

export function validarTelefono(datos: TelefonoEntrada): Errores<TelefonoEntrada> {
  const errores: Errores<TelefonoEntrada> = {}
  const numero = normalizarTelefono(datos.numero)
  if (!/^\+[1-9][0-9]{0,3}$/.test(datos.codigoPais)) errores.codigoPais = 'Usa la forma +57.'
  if (!datos.tipo) errores.tipo = 'Selecciona el tipo.'
  if (!numero) errores.numero = 'El número es obligatorio.'
  else if (!/^[0-9]+$/.test(numero)) errores.numero = 'Solo se admiten números.'
  else if (datos.codigoPais === '+57') {
    if (datos.tipo === 'Móvil' && !/^3[0-9]{9}$/.test(numero)) errores.numero = 'El móvil debe tener 10 dígitos y empezar por 3.'
    else if (!/^(3[0-9]{9}|60[0-9]{8})$/.test(numero)) errores.numero = 'Debe tener 10 dígitos (móvil 3… o fijo 60…).'
  } else if (numero.length < 4 || numero.length + datos.codigoPais.length - 1 > 15) errores.numero = 'La longitud no es válida.'
  return errores
}

export function validarVehiculo(datos: VehiculoEntrada): Errores<VehiculoEntrada> {
  const errores: Errores<VehiculoEntrada> = {}
  const placa = normalizarPlaca(datos.placa)
  if (!placa) errores.placa = 'La placa es obligatoria.'
  else if (!/^(?=.*[A-Z])(?=.*[0-9])[A-Z0-9]{4,8}$/.test(placa)) errores.placa = 'Entre 4 y 8 letras y números (p. ej. ABC123).'
  if (!datos.tipo) errores.tipo = 'Selecciona el tipo de vehículo.'
  return errores
}
