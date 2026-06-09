-- =============================================================
-- Migração: adiciona tabela PERMISSOES e migra coluna USERS.PERMISSAO
-- Execute APENAS em bancos que já existem com o schema anterior.
-- =============================================================

USE PDB_BANCO_V2;
GO

-- 1. Criar tabela de permissões
CREATE TABLE PERMISSOES (
    Id   INT          IDENTITY(1,1) PRIMARY KEY,
    NOME NVARCHAR(20) NOT NULL,
    CONSTRAINT UQ_PERMISSOES_NOME UNIQUE (NOME)
);
GO

-- 2. Inserir os três níveis de acesso
-- Id=1 admin | Id=2 usuario | Id=3 leitor
INSERT INTO PERMISSOES (NOME) VALUES ('admin');
INSERT INTO PERMISSOES (NOME) VALUES ('usuario');
INSERT INTO PERMISSOES (NOME) VALUES ('leitor');
GO

-- 3. Adicionar nova coluna (nullable para popular antes de tornar NOT NULL)
ALTER TABLE USERS ADD PERMISSAO_ID INT NULL;
GO

-- 4. Popular baseado no valor textual atual
UPDATE USERS
SET PERMISSAO_ID = (
    SELECT p.Id FROM PERMISSOES p
    WHERE p.NOME = CASE USERS.PERMISSAO
                       WHEN 'admin' THEN 'admin'
                       ELSE 'usuario'
                   END
);
GO

-- 5. Tornar NOT NULL
ALTER TABLE USERS ALTER COLUMN PERMISSAO_ID INT NOT NULL;
GO

-- 6. Remover constraint e coluna antigas
ALTER TABLE USERS DROP CONSTRAINT CK_USERS_PERMISSAO;
GO
ALTER TABLE USERS DROP COLUMN PERMISSAO;
GO

-- 7. Adicionar FK e DEFAULT (novos usuários entram como 'usuario' = Id 2)
ALTER TABLE USERS
    ADD CONSTRAINT FK_USERS_PERMISSOES  FOREIGN KEY (PERMISSAO_ID) REFERENCES PERMISSOES(Id),
        CONSTRAINT DF_USERS_PERMISSAO_ID DEFAULT 2 FOR PERMISSAO_ID;
GO

-- 8. Verificação
SELECT u.Id, u.IDPDM, p.NOME AS PERMISSAO
FROM USERS u JOIN PERMISSOES p ON u.PERMISSAO_ID = p.Id;
GO
