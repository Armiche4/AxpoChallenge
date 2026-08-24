# PowerPositionReport — Notas de la solución

Este documento recoge las decisiones de diseño tomadas al resolver el reto técnico (`README.md` en la raíz contiene los requisitos originales del ejercicio, no de esta solución).

## Estructura del proyecto

Las carpetas siguen los **pasos del proceso** que describe el enunciado, no categorías técnicas: abrir el proyecto debería bastar para ver el flujo de la solución.

```
src/PowerPositionReport/
├── Program.cs                          composition root: configuración + DI + hosted service
├── appsettings.json
│
├── Scheduling/                         CUÁNDO se ejecuta
│   └── Worker.cs                       PeriodicTimer + política ante fallos (req. 7 y 8)
│
├── Extraction/                         QUÉ hace una ejecución
│   ├── IPowerPositionReportService.cs
│   └── PowerPositionReportService.cs   orquesta: trading system → agregación → CSV
│
├── Aggregation/                        el cálculo, sin E/S
│   ├── HourlyVolume.cs                 par hora local / volumen
│   ├── IPowerPositionAggregator.cs
│   ├── PowerPositionAggregator.cs      suma por hora local, resuelve el DST
│   └── LondonTimeZone.cs               Europe/London: regla del enunciado, no infraestructura
│
├── Reporting/                          la salida
│   ├── ICsvReportWriter.cs
│   └── CsvReportWriter.cs              formato y nombre del fichero (req. 3 y 4)
│
└── Configuration/                      cómo se arranca y se ajusta la aplicación
    ├── AppSettings.cs                  OutputPath / IntervalMinutes + validación
    └── ServiceCollectionExtensions.cs  registro de dependencias
```

Criterios aplicados al organizarlo:

- **Cada carpeta es una etapa, y su nombre sale del enunciado** ("an extract must run...", "aggregated volume per hour", "CSV output"). Un revisor reconoce los requisitos en el árbol de carpetas.
- **La interfaz vive junto a su implementación.** Separarlas en carpetas distintas (estilo `Interfaces/`) obliga a saltar de un sitio a otro para leer una sola idea.
- **Ninguna carpeta se llama por un mecanismo de C#.** Una carpeta `Extensions/` o `Helpers/` acaba siendo un cajón de sastre: el registro de dependencias es *configuración* de la aplicación, así que vive en `Configuration/`.
- **`LondonTimeZone` está en `Aggregation/`, no en una carpeta propia.** Que la hora local sea Europe/London es una regla del problema y la usa quien hace el cálculo; una carpeta con un único fichero de utilidades añade ruido sin añadir información.
- **`Worker` no se queda suelto en la raíz.** La raíz es solo el punto de entrada (`Program.cs`); la planificación es una responsabilidad más y tiene su carpeta como las demás.

El proyecto de tests (`src/PowerPositionReport.Test/`) se ha dejado plano: con seis ficheros cuyos nombres ya dicen qué prueban, añadir carpetas no aportaría nada.

## Configuración (requisito 5 y 6: carpeta de salida e intervalo por línea de comandos o fichero)

La configuración (`AppSettings.OutputPath`, `AppSettings.IntervalMinutes`) se define por defecto en `appsettings.json`, pero también se puede sobrescribir por línea de comandos **sin necesidad de código adicional**.

Esto funciona porque `Host.CreateApplicationBuilder(args)` (en `Program.cs`) registra automáticamente varias fuentes de configuración, en este orden de precedencia (la última que define un valor gana):

1. `appsettings.json`
2. `appsettings.{Environment}.json`
3. Variables de entorno
4. Argumentos de línea de comandos (`args`)

Como los argumentos de línea de comandos son la fuente con mayor precedencia, basta con pasarlos usando la notación de sección de `IConfiguration` (`Sección:Clave=Valor`):

```powershell
dotnet run --project src\PowerPositionReport -- --AppSettings:OutputPath=C:\Reports --AppSettings:IntervalMinutes=10
```

o, ejecutando el binario ya compilado:

```powershell
PowerPositionReport.exe --AppSettings:OutputPath=C:\Reports --AppSettings:IntervalMinutes=10
```

También son válidas las variantes `--AppSettings:OutputPath C:\Reports` (espacio en vez de `=`) y `/AppSettings:OutputPath=C:\Reports`; las tres las reconoce el proveedor de configuración de línea de comandos por defecto de .NET.

### Cómo verificarlo manualmente

> Esta sesión de Cowork no tiene el SDK de .NET instalado (entorno cloud) ni acceso a una shell en tu máquina, así que la ejecución hay que hacerla desde tu terminal/Visual Studio local. Pasos:

