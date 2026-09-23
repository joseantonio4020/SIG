var map;
var layers = {};
var allCodigosFijos = [];
var allManzanas = [];
var allLotes = [];
var allVias = [];
var currentSelectedItem = null;

var estadoColores = {
    1: '#27ae60', // Normal - Verde
    2: '#f39c12', // Para corte - Naranja
    3: '#e74c3c', // Cortado - Rojo
    4: '#8e44ad', // Baja parcial - Morado
    5: '#2c3e50'  // Baja total - Gris oscuro
};

var estadoNombres = {
    1: 'Normal',
    2: 'Para corte',
    3: 'Cortado',
    4: 'Baja parcial',
    5: 'Baja total'
};

document.addEventListener('DOMContentLoaded', function () {
    initMap();
    loadAllData();
    bindEvents();
    showWelcome();
});

var canvasRenderer = null;

function initMap() {
    canvasRenderer = L.canvas({ padding: 0.5 });

    map = L.map('map', {
        center: [-16.3833, -60.9550],
        zoom: 15,
        preferCanvas: true,
        zoomControl: true
    });

    // Capas Base
    layers.osm = L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        maxZoom: 19,
        attribution: '&copy; OpenStreetMap'
    }).addTo(map);

    layers.topo = L.tileLayer('https://{s}.tile.opentopomap.org/{z}/{x}/{y}.png', {
        maxZoom: 17,
        attribution: '&copy; OpenTopoMap'
    });

    // Capas Vectoriales
    layers.manzanas = L.layerGroup().addTo(map);
    layers.lotes = L.layerGroup().addTo(map);
    layers.vias = L.layerGroup();
    layers.codigosFijos = L.layerGroup().addTo(map);
    layers.highlight = L.layerGroup().addTo(map);

    map.on('click', onMapClick);
    map.on('mousemove', onMapMouseMove);
    L.control.scale({ imperial: false, position: 'bottomright' }).addTo(map);
}

function bindEvents() {
    // Drawer móvil para Sidebar
    var btnToggleSidebar = document.getElementById('btnToggleSidebar');
    if (btnToggleSidebar) {
        btnToggleSidebar.addEventListener('click', function () {
            document.getElementById('sidebar').classList.toggle('open');
        });
    }

    var btnCloseSidebar = document.getElementById('btnCloseSidebar');
    if (btnCloseSidebar) {
        btnCloseSidebar.addEventListener('click', function () {
            document.getElementById('sidebar').classList.remove('open');
        });
    }

    // Búsqueda
    var btnSearch = document.getElementById('btnSearch');
    if (btnSearch) btnSearch.addEventListener('click', performSearch);

    var searchInput = document.getElementById('searchInput');
    if (searchInput) {
        searchInput.addEventListener('keypress', function (e) {
            if (e.key === 'Enter') performSearch();
        });
    }

    var btnClearSearch = document.getElementById('btnClearSearch');
    if (btnClearSearch) btnClearSearch.addEventListener('click', clearSearch);

    var btnClearResults = document.getElementById('btnClearResults');
    if (btnClearResults) btnClearResults.addEventListener('click', clearResults);

    var filtroEstado = document.getElementById('filtroEstado');
    if (filtroEstado) filtroEstado.addEventListener('change', filterByEstado);

    // Navegación por zonas jerárquicas
    var filtroUV = document.getElementById('filtroUV');
    if (filtroUV) filtroUV.addEventListener('change', onUVChange);

    var filtroMza = document.getElementById('filtroMza');
    if (filtroMza) filtroMza.addEventListener('change', onMzaChange);

    var btnFiltrarZona = document.getElementById('btnFiltrarZona');
    if (btnFiltrarZona) btnFiltrarZona.addEventListener('click', filterByZona);

    var btnLimpiarZona = document.getElementById('btnLimpiarZona');
    if (btnLimpiarZona) btnLimpiarZona.addEventListener('click', clearZona);

    // Conmutadores de capas
    var toggleCF = document.getElementById('toggleCodigosFijos');
    if (toggleCF) toggleCF.addEventListener('change', function () { toggleLayer(layers.codigosFijos, this.checked); });

    var toggleMza = document.getElementById('toggleManzanas');
    if (toggleMza) toggleMza.addEventListener('change', function () { toggleLayer(layers.manzanas, this.checked); });

    var toggleLot = document.getElementById('toggleLotes');
    if (toggleLot) toggleLot.addEventListener('change', function () {
        if (this.checked && layers.lotes.getLayers().length === 0 && allLotes.length > 0) {
            renderLotes(allLotes);
        }
        toggleLayer(layers.lotes, this.checked);
    });

    var toggleV = document.getElementById('toggleVias');
    if (toggleV) toggleV.addEventListener('change', function () {
        if (this.checked && allVias.length === 0) loadVias();
        toggleLayer(layers.vias, this.checked);
    });

    var baseLayerSelect = document.getElementById('baseLayerSelect');
    if (baseLayerSelect) {
        baseLayerSelect.addEventListener('change', function () {
            map.removeLayer(layers.osm);
            map.removeLayer(layers.topo);
            if (this.value === 'osm') map.addLayer(layers.osm);
            else if (this.value === 'topo') map.addLayer(layers.topo);
        });
    }

    // Botones de herramientas flotantes
    var btnZoomFull = document.getElementById('btnZoomFull');
    if (btnZoomFull) btnZoomFull.addEventListener('click', zoomToAllData);

    var btnCloseDetail = document.getElementById('btnCloseDetail');
    if (btnCloseDetail) {
        btnCloseDetail.addEventListener('click', function () {
            document.getElementById('detailPanel').style.display = 'none';
        });
    }

    var btnToggleDetails = document.getElementById('btnToggleDetails');
    if (btnToggleDetails) {
        btnToggleDetails.addEventListener('click', function () {
            var panel = document.getElementById('detailPanel');
            panel.style.display = panel.style.display === 'none' ? 'block' : 'none';
        });
    }
}

