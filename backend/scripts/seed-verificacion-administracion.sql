-- Datos de PRUEBA para verificar a mano la administración (usuarios, perfiles, organizaciones, zonas).
-- Úsese solo en una base desechable (p. ej. conecsia_verificacion), nunca en la base de trabajo.
-- Contraseña de todas las cuentas de prueba: Verificacion-2026
--   plataforma@verificacion.test   → usuario 1 (administrador de plataforma)
--   admin.alfa@verificacion.test   → administrador de «Empresa Alfa» (usuarios/perfiles completos, organizaciones consultar+editar, zonas consultar)
INSERT INTO tipo_organizacion (nombre, activo) VALUES ('Empresa', true), ('Parque empresarial', true);
INSERT INTO plan (nombre, activo) VALUES ('Básico', true);
INSERT INTO organizacion (id_tipo_organizacion, id_plan, nombre, identificacion, telefono, correo, estado, creado_en) VALUES
  (1, 1, 'Plataforma DG', '900000001', '6015550001', 'plataforma@verificacion.test', 'Activa', now()),
  (1, 1, 'Empresa Alfa', '900000002', '6015550002', 'alfa@verificacion.test', 'Activa', now()),
  (1, 1, 'Empresa Beta', '900000003', '6015550003', 'beta@verificacion.test', 'Activa', now()),
  (1, 1, 'Empresa Gamma', '900000004', '6015550004', 'gamma@verificacion.test', 'Activa', now());
INSERT INTO usuario (id_usuario, correo, correo_normalizado, password_hash, sello_seguridad, intentos_fallidos, activo, creado_en) VALUES
  (1, 'plataforma@verificacion.test', 'PLATAFORMA@VERIFICACION.TEST', 'AQAAAAIAAYagAAAAEOG1ZHc4WCr3dfWIcZaMlL73b1jmvUNUo4jpF+hKKnloSbWJwV2PJ5dxCBFh2Ags6g==', md5(random()::text), 0, true, now());
SELECT setval(pg_get_serial_sequence('public.usuario', 'id_usuario'), 100);
INSERT INTO usuario (correo, correo_normalizado, password_hash, sello_seguridad, intentos_fallidos, activo, creado_en) VALUES
  ('admin.alfa@verificacion.test', 'ADMIN.ALFA@VERIFICACION.TEST', 'AQAAAAIAAYagAAAAEOG1ZHc4WCr3dfWIcZaMlL73b1jmvUNUo4jpF+hKKnloSbWJwV2PJ5dxCBFh2Ags6g==', md5(random()::text), 0, true, now()),
  ('operador.alfa@verificacion.test', 'OPERADOR.ALFA@VERIFICACION.TEST', 'AQAAAAIAAYagAAAAEOG1ZHc4WCr3dfWIcZaMlL73b1jmvUNUo4jpF+hKKnloSbWJwV2PJ5dxCBFh2Ags6g==', md5(random()::text), 0, true, now()),
  ('admin.beta@verificacion.test', 'ADMIN.BETA@VERIFICACION.TEST', 'AQAAAAIAAYagAAAAEOG1ZHc4WCr3dfWIcZaMlL73b1jmvUNUo4jpF+hKKnloSbWJwV2PJ5dxCBFh2Ags6g==', md5(random()::text), 0, true, now());
INSERT INTO membresia_organizacion (id_organizacion, id_usuario, estado, creado_en)
SELECT o.id_organizacion, u.id_usuario, 'Activa', now() FROM usuario u JOIN organizacion o ON
  (u.correo LIKE 'plataforma%' AND o.nombre = 'Plataforma DG') OR (u.correo LIKE '%.alfa@%' AND o.nombre = 'Empresa Alfa') OR (u.correo LIKE '%.beta@%' AND o.nombre = 'Empresa Beta');
INSERT INTO rol (id_organizacion, nombre, descripcion, activo)
SELECT id_organizacion, 'Administrador', 'Administra la organización', true FROM organizacion WHERE nombre IN ('Empresa Alfa', 'Empresa Beta')
UNION ALL SELECT id_organizacion, 'Operador de acceso', 'Registra ingresos y salidas', true FROM organizacion WHERE nombre = 'Empresa Alfa';
INSERT INTO modulos_permisos (id_organizacion, id_rol, id_app_modulo, asignado_en, puede_consultar, puede_anadir, puede_editar)
SELECT r.id_organizacion, r.id_rol, a.id_app_modulo, now(),
       true, a.codigo IN ('usuarios', 'perfiles', 'dashboard'), a.codigo IN ('usuarios', 'perfiles', 'organizaciones', 'dashboard')
FROM rol r CROSS JOIN app_modulos a WHERE r.nombre = 'Administrador' AND a.codigo IN ('dashboard', 'usuarios', 'perfiles', 'organizaciones', 'zonas_empresariales');
INSERT INTO modulos_permisos (id_organizacion, id_rol, id_app_modulo, asignado_en, puede_consultar, puede_anadir, puede_editar)
SELECT r.id_organizacion, r.id_rol, a.id_app_modulo, now(), true, false, false
FROM rol r CROSS JOIN app_modulos a WHERE r.nombre = 'Operador de acceso' AND a.codigo = 'dashboard';
INSERT INTO membresia_rol (id_organizacion, id_membresia, id_rol)
SELECT m.id_organizacion, m.id_membresia, r.id_rol FROM membresia_organizacion m JOIN usuario u ON u.id_usuario = m.id_usuario
JOIN rol r ON r.id_organizacion = m.id_organizacion AND r.nombre = CASE WHEN u.correo LIKE 'operador%' THEN 'Operador de acceso' ELSE 'Administrador' END
WHERE u.id_usuario <> 1;
