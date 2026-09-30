# Lab07 · Biblioteca (arquitectura en capas)

Aplicación de escritorio (WPF, .NET 10) para administrar una biblioteca: **libros, socios, préstamos y devoluciones**.
El objetivo del laboratorio es aprender a **separar el programa en capas**, de modo que cada parte tenga una sola responsabilidad.

---

## ¿Qué puede hacer la aplicación?

| Pantalla | Qué hace |
|---|---|
| **Libros** | Registrar, editar y dar de baja libros. Muestra el nombre del autor. Se busca por **título o autor**. |
| **Socios** | Registrar, editar y dar de baja socios. Se busca por **nombre o DNI**. |
| **Préstamo** | Se elige un socio y se agregan **uno o varios libros**. Al registrar, se descuentan los ejemplares. |
| **Devolución** | Se elige un socio y el libro que devuelve. Muestra los **días de retraso y la multa** antes de confirmar. |
| **Reporte** | Lista los préstamos dentro de un **intervalo de fechas**: socio, libro, fecha límite y estado. |

### Reglas que aplica el sistema

- **Eliminación lógica:** "eliminar" un libro o socio solo marca `Activo = 0`. Nunca se borra el registro, así se conserva el historial.
- **ISBN y DNI únicos:** no se puede repetir ninguno.
- **No se da de baja** a un libro o socio que tenga préstamos pendientes.
- **Máximo 3 libros pendientes** por socio.
- **No se presta** un libro sin ejemplares, ni a socios o libros dados de baja.
- **Préstamo atómico:** cabecera, detalles y descuento de ejemplares se guardan en **una sola transacción**. Si algo falla, no se guarda nada.
- **Multa:** S/ 1.50 por cada día de retraso. La devolución regresa el ejemplar al stock.
- **Cierre automático:** cuando ya no quedan libros pendientes, el préstamo pasa a estado *Devuelto*.
- El plazo de cada préstamo es de **7 días** (constante `DiasDePrestamo`).

---

## La arquitectura en capas (la idea central)

Imagina un restaurante: el **cliente** (pantalla) pide, el **mesero** (negocio) revisa que el pedido tenga sentido y la **cocina** (datos) prepara. El cliente nunca entra a la cocina.

```
┌──────────────────┐
│  Biblioteca.WPF  │  Pantallas. Solo muestra y recoge datos.
└────────┬─────────┘
         │ usa
┌────────▼─────────┐
│Biblioteca.Negocio│  Reglas: valida ANTES de guardar y calcula la multa.
└────────┬─────────┘
         │ usa
┌────────▼─────────┐
│ Biblioteca.Datos │  Habla con SQL Server. No decide nada.
└────────┬─────────┘
         │
┌────────▼─────────┐        Biblioteca.Entidades lo usan todas las capas:
│   BibliotecaDB   │        son las clases "Libro", "Socio", "Prestamo"…
└──────────────────┘        solo con propiedades.
```

### Referencias entre proyectos

| Proyecto | Referencia a |
|---|---|
| `Biblioteca.Entidades` | *(ninguno)* |
| `Biblioteca.Datos` | Entidades |
| `Biblioteca.Negocio` | Datos y Entidades |
| `Biblioteca.WPF` | Negocio y Entidades |

La capa WPF **nunca** usa clases de Datos ni tipos de `SqlClient`.

### ¿Qué hace cada capa?

**Entidades** · Clases simples: `Autor`, `Libro`, `Socio`, `Prestamo`, `DetallePrestamo`. Solo propiedades, sin lógica.

**Datos** · Un repositorio por entidad (`LibroRepositorio`, `SocioRepositorio`, `PrestamoRepositorio`…).
Ejecuta consultas SQL **siempre con parámetros** (`SqlParameter`), nunca concatenando texto, para evitar inyección SQL.
Devuelve entidades o `List<T>`, jamás un `SqlDataReader` o `DataTable`. No contiene reglas de negocio.

**Negocio** · Tres clases con las reglas:

| Clase | Reglas que implementa |
|---|---|
| `LibroNegocio` | Título e ISBN obligatorios; ISBN único; el autor debe existir y estar activo; ejemplares no negativos; no dar de baja un libro con préstamos pendientes. |
| `SocioNegocio` | DNI de 8 dígitos y único; nombre obligatorio; email con formato válido; no dar de baja un socio con libros pendientes. |
| `PrestamoNegocio` | Máximo 3 libros pendientes; libro con ejemplares y activo; socio activo; sin libros repetidos; cálculo de multa (S/ 1.50 por día); cierre del préstamo al devolver el último libro. |

