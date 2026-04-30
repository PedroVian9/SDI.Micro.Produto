CREATE EXTENSION IF NOT EXISTS pgcrypto;

CREATE SCHEMA IF NOT EXISTS sdi;

CREATE OR REPLACE FUNCTION sdi.fn_atualizar_ultima_alteracao()
RETURNS TRIGGER AS $$
BEGIN
    NEW.ultima_alteracao = NOW();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TABLE IF NOT EXISTS sdi.transporte (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),

    nome VARCHAR(150) NOT NULL,
    descricao VARCHAR(500) NULL,

    ativo BOOLEAN NOT NULL DEFAULT TRUE,

    data_cadastro TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    usuario_cadastro UUID NULL,

    ultima_alteracao TIMESTAMPTZ NULL,
    usuario_alteracao UUID NULL
);

CREATE TABLE IF NOT EXISTS sdi.categoria (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),

    categoria_pai_id UUID NULL,

    nome VARCHAR(150) NOT NULL,
    descricao VARCHAR(500) NULL,

    ativo BOOLEAN NOT NULL DEFAULT TRUE,

    data_cadastro TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    usuario_cadastro UUID NULL,

    ultima_alteracao TIMESTAMPTZ NULL,
    usuario_alteracao UUID NULL,

    CONSTRAINT fk_categoria_categoria_pai
        FOREIGN KEY (categoria_pai_id)
        REFERENCES sdi.categoria(id)
);

CREATE TABLE IF NOT EXISTS sdi.unidade_medida (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),

    nome VARCHAR(150) NOT NULL,
    sigla VARCHAR(20) NOT NULL,
    descricao VARCHAR(500) NULL,

    ativo BOOLEAN NOT NULL DEFAULT TRUE,

    data_cadastro TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    usuario_cadastro UUID NULL,

    ultima_alteracao TIMESTAMPTZ NULL,
    usuario_alteracao UUID NULL
);

CREATE TABLE IF NOT EXISTS sdi.produto (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),

    transporte_id UUID NOT NULL,
    categoria_id UUID NOT NULL,
    unidade_medida_id UUID NOT NULL,

    codigo VARCHAR(60) NOT NULL,

    nome VARCHAR(150) NOT NULL,
    descricao VARCHAR(1000) NULL,

    ativo BOOLEAN NOT NULL DEFAULT TRUE,

    data_cadastro TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    usuario_cadastro UUID NULL,

    ultima_alteracao TIMESTAMPTZ NULL,
    usuario_alteracao UUID NULL,

    CONSTRAINT fk_produto_transporte
        FOREIGN KEY (transporte_id)
        REFERENCES sdi.transporte(id),

    CONSTRAINT fk_produto_categoria
        FOREIGN KEY (categoria_id)
        REFERENCES sdi.categoria(id),

    CONSTRAINT fk_produto_unidade_medida
        FOREIGN KEY (unidade_medida_id)
        REFERENCES sdi.unidade_medida(id)
);

DO $$
BEGIN
    IF EXISTS (
        SELECT 1
        FROM pg_constraint
        WHERE conname = 'ck_produto_quantidade_total'
    ) THEN
        ALTER TABLE sdi.produto
        DROP CONSTRAINT ck_produto_quantidade_total;
    END IF;

    IF EXISTS (
        SELECT 1
        FROM pg_constraint
        WHERE conname = 'ck_produto_preco'
    ) THEN
        ALTER TABLE sdi.produto
        DROP CONSTRAINT ck_produto_preco;
    END IF;
END;
$$;

ALTER TABLE sdi.produto
DROP COLUMN IF EXISTS preco;

ALTER TABLE sdi.produto
DROP COLUMN IF EXISTS quantidade_total;

CREATE UNIQUE INDEX IF NOT EXISTS ux_sdi_transporte_nome
ON sdi.transporte (LOWER(nome));

CREATE UNIQUE INDEX IF NOT EXISTS ux_sdi_categoria_nome
ON sdi.categoria (LOWER(nome));

CREATE UNIQUE INDEX IF NOT EXISTS ux_sdi_unidade_medida_nome
ON sdi.unidade_medida (LOWER(nome));

CREATE UNIQUE INDEX IF NOT EXISTS ux_sdi_unidade_medida_sigla
ON sdi.unidade_medida (LOWER(sigla));

CREATE UNIQUE INDEX IF NOT EXISTS ux_sdi_produto_codigo
ON sdi.produto (LOWER(codigo));

CREATE INDEX IF NOT EXISTS ix_sdi_categoria_categoria_pai_id
ON sdi.categoria (categoria_pai_id);

CREATE INDEX IF NOT EXISTS ix_sdi_produto_transporte_id
ON sdi.produto (transporte_id);

CREATE INDEX IF NOT EXISTS ix_sdi_produto_categoria_id
ON sdi.produto (categoria_id);

CREATE INDEX IF NOT EXISTS ix_sdi_produto_unidade_medida_id
ON sdi.produto (unidade_medida_id);

CREATE INDEX IF NOT EXISTS ix_sdi_produto_nome
ON sdi.produto (nome);

DROP TRIGGER IF EXISTS trg_transporte_ultima_alteracao 
ON sdi.transporte;

CREATE TRIGGER trg_transporte_ultima_alteracao
BEFORE UPDATE ON sdi.transporte
FOR EACH ROW
EXECUTE FUNCTION sdi.fn_atualizar_ultima_alteracao();

DROP TRIGGER IF EXISTS trg_categoria_ultima_alteracao 
ON sdi.categoria;

CREATE TRIGGER trg_categoria_ultima_alteracao
BEFORE UPDATE ON sdi.categoria
FOR EACH ROW
EXECUTE FUNCTION sdi.fn_atualizar_ultima_alteracao();

DROP TRIGGER IF EXISTS trg_unidade_medida_ultima_alteracao 
ON sdi.unidade_medida;

CREATE TRIGGER trg_unidade_medida_ultima_alteracao
BEFORE UPDATE ON sdi.unidade_medida
FOR EACH ROW
EXECUTE FUNCTION sdi.fn_atualizar_ultima_alteracao();

DROP TRIGGER IF EXISTS trg_produto_ultima_alteracao 
ON sdi.produto;

CREATE TRIGGER trg_produto_ultima_alteracao
BEFORE UPDATE ON sdi.produto
FOR EACH ROW
EXECUTE FUNCTION sdi.fn_atualizar_ultima_alteracao();
