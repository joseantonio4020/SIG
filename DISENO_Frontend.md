# Visor Web - Interfaz con Leaflet

## Arquitectura Frontend
Aplicación web ASP.NET Core con Razor Pages o MVC, usando Bootstrap 5 y Leaflet para la visualización mapa.

## Estructura de Páginas

### 1. Página de Login (/Login.cshtml / .cshtml.cs)
- Formulario de usuario y contraseña
- Link "¿Olvidó su contraseña?"
- Mensajes de error de autenticación
- Post: /api/auth/login

### 2. Página Principal / Visor (/Mapa.cshtml / .cshtml.cs)
Esta es la página principal con el mapa y todas las funcionalidades.

#### Estructura del Mapa
<div id="mapa" style="height: 100%; width: 100%;"></div>

#### Capas Base y sobrepuestas
- **Mapa base**: OpenStreetMap (por defecto)
- **Capas superpuestas** (toggle): 
  - Manzanas (polígonos)
  - Lotes (polígonos)  
  - CodigosFijos (puntos/medidores)
  - Vias (líneas)

#### Control de Capas
```html
<div class="leaflet-control leaflet-control-layers">
  <fieldset>
    <legend>Capas</legend>
    <label><input type="checkbox" checked> Mapa base OSM</label>
    <label><input type="checkbox" checked> Manzanas (863)</label>
    <label><input type="checkbox" checked> Lotes</label>
    <label><input type="checkbox" checked> CodigosFijos (medidores)</label>
    <label><input type="checkbox" checked> Vías</label>
  </fieldset>
</div>
```

#### Barra de búsqueda
```html
<div class="search-bar">
  <input type="text" id="buscarInput" placeholder="Buscar código fijo, UV, MZA, Lote...">
  <button id="btnBuscar">Buscar</button>
  <span id="estadoBusqueda"></span>
</div>
```

#### Filtros por Estado
```html
<div class="filtros">
  <select id="filtroEstado">
    <option value="">Todos los estados</option>
    <option value="1">Normal</option>
    <option value="2">Para Corte</option>
    <option value="3">Cortado</option>
    <option value="4">Baja Parcial</option>
    <option value="5">Baja Total</option>
  </select>
</div>
```

### 3. Panel de Resultados
```html
<div id="resultadosPanel" class="panel collapsed">
  <div class="panel-header">
    <h4>Resultados <span id="totalResultados">0</span></h4>
    <button class="btn-cerrar">Cerrar</button>
  </div>
  <div class="panel-body">
    <table class="table table-striped tabla-resultados">
      <thead>
        <tr>
          <th>Código</th><th>Nombre</th><th>Estado</th><th>UV/MZA</th><th>Ubicación</th>
        </tr>
      </thead>
      <tbody id="cuerpoResultados"></tbody>
    </table>
    <div class="paginacion" id="paginacion"></div>
  </div>
</div>
```

### 4. Identificación por Clic
```html
<div class="identificacion">
  <button id="btnIdentificar">Identificar en mapa</button>
  <p>Haga clic sobre un elemento en el mapa para ver sus detalles</p>
</div>
```

### 5. Zoom a Resultado
Al hacer clic en un resultado o elemento del mapa, se debe:
1. Crear un círculo/rectángulo alrededor de la geometría
2. Hacer zoom al boundinng box de la geometría
3. Mostrar un popup con los atributos del elemento

## Integración con Leaflet

### Inicialización del Mapa
```javascript
// Incluir CSS y JS de Leaflet y Bootstrap
<link rel="stylesheet" href="https://unpkg.com/leaflet@1.9.4/dist/leaflet.css" />
<script src="https://unpkg.com/leaflet@1.9.4/dist/leaflet.js"></script>

<!-- Inicializar después de cargar la página -->
<script>
    // Centro del mapa (coordenadas Bolivia aprox)
    var map = L.map('mapa').setView([-17.0, -63.0], 10);
    
    // Capa base OpenStreetMap
    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        maxZoom: 19,
        attribution: '&copy; OpenStreetMap contributors'
    }).addTo(map);
    
    // Capa groups para cada tipo de dato
    var gruposCapas = {
        'Manzanas': L.layerGroup(),
        'Lotes': L.layerGroup(),
        'CodigosFijos': L.layerGroup(),
        'Vías': L.layerGroup()
    };
    
    // Control de capas
    L.control.layers(null, gruposCapas).addTo(map);
</script>
```

