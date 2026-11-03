-- Datos de PRUEBA para verificar a mano el módulo de visitas. Ejecútese DESPUÉS de seed-verificacion-administracion.sql
-- y solo en una base desechable (p. ej. conecsia_verificacion), nunca en la base de trabajo.
-- Cuentas (contraseña de seed-verificacion-administracion.sql):
--   admin.alfa@verificacion.test    → personas, visitas y control de accesos completos en «Empresa Alfa»
--   operador.alfa@verificacion.test → visitas (consultar/registrar) y control de accesos completo
-- Los puntos de acceso y las autorizaciones se insertan aquí porque aún no tienen pantalla (HU-015, EP-07).

INSERT INTO modulos_permisos (id_organizacion, id_rol, id_app_modulo, asignado_en, puede_consultar, puede_anadir, puede_editar)
SELECT r.id_organizacion, r.id_rol, a.id_app_modulo, now(), true,
       r.nombre = 'Administrador' OR a.codigo IN ('visitas', 'control_accesos'),
       r.nombre = 'Administrador' OR a.codigo = 'control_accesos'
FROM rol r JOIN organizacion o ON o.id_organizacion = r.id_organizacion
CROSS JOIN app_modulos a
WHERE o.nombre = 'Empresa Alfa' AND r.nombre IN ('Administrador', 'Operador de acceso') AND a.codigo IN ('personas', 'visitas', 'control_accesos')
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

INSERT INTO zona (id_organizacion, id_sede, nombre, activo, creado_en)
SELECT s.id_organizacion, s.id_sede, n.nombre, true, now() FROM sede s CROSS JOIN (VALUES ('Recepción'), ('Parqueadero'), ('Planta')) AS n(nombre)
WHERE s.nombre = 'Sede Principal' AND NOT EXISTS (SELECT 1 FROM zona z WHERE z.id_sede = s.id_sede AND z.nombre = n.nombre);

INSERT INTO punto_acceso (id_organizacion, id_sede, id_zona, nombre, modo_acceso, activo, creado_en)
SELECT z.id_organizacion, z.id_sede, z.id_zona, n.nombre, n.modo, true, now()
FROM zona z JOIN sede s ON s.id_sede = z.id_sede AND s.nombre = 'Sede Principal'
JOIN (VALUES ('Recepción', 'Portería principal', 'Ambos'), ('Parqueadero', 'Salida norte', 'Salida'), ('Planta', 'Acceso a planta', 'Ambos')) AS n(zona, nombre, modo) ON n.zona = z.nombre
WHERE NOT EXISTS (SELECT 1 FROM punto_acceso p WHERE p.id_sede = z.id_sede AND p.nombre = n.nombre);

INSERT INTO persona (id_organizacion, id_tipo_persona, id_tipo_documento, numero_documento, numero_documento_normalizado, nombres, apellidos, correo, empresa_procedencia, activo, creado_en, id_usuario_crea)
SELECT o.id_organizacion, t.id_tipo_persona, d.id_tipo_documento, p.doc, p.doc, p.nombres, p.apellidos, p.correo, 'Proveedores S.A.S.', true, now(), u.id_usuario
FROM organizacion o
JOIN tipo_persona t ON t.id_organizacion = o.id_organizacion AND t.nombre = 'Visitante'
JOIN tipo_documento d ON d.codigo = 'CC'
JOIN usuario u ON u.correo = 'admin.alfa@verificacion.test'
CROSS JOIN (VALUES ('1020304050', 'Laura', 'Méndez', 'laura@verificacion.test'), ('80123456', 'Carlos', 'Rojas', 'carlos@verificacion.test')) AS p(doc, nombres, apellidos, correo)
WHERE o.nombre = 'Empresa Alfa' AND NOT EXISTS (SELECT 1 FROM persona x WHERE x.id_organizacion = o.id_organizacion AND x.numero_documento_normalizado = p.doc);

-- Laura: autorización aprobada y vigente (hoy + 5 días) para Gerencia. Carlos: pendiente de decisión.
INSERT INTO autorizacion (id_organizacion, id_persona, id_sede, id_area, id_membresia_responsable, id_usuario_crea, tipo_emisor, alcance,
    modalidad, fecha_inicio, fecha_fin, hora_inicio, hora_fin, cupo_ingresos, ingresos_consumidos, estado, version, creado_en)
SELECT p.id_organizacion, p.id_persona, s.id_sede, a.id_area, m.id_membresia, u.id_usuario, 'Empresa', 'Privado',
       'Temporal', current_date, current_date + 5, '00:00', '23:59', NULL, 0, CASE WHEN p.nombres = 'Laura' THEN 'Aprobada' ELSE 'Pendiente' END, 0, now()
FROM persona p
JOIN sede s ON s.id_organizacion = p.id_organizacion AND s.nombre = 'Sede Principal'
JOIN area a ON a.id_sede = s.id_sede AND a.nombre = 'Gerencia'
JOIN usuario u ON u.correo = 'admin.alfa@verificacion.test'
JOIN membresia_organizacion m ON m.id_usuario = u.id_usuario AND m.id_organizacion = p.id_organizacion
WHERE p.numero_documento_normalizado IN ('1020304050', '80123456')
  AND NOT EXISTS (SELECT 1 FROM autorizacion x WHERE x.id_persona = p.id_persona);