function toggleLayer(layer, show) {
    if (show) map.addLayer(layer); else map.removeLayer(layer);
}

function showWelcome() {
    var counter = document.getElementById('resultsCount');
    if (counter) counter.innerHTML = '<i class="fas fa-info-circle text-primary"></i> Cargando datos del servidor...';
}

function onMapMouseMove(e) {
    var coordsBar = document.getElementById('coordsBar');
    if (coordsBar) {
        coordsBar.textContent = 'Lat: ' + e.latlng.lat.toFixed(6) + ' | Lon: ' + e.latlng.lng.toFixed(6) + ' | WGS 84 (EPSG:4326)';
    }
}

// ===== CARGA DE DATOS OPTIMIZADA =====
function loadAllData() {
    showLoader();
    loadHierarchyUVs();

    // Carga rápida inicial de Códigos Fijos y Manzanas
    Promise.all([
        fetch('/api/geosql/codigos-fijos').then(r => r.json()),
        fetch('/api/geosql/manzanas').then(r => r.json())
    ]).then(([codigos, manzanas]) => {
        allCodigosFijos = codigos;
        allManzanas = manzanas;
        renderCodigosFijos(codigos);
        renderManzanas(manzanas);
        updateCount(codigos.length);
        renderResultsList(codigos.slice(0, 100));
        hideLoader();

        // Parámetro de búsqueda por URL si existe
        var urlParams = new URLSearchParams(window.location.search);
        var q = urlParams.get('buscar');
        if (q) {
            var searchInput = document.getElementById('searchInput');
            if (searchInput) searchInput.value = q;
            performSearch();
        } else if (codigos.length > 0) {
            zoomToAllData();
        }

        // Cargar lotes de forma asíncrona sin bloquear la interfaz
        loadLotesAsync();
    }).catch(err => {
        console.error('Error cargando capas:', err);
        hideLoader();
        var counter = document.getElementById('resultsCount');
        if (counter) counter.innerHTML = '<i class="fas fa-exclamation-triangle text-danger"></i> Error cargando datos';
    });
}

