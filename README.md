# iRoute Commerce

Aplicación para importar comercios desde CSV, procesar sus registros por fecha y consultar los errores en cuarentena. Incluye inicio de sesión, previsualización antes del envío, API .NET y persistencia en SQL Server mediante procedimientos almacenados.

**Acceso demo:** usuario `demo@iroute.local`, contraseña `Comercio2026!`. La cuenta se crea al inicializar la base en desarrollo. Sigue los pasos siguientes antes de iniciar sesión.

## Tecnologías

- Angular 21, componentes standalone, rutas con carga diferida, formularios reactivos, signals y HttpClient.
- ASP.NET Core 10, controladores REST, JWT, PasswordHasher y CsvHelper.
- SQL Server 2022 o posterior, ADO.NET, parámetros tipados y un parámetro tabular para la importación por lotes.
- xUnit y Vitest para pruebas. Se incluyen pruebas de integración contra SQL Server real.

## Ejecución local en Windows

### Paso 1 Instalar los requisitos

Instala estas herramientas y abre una terminal nueva después de instalarlas:

| Herramienta | Versión o configuración |
| --- | --- |
| [Git](https://git-scm.com/downloads) | Para clonar el repositorio |
| [.NET SDK](https://dotnet.microsoft.com/download/dotnet/10.0) | SDK 10, incluye las herramientas de compilación |
| [Node.js](https://nodejs.org/) | 24 LTS, incluye npm |
| [PowerShell](https://learn.microsoft.com/powershell/scripting/install/installing-powershell-on-windows) | 7, para ejecutar los scripts y las pruebas HTTP |
| [SQL Server](https://www.microsoft.com/sql-server/sql-server-downloads) | 2022 o posterior, edición Developer/Express, o Express LocalDB |

Puedes usar una de estas opciones de base de datos:

- **LocalDB:** instala [SQL Server Express LocalDB](https://learn.microsoft.com/sql/database-engine/configure-windows/sql-server-express-localdb). La aplicación utiliza `(localdb)\MSSQLLocalDB` de forma predeterminada.
- **Instancia de SQL Server:** instala el motor de base de datos, crea una instancia llamada `IROUTE`, selecciona autenticación de Windows y agrega tu usuario como administrador de SQL. Comprueba que su servicio esté iniciado. Si ya tienes otra instancia, reemplaza `localhost\IROUTE` por su nombre en el paso 4.

SQL Server Management Studio es opcional: permite consultar las tablas manualmente, pero la aplicación crea su base y sus procedimientos automáticamente.

### Paso 2 Descargar el código

En PowerShell 7, ejecuta:

```powershell
git clone https://github.com/deadly21a/iroute-commerce-challenge.git
cd iroute-commerce-challenge
```

Mantén esta terminal en la raíz del repositorio, donde está este README.

### Paso 3 Preparar y verificar las herramientas

```powershell
dotnet --version
node --version
git --version
npm install --global pnpm@11.25.0
pnpm --version
dotnet restore iroute-commerce.slnx
```

`dotnet` debe mostrar una versión `10.x` y `node` una versión `v24.x`. Si una herramienta no se reconoce, revisa su instalación y vuelve a abrir la terminal.

### Paso 4 Crear la base de datos y la cuenta demo

**Si utilizas la instancia `IROUTE`, ejecuta:**

```powershell
$connection = 'Server=localhost\IROUTE;Database=IRouteCommerce;Integrated Security=true;Encrypt=true;TrustServerCertificate=true'
./scripts/Initialize-Database.ps1 -ConnectionString $connection
```

**Si utilizas LocalDB, ejecuta en su lugar:**

```powershell
./scripts/Initialize-Database.ps1
```

Espera el mensaje **Base IRouteCommerce inicializada**. Este paso crea la base, las tablas, los índices, los procedimientos almacenados y la cuenta demo. Puede repetirse sin borrar datos. El usuario de Windows debe tener permiso para crear la base; posteriormente requiere acceso a sus tablas y procedimientos.

No necesitas importar datos mediante SQL ni crear manualmente un usuario de la aplicación.

### Paso 5 Iniciar la API

En la misma terminal, para la instancia `IROUTE`:

```powershell
./scripts/Start-Backend.ps1 -ConnectionString $connection
```

Para LocalDB:

```powershell
./scripts/Start-Backend.ps1
```

Espera el mensaje **Now listening on: http://localhost:5080** y deja esta terminal abierta. Comprueba [el estado de la API](http://localhost:5080/api/health): debe devolver `"status": "healthy"`.

### Paso 6 Iniciar Angular

Abre una **segunda terminal** de PowerShell 7, entra en la carpeta que clonaste y luego ejecuta:

```powershell
# Parte desde la raíz iroute-commerce-challenge.
cd frontend
pnpm install --frozen-lockfile
pnpm start
```

Espera el mensaje **Local: http://localhost:4200/** y deja también esta terminal abierta. Angular redirige `/api` a la API mediante el proxy de desarrollo.

### Paso 7 Abrir la aplicación e iniciar sesión

Abre **[http://localhost:4200](http://localhost:4200)** en el navegador. Ingresa esta cuenta pública de demostración, exclusiva para evaluación local:

| Usuario o correo | Contraseña |
| --- | --- |
| `demo@iroute.local` | `Comercio2026!` |

También puedes pulsar **Usar credenciales demo** y luego **Entrar al workspace**. La contraseña se guarda en SQL Server como hash PBKDF2 con salt mediante ASP.NET Identity PasswordHasher. Esta aplicación no ofrece registro público ni recuperación de contraseña.

### Paso 8 Probar el archivo de ejemplo y los endpoints

Sigue el recorrido de la sección siguiente para importar el ejemplo, procesar sus dos fechas y consultar los errores. Para probar los endpoints, abre **[Swagger](http://localhost:5080/swagger)**, ejecuta `/api/auth/login` con las credenciales demo y copia el token de la respuesta en **Authorize**.

### Cómo detener y volver a abrir la aplicación

Pulsa `Ctrl+C` en ambas terminales para detener los servidores. En el siguiente arranque, repite los pasos **5 y 6**, usando `pnpm start` en el frontend. No hace falta reinstalar dependencias ni inicializar otra vez la base; los datos se conservan.

Como alternativa, después de instalar las dependencias puedes iniciar ambos servidores en segundo plano desde la raíz:

```powershell
# Instancia IROUTE
./scripts/Start-Local.ps1 -ConnectionString 'Server=localhost\IROUTE;Database=IRouteCommerce;Integrated Security=true;Encrypt=true;TrustServerCertificate=true'

# O LocalDB
./scripts/Start-Local.ps1
```

Este modo guarda los logs en `.local`. Para revisar el arranque y detenerlo fácilmente con `Ctrl+C`, utiliza el modo de las dos terminales descrito arriba.

### Problemas habituales

| Problema | Qué revisar |
| --- | --- |
| No abre `localhost:4200` | Mantén Angular iniciado con `pnpm start` y revisa su terminal |
| No se conecta con el servidor | Mantén la API iniciada en el puerto 5080 y consulta `/api/health` |
| SQL Server no disponible | Verifica que el servicio de la instancia esté iniciado y que el nombre de servidor sea correcto |
| El login no acepta la cuenta demo | Inicializa la base con el script del paso 4 y usa exactamente las credenciales anteriores |
| El puerto está ocupado | Detén la ejecución anterior antes de iniciar otra en el mismo puerto |
| Archivo ya importado | El contenido ya existe; continúa con el procesamiento en lugar de duplicarlo |
| Se bloquea la ejecución de `.ps1` | Desde la raíz, usa los comandos equivalentes que se muestran a continuación |

Comandos equivalentes si la política de PowerShell impide ejecutar scripts, para la instancia `IROUTE`:

```powershell
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:ConnectionStrings__Commerce = 'Server=localhost\IROUTE;Database=IRouteCommerce;Integrated Security=true;Encrypt=true;TrustServerCertificate=true'
dotnet run --project backend/Commerce.Api -- --initialize-db
dotnet run --project backend/Commerce.Api --urls http://localhost:5080
```

Para LocalDB, omite la línea de `ConnectionStrings__Commerce` en una terminal nueva. El frontend se inicia con los mismos comandos del paso 6.

## Demostración de principio a fin

1. Inicia sesión.
2. En **Importar archivo**, descarga el ejemplo o selecciona `samples/commerce_07102026.csv`.
3. Revisa la previsualización: 15 registros y dos fechas. Hasta ese momento el archivo permanece en el navegador.
4. Pulsa **Importar 15 registros**. Se almacenan las filas completas en `commerce`, incluidas las que incumplen las reglas de negocio.
5. En **Procesar registros**, selecciona `2026-10-07`: se separan **8 registros** y se conservan **4** para esa fecha.
6. Consulta **Cuarentena**. Cada fila conserva el lote y los motivos; **Detalles** muestra todas sus columnas.
7. Procesa `2026-10-08`: se separan **2 registros** y se conserva **1** para esa fecha.
8. Vuelve a procesar una fecha: el contador de nuevos registros en cuarentena es **0**. Al final quedan **5** registros en `commerce` y **10** en cuarentena.

Estos valores presuponen una base sin importaciones previas del ejemplo. Importar exactamente el mismo contenido nuevamente devuelve **409**, evitando duplicados por SHA-256.

## Contrato CSV

Codificación UTF-8, con o sin BOM; separador coma; encabezados exactos y en este orden:

```csv
pc_processdate,pc_nomcomred,pc_numdoc,pc_email,pc_telefono,pc_direccion
2026-10-07,"Comercio, Ejemplo",0012345678,demo@example.com,0000000000,"Avenida Ficticia, Local 1"
```

| Columna | SQL | Regla |
| --- | --- | --- |
| `pc_processdate` | `DATE` | Fecha válida `yyyy-MM-dd` |
| `pc_nomcomred` | `NVARCHAR(200)` | Al procesar no puede estar vacío ni contener solamente espacios |
| `pc_numdoc` | `NVARCHAR(50)` | Al procesar es obligatorio y solo admite dígitos ASCII `0`–`9` |
| `pc_email` | `NVARCHAR(254)` | Se conserva; no tiene regla de negocio en el enunciado |
| `pc_telefono` | `NVARCHAR(40)` | Se conserva como texto |
| `pc_direccion` | `NVARCHAR(300)` | Se conserva como texto |

El archivo se llama `commerce_DDMMYYYY.csv`, con una fecha de calendario válida. El nombre identifica el archivo; el procesamiento usa la columna `pc_processdate`, por lo que un archivo puede contener varias fechas.

Límites: 10 MB y 50 000 filas. Fechas inválidas, encabezados incorrectos, comillas mal cerradas, cantidades de columnas incorrectas y campos demasiado largos rechazan el archivo entero con **400**, sin guardar filas parcialmente. Un archivo vacío o que solo tenga encabezados también devuelve **400**. Los nombres vacíos y documentos inválidos se admiten al importar y se validan en SQL al procesar.

Los números de documento nunca se convierten a números: los ceros iniciales se conservan. Los espacios dentro o alrededor del documento son caracteres no permitidos; no se corrigen silenciosamente. Los motivos combinados se concatenan con `; `.

Las seis columnas fueron definidas para esta solución porque el enunciado permite crear el CSV y solo fija tres de ellas. Las tablas añaden `id` y `batch_id` para identificar y rastrear las filas; cuarentena añade `motivo` y `quarantined_at`.

## API

Las rutas `/api/commerce/*` requieren `Authorization: Bearer <token>`.

| Método | Ruta | Resultado |
| --- | --- | --- |
| POST | `/api/auth/login` | Token, vencimiento y usuario |
| POST | `/api/commerce/import` | Recibe `multipart/form-data`, campo `file`; devuelve lote y cantidad insertada |
| POST | `/api/commerce/process` | Recibe `{ "processDate": "2026-10-07" }`; devuelve nuevos registros en cuarentena y restantes |
| GET | `/api/commerce/quarantine` | Consulta paginada; admite `processDate`, `page`, `pageSize` |
| GET | `/api/commerce/overview` | Totales, fechas e importaciones recientes |
| GET | `/api/health` | Comprueba conexión y esquema de SQL Server |

Los errores usan `ProblemDetails`. Las credenciales incorrectas y los tokens inválidos generan **401**. El login admite 10 intentos por minuto por IP; excederlos genera **429**. Los problemas de conexión o ejecución SQL generan **503**, con un identificador para correlacionar los logs del servidor y sin divulgar detalles internos al navegador.

## Base de datos y arquitectura

```text
Angular → controlador .NET → servicio CSV / repositorio → SQL Server
                ↑                                         |
              JWT                 sp_create_commerce ← TVP con todas las filas
                                  sp_process_commerce → movimiento transaccional
                                  sp_list_commerce_quarantine → filtro y paginación
```

- `database/001_schema.sql`: tablas, índices y tipo tabular. Agrega explícitamente `motivo` a cuarentena.
- `database/002_procedures.sql`: importación, procesamiento, listado y resumen.
- `backend/Commerce.Api/Controllers`: contratos HTTP y respuestas.
- `backend/Commerce.Api/Services`: lectura del CSV y emisión de tokens.
- `backend/Commerce.Api/Data`: inicialización y acceso parametrizado a SQL.
- `frontend/src/app/core`: autenticación, contratos, cliente HTTP y previsualización CSV.
- `frontend/src/app/pages`: pantallas independientes cargadas bajo demanda.

`sp_create_commerce` inserta el lote y todas sus filas en una transacción. El procesamiento usa `DELETE ... OUTPUT` hacia una tabla variable, seguido de `INSERT` a cuarentena en la misma transacción: si falla la inserción, también se revierte la eliminación. Esta tabla intermedia permite mantener la clave foránea de cuarentena al lote, ya que SQL Server restringe `OUTPUT INTO` sobre destinos con claves foráneas. Los bloqueos serializan el procesamiento concurrente de una misma fecha.

Los índices por fecha evitan recorrer todas las filas al procesar; el listado es paginado y el resumen de importaciones está limitado a cinco lotes. El CSV se lee por completo antes de escribir y la API envía un parámetro tabular, evitando ejecutar un procedimiento por cada fila. El límite de tamaño mantiene acotado el uso de memoria.

## Compilación y pruebas

```powershell
dotnet build iroute-commerce.slnx --configuration Release
dotnet test iroute-commerce.slnx --configuration Release
cd frontend
pnpm build
pnpm test
```

Para ejecutar también las cuatro pruebas SQL, define la instancia disponible y vuelve a ejecutar `dotnet test` desde la raíz:

```powershell
$env:COMMERCE_TEST_CONNECTION = 'Server=localhost\IROUTE;Database=master;Integrated Security=true;Encrypt=true;TrustServerCertificate=true'
dotnet test iroute-commerce.slnx --configuration Release
```

Sin esa variable, las pruebas SQL se omiten explícitamente. Cada prueba crea una base temporal `IRouteCommerceTests_<guid>` y elimina exclusivamente esa base al terminar. Verifican motivos combinados, selección por fecha, reprocesamiento, duplicados, concurrencia y paginación. Se requiere permiso de creación y eliminación de esas bases temporales.

Con la API iniciada, ejecuta además:

```powershell
./scripts/Test-Api.ps1
```

Este script prueba login, importación, duplicados, procesamiento, consulta y autorización. Conserva sus datos de demostración en las fechas `2099-12-30` y `2099-12-31`.

## Configuración fuera del entorno local

La configuración local usa HTTP y `TrustServerCertificate=true` para certificados de SQL Server de desarrollo. Para otro entorno, configura HTTPS, un certificado SQL verificable, un usuario de base con privilegios mínimos y orígenes CORS explícitos. Proporciona la conexión y `Authentication__SigningKey` mediante variables de entorno o un gestor de secretos; la clave requiere al menos 32 bytes y no está guardada en Git.

En `Development`, una clave aleatoria en memoria se genera al iniciar: reiniciar la API invalida las sesiones. El token dura 60 minutos y Angular lo conserva en `sessionStorage`; para un producto con información sensible conviene adoptar un proveedor de identidad y cookies HttpOnly. La cuenta demo se crea únicamente en desarrollo. En producción la aplicación exige una clave configurada y Swagger queda deshabilitado.

El build de Angular se genera en `frontend/dist/commerce/browser`. Al desplegarlo, configura el servidor para devolver `index.html` en rutas de Angular y redirigir `/api` a la API. El enlace de este repositorio entrega el código; no es un alojamiento público de la aplicación.
