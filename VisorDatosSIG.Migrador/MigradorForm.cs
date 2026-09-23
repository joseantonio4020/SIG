using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;
using NetTopologySuite;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using NtsPoint = NetTopologySuite.Geometries.Point;

namespace VisorDatosSIG.Migrador;

public partial class MigradorForm : Form
{
    private string _carpetaOrigen = "";
    private List<CapaSHP> _capasDetectadas = new();
    private string _connectionString = "Server=localhost;Database=VisorDatosSIG;Trusted_Connection=True;TrustServerCertificate=True;";
    private readonly GeometryFactory _geometryFactory = new GeometryFactory();
    private StringBuilder _resumenMigracion = new();

    private ListBox lstCapas = null!;
    private Button btnMigrar = null!;
    private Button btnSeleccionar = null!;
    private Button btnConexion = null!;
    private Button btnPrevisualizar = null!;
    private Button btnExportarResumen = null!;
    private Button btnReconstruirIndices = null!;
    private RadioButton rbReemplazar = null!;
    private RadioButton rbAnexar = null!;
    private ProgressBar pgbProgreso = null!;
    private Label lblEstado = null!;
    private TextBox txtLog = null!;
    private TextBox txtServer = null!;
    private TextBox txtDatabase = null!;

    public MigradorForm() { ConfigurarFormulario(); }

