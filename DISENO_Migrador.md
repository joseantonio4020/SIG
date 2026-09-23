# Migrador SHP → SQL Server - VisorDatosSIG

## Descripción General
Aplicación de escritorio .NET 10 (WinForms recomendado) para migrar archivos Shapefile 
(SHP, SHX, DBF, PRJ) a la base de datos SQL Server 2022.

## Características Principales

### 1. Interfaz de Usuario
- **Selector de carpeta**: Botón "Seleccionar carpeta con SHP"
- **Lista de capas detectadas**: Muestra las 4 capas esperadas
- **Vista previa de geometrías**: thumbnail de los primeros registros
- **Estadísticas en tiempo real**: Registros procesados, exitosos, omitidos, fallidos
- **Barra de progreso**: Porcentaje y velocidad de procesamiento
- **Botones**: Iniciar, Cancelar, Reemplazar, Anexar, Generar reporte

### 2. detección de Archivos
- Buscar recursivamente en la carpeta seleccionada
- Identificar archivos por extensión: .shp, .shx, .dbf, .prj
- Emparejar las 4 capas por nombre base:
  - Exp_CodigoFijo → CodigosFijos
  - Exp_MapaBase_LOTES → Lotes
  - Exp_MapaBase_MZA → Manzanas
  - Exp_MapaBase_VIAS → Vias

### 3. Validación Pre-Migración
- **Validar SRID 4326**: Verificar que todas las geometrías estén en WGS 84
- **Detectar geometrías inválidas**: Reportar y opcional corregir
- **Validar estructura DBF**: Campos obligatorios presentes
- **Validar PRJ**: Coordenadas systema consistente (debería ser EPSG:32720 o EPSG:4326)
- **Contar registros esperados**: Comparar con conteo en archivo

### 4. Proceso de Migración
- **Modo Reemplazar**: Limpiar tablas destino antes de insertar
- **Modo Anexar**: Insertar sin borrar datos existentes
- **Usar transacciones**: Rollback en caso de error crítico
- **Procesar en lotes**: 1000-5000 registros por transacción
- **Conversión de geometría**: SHP geometry → SQL geometry avec SRID 4326

### 5. Mapeo de Campos (SHP → SQL)

#### CodigosFijos
| Campo SHP | Tipo SQL | Observaciones |
|-----------|----------|---------------|
| CodF      | int      | Código fijo numérico |
| Nombre    | nvarchar(120) | Nombre/descripción |
| Geom      | geometry | Must be converted to SRID 4326 |
| Longitud  | float    | Opcional, derivado de Geom |
| Latitud   | float    | Opcional, derivado de Geom |

#### Lotes
| Campo SHP | Tipo SQL | Observaciones |
|-----------|----------|---------------|
| NroLote   | nvarchar(15) | Número de lote |
| IdManzana | int      | FK a Manzanas (calculado vía spatial join) |
| Geom      | geometry | Must be converted to SRID 4326 |

#### Manzanas
| Campo SHP | Tipo SQL | Observaciones |
|-----------|----------|---------------|
| UV        | nvarchar(15) | Unidad vecinal |
| MZA       | nvarchar(10) | Manzana |
| Geom      | geometry | Must be converted to SRID 4326 |

#### Vias
| Campo SHP | Tipo SQL | Observaciones |
|-----------|----------|---------------|
| Nombre    | nvarchar(40) | Nombre de la vía |
| TipoVia   | nvarchar(30) | Tipo de vía |
| Geom      | geometry | Must be converted to SRID 4326 |

### 6. Validación de Geometrías
- Verificar STIsValid() en cada geometría
- Reportar geometrías inválidas por capa
- Opción: corregir con STBuffer(0) o descartar
- Verificar SRID consistente dentro de cada capa

### 7. Generación de Bitácora/Reporte
Al finalizar, generar reporte con:
- Total de registros por capa
- Éxitos: INSERT exitosos
- Omitidos: Registros que no cumplieron validaciones
- Fallidos: Errores de conversión o violación de restricciones
- Tiempo total de procesamiento
- Geometrías inválidas encontradas
- SRID confirmado

### 8. Manejo de Errores
- Try/catch alrededor de cada lote de inserción
- Guardar errores en archivo de log
- Continuar con siguiente registro después de error
- Reportar fila problemática en reporte final

### 9. Cancelación Segura
- Flag de cancelación verificada en cada iteración
- Transacciones parciales completadas se conservan
- Estado limpio si se cancela antes de completar

### 10. Flujo de Trabajo Esperado
1. Seleccionar carpeta con archivos SHP
2. Revisar lista de capas detectadas
3. Validar SRID y geometrías
4. Elegir modo: Reemplazar o Anexar
5. Iniciar migración
6. Monitorear progreso en tiempo real
7. Ver reporte final
8. Ejecutar sp_ActualizarLoteCodigosFijos en SQL para completar relaciones