1. Ejecuta sin argumentos y confirma que usa los valores de `appsettings.json` (`OutputPath=C:\PowerReports`, `IntervalMinutes=5`):

   ```powershell
   dotnet run --project src\PowerPositionReport
   ```

   En el log de arranque debe aparecer: `Worker starting. OutputPath=C:\PowerReports, IntervalMinutes=5`.

2. Ejecuta pasando argumentos y confirma que los sobrescriben:

   ```powershell
   dotnet run --project src\PowerPositionReport -- --AppSettings:OutputPath=C:\Temp\PowerReportsTest --AppSettings:IntervalMinutes=1
   ```

   El log debe mostrar ahora: `Worker starting. OutputPath=C:\Temp\PowerReportsTest, IntervalMinutes=1`.

3. (Opcional) Prueba con un valor inválido, p.ej. `--AppSettings:IntervalMinutes=0`, y confirma que el arranque falla por la validación (`ValidateDataAnnotations().ValidateOnStart()` en `Program.cs` + `[Range(1, int.MaxValue)]` en `AppSettings.cs`) en lugar de arrancar con un valor incorrecto.

Si el paso 2 muestra los valores pasados por línea de comandos, la configuración por línea de comandos funciona correctamente y no requiere ningún cambio de código adicional.

## Scheduler (requisitos 7 y 8: no perder una extracción programada, y ejecutar una al arrancar)

`Worker.cs` ya no es un bucle vacío: ahora orquesta las extracciones a través de `IPowerPositionReportService` (ver más abajo) usando un `PeriodicTimer`.

Decisiones tomadas:

- **`PeriodicTimer` en vez de `Task.Delay` en bucle.** Con `Task.Delay(intervalo)` dentro de un `while`, el intervalo real entre ejecuciones es `intervalo + tiempo que tardó la extracción anterior`: el retraso se va acumulando (drift) cada ciclo. `PeriodicTimer.WaitForNextTickAsync` mide cada periodo desde que terminó la espera anterior, por lo que el ritmo de ejecución no se desvía con el tiempo. Es además el tipo recomendado por .NET para este patrón exacto ("tick cada X"), y es *cancellation-aware*: `WaitForNextTickAsync(stoppingToken)` responde inmediatamente al `stoppingToken` cuando el host pide parar el servicio.
- **`do { extraer } while (esperar tick)`, y no `while (esperar tick) { extraer }` (requisito 8).** El `do/while` hace que la primera extracción ocurra **antes** de la primera espera, así que siempre hay un informe justo al arrancar, sea el intervalo de 1 minuto o de 60. Como efecto secundario evita duplicar la llamada (una fuera del bucle y otra dentro), que era la alternativa.
- **`try/catch` dentro del bucle, no alrededor de él (requisito 7, el más importante).** Si una extracción falla (`Axpo.PowerServiceException` porque el sistema de trading no responde, un error de E/S al escribir el CSV...), la excepción se registra con `logger.LogError` y el bucle continúa. Si escapara, deshacería la pila hasta salir de `ExecuteAsync`, lo que terminaría el `BackgroundService` entero y con él el `PeriodicTimer` y **todas** las extracciones futuras — justo lo que el requisito 7 prohíbe. Con el `catch` dentro, un fallo cuesta una ejecución, no la planificación.
- **El filtro `when (!stoppingToken.IsCancellationRequested)` distingue "fallo" de "apagado".** Cuando el host para el servicio, la extracción en curso lanza `OperationCanceledException`: eso no es un error y no debe ensuciar el log ni reintentarse. El filtro hace que en ese caso el `catch` no capture nada, la excepción salga del bucle y el worker termine. Es una línea en lugar de un segundo `catch`.

No hace falta capturar esa `OperationCanceledException` en ningún sitio: el host de .NET ya distingue una tarea cancelada durante el apagado de un fallo real, y no la registra como error.

### `IPowerPositionReportService` (nueva pieza, orquestador)

Para que `Worker.cs` solo se ocupara de la temporización (principio de responsabilidad única: un `BackgroundService` no debería saber nada de `PowerService`, agregación o CSV), se ha añadido `Extraction/IPowerPositionReportService` con un único método `RunExtractionAsync`. Su implementación, `PowerPositionReportService`, es la que:

1. Calcula "mañana" en hora local de Europe/London (posición day-ahead; ver más abajo).
2. Llama a `IPowerService.GetTradesAsync` con esa fecha.
3. Agrega los trades con `IPowerPositionAggregator`.
4. Escribe el CSV con `ICsvReportWriter`.