    private void ConfigurarFormulario()
    {
        this.Text = "Migrador SHP \u2192 SQL Server - VisorDatosSIG 2026";
        this.Size = new System.Drawing.Size(960, 750);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.MinimumSize = new System.Drawing.Size(850, 650);

        // 1. CONEXIÓN
        var grpConexion = new GroupBox { Text = " 1. Conexión a SQL Server", Location = new System.Drawing.Point(12, 12), Size = new System.Drawing.Size(920, 65) };
        var lblServer = new Label { Text = "Servidor:", Location = new System.Drawing.Point(10, 25), AutoSize = true };
        txtServer = new TextBox { Text = "localhost", Location = new System.Drawing.Point(70, 22), Size = new System.Drawing.Size(160, 23) };
        var lblDb = new Label { Text = "Base de Datos:", Location = new System.Drawing.Point(245, 25), AutoSize = true };
        txtDatabase = new TextBox { Text = "VisorDatosSIG", Location = new System.Drawing.Point(340, 22), Size = new System.Drawing.Size(160, 23) };
        btnConexion = new Button { Text = "Probar Conexión", Location = new System.Drawing.Point(520, 20), Size = new System.Drawing.Size(130, 28) };
        btnConexion.Click += async (s, e) => await ProbarConexion();

        btnReconstruirIndices = new Button { Text = "Reconstruir Índices", Location = new System.Drawing.Point(670, 20), Size = new System.Drawing.Size(150, 28) };
        btnReconstruirIndices.Click += async (s, e) => await ReconstruirIndicesEspacialesAsync();

        grpConexion.Controls.AddRange(new Control[] { lblServer, txtServer, lblDb, txtDatabase, btnConexion, btnReconstruirIndices });

        // 2. SELECCIÓN DE CARPETA
        var lblInstrucciones = new Label { Text = "2. Seleccione la carpeta con los archivos SHP (WGS 84 / EPSG:4326):", Location = new System.Drawing.Point(12, 85), AutoSize = true };
        btnSeleccionar = new Button { Text = "Seleccionar carpeta...", Location = new System.Drawing.Point(12, 105), Size = new System.Drawing.Size(180, 30) };
        btnSeleccionar.Click += BtnSeleccionar_Click;
        Label lblCarpeta = new Label { Text = "Carpeta: (no seleccionada)", Location = new System.Drawing.Point(205, 112), AutoSize = true, ForeColor = System.Drawing.Color.Gray };
        lblCarpeta.Name = "lblCarpeta";

        // 3. CAPAS Y PREVISUALIZACIÓN
        var lblCapas = new Label { Text = "3. Capas detectadas y validación de proyección WGS 84 (.prj):", Location = new System.Drawing.Point(12, 145), AutoSize = true };
        lstCapas = new ListBox { Location = new System.Drawing.Point(12, 165), Size = new System.Drawing.Size(740, 140), SelectionMode = SelectionMode.One };
        lstCapas.SelectedIndexChanged += (s, e) => { btnPrevisualizar.Enabled = lstCapas.SelectedIndex >= 0; };

        btnPrevisualizar = new Button { Text = "Previsualizar\n(20 registros)", Location = new System.Drawing.Point(765, 165), Size = new System.Drawing.Size(165, 50), Enabled = false };
        btnPrevisualizar.Click += BtnPrevisualizar_Click;

        // 4. MODALIDAD DE CARGA (RF-MIG-06)
        var grpModo = new GroupBox { Text = " 4. Modalidad de Carga", Location = new System.Drawing.Point(12, 315), Size = new System.Drawing.Size(920, 50) };
        rbReemplazar = new RadioButton { Text = "Reemplazar datos existentes (Elimina registros previos de las tablas)", Location = new System.Drawing.Point(20, 20), AutoSize = true, Checked = true };
        rbAnexar = new RadioButton { Text = "Anexar nuevos registros (Conserva los existentes)", Location = new System.Drawing.Point(480, 20), AutoSize = true };
        grpModo.Controls.AddRange(new Control[] { rbReemplazar, rbAnexar });

        // 5. ACCIONES Y PROGRESO
        btnMigrar = new Button { Text = "Iniciar Migración", Location = new System.Drawing.Point(12, 375), Size = new System.Drawing.Size(180, 38), Enabled = false, BackColor = System.Drawing.Color.FromArgb(46, 125, 50), ForeColor = System.Drawing.Color.White, Font = new System.Drawing.Font("Segoe UI", 10, System.Drawing.FontStyle.Bold) };
        btnMigrar.Click += async (s, e) => await IniciarMigracionAsync();

        btnExportarResumen = new Button { Text = "Exportar Resumen...", Location = new System.Drawing.Point(205, 375), Size = new System.Drawing.Size(170, 38), Enabled = false };
        btnExportarResumen.Click += BtnExportarResumen_Click;

        pgbProgreso = new ProgressBar { Location = new System.Drawing.Point(12, 420), Size = new System.Drawing.Size(920, 25), Minimum = 0, Maximum = 100 };
        lblEstado = new Label { Text = "Estado: Esperando selección de carpeta con shapefiles...", Location = new System.Drawing.Point(12, 452), AutoSize = true, ForeColor = System.Drawing.Color.DarkBlue };

        var lblLog = new Label { Text = "Registro de Operaciones / Log:", Location = new System.Drawing.Point(12, 475), AutoSize = true };
        txtLog = new TextBox { Location = new System.Drawing.Point(12, 495), Size = new System.Drawing.Size(920, 200), Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, BackColor = System.Drawing.Color.Black, ForeColor = System.Drawing.Color.LimeGreen, Font = new System.Drawing.Font("Consolas", 9) };

        this.Controls.AddRange(new Control[] { grpConexion, lblInstrucciones, btnSeleccionar, lblCarpeta, lblCapas, lstCapas, btnPrevisualizar, grpModo, btnMigrar, btnExportarResumen, pgbProgreso, lblEstado, lblLog, txtLog });
    }

    private async Task ProbarConexion()
    {
        _connectionString = $"Server={txtServer.Text};Database={txtDatabase.Text};Trusted_Connection=True;TrustServerCertificate=True;";
        try
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            MessageBox.Show("¡Conexión exitosa a SQL Server!", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
            Log("Conexión a SQL Server exitosa.");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error de conexión: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            Log($"Error de conexión: {ex.Message}");
        }
    }

