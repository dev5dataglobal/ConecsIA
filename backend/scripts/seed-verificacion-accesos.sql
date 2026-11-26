-- Datos de PRUEBA para verificar a mano el módulo unificado de Control de accesos (ADR-029). Ejecútese DESPUÉS de
-- seed-verificacion-administracion.sql y solo en una base desechable (p. ej. conecsia_verificacion), nunca en la de trabajo.
-- Cuentas (contraseña de seed-verificacion-administracion.sql):
--   admin.alfa@verificacion.test → personas, control de accesos, autorizaciones, vehículos y configuración completos en «Empresa Alfa»
-- Reemplaza a seed-verificacion-visitas.sql, que dependía de las zonas de acceso retiradas (ADR-028).

INSERT INTO modulos_permisos (id_organizacion, id_rol, id_app_modulo, asignado_en, puede_consultar, puede_anadir, puede_editar)
SELECT r.id_organizacion, r.id_rol, a.id_app_modulo, now(), true, true, true
FROM rol r JOIN organizacion o ON o.id_organizacion = r.id_organizacion
CROSS JOIN app_modulos a
WHERE o.nombre = 'Empresa Alfa' AND r.nombre = 'Administrador'
  AND a.codigo IN ('personas', 'visitas', 'control_accesos', 'autorizaciones', 'vehiculos', 'dispositivos', 'configuracion')
  AND NOT EXISTS (SELECT 1 FROM modulos_permisos m WHERE m.id_organizacion = r.id_organizacion AND m.id_rol = r.id_rol AND m.id_app_modulo = a.id_app_modulo);

INSERT INTO tipo_persona (id_organizacion, nombre, activo)
SELECT o.id_organizacion, 'Visitante', true FROM organizacion o
WHERE o.nombre = 'Empresa Alfa' AND NOT EXISTS (SELECT 1 FROM tipo_persona t WHERE t.id_organizacion = o.id_organizacion AND t.nombre = 'Visitante');

INSERT INTO sede (id_organizacion, id_ciudad, nombre, direccion, zona_horaria, activo)
SELECT o.id_organizacion, (SELECT min(id_ciudad) FROM ciudad), 'Sede Principal', 'Calle 100 # 10-20', 'America/Bogota', true
FROM organizacion o WHERE o.nombre = 'Empresa Alfa' AND NOT EXISTS (SELECT 1 FROM sede s WHERE s.id_organizacion = o.id_organizacion AND s.nombre = 'Sede Principal');

INSERT INTO area (id_organizacion, id_sede, nombre, es_comun_zona_empresarial, activo)
SELECT s.id_organizacion, s.id_sede, n.nombre, false, true FROM sede s CROSS JOIN (VALUES ('Gerencia'), ('Compras')) AS n(nombre)
WHERE s.nombre = 'Sede Principal' AND NOT EXISTS (SELECT 1 FROM area a WHERE a.id_sede = s.id_sede AND a.nombre = n.nombre);

INSERT INTO punto_acceso (id_organizacion, id_sede, nombre, modo_acceso, es_comun_zona_empresarial, activo, creado_en)
SELECT s.id_organizacion, s.id_sede, 'Portería principal', 'Ambos', false, true, now() FROM sede s
WHERE s.nombre = 'Sede Principal' AND NOT EXISTS (SELECT 1 FROM punto_acceso p WHERE p.id_sede = s.id_sede AND p.nombre = 'Portería principal');

-- Categorías de la organización desde las plantillas de su tipo (lo que hace la API al crear organizaciones).
INSERT INTO categoria_acceso (id_organizacion, codigo, nombre, descripcion, flujo_aprobacion, modalidades_permitidas, exige_anfitrion, exige_motivo, permite_vehiculos, permite_dispositivos, orden, activo, creado_en)
SELECT o.id_organizacion, p.codigo, p.nombre, p.descripcion, p.flujo_aprobacion, p.modalidades_permitidas, p.exige_anfitrion, true, p.permite_vehiculos, p.permite_dispositivos, p.orden, true, now()
FROM organizacion o JOIN plantilla_categoria_acceso p ON p.id_tipo_organizacion = o.id_tipo_organizacion
WHERE o.nombre = 'Empresa Alfa'
ON CONFLICT (id_organizacion, codigo) DO NOTHING;

-- Horario base de la organización: lunes a viernes 07:00–18:00.
INSERT INTO horario_base (id_organizacion, id_sede, dia_semana, hora_inicio, hora_fin)
SELECT o.id_organizacion, NULL, d, '07:00', '18:00' FROM organizacion o CROSS JOIN generate_series(1, 5) d
WHERE o.nombre = 'Empresa Alfa' AND NOT EXISTS (SELECT 1 FROM horario_base h WHERE h.id_organizacion = o.id_organizacion);