`Worker` depende únicamente de `IPowerPositionReportService` (inyectado por constructor), no conoce `IPowerService`, `IPowerPositionAggregator` ni `ICsvReportWriter` directamente.

## Fecha day-ahead pasada a `GetTradesAsync`

El README dice "an intra-day report to give them their day ahead power position". Son dos ejes distintos: "intra-day" es la cadencia (el job corre varias veces a lo largo de hoy); "day ahead power position" es el contenido (la posición del día de entrega de **mañana**, terminología estándar de mercados eléctricos).

Por la convención de `PowerService` (el periodo 1 de `GetTrades(date)` arranca a las 23:00 del día anterior a `date`, y el periodo 24 termina a las 23:00 de `date`), `GetTradesAsync(D)` devuelve el bloque horario `[23:00 de D-1, 23:00 de D)`. Para obtener la posición de mañana (`[23:00 hoy, 23:00 mañana)`) hay que pasar la fecha de **mañana**, no la de hoy.

Este cálculo se hace en `PowerPositionReportService.RunExtractionAsync` usando la hora local de Europe/London (`LondonTimeZone`), no la hora del servidor (que en producción probablemente sea UTC): `TimeZoneInfo.ConvertTime(DateTimeOffset.Now, LondonTimeZone.Instance).Date.AddDays(1)`.

Es una interpretación del enunciado (no dice literalmente "usa la fecha de mañana"), así que queda documentada aquí explícitamente — es un punto probable de pregunta en la entrevista ("¿por qué mañana y no hoy?").

Dos fechas que no hay que confundir en el código: (1) el timestamp del nombre del CSV (`PowerPosition_YYYYMMDD_HHMM.csv`) es "the local time of extract" = el momento en que corre el job, hoy; (2) la fecha que se pasa a `GetTradesAsync` es la del día de entrega reportado, mañana. Son independientes, y `PowerPositionReportService` las calcula por separado a partir del mismo `londonNow`.

## Registro de dependencias (`Configuration/ServiceCollectionExtensions.cs`)

Los registros de DI viven en un método de extensión, `AddPowerPositionReportServices(this IServiceCollection)`, en vez de estar sueltos en `Program.cs`. Así `Program.cs` se queda como un *composition root* corto (configuración → servicios → hosted service) y no crece con cada pieza nueva, y el registro de cada interfaz queda junto a las demás, en un solo sitio donde mirar.

Se sigue usando el contenedor estándar de .NET (`Microsoft.Extensions.DependencyInjection`); no se ha implementado un contenedor propio, que para este ejercicio sería sobre-ingeniería.

Cada interfaz se registra contra su única implementación (`IPowerService`, `IPowerPositionAggregator`, `ICsvReportWriter`, `IPowerPositionReportService`), todas como *singleton*: no tienen estado mutable y se reutilizan en cada extracción.

## Logging (requisito 9: logging adecuado para que soporte de producción diagnostique problemas)

Antes del cambio, la aplicación no configuraba ningún proveedor de logging explícito: usaba los que registra `Host.CreateApplicationBuilder` por defecto (consola, Debug y Event Log de Windows). Eso vale para desarrollo, pero no cumple bien el requisito 9 en producción: la app corre como `Worker Service`, previsiblemente desplegado como servicio de Windows, y ahí **no hay ninguna consola atendida** — nadie ve esa salida, y no queda nada fácil de revisar si una extracción falla de madrugada. Quedaba el Event Log, pero tiene limitaciones prácticas para diagnóstico: solo Windows, hace falta acceso remoto a la máquina para leerlo, no se puede filtrar/`grep` con facilidad, y se mezcla con el resto de eventos del sistema.

**Decisión: Serilog, escribiendo a consola (para `dotnet run`/F5) y a fichero con rotación diaria (para revisar después).** Configurado en `Program.cs` con `builder.Services.AddSerilog(...)`, que sustituye a los proveedores por defecto en vez de apilarse sobre ellos, así que la consola no duplica líneas.

El **nivel mínimo** se lee de la sección `Serilog:MinimumLevel` de `appsettings.json` (paquete `Serilog.Settings.Configuration`, `.ReadFrom.Configuration(builder.Configuration)`), no está fijado en código. Es la parte de "logging para soporte de producción" (requisito 9) que más vale la pena poder tocar sin recompilar: si en producción hace falta más detalle para diagnosticar un problema puntual, basta con cambiar `Information` por `Debug` en el `appsettings.json` desplegado y reiniciar el servicio, sin generar un build nuevo. Nótese que la sección se llama `Serilog`, no `Logging`: esa última es el esquema que leen los proveedores por defecto de Microsoft.Extensions.Logging (que Serilog sustituye), y reutilizar su nombre para una configuración que ya no la usa sería confuso. `appsettings.Development.json` sobrescribe el nivel a `Debug` como ejemplo de por qué merece la pena que sea configuración y no una constante.