function loadLotesAsync() {
    fetch('/api/geosql/lotes')
        .then(r => r.json())
        .then(function (lotes) {
            allLotes = lotes;
            var toggleLot = document.getElementById('toggleLotes');
            if (toggleLot && toggleLot.checked) {
                renderLotes(lotes);
            }
        })
        .catch(function (err) {
            console.warn('Carga diferida de lotes:', err);
        });
}

function loadHierarchyUVs() {
    fetch('/api/geosql/zonas-jerarquia')
        .then(r => r.json())
        .then(function (res) {
            var filtroUV = document.getElementById('filtroUV');
            if (!filtroUV) return;
            var list = res.uvs || (Array.isArray(res) ? res : []);
            filtroUV.innerHTML = '<option value="">Todas las UV (' + list.length + ')</option>';
            list.forEach(function (uv) {
                var opt = document.createElement('option');
                opt.value = uv;
                opt.textContent = 'UV ' + uv;
                filtroUV.appendChild(opt);
            });
        })
        .catch(function (err) { console.error('Error cargando UVs:', err); });
}

function showLoader() {
    var counter = document.getElementById('resultsCount');
    if (counter) counter.innerHTML = '<span class="spinner-border spinner-border-sm text-primary"></span> Consultando BD...';
}

function hideLoader() { }

function updateCount(count) {
    var counter = document.getElementById('resultsCount');
    if (counter) counter.textContent = count.toLocaleString() + ' registros encontrados';
}

// ===== RENDER CODIGOS FIJOS CON CANVAS =====
function renderCodigosFijos(data) {
    layers.codigosFijos.clearLayers();
    data.forEach(function (item) {
        if (!item.latitud || !item.longitud) return;
        var color = estadoColores[item.estado] || '#7f8c8d';
        var estTexto = estadoNombres[item.estado] || 'Estado ' + item.estado;

        var marker = L.circleMarker([item.latitud, item.longitud], {
            renderer: canvasRenderer,
            radius: 6,
            fillColor: color,
            color: '#ffffff',
            weight: 1.5,
            opacity: 1,
            fillOpacity: 0.95
        });

        marker.bindTooltip('<b>' + (item.codFijo || item.idCodigo) + '</b> - ' + (item.nombre || 'N/A') + '<br><span style="color:' + color + ';font-weight:bold;">' + estTexto + '</span>', {
            direction: 'top',
            offset: [0, -6]
        });

        marker.on('click', function () {
            selectResult(item);
        });

        marker._codigoId = item.idCodigo;
        marker._item = item;
        layers.codigosFijos.addLayer(marker);
    });
}

// ===== RENDER MANZANAS CON CANVAS =====
function renderManzanas(data) {
    layers.manzanas.clearLayers();
    data.forEach(function (item) {
        if (!item.geom) return;
        try {
            var geo = typeof item.geom === 'string' ? JSON.parse(item.geom) : item.geom;
            var geoJsonLayer = L.geoJSON(geo, {
                renderer: canvasRenderer,
                style: { color: '#2980b9', weight: 2, opacity: 0.8, fillColor: '#3498db', fillOpacity: 0.08 }
            });
            geoJsonLayer.bindTooltip('<b>Manzana:</b> ' + (item.mza || item.MZA || '') + ' (UV ' + (item.uv || item.UV || '') + ')', { sticky: true });
            geoJsonLayer.on('click', function (e) {
                L.DomEvent.stopPropagation(e);
                showManzanaDetail(item);
            });
            layers.manzanas.addLayer(geoJsonLayer);
        } catch (e) { }
    });
}

