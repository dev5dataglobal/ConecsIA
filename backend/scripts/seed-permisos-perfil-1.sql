-- Catálogo de permisos usados por los controladores (AdministracionAuthorization) y
-- asignación al perfil 1 "Administrador plataforma" (organización 1). Idempotente.
INSERT INTO permiso (codigo, descripcion, activo) VALUES
 ('usuarios.consultar','Consultar usuarios de la organización',true),
 ('usuarios.crear','Crear usuarios en la organización',true),
 ('usuarios.modificar','Modificar usuarios de la organización',true),
 ('roles.consultar','Consultar perfiles de la organización',true),
 ('roles.administrar','Administrar perfiles de la organización',true),
 ('permisos.administrar','Asignar cualquier permiso a perfiles',true),
 ('configuracion.consultar','Consultar configuración de la organización',true),
 ('sedes.consultar','Consultar sedes',true),
 ('sedes.administrar','Administrar sedes',true),
 ('zonas_empresariales.consultar','Consultar zonas empresariales',true),
 ('zonas_empresariales.administrar','Administrar zonas empresariales',true),
 ('autorizaciones.administrar','Administrar autorizaciones',true),
 ('saas.usuarios.consultar','Plataforma: consultar usuarios de todas las organizaciones',true),
 ('saas.usuarios.crear','Plataforma: crear usuarios en cualquier organización',true),
 ('saas.usuarios.modificar','Plataforma: modificar usuarios de cualquier organización',true),
 ('saas.roles.consultar','Plataforma: consultar perfiles de cualquier organización',true),
 ('saas.roles.administrar','Plataforma: administrar perfiles de cualquier organización',true),
 ('saas.organizaciones.consultar','Plataforma: consultar todas las organizaciones',true),
 ('saas.organizaciones.administrar','Plataforma: crear y modificar organizaciones',true)
ON CONFLICT (codigo) DO NOTHING;

INSERT INTO rol_permiso (id_organizacion, id_rol, id_permiso)
SELECT r.id_organizacion, r.id_rol, p.id_permiso FROM rol r CROSS JOIN permiso p
WHERE r.id_rol = 1 AND p.activo
ON CONFLICT DO NOTHING;
