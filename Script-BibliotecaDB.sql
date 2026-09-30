-- Base de datos del Laboratorio 07 (Semana 07: arquitectura en capas)
-- Ejecutar completo en SSMS conectado al servidor (ej. .\SQLEXPRESS).
IF DB_ID(N'BibliotecaDB') IS NOT NULL
BEGIN
    ALTER DATABASE BibliotecaDB SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE BibliotecaDB;
END
GO
CREATE DATABASE BibliotecaDB;
GO
USE BibliotecaDB;
GO

-- ========== TABLAS ==========
-- Activo = 0 implica eliminación lógica (nunca DELETE físico).
CREATE TABLE Autores (
    AutorId       INT IDENTITY(1,1) PRIMARY KEY,
    Nombre        NVARCHAR(100) NOT NULL,
    Nacionalidad  NVARCHAR(50)  NULL,
    Activo        BIT           NOT NULL DEFAULT 1
);
GO

CREATE TABLE Libros (
    LibroId     INT IDENTITY(1,1) PRIMARY KEY,
    Titulo      NVARCHAR(150) NOT NULL,
    ISBN        NVARCHAR(20)  NOT NULL UNIQUE,
    AutorId     INT           NOT NULL REFERENCES Autores(AutorId),
    Ejemplares  INT           NOT NULL CHECK (Ejemplares >= 0),  -- disponibles en stock
    Activo      BIT           NOT NULL DEFAULT 1
);
GO

CREATE TABLE Socios (
    SocioId  INT IDENTITY(1,1) PRIMARY KEY,
    DNI      NVARCHAR(8)   NOT NULL UNIQUE,
    Nombre   NVARCHAR(100) NOT NULL,
    Email    NVARCHAR(100) NULL,
    Activo   BIT           NOT NULL DEFAULT 1
);
GO

-- La multa NO se guarda: la calcula la capa de Negocio (S/ 1.50 por día de retraso).
CREATE TABLE Prestamos (
    PrestamoId     INT IDENTITY(1,1) PRIMARY KEY,
    SocioId        INT          NOT NULL REFERENCES Socios(SocioId),
    FechaPrestamo  DATE         NOT NULL,
    FechaLimite    DATE         NOT NULL,
    Estado         NVARCHAR(20) NOT NULL DEFAULT N'Pendiente'
                   CHECK (Estado IN (N'Pendiente', N'Devuelto')),
    CHECK (FechaLimite >= FechaPrestamo)
);
GO

-- FechaDevolucion NULL = libro aún pendiente. Clave primaria compuesta.
CREATE TABLE DetallePrestamo (
    PrestamoId       INT  NOT NULL REFERENCES Prestamos(PrestamoId),
    LibroId          INT  NOT NULL REFERENCES Libros(LibroId),
    FechaDevolucion  DATE NULL,
    PRIMARY KEY (PrestamoId, LibroId)
);
GO

-- ========== DATOS DE PRUEBA ==========
INSERT INTO Autores (Nombre, Nacionalidad) VALUES
    (N'Gabriel García Márquez', N'Colombiana'),   -- 1
    (N'Mario Vargas Llosa',     N'Peruana'),      -- 2
    (N'Isabel Allende',         N'Chilena'),      -- 3
    (N'Jorge Luis Borges',      N'Argentina'),    -- 4
    (N'Julio Cortázar',         N'Argentina'),    -- 5
    (N'Ricardo Palma',          N'Peruana'),      -- 6
    (N'Miguel de Cervantes',    N'Española'),     -- 7
    (N'Octavio Paz',            N'Mexicana');     -- 8
GO