// ===== RENDER LOTES CON CANVAS =====
function renderLotes(data) {
    layers.lotes.clearLayers();
    data.forEach(function (item) {
        if (!item.geom) return;
        try {
            var geo = typeof item.geom === 'string' ? JSON.parse(item.geom) : item.geom;
            var geoJsonLayer = L.geoJSON(geo, {
                renderer: canvasRenderer,
                style: { color: '#27ae60', weight: 1, opacity: 0.7, fillColor: '#2ecc71', fillOpacity: 0.06 }
            });
            geoJsonLayer.bindTooltip('<b>Lote:</b> ' + (item.nroLote || item.NroLote || ''), { sticky: true });
            geoJsonLayer.on('click', function (e) {
                L.DomEvent.stopPropagation(e);
                showLoteDetail(item);
            });
            layers.lotes.addLayer(geoJsonLayer);
        } catch (e) { }
    });
}

// ===== RENDER VIAS CON CANVAS =====
function loadVias() {
    fetch('/api/geosql/vias').then(r => r.json()).then(function (data) {
        allVias = data;
        layers.vias.clearLayers();
        data.forEach(function (item) {
            if (!item.geom) return;
            try {
                var geo = typeof item.geom === 'string' ? JSON.parse(item.geom) : item.geom;
                var geoJsonLayer = L.geoJSON(geo, {
                    renderer: canvasRenderer,
                    style: { color: '#7f8c8d', weight: 3, opacity: 0.85 }
                });
                geoJsonLayer.bindTooltip('<b>' + (item.nombre || 'Vía') + '</b> (' + (item.tipoVia || '') + ')', { sticky: true });
                layers.vias.addLayer(geoJsonLayer);
            } catch (e) { }
        });
    }).catch(function () { });
}

// ===== JERARQUIA DINAMICA (UV -> MZA -> LOTE) =====
function onUVChange() {
    var uv = document.getElementById('filtroUV').value;
    var mzaSelect = document.getElementById('filtroMza');
    var loteSelect = document.getElementById('filtroLote');
    mzaSelect.innerHTML = '<option value="">Todas las Manzanas</option>';
    loteSelect.innerHTML = '<option value="">Todos los Lotes</option>';

    if (uv) {
        fetch('/api/geosql/zonas-jerarquia?uv=' + encodeURIComponent(uv))
            .then(r => r.json())
            .then(function (res) {
                var list = res.mzas || (Array.isArray(res) ? res : []);
                mzaSelect.innerHTML = '<option value="">Todas las Manzanas (' + list.length + ')</option>';
                list.forEach(function (mza) {
                    var opt = document.createElement('option');
                    opt.value = mza;
                    opt.textContent = 'Mza ' + mza;
                    mzaSelect.appendChild(opt);
                });
            })
            .catch(function (err) { console.error('Error cargando manzanas:', err); });
    }
}

function onMzaChange() {
    var uv = document.getElementById('filtroUV').value;
    var mza = document.getElementById('filtroMza').value;
    var loteSelect = document.getElementById('filtroLote');
    loteSelect.innerHTML = '<option value="">Todos los Lotes</option>';

    if (uv && mza) {
        fetch('/api/geosql/zonas-jerarquia?uv=' + encodeURIComponent(uv) + '&mza=' + encodeURIComponent(mza))
            .then(r => r.json())
            .then(function (res) {
                var list = res.lotes || (Array.isArray(res) ? res : []);
                loteSelect.innerHTML = '<option value="">Todos los Lotes (' + list.length + ')</option>';
                list.forEach(function (lote) {
                    var opt = document.createElement('option');
                    opt.value = lote;
                    opt.textContent = 'Lote ' + lote;
                    loteSelect.appendChild(opt);
                });
            })
            .catch(function (err) { console.error('Error cargando lotes:', err); });
    }
}

function filterByZona() {
    var uv = document.getElementById('filtroUV').value;
    var mza = document.getElementById('filtroMza').value;
    var lote = document.getElementById('filtroLote').value;

    var filtered = allCodigosFijos.filter(function (c) {
        if (uv && c.uv !== uv) return false;
        if (mza && c.mza !== mza) return false;
        if (lote && c.lote !== lote) return false;
        return true;
    });

    renderCodigosFijos(filtered);
    renderResultsList(filtered.slice(0, 100));
    updateCount(filtered.length);

    if (filtered.length > 0) {
        zoomToResults(filtered);
        showToast(filtered.length + ' códigos fijos encontrados en la zona seleccionada', 'success');
    } else {
        showToast('No se encontraron registros en la zona seleccionada', 'info');
    }

    // Cerrar sidebar en pantallas reducidas
    if (window.innerWidth <= 768) {
        var sb = document.getElementById('sidebar');
        if (sb) sb.classList.remove('open');
    }
}