### Cargar datos GeoJSON desde API
```javascript
// Ejemplo: Cargar CodigosFijos
$.ajax({
    url: '/api/geojson/codigos-fijos',
    type: 'GET',
    success: function(data) {
        var layer = L.geoJSON(data, {
            pointToLayer: function(feature, latlng) {
                return L.marker(latlazing, {
                    riseOnHover: true,
                    wepperty: feature.properties
                });
            },
            onEachFeature: function(feature, layer) {
                layer.bindPopup(`
                    <strong>Código:</strong> ${feature.properties.CodFijo}<br>
                    <strong>Nombre:</strong> ${feature.properties.Nombre}<br>
                    <strong>Estado:</strong> ${getEstadoTexto(feature.properties.Estado)}
                `);
            }
        }).addTo(gruposCapas['CodigosFijos']);
    }
});
```

### Función getEstadoTexto
```javascript
function getEstadoTexto(estado) {
    switch(estado) {
        case 1: return 'Normal';
        case 2: return 'Para Corte';
        case 3: return 'Cortado';
        case 4: return 'Baja Parcial';
        case 5: return 'Baja Total';
        default: return 'Desconocido';
    }
}
```

### Búsqueda por texto
```javascript
$('#btnBuscar').click(function() {
    var texto = $('#buscarInput').val().trim();
    if (!texto) return;
    
    $.ajax({
        url: '/api/codigos-fijos/buscar',
        type: 'GET',
        data: { texto: texto },
        success: function(resultados) {
            mostrarResultados(resultados);
            // Fit map to all results
            if (resultados.length > 0) {
                var bounds = L.latLngBounds(resultados.map r => [r.lat, r.lon]);
                map.fitBounds(bounds);
            }
        }
    });
});
```

### Filtro por Estado
```javascript
$('#filtroEstado').change(function() {
    var estado = $(this).val();
    // Filtrar todas las capas según el estado seleccionado
    filtrarPorEstado(estado);
});
```

### Identificación por clic
```javascript
map.on('click', function(e) {
    // Consultar API de identificación
    $.ajax({
        url: '/api/buscar/identificar',
        type: 'GET',
        data: { lat: e.latlng.lat, lon: e.latlng.lng, radio: 100 }, // 100m radio
        success: function(elemento) {
            if (elemento) {
                // Mostrar popup con detalles
                L.popup()
                    .setLatLng([e.latlng.lat, e.latlng.lng])
                    .setContent(`
                        <strong>${elemento.nombre}</strong><br>
                        <strong>Código:</strong> ${elemento.codFijo}<br>
                        <strong>Estado:</strong> ${getEstadoTexto(elemento.estado)}
                    `)
                    .openOn(map);
                
                // Zoom a la posición
                map.setZoom(15);
            }
        }
    });
});
```

## Responsividad

### Breakpoints
- **PC (1366px+)**: Cuadricula completa, panel lateral izquierdo, mapa a la derecha
- **Tablet (768px-1365)**: Panel superior, mapa ocupa 70% ancho, panel 30% con filtros/búsqueda
- **Celular (360px-767)**: Mapa full width, filtros y búsqueda debajo en sección vertical

### Elementos que se reorganizan
- **Navbar**: En móvil se convierte en menú hamburger
- **Panel de filtros**: Debajo del mapa en tablet/celular
- **Tabla de resultados**: En móvil, mostrar en formato acordeón o lista simple
- **Control de capas**: Position se mueve a esquina inferior derecha en móvil

## Flujo de Usuario

### Caso 1: Buscar un código fijo
1. Usuario entra al visor (ya logueado)
2. Escribe "12345" en el buscador
3. Hace clic en lupa
4. Sistema consulta API: GET /api/codigos-fijos/buscar?texto=12345
5. Resultado mostrado en panel lateral
6. Elemento resaltado en mapa con popup
7. Map se hace zoom automático a la ubicación

### Caso 2: Buscar un lote
1. Similar al caso 1 pero buscando por NroLote
2. API: GET /api/lotes/buscar?nroLote=L-123
3. Mostrar límites del lote en mapa

### Caso 3: Buscar una manzana
1. Buscar por UV o MZA
2. API: GET /api/manzanas/buscar?UV=UV01 o ?MZA=MZA-10
3. Resaltar polígono de la manzana

### Caso 4: Buscar una vía
1. Buscar por nombre o tipo
2. API: GET /api/vias/buscar?nombre=Av. Principal

### Caso 5: Clic en geometría
1. Usuario hace clic en el mapa
2. API: GET /api/buscar/identificar?lat=xx&lon=yy&radio=50
3. Retornar el elemento más cercano
4. Mostrar popup con detalles

### Caso 6: Aplicar filtro
1. Seleccionar estado "3 - Cortado" del dropdown
2. Filtrar todas las capas visibles
3. Solo mostrar elementos con Estado=3

### Caso 7: Seleccionar fila de resultados
1. Hacer clic en una fila de la tabla de resultados
2. Panel se expande con más detalles
3. Hacer zoom a esa ubicación específica
4. Abrir popup con información completa