-- =============================================================
-- Script de criação do PDB_BANCO_V2
-- Banco melhorado com FKs, tipos corretos e sem dados redundantes
-- =============================================================

USE master;
GO

IF EXISTS (SELECT 1 FROM sys.databases WHERE name = N'PDB_BANCO_V2')
BEGIN
    ALTER DATABASE PDB_BANCO_V2 SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE PDB_BANCO_V2;
END
GO

CREATE DATABASE PDB_BANCO_V2;
GO

USE PDB_BANCO_V2;
GO

-- =============================================================
-- TIPOS
-- Armazena os labels das dimensões por tipo de peça.
-- M4 é NULL quando o tipo só tem 3 dimensões.
-- =============================================================
CREATE TABLE TIPOS (
    Id  INT           IDENTITY(1,1) PRIMARY KEY,
    M1  NVARCHAR(50)  NOT NULL,
    M2  NVARCHAR(50)  NOT NULL,
    M3  NVARCHAR(50)  NOT NULL,
    M4  NVARCHAR(50)  NULL          -- NULL substitui o sentinel '-' do schema antigo
);
GO

-- =============================================================
-- CATEGORIAS
-- Cada categoria aponta para um TIPO que define os labels.
-- =============================================================
CREATE TABLE CATEGORIAS (
    Id      INT           IDENTITY(1,1) PRIMARY KEY,
    NOME    NVARCHAR(100) NOT NULL,
    TIPO_ID INT           NOT NULL,
    CONSTRAINT UQ_CATEGORIAS_NOME  UNIQUE (NOME),
    CONSTRAINT FK_CATEGORIAS_TIPOS FOREIGN KEY (TIPO_ID) REFERENCES TIPOS(Id)
);
GO

-- =============================================================
-- USERS
-- Usuários do sistema. PERMISSAO restrita a 'admin' ou 'user'.
-- =============================================================
CREATE TABLE USERS (
    Id        INT           IDENTITY(1,1) PRIMARY KEY,
    IDPDM     NVARCHAR(100) NOT NULL,
    PERMISSAO NVARCHAR(10)  NOT NULL DEFAULT 'user',
    CONSTRAINT UQ_USERS_IDPDM     UNIQUE (IDPDM),
    CONSTRAINT CK_USERS_PERMISSAO CHECK (PERMISSAO IN ('admin', 'user'))
);
GO

-- =============================================================
-- MEDIDAS
-- Nomes dos parâmetros do SolidWorks por categoria (1:1 com CATEGORIAS).
-- Usados na coleta automática de dimensões.
-- =============================================================
CREATE TABLE MEDIDAS (
    Id           INT           IDENTITY(1,1) PRIMARY KEY,
    CATEGORIA_ID INT           NOT NULL,
    M1           NVARCHAR(100) NULL,
    M2           NVARCHAR(100) NULL,
    M3           NVARCHAR(100) NULL,
    M4           NVARCHAR(100) NULL,
    CONSTRAINT UQ_MEDIDAS_CATEGORIA  UNIQUE (CATEGORIA_ID),        -- garante 1:1
    CONSTRAINT FK_MEDIDAS_CATEGORIAS FOREIGN KEY (CATEGORIA_ID) REFERENCES CATEGORIAS(Id)
);
GO

-- =============================================================
-- MATERIAIS
-- Peças cadastradas.
-- M1-M4 como DECIMAL em vez de varchar.
-- CAD_POR como FK em vez de texto livre.
-- DATE_PROJ como DATE em vez de varchar.
-- PATH_FILE e NAME_FILE removidos (calculados na aplicação).
-- UNIQUE em (COD1, COD2, COD3) previne duplicatas no banco.
-- =============================================================
CREATE TABLE MATERIAIS (
    Id           INT           IDENTITY(1,1) PRIMARY KEY,
    CATEGORIA_ID INT           NOT NULL,
    M1           DECIMAL(10,3) NOT NULL,
    M2           DECIMAL(10,3) NOT NULL,
    M3           DECIMAL(10,3) NOT NULL,
    M4           DECIMAL(10,3) NULL,           -- NULL quando tipo tem só 3 dimensões
    COD1         CHAR(3)       NOT NULL,
    COD2         CHAR(3)       NOT NULL,
    COD3         CHAR(4)       NOT NULL,
    CAD_POR      INT           NOT NULL,
    DATE_PROJ    DATE          NOT NULL DEFAULT CAST(GETDATE() AS DATE),
    CONSTRAINT UQ_MATERIAIS_CODIGO     UNIQUE (COD1, COD2, COD3),
    CONSTRAINT FK_MATERIAIS_CATEGORIAS FOREIGN KEY (CATEGORIA_ID) REFERENCES CATEGORIAS(Id),
    CONSTRAINT FK_MATERIAIS_USERS      FOREIGN KEY (CAD_POR)      REFERENCES USERS(Id)
);
GO

CREATE INDEX IX_MATERIAIS_CATEGORIA ON MATERIAIS(CATEGORIA_ID);
CREATE INDEX IX_MATERIAIS_CODIGO    ON MATERIAIS(COD1, COD2, COD3);
GO