function clearZona() {
    document.getElementById('filtroUV').value = '';
    document.getElementById('filtroMza').innerHTML = '<option value="">Todas las Manzanas</option>';
    document.getElementById('filtroLote').innerHTML = '<option value="">Todos los Lotes</option>';
    renderCodigosFijos(allCodigosFijos);
    renderResultsList(allCodigosFijos.slice(0, 100));
    updateCount(allCodigosFijos.length);
    zoomToAllData();
    showToast('Filtro de zona restablecido', 'info');
}

// ===== BUSQUEDA ALFANUMERICA =====
function performSearch() {
    var texto = document.getElementById('searchInput').value.trim();
    if (!texto) {
        loadAllData();
        return;
    }

    showLoader();
    fetch('/api/geosql/codigos-fijos/buscar?texto=' + encodeURIComponent(texto))
        .then(r => r.json())
        .then(function (data) {
            updateCount(data.length);
            renderResultsList(data.slice(0, 100));
            renderCodigosFijos(data);
            hideLoader();
            if (data.length > 0) {
                zoomToResults(data);
                showToast(data.length + ' resultados para "' + texto + '"', 'success');
            } else {
                showToast('No se encontraron resultados para: ' + texto, 'info');
            }
        })
        .catch(function () {
            hideLoader();
            showToast('Error al ejecutar la búsqueda', 'error');
        });
}

function clearSearch() {
    document.getElementById('searchInput').value = '';
    loadAllData();
}

function clearResults() {
    document.getElementById('resultsList').innerHTML = '';
    loadAllData();
}

// ===== FILTRAR POR ESTADO =====
function filterByEstado() {
    var estado = document.getElementById('filtroEstado').value;
    var data = !estado ? allCodigosFijos : allCodigosFijos.filter(function (c) {
        return c.estado === parseInt(estado);
    });
    renderCodigosFijos(data);
    renderResultsList(data.slice(0, 100));
    updateCount(data.length);
}

// ===== LISTA DE RESULTADOS =====
function renderResultsList(data) {
    var container = document.getElementById('resultsList');
    container.innerHTML = '';

    if (data.length === 0) {
        container.innerHTML = '<div class="p-3 text-muted text-center" style="font-size:12px;">Sin registros que mostrar</div>';
        return;
    }

    data.forEach(function (item) {
        var color = estadoColores[item.estado] || '#7f8c8d';
        var estTexto = estadoNombres[item.estado] || 'Estado ' + item.estado;
        var div = document.createElement('div');
        div.className = 'result-item';
        div.setAttribute('data-id', item.idCodigo);
        div.innerHTML =
            '<div class="d-flex justify-content-between align-items-center">' +
                '<span class="result-type" style="color:' + color + '"><strong>Cod. ' + (item.codFijo || item.idCodigo) + '</strong></span>' +
                '<span class="badge" style="background:' + color + '; font-size:10px;">' + estTexto + '</span>' +
            '</div>' +
            '<div class="result-name text-truncate" title="' + (item.nombre || 'Sin nombre') + '">' + (item.nombre || 'Sin nombre') + '</div>' +
            '<div class="result-detail text-muted"><i class="fas fa-map-marker-alt"></i> UV ' + (item.uv || '00') + ' - Mz ' + (item.mza || '00') + ' - Lote ' + (item.lote || '00') + '</div>';

        div.addEventListener('click', function () {
            selectResult(item);
        });
        container.appendChild(div);
    });
}