    private async Task ReconstruirIndicesEspacialesAsync()
    {
        _connectionString = $"Server={txtServer.Text};Database={txtDatabase.Text};Trusted_Connection=True;TrustServerCertificate=True;";
        try
        {
            Log("Reconstruyendo índices espaciales en la base de datos...");
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            var sql = @"
                IF EXISTS (SELECT * FROM sys.tables WHERE name='CodigosFijos') ALTER INDEX ALL ON CodigosFijos REBUILD;
                IF EXISTS (SELECT * FROM sys.tables WHERE name='Manzanas') ALTER INDEX ALL ON Manzanas REBUILD;
                IF EXISTS (SELECT * FROM sys.tables WHERE name='Lotes') ALTER INDEX ALL ON Lotes REBUILD;
                IF EXISTS (SELECT * FROM sys.tables WHERE name='Vias') ALTER INDEX ALL ON Vias REBUILD;
            ";
            using var cmd = new SqlCommand(sql, conn);
            cmd.CommandTimeout = 120;
            await cmd.ExecuteNonQueryAsync();
            Log("Índices espaciales reconstruidos exitosamente.");
            MessageBox.Show("Índices espaciales optimizados y reconstruidos.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            Log($"Error al reconstruir índices: {ex.Message}");
            MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void BtnSeleccionar_Click(object? sender, EventArgs e)
    {
        using var fb = new FolderBrowserDialog { Description = "Seleccione la carpeta con los archivos shapefile (.shp)" };
        if (fb.ShowDialog() == DialogResult.OK)
        {
            _carpetaOrigen = fb.SelectedPath;
            var lbl = this.Controls.OfType<Label>().FirstOrDefault(l => l.Name == "lblCarpeta");
            if (lbl != null) { lbl.Text = $"Carpeta: {_carpetaOrigen}"; lbl.ForeColor = System.Drawing.Color.Black; }
            DetectarCapas();
        }
    }

    private void DetectarCapas()
    {
        lstCapas.Items.Clear();
        _capasDetectadas.Clear();

        var shpFiles = Directory.GetFiles(_carpetaOrigen, "*.shp", SearchOption.AllDirectories);
        Log($"Se encontraron {shpFiles.Length} archivos .shp en {_carpetaOrigen}");

        var bases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "Exp_CodigoFijo_4326", "CodigosFijos" }, { "Exp_CodigoFijo", "CodigosFijos" },
            { "Exp_MapaBase_LOTES_4326", "Lotes" }, { "Exp_MapaBase_LOTES", "Lotes" },
            { "Exp_MapaBase_MZA_4326", "Manzanas" }, { "Exp_MapaBase_MZA", "Manzanas" },
            { "Exp_MapaBase_VIAS_4326", "Vias" }, { "Exp_MapaBase_VIAS", "Vias" }
        };

        foreach (var shpFile in shpFiles)
        {
            var nombreBase = Path.GetFileNameWithoutExtension(shpFile);
            if (bases.TryGetValue(nombreBase, out var tabla))
            {
                var dbfFile = Path.ChangeExtension(shpFile, ".dbf");
                var shxFile = Path.ChangeExtension(shpFile, ".shx");
                var prjFile = Path.ChangeExtension(shpFile, ".prj");

                bool tieneArchivos = File.Exists(dbfFile) && File.Exists(shxFile) && File.Exists(prjFile);
                bool esWGS84 = false;

                // Validación WGS 84 (RF-MIG-04)
                if (File.Exists(prjFile))
                {
                    try
                    {
                        var prjText = File.ReadAllText(prjFile).ToUpperInvariant();
                        esWGS84 = prjText.Contains("WGS_1984") || prjText.Contains("WGS 84") || prjText.Contains("4326") || prjText.Contains("GCS_WGS_1984");
                    }
                    catch { }
                }

                int registros = 0;
                if (tieneArchivos)
                {
                    try
                    {
                        using var shp = new ShapefileDataReader(shpFile, _geometryFactory);
                        while (shp.Read()) registros++;
                    }
                    catch { registros = -1; }
                }

                string statusPrj = esWGS84 ? "WGS 84 OK" : "NO WGS84";
                string status = tieneArchivos ? $"OK ({registros} reg, {statusPrj})" : "FALTAN ARCHIVOS";

                lstCapas.Items.Add($"{tabla,-13} | {nombreBase,-28} | {status}");
                Log($"Capa detectada: {tabla} -> {nombreBase} ({status})");

                if (tieneArchivos)
                {
                    _capasDetectadas.Add(new CapaSHP
                    {
                        NombreBase = nombreBase,
                        ArchivoSHP = shpFile,
                        TablaDestino = tabla,
                        NumRegistros = registros,
                        EsWgs84 = esWGS84
                    });
                }
            }
        }

        var nombres = _capasDetectadas.Select(c => c.TablaDestino).Distinct().ToList();
        bool todasPresentes = nombres.Count >= 4;
        btnMigrar.Enabled = _capasDetectadas.Count > 0;

        if (todasPresentes)
        {
            int total = _capasDetectadas.Sum(c => c.NumRegistros);
            lblEstado.Text = $"4 capas identificadas listas para migrar. Total estimado: {total:N0} registros";
            lblEstado.ForeColor = System.Drawing.Color.Green;
        }
        else
        {
            lblEstado.Text = $"Capas detectadas: {nombres.Count}/4 ({string.Join(", ", nombres)})";
            lblEstado.ForeColor = System.Drawing.Color.DarkBlue;
        }
    }

    private void BtnPrevisualizar_Click(object? sender, EventArgs e)
    {
        if (lstCapas.SelectedIndex < 0 || lstCapas.SelectedIndex >= _capasDetectadas.Count) return;

        var capa = _capasDetectadas[lstCapas.SelectedIndex];

        try
        {
            using var shp = new ShapefileDataReader(capa.ArchivoSHP, _geometryFactory);
            var dt = new DataTable();

            for (int i = 0; i < shp.FieldCount; i++)
            {
                dt.Columns.Add(shp.GetName(i));
            }
            dt.Columns.Add("Geometria_Tipo");

            int rows = 0;
            while (shp.Read() && rows < 20)
            {
                var row = dt.NewRow();
                for (int i = 0; i < shp.FieldCount; i++)
                {
                    row[i] = shp.GetValue(i)?.ToString() ?? "";
                }
                row["Geometria_Tipo"] = shp.Geometry?.GeometryType ?? "NULL";
                dt.Rows.Add(row);
                rows++;
            }

            var frmPreview = new Form
            {
                Text = $"Vista previa (primeros 20 registros) - {capa.TablaDestino} ({capa.NombreBase})",
                Size = new System.Drawing.Size(900, 450),
                StartPosition = FormStartPosition.CenterParent
            };

            var grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                DataSource = dt,
                ReadOnly = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells
            };

            frmPreview.Controls.Add(grid);
            frmPreview.ShowDialog(this);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al leer la capa: {ex.Message}", "Error de previsualización", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task IniciarMigracionAsync()
    {
        btnMigrar.Enabled = false;
        btnSeleccionar.Enabled = false;
        btnExportarResumen.Enabled = false;
        pgbProgreso.Value = 0;
        _resumenMigracion.Clear();

        bool reemplazar = rbReemplazar.Checked;
        string modo = reemplazar ? "Reemplazar" : "Anexar";

        _resumenMigracion.AppendLine("==================================================================");
        _resumenMigracion.AppendLine("INFORME DE MIGRACIÓN DE DATOS SIG - VisorDatosSIG 2026");
        _resumenMigracion.AppendLine("==================================================================");
        _resumenMigracion.AppendLine($"Fecha y Hora: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        _resumenMigracion.AppendLine($"Usuario Operador: {Environment.UserName}");
        _resumenMigracion.AppendLine($"Carpeta Origen: {_carpetaOrigen}");
        _resumenMigracion.AppendLine($"Modalidad de Carga: {modo}");
        _resumenMigracion.AppendLine("------------------------------------------------------------------");

        int totalGlobal = _capasDetectadas.Sum(c => c.NumRegistros);
        int procesadosGlobal = 0;
        int exitososGlobal = 0;
        int fallidosGlobal = 0;

        Log($"=== INICIO DE MIGRACIÓN (Modo: {modo}) ===");

        try
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            Log("Conexión establecida con la base de datos.");

            if (reemplazar)
            {
                Log("Limpiando tablas de destino según modalidad Reemplazar...");
                using var cmdClean = new SqlCommand(@"
                    DELETE FROM CodigosFijos;
                    DELETE FROM Lotes;
                    DELETE FROM Manzanas;
                    DELETE FROM Vias;
                ", conn);
                await cmdClean.ExecuteNonQueryAsync();
                Log("Tablas vaciadas correctamente.");
            }

            foreach (var capa in _capasDetectadas)
            {
                Log($"--- Procesando {capa.TablaDestino} ({capa.NumRegistros} registros) ---");
                lblEstado.Text = $"Migrando {capa.TablaDestino}...";
                Application.DoEvents();

                var (exitosos, fallidos) = await MigrarCapaDetalladaAsync(conn, capa);
                exitososGlobal += exitosos;
                fallidosGlobal += fallidos;
                procesadosGlobal += (exitosos + fallidos);

                pgbProgreso.Value = totalGlobal > 0 ? (int)((double)procesadosGlobal / totalGlobal * 100) : 0;
                Log($"Capa {capa.TablaDestino}: {exitosos} exitosos, {fallidos} fallidos.");

                _resumenMigracion.AppendLine($"Capa: {capa.TablaDestino,-15} | Archivo: {capa.NombreBase,-25} | Éxito: {exitosos,6} | Fallidos: {fallidos,4}");
            }

            // Registrar en base de datos
            await RegistrarBitacoraAsync(conn, procesadosGlobal, exitososGlobal, modo);

            pgbProgreso.Value = 100;
            lblEstado.Text = $"Migración finalizada. Éxito: {exitososGlobal:N0}, Fallidos: {fallidosGlobal}";
            lblEstado.ForeColor = System.Drawing.Color.Green;
            btnExportarResumen.Enabled = true;

            _resumenMigracion.AppendLine("------------------------------------------------------------------");
            _resumenMigracion.AppendLine($"TOTAL PROCESADOS: {procesadosGlobal:N0}");
            _resumenMigracion.AppendLine($"TOTAL EXITOSOS:   {exitososGlobal:N0}");
            _resumenMigracion.AppendLine($"TOTAL FALLIDOS:   {fallidosGlobal:N0}");
            _resumenMigracion.AppendLine("==================================================================");

            Log($"=== MIGRACIÓN FINALIZADA CON ÉXITO ===");
            MessageBox.Show($"Migración completada exitosamente.\n\nTotal procesados: {procesadosGlobal:N0}\nExitosos: {exitososGlobal:N0}\nFallidos: {fallidosGlobal}", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            lblEstado.Text = $"Error: {ex.Message}";
            lblEstado.ForeColor = System.Drawing.Color.Red;
            Log($"ERROR: {ex.Message}");
            _resumenMigracion.AppendLine($"ERROR CRÍTICO: {ex.Message}");
            MessageBox.Show($"Ocurrió un error en la migración:\n{ex.Message}", "Error de Migración", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            btnMigrar.Enabled = true;
            btnSeleccionar.Enabled = true;
        }
    }

    private async Task<(int Exitosos, int Fallidos)> MigrarCapaDetalladaAsync(SqlConnection conn, CapaSHP capa)
    {
        int exitosos = 0;
        int fallidos = 0;

        using var shp = new ShapefileDataReader(capa.ArchivoSHP, _geometryFactory);
        int fieldCount = shp.FieldCount;

        var fieldNames = new string[fieldCount];
        for (int i = 0; i < fieldCount; i++)
            fieldNames[i] = shp.GetName(i).Trim().ToUpperInvariant();

        using var txn = conn.BeginTransaction();
        try
        {
            while (shp.Read())
            {
                var geom = shp.Geometry;
                if (geom != null && geom.SRID != 4326) geom.SRID = 4326;
                if (geom != null) geom = StripZ(geom);

                var values = new string[fieldCount];
                for (int i = 0; i < fieldCount; i++)
                {
                    values[i] = shp.GetValue(i)?.ToString()?.Trim() ?? "";
                }

                try
                {
                    switch (capa.TablaDestino)
                    {
                        case "CodigosFijos":
                            await InsertCodigoFijoAsync(conn, txn, fieldNames, values, geom);
                            break;
                        case "Manzanas":
                            await InsertManzanaAsync(conn, txn, fieldNames, values, geom);
                            break;
                        case "Lotes":
                            await InsertLoteAsync(conn, txn, fieldNames, values, geom);
                            break;
                        case "Vias":
                            await InsertViaAsync(conn, txn, fieldNames, values, geom);
                            break;
                    }
                    exitosos++;
                }
                catch
                {
                    fallidos++;
                }

                if ((exitosos + fallidos) % 100 == 0)
                {
                    lblEstado.Text = $"Migrando {capa.TablaDestino}: {exitosos + fallidos}/{capa.NumRegistros}...";
                    Application.DoEvents();
                }
            }
            txn.Commit();
        }
        catch (Exception ex)
        {
            txn.Rollback();
            Log($"Error en transacción {capa.TablaDestino}: {ex.Message}");
            throw;
        }

        return (exitosos, fallidos);
    }

    private async Task InsertCodigoFijoAsync(SqlConnection conn, SqlTransaction txn, string[] fields, string[] vals, Geometry? geom)
    {
        int? GetInt(params string[] names) => FindField(fields, vals, names, (s) => int.TryParse(s, out var r) ? r : (int?)null);
        string? GetString(params string[] names) => FindFieldStr(fields, vals, names);
        double? GetDbl(params string[] names) => FindField(fields, vals, names, (s) => double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var r) ? r : (double?)null);

        string sql = @"INSERT INTO CodigosFijos (CodF_SQL, CodF_SIG, CodFijo, Nombre, Estado, FechaCambioEstado, Longitud, Latitud, Geom)
                       VALUES (@a,@b,@c,@d,@e,@f,@g,@h,geometry::STGeomFromText(@geom, 4326))";
        using var cmd = new SqlCommand(sql, conn, txn);
        cmd.Parameters.AddWithValue("@a", (object?)GetInt("CODF_SQL", "COD_F_SQL") ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@b", (object?)GetString("CODF_SIG", "COD_F_SIG", "COD_SIG") ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@c", (object?)GetInt("CODFIJO", "COD_FIJO", "CODIGO") ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@d", (object?)GetString("NOMBRE", "NOM", "PROPIETAR") ?? DBNull.Value);

        // Si el estado no viene especificado o es 0, asignar 1 (Normal)
        int est = GetInt("ESTADO") ?? 1;
        if (est < 1 || est > 5) est = 1;
        cmd.Parameters.AddWithValue("@e", est);
        cmd.Parameters.AddWithValue("@f", DateTime.Now);

        double? lon = GetDbl("LONGITUD", "LON", "X");
        double? lat = GetDbl("LATITUD", "LAT", "Y");
        if (geom is NtsPoint pt) { lon ??= pt.X; lat ??= pt.Y; }
        cmd.Parameters.AddWithValue("@g", (object?)lon ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@h", (object?)lat ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@geom", geom != null ? geom.AsText() : (object)"POINT EMPTY");
        await cmd.ExecuteNonQueryAsync();
    }

    private async Task InsertManzanaAsync(SqlConnection conn, SqlTransaction txn, string[] fields, string[] vals, Geometry? geom)
    {
        string? GetString(params string[] names) => FindFieldStr(fields, vals, names);
        int? GetInt(params string[] names) => FindField(fields, vals, names, (s) => int.TryParse(s, out var r) ? r : (int?)null);

        string sql = @"INSERT INTO Manzanas (IdOrigen, UV_MZA, UV, MZA, Geom)
                       VALUES (@a,@b,@c,@d,geometry::STGeomFromText(@geom, 4326))";
        using var cmd = new SqlCommand(sql, conn, txn);
        cmd.Parameters.AddWithValue("@a", (object?)GetInt("OBJECTID", "ID", "FID") ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@b", (object?)GetString("UV_MZA", "UV-MZA", "UVMZA") ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@c", (object?)GetString("UV", "UNIDAD_VEC", "UNIDADVEC") ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@d", (object?)GetString("MZA", "MANZANA", "MANZ") ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@geom", geom != null ? geom.AsText() : (object)"POLYGON EMPTY");
        await cmd.ExecuteNonQueryAsync();
    }

    private async Task InsertLoteAsync(SqlConnection conn, SqlTransaction txn, string[] fields, string[] vals, Geometry? geom)
    {
        string? GetString(params string[] names) => FindFieldStr(fields, vals, names);
        int? GetInt(params string[] names) => FindField(fields, vals, names, (s) => int.TryParse(s, out var r) ? r : (int?)null);

        string sql = @"INSERT INTO Lotes (IdOrigen, NroLote, Geom)
                       VALUES (@a,@b,geometry::STGeomFromText(@geom, 4326))";
        using var cmd = new SqlCommand(sql, conn, txn);
        cmd.Parameters.AddWithValue("@a", (object?)GetInt("OBJECTID", "ID", "FID") ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@b", (object?)GetString("NROLOTE", "NRO_LOTE", "LOTE", "NOM_LOTE") ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@geom", geom != null ? geom.AsText() : (object)"POLYGON EMPTY");
        await cmd.ExecuteNonQueryAsync();
    }

    private async Task InsertViaAsync(SqlConnection conn, SqlTransaction txn, string[] fields, string[] vals, Geometry? geom)
    {
        string? GetString(params string[] names) => FindFieldStr(fields, vals, names);
        int? GetInt(params string[] names) => FindField(fields, vals, names, (s) => int.TryParse(s, out var r) ? r : (int?)null);

        string sql = @"INSERT INTO Vias (OBJECTID, Nombre, TipoVia, OSMID, Geom)
                       VALUES (@a,@b,@c,@d,geometry::STGeomFromText(@geom, 4326))";
        using var cmd = new SqlCommand(sql, conn, txn);
        cmd.Parameters.AddWithValue("@a", (object?)GetInt("OBJECTID", "ID", "FID") ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@b", (object?)GetString("NOMBRE", "NOM", "NAME") ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@c", (object?)GetString("TIPOVIA", "TIPO_VIA", "TIPO", "HIGHWAY") ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@d", (object?)GetString("OSMID", "OSM_ID") ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@geom", geom != null ? geom.AsText() : (object)"LINESTRING EMPTY");
        await cmd.ExecuteNonQueryAsync();
    }

    private static T? FindField<T>(string[] fields, string[] vals, string[] names, Func<string, T?> converter)
        where T : struct
    {
        foreach (var name in names)
        {
            int idx = Array.IndexOf(fields, name.ToUpperInvariant());
            if (idx >= 0 && idx < vals.Length && !string.IsNullOrWhiteSpace(vals[idx]))
            {
                var result = converter(vals[idx]);
                if (result != null) return result;
            }
        }
        return null;
    }

    private static string? FindFieldStr(string[] fields, string[] vals, string[] names)
    {
        foreach (var name in names)
        {
            int idx = Array.IndexOf(fields, name.ToUpperInvariant());
            if (idx >= 0 && idx < vals.Length && !string.IsNullOrWhiteSpace(vals[idx]))
                return vals[idx];
        }
        return null;
    }

    private static Geometry StripZ(Geometry geom)
    {
        if (geom.IsEmpty || double.IsNaN(geom.Coordinate?.Z ?? double.NaN))
            return geom;

        var factory = new GeometryFactory(new PrecisionModel(), geom.SRID);

        static Coordinate[] Strip(Coordinate[] coords) =>
            coords.Select(c => new Coordinate(c.X, c.Y)).ToArray();

        static LinearRing MakeRing(GeometryFactory f, Coordinate[] coords) =>
            f.CreateLinearRing(Strip(coords));

        static Polygon StripPolygon(GeometryFactory f, Polygon poly)
        {
            var shell = MakeRing(f, poly.ExteriorRing.Coordinates);
            var holes = poly.InteriorRings
                .Cast<LinearRing>()
                .Select(ir => MakeRing(f, ir.Coordinates))
                .ToArray();
            return f.CreatePolygon(shell, holes);
        }

        var coord = geom.Coordinate!;

        return geom.GeometryType switch
        {
            "Point" =>
                factory.CreatePoint(new Coordinate(coord.X, coord.Y)),

            "MultiPoint" =>
                factory.CreateMultiPoint(
                    ((MultiPoint)geom).Geometries
                        .Cast<NtsPoint>()
                        .Select(p => factory.CreatePoint(new Coordinate(p.X, p.Y)))
                        .ToArray()),

            "LineString" =>
                factory.CreateLineString(Strip(geom.Coordinates)),

            "MultiLineString" =>
                factory.CreateMultiLineString(
                    ((MultiLineString)geom).Geometries
                        .Cast<LineString>()
                        .Select(ls => factory.CreateLineString(Strip(ls.Coordinates)))
                        .ToArray()),

            "Polygon" =>
                StripPolygon(factory, (Polygon)geom),

            "MultiPolygon" =>
                factory.CreateMultiPolygon(
                    ((MultiPolygon)geom).Geometries
                        .Cast<Polygon>()
                        .Select(poly => StripPolygon(factory, poly))
                        .ToArray()),

            "GeometryCollection" =>
                factory.CreateGeometryCollection(
                    ((GeometryCollection)geom).Geometries
                        .Select(g => StripZ(g))
                        .ToArray()),

            _ => geom
        };
    }

    private async Task RegistrarBitacoraAsync(SqlConnection conn, int total, int exitosos, string modo)
    {
        try
        {
            using var cmd = new SqlCommand(@"INSERT INTO BitacoraMigracion (FechaMigracion, Usuario, CapasMigradas, TotalRegistros, Estado, Observaciones)
                VALUES (@f,@u,@c,@t,@e,@o)", conn);
            cmd.Parameters.AddWithValue("@f", DateTime.Now);
            cmd.Parameters.AddWithValue("@u", Environment.UserName);
            cmd.Parameters.AddWithValue("@c", _capasDetectadas.Count);
            cmd.Parameters.AddWithValue("@t", exitosos);
            cmd.Parameters.AddWithValue("@e", "Completado");
            cmd.Parameters.AddWithValue("@o", $"Modalidad {modo}. Origen: {_carpetaOrigen}");
            await cmd.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            Log($"Advertencia: Bitácora no registrada: {ex.Message}");
        }
    }

    private void BtnExportarResumen_Click(object? sender, EventArgs e)
    {
        using var sfd = new SaveFileDialog
        {
            Filter = "Archivo de Texto (*.txt)|*.txt|Archivo CSV (*.csv)|*.csv",
            FileName = $"Resumen_Migracion_{DateTime.Now:yyyyMMdd_HHmm}.txt"
        };

        if (sfd.ShowDialog() == DialogResult.OK)
        {
            try
            {
                File.WriteAllText(sfd.FileName, _resumenMigracion.ToString(), Encoding.UTF8);
                MessageBox.Show("Resumen exportado exitosamente.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    private void Log(string msg)
    {
        txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {msg}\r\n");
    }

    private class CapaSHP
    {
        public string NombreBase { get; set; } = "";
        public string ArchivoSHP { get; set; } = "";
        public string TablaDestino { get; set; } = "";
        public int NumRegistros { get; set; }
        public bool EsWgs84 { get; set; }
    }
}