Los **sinks** (a dónde van los logs: consola + fichero con esa ruta y esa rotación) sí se han dejado fijados en código. A diferencia del nivel, cambiar de sitio los logs es una decisión de despliegue, no algo que soporte de producción necesite tocar sobre la marcha; moverlo también a `appsettings.json` añadiría configuración (rutas, plantillas de sink) sin un caso de uso real para este ejercicio.

- **Ruta del fichero**: `logs/powerpositionreport-.log`, relativa a `AppContext.BaseDirectory` (la carpeta del ejecutable), no al directorio de trabajo — importante porque un servicio de Windows puede arrancar con un directorio de trabajo distinto (p.ej. `System32`). Limitación conocida: si el servicio se instala en una ruta protegida como `Program Files`, la cuenta del servicio necesita permiso de escritura ahí; en un despliegue real la ruta de logs debería ser configurable igual que `OutputPath`, pero para este ejercicio se ha preferido no añadir un segundo ajuste de carpeta.
- **Rotación diaria y retención de 31 días** (`rollingInterval: RollingInterval.Day`, `retainedFileCountLimit: 31`), para que los ficheros de log no crezcan sin límite.
- **Duración de cada extracción**: `PowerPositionReportService.RunExtractionAsync` mide con `Stopwatch` el tiempo desde justo antes de llamar a `PowerService.GetTradesAsync` hasta después de escribir el CSV, y lo añade al log final (`DurationMs=...`). Es la parte de la extracción con E/S real — `PowerService` simula 0–5 s de latencia por llamada — y es la métrica que ayudaría a diagnosticar una extracción lenta o colgada, que es justo el escenario que pide el requisito 9.

## Tests

Los tests (xUnit, proyecto `src/PowerPositionReport.Test`) están escritos con *fakes* hechos a mano en lugar de una librería de mocking: cada fake solo apunta lo que recibe y devuelve una respuesta prefijada, lo que mantiene los tests legibles y sin dependencias extra.

El criterio ha sido **pocos tests, cada uno fijando una decisión de diseño**, en vez de buscar cobertura exhaustiva: un test que solo repite lo que ya dice el código añade mantenimiento sin añadir confianza.

| Fichero | Qué cubre |
| --- | --- |
| `PowerPositionAggregatorTests` | Replica el ejemplo del README (agregación por hora local). |
| `CsvReportWriterTests` | Formato del CSV, nombre del fichero, creación de la carpeta de salida. |
| `AppSettingsValidationTests` | Validación de configuración (`OutputPath`, `IntervalMinutes`). |
| `PowerPositionReportServiceTests` | El orquestador, en 3 tests: que pide **mañana** y no hoy, que encadena trading system → aggregator → writer usando el `OutputPath` de configuración, y que **propaga** los fallos en vez de tragárselos. |
| `WorkerTests` | El scheduler, en 2 tests: que ejecuta una extracción al arrancar (requisito 8) y que **sigue vivo cuando una extracción lanza excepción** (requisito 7). |
| `ServiceRegistrationTests` | *Smoke test* de DI: monta el contenedor igual que `Program.cs` con `ValidateOnBuild` y resuelve todo. Detecta en el build un registro olvidado en `ServiceCollectionExtensions`, que si no solo se manifestaría al arrancar la aplicación. |

### Qué no cubren, y por qué

Probar que **el siguiente tick programado sí se dispara** tras un fallo obligaría a esperar un tick real del `PeriodicTimer` (un minuto con el intervalo mínimo configurable), demasiado lento para un test unitario. Lo que sí se prueba es justo la condición que lo rompería: que una extracción fallida no deshace `ExecuteAsync` ni, con él, el `PeriodicTimer`.

Lo mismo pasa con la fecha day-ahead: al depender de `DateTimeOffset.Now`, el test calcula "mañana" de la misma forma que el código de producción, y solo fallaría si el reloj cruzase la medianoche justo entre esas dos líneas.

La forma canónica de hacer ambas cosas deterministas sería inyectar `TimeProvider` (tipo del BCL desde .NET 8) en `Worker` y en el orquestador, y usar `FakeTimeProvider` (paquete `Microsoft.Extensions.TimeProvider.Testing`) en los tests para fijar el reloj y adelantar el tiempo a voluntad. Se ha dejado fuera a propósito para no añadir una abstracción más al código de producción a cambio de cobertura en un caso acotado; es una decisión reversible y un buen punto de conversación sobre el diseño.
