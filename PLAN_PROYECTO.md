# VisorDatosSIG 2026 - Plan de Desarrollo

## Estado Actual
- Proyecto: VisorDatosSIG 2026 (Sistema de Información Geográfica para Bolivia)
- Fuente principal: PDF Especificaciones + guia.docx (transcripción video docente)
- Base de datos: SQL Server 2022 con geometría spatial
- Datos iniciales: Shapefiles en DatosSIG_Reproj (863 manzanas, códigos fijos, lotes, vías)

## Tecnologías Obligatorias (del PDF/guia)
- **Backend**: C#, .NET 10, ASP.NET Core 10, MVC/Razor
- **Base de datos**: SQL Server 2022, tipo de datos geometry, SRID 4326 (WGS 84)
- **Frontend**: HTML5, CSS3, JavaScript, Bootstrap 5, Leaflet, GeoJSON
- **Migrador**: Aplicación de escritorio .NET 10 (WinForms/WPF), lectura Shapefile
- **Control de versiones**: Git

## Estructura de Proyectos Recomendada
```
VisorDatosSIG.sln
├── VisorDatosSIG.Domain/         # Entidades, interfaces, lógica pura
├── VisorDatosSIG.Application/    # Servicios, casos de uso, DTOs
├── VisorDatosSIG.Infrastructure/ # Repositorios, servicios spatial, implementaciones
├── VisorDatosSIG.Api/            # API REST endpoints
├── VisorDatosSIG.Web/            # Interfaz web Razor Pages/MVC con Leaflet
├── VisorDatosSIG.Migrador/       # Aplicación de escritorio .NET 10
└── VisorDatosSIG.Tests/          # Pruebas unitarias e integración
```

## Fases de Desarrollo (cronograma 45 días)

### Fase 1: Análisis y Estructura (Días 1-5)
- [x] Revisar PDF y guía (COMPLETADO)
- [ ] Definir arquitectura por capas
- [ ] Crear estructura de solución Visual Studio
- [ ] Configurar Git repository

### Fase 2: Base de Datos (Días 6-15)
- [ ] Ejecutar 01_CrearBD.sql en SQL Server 2022
- [ ] Verificar tablas: Usuarios, Roles, UsuariosRoles, MenuOpciones, UsuarioMenu
- [ ] Crear tablas principales: CodigosFijos, Manzanas, Lotes, Vias
- [ ] Implementar geometría tipo geometry con SRID 4326
- [ ] Crear índices espaciales (SPATIAL INDEX)
- [ ] Insertar datos iniciales (admin, roles, usuarios sample)
- [ ] Crear triggers y stored procedures (sp_BuscarInmueble, sp_ActualizarLoteCodigosFijos)

### Fase 3: Migrador SHP → SQL Server (Días 16-25)
- [ ] Aplicación de escritorio .NET 10 (WinForms recomendado)
- [ ] Seleccionar carpeta con archivos SHP/SHX/DBF/PRJ
- [ ] Detectar y listar las 4 capas: CodigosFijos, Lotes, Manzanas, Vias
- [ ] Validar WGS 84 / SRID 4326 en todas las geometrías
- [ ] Validar geometrías inválidas y reportar
- [ ] Mapeo SHP → Tablas SQL (geometry tipo)
- [ ] Usar transacciones para integridad
- [ ] Generar bitácora de migración (éxitos, omitidos, fallidos)
- [ ] Resumen estadístico al final
- [ ] Permitir reemplazar o anexar datos

### Fase 4: API y Servicios (Días 26-35)
- [ ] Controladores API REST para cada tabla
- [ ] Endpoints de búsqueda (sp_BuscarInmueble adaptado)
- [ ] Endpoints de filtros por UV, MZA, Lote, Estado
- [ ] Endpoint de conversión a GeoJSON
- [ ] Validación SQL parametrizado (protección SQL Injection)
- [ ] Manejo de excepciones y errores

### Fase 5: Visor Web con Leaflet (Días 36-42)
- [ ] Página de Login con autenticación (hash PBKDF2-SHA256)
- [ ] Menú role-based (Administrador, Catastro, Lecturador, Cortador, Reconexión)
- [ ] Mapa base con capas toggle (Manzanas, Lotes, CodigosFijos, Vias)
- [ ] Capa de búsqueda por: código fijo, UV, MZA, Lote
- [ ] Filtros por Estado (1=Normal, 2=Para Corte, 3=Cortado, 4=Baja Parcial, 5=Baja Total)
- [ ] Identificación por favor
<tool_call>
<function=bash>
<parameter=command>
python3 -c "import sqlite3; conn = sqlite3.connect(':memory:'); print('db works')" 2>&1 || echo "no python sqlite"