Cuando una regla no se cumple, lanza `ReglaNegocioException` con un mensaje claro.

**WPF** · Las pantallas. Solo captura `ReglaNegocioException` (para mostrar el aviso) y cualquier otra excepción genérica; **nunca** `SqlException`.

### ¿Por qué las reglas están en Negocio y no en otra capa?

- **No van en Datos:** la capa de Datos solo debe saber *cómo guardar*, no *si conviene guardar*. Si las reglas estuvieran ahí, cambiar de base de datos obligaría a reescribirlas.
- **No van en WPF:** si una regla viviera en una ventana, habría que copiarla en cada pantalla o programa nuevo, y cualquiera podría saltársela. Al estar en Negocio, **todas** las pantallas obedecen lo mismo.

---

## Detalles técnicos

- **Async de punta a punta:** desde el evento del botón (`private async void btn_Click`) hasta ADO.NET (`ExecuteReaderAsync`, `OpenAsync`). No se usa `.Result` ni `.Wait()`, así la ventana nunca se congela.
- **Transacciones:** `PrestamoRepositorio` registra el préstamo y la devolución dentro de una transacción con *rollback* ante cualquier error. El `UPDATE` de stock incluye `Ejemplares > 0` para evitar que dos usuarios se lleven el último ejemplar a la vez.
- **Reporte:** se obtiene con un `INNER JOIN` entre `Prestamos`, `DetallePrestamo`, `Libros` y `Socios`.
- **Inyección por constructor:** las clases de Negocio reciben interfaces de repositorio (`ILibroRepositorio`, etc.), y tienen un constructor por defecto para que WPF no tenga que referenciar Datos.

---

## Base de datos

Archivo: [`Script-BibliotecaDB.sql`](Script-BibliotecaDB.sql)

Tablas: `Autores`, `Libros`, `Socios`, `Prestamos` y `DetallePrestamo` (clave primaria compuesta, `FechaDevolucion` admite `NULL`).

Incluye datos de prueba: 8 autores, 20 libros, 10 socios y 5 préstamos. Sirven para probar las reglas:

- **María Moya** (socio 1) tiene 3 libros pendientes: no puede pedir más y su préstamo ya venció, así que genera multa al devolver.
- **Crónica de una muerte anunciada** tiene 0 ejemplares: no se puede prestar.

---

## Cómo ejecutarlo

1. **Crear la base de datos:** abre `Script-BibliotecaDB.sql` en SQL Server Management Studio y ejecútalo completo (F5).
2. **Revisar la conexión:** en `Biblioteca.WPF/App.config` ajusta el servidor si no usas `.\SQLEXPRESS`:
   ```xml
   <add name="BibliotecaDB"
        connectionString="Server=.\SQLEXPRESS;Database=BibliotecaDB;Integrated Security=True;TrustServerCertificate=True;" />
   ```
3. **Abrir la solución:** `Lab07.slnx` en Visual Studio.
4. **Proyecto de inicio:** clic derecho en `Biblioteca.WPF` → *Establecer como proyecto de inicio*.
5. Presiona **F5**.

Requisitos: Visual Studio con la carga de trabajo de escritorio .NET, .NET 10 y SQL Server (Express o LocalDB).

---

## Estructura de carpetas

```
Lab07/
├── Lab07.slnx
├── Script-BibliotecaDB.sql
├── Biblioteca.Entidades/     Autor, Libro, Socio, Prestamo, DetallePrestamo (+ clases de apoyo)
├── Biblioteca.Datos/         Repositorios e interfaces (ADO.NET con SqlClient)
├── Biblioteca.Negocio/       LibroNegocio, SocioNegocio, PrestamoNegocio, AutorNegocio, ReglaNegocioException
└── Biblioteca.WPF/           App.config, MainWindow y las 5 pantallas (Views/)
```

---

## Tecnologías

C# · .NET 10 · WPF · [WPF-UI](https://github.com/lepoco/wpfui) (estilo Fluent) · SQL Server · ADO.NET (`Microsoft.Data.SqlClient`)