function selectResult(item) {
    currentSelectedItem = item;
    document.querySelectorAll('.result-item').forEach(function (el) { el.classList.remove('active'); });
    var el = document.querySelector('[data-id="' + item.idCodigo + '"]');
    if (el) {
        el.classList.add('active');
        el.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
    }

    if (item.latitud && item.longitud) {
        map.setView([item.latitud, item.longitud], 18);
        addHighlight(item.latitud, item.longitud);
    }

    openDetailPanel(item);
}

function openDetailPanel(item) {
    var panel = document.getElementById('detailPanel');
    var content = document.getElementById('detailContent');
    var btnToggle = document.getElementById('btnToggleDetails');
    if (btnToggle) btnToggle.style.display = 'inline-block';

    var color = estadoColores[item.estado] || '#7f8c8d';
    var estNombre = estadoNombres[item.estado] || 'Desconocido';
    var fechaStr = item.fechaCambioEstado ? new Date(item.fechaCambioEstado).toLocaleDateString('es-BO') : 'N/A';

    content.innerHTML =
        '<div class="mb-3 text-center">' +
            '<span class="badge mb-2 px-3 py-2" style="background:' + color + '; font-size:13px;">' +
                '<i class="fas fa-tag me-1"></i> Estado ' + item.estado + ': ' + estNombre +
            '</span>' +
            '<h5 class="fw-bold mb-0 text-dark">' + (item.nombre || 'Sin Titular Registrado') + '</h5>' +
            '<small class="text-muted">ID Sistema: #' + (item.codF_SQL || item.idCodigo) + '</small>' +
        '</div>' +
        '<table class="table table-sm table-bordered" style="font-size:12px;">' +
            '<tbody>' +
                '<tr><th class="bg-light w-40">Código Fijo:</th><td><strong>' + (item.codFijo || 'N/A') + '</strong></td></tr>' +
                '<tr><th class="bg-light">Código SIG:</th><td><code>' + (item.codF_SIG || 'N/A') + '</code></td></tr>' +
                '<tr><th class="bg-light">Unidad Vecinal:</th><td>UV ' + (item.uv || '00') + '</td></tr>' +
                '<tr><th class="bg-light">Manzana:</th><td>Mz ' + (item.mza || '00') + '</td></tr>' +
                '<tr><th class="bg-light">Lote:</th><td>Lote ' + (item.lote || '00') + '</td></tr>' +
                '<tr><th class="bg-light">Latitud:</th><td>' + (item.latitud ? item.latitud.toFixed(6) : 'N/A') + '</td></tr>' +
                '<tr><th class="bg-light">Longitud:</th><td>' + (item.longitud ? item.longitud.toFixed(6) : 'N/A') + '</td></tr>' +
                '<tr><th class="bg-light">Último Cambio:</th><td>' + fechaStr + '</td></tr>' +
            '</tbody>' +
        '</table>' +
        '<div class="d-flex gap-2 mt-2">' +
            '<button class="btn btn-sm btn-outline-primary flex-fill" onclick="copiarCoordenadas(' + item.latitud + ', ' + item.longitud + ')">' +
                '<i class="fas fa-copy"></i> Copiar Coordenadas' +
            '</button>' +
            '<a class="btn btn-sm btn-primary flex-fill" href="/Consultas?texto=' + encodeURIComponent(item.codF_SIG || item.codFijo || '') + '">' +
                '<i class="fas fa-table"></i> Ver en Consultas' +
            '</a>' +
        '</div>';

    panel.style.display = 'block';
}

function showManzanaDetail(mza) {
    var panel = document.getElementById('detailPanel');
    var content = document.getElementById('detailContent');
    content.innerHTML =
        '<div class="mb-3 text-center">' +
            '<span class="badge bg-primary mb-2 px-3 py-2"><i class="fas fa-cube me-1"></i> Manzana Catastral</span>' +
            '<h5 class="fw-bold mb-0 text-dark">' + (mza.mza || mza.MZA || 'Manzana') + '</h5>' +
        '</div>' +
        '<table class="table table-sm table-bordered" style="font-size:12px;">' +
            '<tbody>' +
                '<tr><th class="bg-light w-40">UV:</th><td>' + (mza.uv || mza.UV || 'N/A') + '</td></tr>' +
                '<tr><th class="bg-light">Manzana:</th><td>' + (mza.mza || mza.MZA || 'N/A') + '</td></tr>' +
                '<tr><th class="bg-light">ID Origen:</th><td>' + (mza.idManzana || 'N/A') + '</td></tr>' +
            '</tbody>' +
        '</table>';
    panel.style.display = 'block';
}

