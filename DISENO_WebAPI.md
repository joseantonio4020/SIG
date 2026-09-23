# Visor Web API - VisorDatosSIG

## Arquitectura
API REST ASP.NET Core 10 con C#, que sirve datos geoespaciales a la interfaz web de Leaflet.

## Endpoints Principales

### 1. Autenticación y Seguridad
- POST /api/auth/login - Iniciar sesión (verificar hash PBKDF2)
- POST /api/auth/logout - Cerrar sesión
- GET /api/auth/usuario - Obtener datos del usuario actual
- GET /api/auth/roles - Obtener roles y permisos del usuario

### 2. Capas y Mapas Base
- GET /api/layers - Listar capas disponibles (CodigosFijos, Lotes, Manzanas, Vias)
- GET /api/layers/{layer}/estatus - Estado activ/desactivado de capa
- PUT /api/layers/{layer}/estatus - Activar/desactivar capa

### 3. CodigosFijos (Códigos Fijos / Medidores)
- GET /api/codigos-fijos - Listar todos con paginación y filtros
- GET /api/codigos-fijos/buscar - Buscar por texto, UV, MZA, Lote, Estado
- GET /api/codigos-fijos/{id} - Obtener uno por ID
- GET /api/codigos-fijos/geoJSON - Todos los geometrías en GeoJSON
- GET /api/codigos-fijos/estadisticas - Conteos por estado

**Filtros disponibles:**
- @Texto: Buscar por CodFijo o Nombre
- @UV: Filtrar por UV
- @Mza: Filtrar por MZA
- @Lote: Filtrar por NroLote
- @Estado: Filtrar por Estado (1-5)

### 4. Lotes
- GET /api/lotes - Listar todos los lotes
- GET /api/lotes/buscar - Buscar por NroLote
- GET /api/lotes/{id} - Obtener uno por ID
- GET /api/lotes/geoJSON - Geometrías en GeoJSON
- GET /api/lotes/manzana/{idManzana} - Lotes de una manzana

### 5. Manzanas
- GET /api/manzanas - Listar todas las manzanas
- GET /api/manzanas/buscar - Buscar por UV o MZA
- GET /api/manzanas/{id} - Obtener una manzana
- GET /api/manzanas/geoJSON - Geometrías en GeoJSON
- GET /api/manzanas/estadisticas - Total de manzanas, etc.

### 6. Vias
- GET /api/vias - Listar todas las vías
- GET /api/vias/buscar - Buscar por Nombre o TipoVia
- GET /api/vias/{id} - Obtener una vía
- GET /api/vias/geoJSON - Geometrías en GeoJSON

### 7. Búsqueda Espacial y Identificación
- GET /api/buscar/identificar?lat={lat}&lon={lon}&radio={radio} - Identificar elementos cercanos a un punto
- GET /api/buscar/zoom-resultado?codFijo={cod} - Obtener datos y coordenadas para zoom al resultado
- GET /api/buscar/buffer?geom=... - Elementos dentro de un buffer

### 8. GeoJSON y Visualización en Leaflet
- GET /api/geojson/codigos-fijos - FeatureCollection completo
- GET /api/geojson/lotes - FeatureCollection completo
- GET /api/geojson/manzanas - FeatureCollection completo
- GET /api/geojson/vias - FeatureCollection completo

**Formato de respuesta GeoJSON:**
```json
{
  "type": "FeatureCollection",
  "features": [
    {
      "type": "Feature",
      "properties": {
        "IdCodigo": 1,
        "CodFijo": "12345",
        "Nombre": "Medidor principal",
        "Estado": 1
      },
      "geometry": {
        "type": "Point",
        "coordinates": [-63.45, -17.26]  // [long, lat] WGS 84
      }
    }
  ]
}
```

### 9. Menú y Permisos
- GET /api/menu - Estructura completa del menú con permisos por rol
- GET /api/permisos/usuario - Permisos del usuario logueado

## Seguridad Implementada

### Login/Autenticación
- Usar autenticación JWT (JSON Web Tokens)
- Passwords hasheados con PBKDF2-SHA256 (100,000 iteraciones como en el script)
- Cookie HttpOnly, Secure, SameSite=Strict
- Token expirable (24h máximo)

### Autorización por Roles
- Middleware de autorización que verifica rol del usuario
- Endpoints protegidos según rol:
  - Administrador: Todo acceso (ver, crear, editar, eliminar)
  - Catastro: Ver y editar sus propias regiones
  - Lecturador: Solo lectura de CodigosFijos y consulta básica
  - Cortador: Ver Vias y lotes para corte de servicios
  - Reconexion: Ver datos de reconexión

### Protección SQL
- Todos los queries usan parámetros (SqlParameter), n
- No concatenación de strings en queries
- Validación de inputs en el servidor

### Manejo de Excepciones
- Global exception handler que no muestra detalles de SQL al cliente
- Log de errores en archivo/BD
- HTTP 500 con mensaje genérico en producción

### Cadenas de Conexión
- Almacenadas en appsettings.json
- Nivel de privilegio mínimo (solo execute en stored procedures necesarias)
- Secretos fuera del código fuente (user-secrets o Azure Key Vault)

## Rendimiento

### Índices Espaciales
- SPATIAL INDEX en todas las tablas con columna Geom
- Índices convencionales en: CodFijo, Nombre, UV, MZA, NroLote, Estado

### Paginación
- Todos los listados con paginación (page number, page size)
- Default: 50 registros por página
- Max: 500 registros por página

### Cache
- Cache leve de estructuras de menú y roles
- Cache de consultas estáticas (listado de estados, etc.)
- Invalidación al cambiar datos

## Respuesta de Errores Estándar
```json
{
  "error": "Nombre del error",
  "message": "Mensaje amigable",
  "code": "CódigoError",
  "timestamp": "2024-01-15T10:30:00Z"
}
```

## Estados de Respuesta HTTP
- 200 OK - Éxito
- 201 Created - Recurso creado
- 400 Bad Request - Datos inválidos
- 401 Unauthorized - No autenticado
- 403 Forbidden - Sin permisos
- 404 Not Found - Recurso no existe
- 415 Unsupported Media Type - Formato no soportado
- 429 Too Many Requests - Rate limiting
- 500 Internal Server Error - Error interno