-- Ejemplares = disponibles ahora (ya descontados los prestados pendientes).
INSERT INTO Libros (Titulo, ISBN, AutorId, Ejemplares) VALUES
    (N'Cien años de soledad',              N'978-0-00-000001', 1, 2),  -- 1 (1 prestado)
    (N'El amor en los tiempos del cólera', N'978-0-00-000002', 1, 1),  -- 2 (1 prestado)
    (N'Crónica de una muerte anunciada',   N'978-0-00-000003', 1, 0),  -- 3 (sin stock, prueba de regla)
    (N'La ciudad y los perros',            N'978-0-00-000004', 2, 2),  -- 4 (1 prestado)
    (N'Conversación en La Catedral',       N'978-0-00-000005', 2, 3),  -- 5
    (N'La casa verde',                     N'978-0-00-000006', 2, 2),  -- 6
    (N'La casa de los espíritus',          N'978-0-00-000007', 3, 1),  -- 7 (1 prestado)
    (N'Eva Luna',                          N'978-0-00-000008', 3, 2),  -- 8
    (N'Paula',                             N'978-0-00-000009', 3, 2),  -- 9
    (N'Ficciones',                         N'978-0-00-000010', 4, 3),  -- 10
    (N'El Aleph',                          N'978-0-00-000011', 4, 2),  -- 11
    (N'Rayuela',                           N'978-0-00-000012', 5, 2),  -- 12
    (N'Bestiario',                         N'978-0-00-000013', 5, 1),  -- 13
    (N'Historias de cronopios y de famas', N'978-0-00-000014', 5, 2),  -- 14
    (N'Tradiciones peruanas',              N'978-0-00-000015', 6, 4),  -- 15
    (N'Don Quijote de la Mancha',          N'978-0-00-000016', 7, 3),  -- 16
    (N'Novelas ejemplares',                N'978-0-00-000017', 7, 2),  -- 17
    (N'El laberinto de la soledad',        N'978-0-00-000018', 8, 2),  -- 18
    (N'Libertad bajo palabra',             N'978-0-00-000019', 8, 1),  -- 19
    (N'Piedra de sol',                     N'978-0-00-000020', 8, 2);  -- 20
GO

INSERT INTO Socios (DNI, Nombre, Email) VALUES
    (N'70000001', N'María Moya',       N'maria.moya@mail.com'),      -- 1 (3 pendientes)
    (N'70000002', N'Carlos Quispe',    N'carlos.quispe@mail.com'),   -- 2
    (N'70000003', N'Lucía Fernández',  N'lucia.fernandez@mail.com'), -- 3
    (N'70000004', N'Jorge Ramírez',    NULL),                        -- 4
    (N'70000005', N'Ana Torres',       N'ana.torres@mail.com'),      -- 5
    (N'70000006', N'Luis Ramos',       N'luis.ramos@mail.com'),      -- 6
    (N'70000007', N'Carla Vega',       NULL),                        -- 7
    (N'70000008', N'Pedro Salazar',    N'pedro.salazar@mail.com'),   -- 8
    (N'70000009', N'Rosa Huamán',      N'rosa.huaman@mail.com'),     -- 9
    (N'70000010', N'Diego Castillo',   N'diego.castillo@mail.com');  -- 10
GO

INSERT INTO Prestamos (SocioId, FechaPrestamo, FechaLimite, Estado) VALUES
    (1, '2026-09-10', '2026-09-20', N'Pendiente'),  -- 1: socio con 3 libros pendientes (vencido)
    (2, '2026-09-25', '2026-10-05', N'Pendiente'),  -- 2
    (3, '2026-09-01', '2026-09-11', N'Devuelto'),   -- 3
    (4, '2026-09-15', '2026-09-25', N'Pendiente'),  -- 4: un libro devuelto, otro pendiente
    (5, '2026-09-05', '2026-09-15', N'Devuelto');   -- 5: devuelto con retraso
GO

INSERT INTO DetallePrestamo (PrestamoId, LibroId, FechaDevolucion) VALUES
    (1, 1, NULL),
    (1, 2, NULL),
    (1, 3, NULL),
    (2, 4, NULL),
    (3, 5, '2026-09-10'),
    (4, 6, '2026-09-22'),
    (4, 7, NULL),
    (5, 8, '2026-09-19');   -- 4 días de retraso => S/ 6.00
GO

-- Verificación rápida
SELECT (SELECT COUNT(*) FROM Autores)         AS Autores,
       (SELECT COUNT(*) FROM Libros)          AS Libros,
       (SELECT COUNT(*) FROM Socios)          AS Socios,
       (SELECT COUNT(*) FROM Prestamos)       AS Prestamos,
       (SELECT COUNT(*) FROM DetallePrestamo) AS Detalles;
GO