function showLoteDetail(lote) {
    var panel = document.getElementById('detailPanel');
    var content = document.getElementById('detailContent');
    content.innerHTML =
        '<div class="mb-3 text-center">' +
            '<span class="badge bg-success mb-2 px-3 py-2"><i class="fas fa-home me-1"></i> Lote Catastral</span>' +
            '<h5 class="fw-bold mb-0 text-dark">Lote ' + (lote.nroLote || lote.NroLote || 'N/A') + '</h5>' +
        '</div>' +
        '<table class="table table-sm table-bordered" style="font-size:12px;">' +
            '<tbody>' +
                '<tr><th class="bg-light w-40">Número de Lote:</th><td>' + (lote.nroLote || lote.NroLote || 'N/A') + '</td></tr>' +
                '<tr><th class="bg-light">ID Origen:</th><td>' + (lote.idLote || 'N/A') + '</td></tr>' +
            '</tbody>' +
        '</table>';
    panel.style.display = 'block';
}

function copiarCoordenadas(lat, lon) {
    var texto = lat + ', ' + lon;
    navigator.clipboard.writeText(texto).then(function () {
        showToast('Coordenadas copiadas: ' + texto, 'info');
    });
}

function zoomToResults(data) {
    var points = data.filter(function (d) { return d.latitud && d.longitud; })
        .map(function (d) { return [d.latitud, d.longitud]; });
    if (points.length > 0) {
        map.fitBounds(L.latLngBounds(points), { padding: [40, 40], maxZoom: 18 });
    }
}

function zoomToAllData() {
    if (allCodigosFijos.length > 0) {
        zoomToResults(allCodigosFijos);
    } else {
        map.setView([-17.7833, -63.1821], 14);
    }
}

// ===== HIGHLIGHT =====
function addHighlight(lat, lon) {
    layers.highlight.clearLayers();
    var circle = L.circleMarker([lat, lon], {
        radius: 14,
        fillColor: '#3498db',
        color: '#2980b9',
        weight: 3,
        opacity: 0.8,
        fillOpacity: 0.25,
        dashArray: '4,4'
    });
    layers.highlight.addLayer(circle);
}

// ===== MAP CLICK =====
function onMapClick(e) {
    var lat = e.latlng.lat;
    var lng = e.latlng.lng;

    var nearest = null;
    var minDist = Infinity;

    allCodigosFijos.forEach(function (item) {
        if (!item.latitud || !item.longitud) return;
        var dist = Math.sqrt(Math.pow(item.latitud - lat, 2) + Math.pow(item.longitud - lng, 2));
        if (dist < minDist) { minDist = dist; nearest = item; }
    });

    if (nearest && minDist < 0.003) { // Coincidencia espacial cercana
        selectResult(nearest);
    }
}

// ===== TOAST =====
function showToast(message, type) {
    var toast = document.createElement('div');
    toast.style.cssText = 'position:fixed;bottom:24px;right:24px;z-index:99999;padding:10px 18px;border-radius:6px;color:white;font-size:13px;box-shadow:0 4px 14px rgba(0,0,0,0.25);transition:opacity 0.3s;font-weight:500;';
    toast.style.background = type === 'error' ? '#e74c3c' : type === 'info' ? '#3498db' : '#27ae60';
    toast.innerHTML = (type === 'error' ? '<i class="fas fa-exclamation-circle me-1"></i> ' : '<i class="fas fa-check-circle me-1"></i> ') + message;
    document.body.appendChild(toast);
    setTimeout(function () {
        toast.style.opacity = '0';
        setTimeout(function () { toast.remove(); }, 300);
    }, 3200);
}
