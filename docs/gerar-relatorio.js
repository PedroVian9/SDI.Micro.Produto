const {
  Document, Packer, Paragraph, TextRun, Table, TableRow, TableCell,
  Header, AlignmentType, HeadingLevel, BorderStyle, WidthType,
  ShadingType, VerticalAlign, PageNumber, PageBreak, LevelFormat, ImageRun
} = require('docx');
const fs   = require('fs');
const path = require('path');
const zlib = require('zlib');
const https = require('https');

// ───────────────────────────────────────────
// PlantUML → PNG via plantuml.com
// ───────────────────────────────────────────
function encode6bit(b) {
  if (b < 10) return String.fromCharCode(48 + b);
  b -= 10;
  if (b < 26) return String.fromCharCode(65 + b);
  b -= 26;
  if (b < 26) return String.fromCharCode(97 + b);
  b -= 26;
  if (b === 0) return '-';
  if (b === 1) return '_';
  return '?';
}
function append3bytes(b1, b2, b3) {
  return encode6bit(b1 >> 2)
    + encode6bit(((b1 & 3) << 4) | (b2 >> 4))
    + encode6bit(((b2 & 0xF) << 2) | (b3 >> 6))
    + encode6bit(b3 & 0x3F);
}
function encode64buf(buf) {
  let r = '';
  for (let i = 0; i < buf.length; i += 3) {
    const b1 = buf[i], b2 = buf[i+1] || 0, b3 = buf[i+2] || 0;
    r += append3bytes(b1, b2, b3);
  }
  return r;
}
function encodePlantUML(src) {
  const compressed = zlib.deflateRawSync(Buffer.from(src, 'utf-8'), { level: 9 });
  return '~1' + encode64buf(compressed);
}

function fetchPNG(pumlSrc) {
  return new Promise((resolve, reject) => {
    const encoded = encodePlantUML(pumlSrc);
    const url = `https://www.plantuml.com/plantuml/png/${encoded}`;
    function get(u, depth) {
      if (depth > 5) return reject(new Error('too many redirects'));
      const mod = u.startsWith('https') ? require('https') : require('http');
      mod.get(u, res => {
        if (res.statusCode === 301 || res.statusCode === 302) {
          return get(res.headers.location, depth + 1);
        }
        if (res.statusCode !== 200) return reject(new Error('HTTP ' + res.statusCode));
        const chunks = [];
        res.on('data', d => chunks.push(d));
        res.on('end', () => resolve(Buffer.concat(chunks)));
        res.on('error', reject);
      }).on('error', reject);
    }
    get(url, 0);
  });
}

async function loadDiagram(pumlFile) {
  const src = fs.readFileSync(pumlFile, 'utf-8');
  console.log(`  Baixando diagrama: ${path.basename(pumlFile)} ...`);
  const buf = await fetchPNG(src);
  console.log(`  OK (${(buf.length/1024).toFixed(1)} KB)`);
  return buf;
}

function imgParagraph(data, widthPx, heightPx, caption) {
  const scale = Math.min(1, CONTENT_W / (widthPx * 9525 / 9525));
  const w = Math.round(widthPx * scale);
  const h = Math.round(heightPx * scale);
  const items = [
    new Paragraph({
      alignment: AlignmentType.CENTER,
      spacing: { before: 120, after: 60 },
      children: [new ImageRun({
        type: 'png',
        data,
        transformation: { width: Math.min(w, 580), height: Math.round(h * Math.min(w, 580) / w) },
        altText: { title: caption, description: caption, name: caption }
      })]
    })
  ];
  if (caption) {
    items.push(new Paragraph({
      alignment: AlignmentType.CENTER,
      spacing: { before: 0, after: 160 },
      children: [new TextRun({ text: caption, font: 'Arial', size: 18, italics: true, color: '555555' })]
    }));
  }
  return items;
}

// ───────────────────────────────────────────
// Helpers de formatação
// ───────────────────────────────────────────
const BLUE       = '1F4E79';
const BLUE_MID   = '2E75B6';
const GRAY_LIGHT = 'F2F2F2';
const GRAY_LINE  = 'BFBFBF';
const WHITE      = 'FFFFFF';
const BLACK      = '000000';

const border1  = { style: BorderStyle.SINGLE, size: 1, color: GRAY_LINE };
const borders  = { top: border1, bottom: border1, left: border1, right: border1 };

const PAGE_W   = 11906;
const MARGIN   = 1080;
const CONTENT_W = PAGE_W - 2 * MARGIN;

function h1(text) {
  return new Paragraph({
    heading: HeadingLevel.HEADING_1,
    spacing: { before: 360, after: 120 },
    children: [new TextRun({ text, bold: true, color: BLUE, size: 32, font: 'Arial' })]
  });
}
function h2(text) {
  return new Paragraph({
    heading: HeadingLevel.HEADING_2,
    spacing: { before: 280, after: 80 },
    children: [new TextRun({ text, bold: true, color: BLUE_MID, size: 26, font: 'Arial' })]
  });
}
function h3(text) {
  return new Paragraph({
    heading: HeadingLevel.HEADING_3,
    spacing: { before: 200, after: 60 },
    children: [new TextRun({ text, bold: true, color: BLUE_MID, size: 24, font: 'Arial' })]
  });
}
function p(text, opts = {}) {
  return new Paragraph({
    spacing: { before: 60, after: 100 },
    children: [new TextRun({ text, font: 'Arial', size: 22, color: BLACK, ...opts })]
  });
}
function pBold(text) { return p(text, { bold: true }); }

function bullet(text) {
  return new Paragraph({
    numbering: { reference: 'bullets', level: 0 },
    spacing: { before: 40, after: 40 },
    children: [new TextRun({ text, font: 'Arial', size: 22, color: BLACK })]
  });
}
function code(text) {
  return new Paragraph({
    spacing: { before: 60, after: 60 },
    indent: { left: 360 },
    border: { left: { style: BorderStyle.SINGLE, size: 6, color: BLUE_MID, space: 6 } },
    children: [new TextRun({ text, font: 'Courier New', size: 18, color: '444444' })]
  });
}
function pageBreak() { return new Paragraph({ children: [new PageBreak()] }); }
function divider() {
  return new Paragraph({
    spacing: { before: 120, after: 120 },
    border: { bottom: { style: BorderStyle.SINGLE, size: 4, color: BLUE_MID, space: 1 } },
    children: []
  });
}

// ───────────────────────────────────────────
// Tabelas
// ───────────────────────────────────────────
function headerCell(text, w) {
  return new TableCell({
    borders, width: { size: w, type: WidthType.DXA },
    shading: { fill: BLUE_MID, type: ShadingType.CLEAR },
    margins: { top: 80, bottom: 80, left: 120, right: 120 },
    verticalAlign: VerticalAlign.CENTER,
    children: [new Paragraph({
      alignment: AlignmentType.CENTER,
      children: [new TextRun({ text, bold: true, color: WHITE, font: 'Arial', size: 20 })]
    })]
  });
}
function dataCell(text, w, shade) {
  return new TableCell({
    borders, width: { size: w, type: WidthType.DXA },
    shading: { fill: shade ? GRAY_LIGHT : WHITE, type: ShadingType.CLEAR },
    margins: { top: 60, bottom: 60, left: 120, right: 120 },
    children: [new Paragraph({
      children: [new TextRun({ text: text || '', font: 'Arial', size: 20, color: BLACK })]
    })]
  });
}
function tbl(headers, rows, colWidths) {
  return new Table({
    width: { size: CONTENT_W, type: WidthType.DXA },
    columnWidths: colWidths,
    rows: [
      new TableRow({ tableHeader: true, children: headers.map((h, i) => headerCell(h, colWidths[i])) }),
      ...rows.map((row, ri) =>
        new TableRow({ children: row.map((c, i) => dataCell(c, colWidths[i], ri % 2 === 1)) }))
    ]
  });
}

function sp(before = 120) { return new Paragraph({ spacing: { before } }); }

// ───────────────────────────────────────────
// Capa
// ───────────────────────────────────────────
function coverPage() {
  return [
    sp(1200),
    new Paragraph({
      alignment: AlignmentType.CENTER, spacing: { before: 0, after: 80 },
      children: [new TextRun({ text: 'PORTAL B2B', bold: true, font: 'Arial', size: 52, color: BLUE })]
    }),
    new Paragraph({
      alignment: AlignmentType.CENTER, spacing: { before: 0, after: 240 },
      children: [new TextRun({ text: 'SDI.Micro.Produto', font: 'Arial', size: 32, color: BLUE_MID })]
    }),
    divider(),
    new Paragraph({
      alignment: AlignmentType.CENTER, spacing: { before: 200, after: 80 },
      children: [new TextRun({ text: 'RELATÓRIO TÉCNICO', bold: true, font: 'Arial', size: 40, color: BLUE })]
    }),
    new Paragraph({
      alignment: AlignmentType.CENTER, spacing: { before: 0, after: 400 },
      children: [new TextRun({ text: 'Microsserviços  •  Banco de Dados  •  Arquitetura e Infraestrutura', font: 'Arial', size: 24, color: '555555', italics: true })]
    }),
    new Paragraph({
      alignment: AlignmentType.CENTER, spacing: { before: 600, after: 60 },
      children: [new TextRun({ text: 'Disciplina: ', bold: true, font: 'Arial', size: 22, color: BLUE }), new TextRun({ text: 'Sistemas Distribuídos e Integração (SDI)', font: 'Arial', size: 22 })]
    }),
    new Paragraph({
      alignment: AlignmentType.CENTER, spacing: { before: 60, after: 60 },
      children: [new TextRun({ text: 'Equipe: ', bold: true, font: 'Arial', size: 22, color: BLUE }), new TextRun({ text: 'Pedro Viana', font: 'Arial', size: 22 })]
    }),
    new Paragraph({
      alignment: AlignmentType.CENTER, spacing: { before: 60, after: 0 },
      children: [new TextRun({ text: 'Data: ', bold: true, font: 'Arial', size: 22, color: BLUE }), new TextRun({ text: 'Junho de 2026', font: 'Arial', size: 22 })]
    }),
    pageBreak()
  ];
}

// ───────────────────────────────────────────
// Seções
// ───────────────────────────────────────────
function c2(a, b)  { return [Math.round(CONTENT_W*a), Math.round(CONTENT_W*b)]; }
function c3(a,b,c) { return [Math.round(CONTENT_W*a), Math.round(CONTENT_W*b), Math.round(CONTENT_W*c)]; }
function c4(a,b,c,d) { return [Math.round(CONTENT_W*a), Math.round(CONTENT_W*b), Math.round(CONTENT_W*c), Math.round(CONTENT_W*d)]; }

function sec1(imgs) {
  return [
    h1('1. MICROSSERVIÇOS (INCLUINDO AUTENTICAÇÃO)'),
    divider(),

    // 1.1
    h2('1.1 Requisitos de Negócio'),
    h3('Contexto do Domínio'),
    p('O Portal B2B é uma plataforma de comércio eletrônico voltada à transação entre empresas (Business-to-Business). Fornecedores disponibilizam catálogos de produtos para compradores empresariais, que realizam pedidos com base nesses catálogos.'),
    p('O microsserviço de Produtos (SDI.Micro.Produto) é a fonte de verdade centralizada para informações descritivas de produtos. Ele não armazena preços nem estoques — essas responsabilidades pertencem ao domínio de Fornecedores e Inventário, seguindo o Bounded Context do DDD.'),
    h3('Problemas de Negócio Resolvidos'),
    tbl(
      ['Problema', 'Solução'],
      [
        ['Ausência de catálogo centralizado e padronizado', 'Cadastro único com código e nome únicos em todo o sistema'],
        ['Necessidade de classificar produtos por finalidade', 'Gerenciamento hierárquico de categorias com N níveis'],
        ['Diferentes métodos de transporte por produto', 'Cadastro de modalidades de transporte associadas ao produto'],
        ['Inconsistência de unidades de medida entre fornecedores', 'Unidades padronizadas com sigla normalizada (UPPERCASE)'],
        ['Propagação de mudanças para outros serviços', 'Publicação de eventos via Kafka para sincronização assíncrona'],
      ],
      c2(0.35, 0.65)
    ),
    sp(120),
    h3('Regras de Negócio Centrais'),
    bullet('Unicidade: nome, código, sigla são únicos no sistema (case-insensitive).'),
    bullet('Catálogo puro: sem preço nem quantidade — pertencem a outros domínios.'),
    bullet('Soft Delete: o campo ativo=FALSE substitui a exclusão física.'),
    bullet('Hierarquia de Categorias: auto-referência sem limite de profundidade.'),
    bullet('Auditoria: data_cadastro, usuario_cadastro, ultima_alteracao, usuario_alteracao em todo registro.'),
    bullet('Propagação via Eventos: toda alteração gera evento Kafka padronizado.'),

    // 1.2
    sp(200),
    h2('1.2 Jornada do Usuário'),
    p('As etapas da jornada são: (1) Autenticação via JWT, (2) Configuração de pré-requisitos (transportes, categorias, unidades), (3) Cadastro do produto, (4) Consulta com filtros paginados, (5) Edição, (6) Inativação com publicação de evento.'),
    // DIAGRAMA 1 — Jornada do Usuário
    sp(80),
    ...imgParagraph(imgs.jornada, 800, 900, 'Figura 1 — Diagrama de Sequência: Jornada do Usuário'),

    // 1.3
    sp(200),
    h2('1.3 Protótipo (UX)'),
    tbl(
      ['Tela', 'Título', 'Descrição'],
      [
        ['1', 'Login', 'Formulário e-mail/senha. Autentica via auth-service e redireciona ao dashboard.'],
        ['2', 'Catálogo de Produtos', 'Tabela paginada com código, nome, categoria, transporte, unidade e status. Busca e filtros.'],
        ['3', 'Novo/Editar Produto', 'Formulário com validação Zod em tempo real. Dropdowns de categoria, transporte, unidade.'],
        ['4', 'Categorias', 'Lista hierárquica com subcategorias. CRUD completo com ativação/inativação.'],
        ['5', 'Transportes', 'Listagem de modalidades de transporte. CRUD com ativação/inativação.'],
        ['6', 'Unidades de Medida', 'Unidades com nome e sigla (UPPERCASE automático). CRUD completo.'],
      ],
      c3(0.08, 0.25, 0.67)
    ),

    // 1.4
    sp(200),
    h2('1.4 Requisitos Funcionais'),
    tbl(
      ['Código', 'Requisito'],
      [
        ['RF-01.1', 'Cadastrar produtos com código único (UPPERCASE), nome, descrição, categoria, transporte e unidade de medida'],
        ['RF-01.2', 'Listagem paginada com filtros por busca textual, categoria, transporte, unidade e status'],
        ['RF-01.3', 'Consulta de produto por ID'],
        ['RF-01.4', 'Atualização completa dos dados'],
        ['RF-01.5', 'Ativação e inativação (soft delete via PATCH)'],
        ['RF-01.6', 'Publicação de evento Kafka em toda operação de escrita'],
        ['RF-02.1', 'Categorias com hierarquia de N níveis (auto-referência)'],
        ['RF-05.1', 'Todas as rotas (exceto /saude) exigem JWT Bearer'],
        ['RF-05.3', 'O userId do JWT é registrado nos campos de auditoria'],
        ['RF-06.1', 'GET /saude valida API e conectividade com PostgreSQL'],
      ],
      c2(0.18, 0.82)
    ),
    sp(120),
    // DIAGRAMA 2 — Componentes internos
    ...imgParagraph(imgs.componentes, 800, 600, 'Figura 2 — Diagrama de Componentes: produtos-service (ASP.NET Core 10)'),

    // 1.5
    sp(200),
    h2('1.5 Requisitos Não Funcionais'),
    tbl(
      ['Código', 'Categoria', 'Requisito'],
      [
        ['RNF-01.1', 'Desempenho', 'Latência < 100ms em leituras com índices otimizados'],
        ['RNF-01.2', 'Desempenho', 'Pool de conexões PostgreSQL: até 100 conexões por instância'],
        ['RNF-01.3', 'Desempenho', 'Paginação obrigatória em todas as listagens (máx. 100 itens/página)'],
        ['RNF-02.1', 'Segurança', 'JWT com validação de issuer, audience, expiração e clock skew'],
        ['RNF-02.3', 'Segurança', 'Queries parametrizadas (Dapper) — prevenção de SQL Injection'],
        ['RNF-03.1', 'Disponibilidade', 'Restart policy unless-stopped no Docker Compose'],
        ['RNF-03.3', 'Disponibilidade', 'Falha no Kafka não derruba a operação HTTP (FAIL_ON_PUBLISH_ERROR=false)'],
        ['RNF-04.1', 'Observabilidade', 'Logging estruturado via Serilog (JSON) em todos os níveis'],
        ['RNF-04.3', 'Observabilidade', 'Auditoria completa em todos os registros (4 campos de auditoria)'],
      ],
      c3(0.12, 0.20, 0.68)
    ),
    sp(120),
    h3('Tecnologias NÃO Utilizadas e Justificativas'),
    tbl(
      ['Tecnologia', 'Motivo da Não Utilização'],
      [
        ['Entity Framework Core', 'Substituído por Dapper — maior controle sobre SQL e performance em queries complexas'],
        ['Redis (Cache)', 'Não justificado: volume de dados moderado e necessidade de dados sempre atualizados'],
        ['gRPC', 'Comunicação assíncrona via Kafka; REST adequado para frontend ↔ backend'],
        ['RabbitMQ', 'Substituído por Apache Kafka (Redpanda) — decisão da equipe de infraestrutura'],
        ['Oracle / SQL Server', 'Substituído por PostgreSQL (open-source, UUID nativo, decisão da equipe de infra)'],
        ['Autenticação própria', 'Delegada ao microsserviço de autenticação — JWT validado de forma stateless'],
      ],
      c2(0.30, 0.70)
    ),

    // 1.6
    sp(200),
    h2('1.6 Tecnologias Utilizadas'),
    h3('Backend — ASP.NET Core 10'),
    tbl(
      ['Tecnologia', 'Versão', 'Função'],
      [
        ['ASP.NET Core', '10.0', 'Framework principal. DI nativo, Middleware Pipeline, JWT Bearer.'],
        ['Dapper', '2.1.66', 'Micro-ORM. SQL puro parametrizado com mapeamento snake_case → PascalCase.'],
        ['Npgsql', '9.0.3', 'Driver PostgreSQL. Connection pooling, UUID e TIMESTAMPTZ nativos.'],
        ['Confluent.Kafka', '2.8.0', 'Cliente Kafka oficial. Publica eventos de integração assíncronos.'],
        ['Serilog', '8.0.1', 'Logging estruturado em JSON. Integra com ELK Stack / Grafana Loki.'],
        ['Swashbuckle', '6.6.2', 'Documentação OpenAPI 3.0 gerada automaticamente.'],
      ],
      c3(0.22, 0.13, 0.65)
    ),
    sp(100),
    h3('Frontend — React + Vite'),
    tbl(
      ['Tecnologia', 'Versão', 'Função'],
      [
        ['React', '18.3.1', 'Biblioteca de UI declarativa com hooks modernos.'],
        ['Vite', '5.4.19', 'Build tool com HMR. Proxy reverso /api para o backend em dev.'],
        ['TypeScript', '5.8', 'Tipagem estática. Detecção de erros em compile-time.'],
        ['Tailwind CSS', '3.4', 'Framework CSS utilitário com purge automático no build.'],
        ['Radix UI + shadcn/ui', 'latest', 'Componentes acessíveis (WAI-ARIA) customizáveis com Tailwind.'],
        ['TanStack React Query', '5.83', 'Cache de dados remotos com revalidação e retry automáticos.'],
        ['Zustand', '5.0.8', 'Estado global (JWT, usuário). Sem boilerplate excessivo.'],
        ['React Hook Form + Zod', '7.61.1', 'Formulários com validação de schema declarativa.'],
        ['Axios', '1.12.2', 'HTTP client com interceptors para Bearer Token automático.'],
      ],
      c3(0.22, 0.13, 0.65)
    ),

    // 1.7
    sp(200),
    h2('1.7 Considerações Gerais'),
    p('O produtos-service segue arquitetura em camadas: Controller → Service → Repository → PostgreSQL + Kafka. O Bounded Context de Catálogo exclui preços e estoques. O GlobalExceptionMiddleware converte exceções em respostas HTTP padronizadas (DomainException→400, UniqueViolation→409, FKViolation→400).'),

    // 1.8
    sp(160),
    h2('1.8 Instruções de Implantação'),
    tbl(['Componente','Versão Mínima'], [
      ['Docker Engine','24.0+'], ['Docker Compose','2.20+'],
      ['PostgreSQL','16 (na rede Docker)'], ['Redpanda/Kafka','compat. Kafka 3.x'],
    ], c2(0.35, 0.65)),
    sp(80),
    pBold('Passo 1 — Criar a rede Docker:'), code('docker network create portal-b2b-network'),
    pBold('Passo 2 — Configurar .env:'), code('POSTGRESQL_HOST=postgres | Jwt__SecretKey=CHAVE | KAFKA_BOOTSTRAP_SERVERS=redpanda:9092'),
    pBold('Passo 3 — Executar script de BD:'), code('psql -h localhost -U usuario -d portal_b2b -f Scripts/ScriptDeCriacao.sql'),
    pBold('Passo 4 — Subir containers:'), code('docker compose up -d --build'),
    pBold('Passo 5 — Verificar saúde:'), code('curl http://localhost:5002/saude'),

    pageBreak()
  ];
}

function sec2(imgs) {
  return [
    h1('2. BANCO DE DADOS'),
    divider(),

    // 2.1
    h2('2.1 Modelo de Dados Geral'),
    p('O sistema utiliza PostgreSQL compartilhado (portal_b2b) com isolamento por schema. O microsserviço de Produtos utiliza o schema portal_b2b com 4 tabelas.'),
    // DIAGRAMA 3 — ER
    sp(80),
    ...imgParagraph(imgs.er, 900, 600, 'Figura 3 — Diagrama Entidade-Relacionamento: Schema portal_b2b (Produtos)'),
    tbl(
      ['Tabela Origem', 'Coluna FK', 'Tabela Destino', 'Cardinalidade'],
      [
        ['produtos_produto', 'transporte_id', 'produtos_transporte.id', 'N:1'],
        ['produtos_produto', 'categoria_id', 'produtos_categoria.id', 'N:1'],
        ['produtos_produto', 'unidade_medida_id', 'produtos_unidade_medida.id', 'N:1'],
        ['produtos_categoria', 'categoria_pai_id', 'produtos_categoria.id', 'N:1 (self)'],
      ],
      c4(0.28, 0.24, 0.28, 0.20)
    ),

    // 2.2
    sp(200),
    h2('2.2 Padrões de Nomenclatura'),
    p('Todos os campos, tabelas e índices seguem snake_case. PostgreSQL converte identificadores para minúsculas por padrão; o snake_case evita necessidade de aspas duplas no SQL.'),
    p('Mapeamento no .NET: DefaultTypeMap.MatchNamesWithUnderscores = true faz o Dapper mapear colunas snake_case para propriedades PascalCase automaticamente.'),
    tbl(
      ['Padrão', 'Exemplo', 'Descrição'],
      [
        ['Chave primária', 'id', 'UUID gerado por gen_random_uuid()'],
        ['Chave estrangeira', 'categoria_pai_id', '{tabela_referenciada}_id'],
        ['Status', 'ativo', 'Booleano para soft delete (padrão TRUE)'],
        ['Data de criação', 'data_cadastro', 'TIMESTAMPTZ DEFAULT NOW()'],
        ['Data de atualização', 'ultima_alteracao', 'TIMESTAMPTZ atualizado por trigger BEFORE UPDATE'],
        ['Prefixo de tabela', 'produtos_*', 'Identifica o domínio/microsserviço'],
      ],
      c3(0.25, 0.25, 0.50)
    ),

    // 2.3
    sp(200),
    h2('2.3 Dicionário de Dados'),
    h3('Tabela: produtos_transporte'),
    tbl(
      ['Campo', 'Tipo', 'Null', 'Descrição'],
      [
        ['id', 'UUID', 'NOT NULL', 'Identificador único — gen_random_uuid()'],
        ['nome', 'VARCHAR(150)', 'NOT NULL', 'Nome único da modalidade (LOWER index)'],
        ['descricao', 'VARCHAR(500)', 'NULL', 'Descrição opcional'],
        ['ativo', 'BOOLEAN', 'NOT NULL', 'TRUE=ativo, FALSE=inativado. Padrão TRUE'],
        ['data_cadastro', 'TIMESTAMPTZ', 'NOT NULL', 'Criação — DEFAULT NOW()'],
        ['usuario_cadastro', 'UUID', 'NULL', 'ID do criador (extraído do JWT)'],
        ['ultima_alteracao', 'TIMESTAMPTZ', 'NULL', 'Atualizado por trigger BEFORE UPDATE'],
        ['usuario_alteracao', 'UUID', 'NULL', 'ID do último alterador'],
      ],
      c4(0.22, 0.20, 0.12, 0.46)
    ),
    sp(100),
    h3('Tabela: produtos_categoria'),
    tbl(
      ['Campo', 'Tipo', 'Null', 'Descrição'],
      [
        ['id', 'UUID', 'NOT NULL', 'Identificador único'],
        ['categoria_pai_id', 'UUID', 'NULL', 'FK auto-referencial — NULL = categoria raiz'],
        ['nome', 'VARCHAR(150)', 'NOT NULL', 'Nome único (case-insensitive)'],
        ['descricao', 'VARCHAR(500)', 'NULL', 'Descrição opcional'],
        ['ativo', 'BOOLEAN', 'NOT NULL', 'Status. Padrão TRUE'],
        ['data_cadastro', 'TIMESTAMPTZ', 'NOT NULL', 'Timestamp de criação'],
        ['usuario_cadastro', 'UUID', 'NULL', 'ID do criador'],
        ['ultima_alteracao', 'TIMESTAMPTZ', 'NULL', 'Trigger BEFORE UPDATE'],
        ['usuario_alteracao', 'UUID', 'NULL', 'ID do último alterador'],
      ],
      c4(0.22, 0.20, 0.12, 0.46)
    ),
    sp(100),
    h3('Tabela: produtos_unidade_medida'),
    tbl(
      ['Campo', 'Tipo', 'Null', 'Descrição'],
      [
        ['id', 'UUID', 'NOT NULL', 'Identificador único'],
        ['nome', 'VARCHAR(150)', 'NOT NULL', 'Ex.: "Quilograma". Único (case-insensitive)'],
        ['sigla', 'VARCHAR(20)', 'NOT NULL', 'Ex.: "KG". Armazenada em UPPERCASE. Única'],
        ['descricao', 'VARCHAR(500)', 'NULL', 'Descrição opcional'],
        ['ativo', 'BOOLEAN', 'NOT NULL', 'Status. Padrão TRUE'],
        ['data_cadastro', 'TIMESTAMPTZ', 'NOT NULL', 'Timestamp de criação'],
        ['usuario_cadastro', 'UUID', 'NULL', 'ID do criador'],
        ['ultima_alteracao', 'TIMESTAMPTZ', 'NULL', 'Trigger BEFORE UPDATE'],
        ['usuario_alteracao', 'UUID', 'NULL', 'ID do último alterador'],
      ],
      c4(0.22, 0.20, 0.12, 0.46)
    ),
    sp(100),
    h3('Tabela: produtos_produto'),
    p('Entidade central. Catálogo puro — sem preço ou quantidade.'),
    tbl(
      ['Campo', 'Tipo', 'Null', 'Descrição'],
      [
        ['id', 'UUID', 'NOT NULL', 'Identificador único do produto'],
        ['transporte_id', 'UUID', 'NOT NULL', 'FK para produtos_transporte.id'],
        ['categoria_id', 'UUID', 'NOT NULL', 'FK para produtos_categoria.id'],
        ['unidade_medida_id', 'UUID', 'NOT NULL', 'FK para produtos_unidade_medida.id'],
        ['codigo', 'VARCHAR(60)', 'NOT NULL', 'Código UPPERCASE único (case-insensitive)'],
        ['nome', 'VARCHAR(150)', 'NOT NULL', 'Nome descritivo do produto'],
        ['descricao', 'VARCHAR(1000)', 'NULL', 'Descrição detalhada opcional'],
        ['ativo', 'BOOLEAN', 'NOT NULL', 'Status. Padrão TRUE'],
        ['data_cadastro', 'TIMESTAMPTZ', 'NOT NULL', 'Timestamp de criação'],
        ['usuario_cadastro', 'UUID', 'NULL', 'ID do criador'],
        ['ultima_alteracao', 'TIMESTAMPTZ', 'NULL', 'Trigger BEFORE UPDATE'],
        ['usuario_alteracao', 'UUID', 'NULL', 'ID do último alterador'],
      ],
      c4(0.22, 0.20, 0.12, 0.46)
    ),

    // 2.4
    sp(200),
    h2('2.4 Requisitos Não Funcionais — Banco Distribuído'),
    p('Estratégia adotada: Shared Database, Separate Schema — banco PostgreSQL compartilhado com isolamento por schema. Cada microsserviço conecta com usuário PostgreSQL com permissões restritas ao seu schema.'),
    tbl(
      ['Aspecto', 'Estratégia'],
      [
        ['Isolamento', 'Schema separado por contexto (GRANT USAGE ON SCHEMA porta_b2b TO usuario_produtos)'],
        ['Connection Pooling', 'Npgsql: até 100 conexões por instância (Maximum Pool Size=100)'],
        ['Resiliência', 'Restart policy unless-stopped. Triggers de auditoria no banco (independem da app)'],
        ['Sincronização', 'Via Kafka: cada serviço mantém cópia dos dados que precisa'],
      ],
      c2(0.28, 0.72)
    ),

    // 2.5
    sp(200),
    h2('2.5 Modelo por Microsserviço'),
    p('Todas as 4 tabelas pertencem ao schema portal_b2b do microsserviço de Produtos:'),
    bullet('produtos_transporte (1) — (N) produtos_produto'),
    bullet('produtos_categoria  (1) — (N) produtos_produto'),
    bullet('produtos_unidade_medida (1) — (N) produtos_produto'),
    bullet('produtos_categoria (1) — (N) produtos_categoria [hierarquia recursiva]'),

    // 2.6
    sp(200),
    h2('2.6 Tecnologias e Decisões Técnicas'),
    p('PostgreSQL 16 escolhido por: UUID nativo (gen_random_uuid()), TIMESTAMPTZ com timezone, índices funcionais LOWER() para unicidade case-insensitive, ACID compliance e licença open-source.'),
    p('Dapper escolhido em detrimento do EF Core: maior controle sobre SQL, performance superior em queries com JOINs complexos, debugging facilitado (SQL visível no código).'),

    // 2.7
    sp(200),
    h2('2.7 Conexão dos Microsserviços com o Banco de Dados'),
    p('Hierarquia de conexão: NpgsqlDataSource (Singleton) → IDbConnectionFactory (Scoped) → NpgsqlConnection → Dapper → PostgreSQL :5432.'),
    p('Resolução da connection string (prioridade): (1) variáveis de ambiente POSTGRESQL_* ; (2) ConnectionStrings:DefaultConnection no appsettings. O parâmetro Search Path=portal_b2b define o schema padrão, eliminando prefixo schema.tabela no SQL.'),

    pageBreak()
  ];
}

function sec3(imgs) {
  return [
    h1('3. ARQUITETURA E INFRAESTRUTURA'),
    divider(),

    // 3.1
    h2('3.1 Desenho da Arquitetura Atual'),
    // DIAGRAMA 4 — Arquitetura Geral
    sp(80),
    ...imgParagraph(imgs.arquitetura, 900, 700, 'Figura 4 — Arquitetura Geral: Portal B2B (Docker Network)'),
    // DIAGRAMA 5 — Deployment
    ...imgParagraph(imgs.deployment, 800, 600, 'Figura 5 — Diagrama de Implantação (Docker Compose)'),
    tbl(
      ['Componente', 'Tecnologia', 'Porta', 'Responsabilidade'],
      [
        ['produtos-front', 'React + Nginx', ':8081', 'Interface do usuário (SPA)'],
        ['produtos-service', 'ASP.NET Core 10', ':5002', 'API REST do catálogo de produtos'],
        ['auth-service', 'Equipe de Auth', ':5001', 'Emissão e validação de JWT'],
        ['postgres', 'PostgreSQL 16', ':5432', 'Banco relacional compartilhado'],
        ['redpanda', 'Redpanda (Kafka)', ':9092', 'Message broker para eventos'],
        ['portal-b2b-network', 'Docker bridge', '—', 'Rede interna de todos os serviços'],
      ],
      c4(0.20, 0.22, 0.10, 0.48)
    ),

    // 3.2
    sp(200),
    h2('3.2 Requisitos Funcionais'),
    tbl(
      ['Código', 'Requisito'],
      [
        ['RF-ARQ-01', 'Microsserviços independentes com responsabilidade de domínio única'],
        ['RF-ARQ-02', 'Cada microsserviço expõe API REST com documentação OpenAPI'],
        ['RF-ARQ-03', 'Comunicação entre serviços via eventos assíncronos (Kafka)'],
        ['RF-ARQ-04', 'Autenticação centralizada e stateless (JWT)'],
        ['RF-ARQ-05', 'Health check exposto em cada microsserviço'],
        ['RF-ARQ-06', 'Frontend em container dedicado, separado do backend'],
        ['RF-ARQ-07', 'Ambiente inicializável com docker compose up -d --build'],
      ],
      c2(0.18, 0.82)
    ),

    // 3.3
    sp(200),
    h2('3.3 Requisitos Não Funcionais'),
    h3('Docker & Docker Compose'),
    p('Multi-stage builds: backend compila em sdk:10.0 (~800MB) e executa em aspnet:10.0 (~200MB); frontend compila em node:20-alpine e serve via nginx:1.27-alpine (~50MB). Restart policy unless-stopped garante reinicialização automática.'),
    h3('Apache Kafka (Redpanda)'),
    p('Redpanda é implementação Kafka em C++ sem JVM: menor latência, sem ZooKeeper, 100% compatível com clientes Kafka. Produção com acks=all. KAFKA_FAIL_ON_PUBLISH_ERROR=false impede que falha no broker derrube a operação HTTP.'),
    // DIAGRAMA 6 — Kafka
    sp(80),
    ...imgParagraph(imgs.kafka, 800, 700, 'Figura 6 — Fluxo de Eventos Kafka: Tópicos publicados pelo produtos-service'),
    h3('PostgreSQL 16'),
    bullet('WAL (Write-Ahead Logging): durabilidade e suporte a replicação'),
    bullet('MVCC: leituras não bloqueiam escritas'),
    bullet('Índices funcionais LOWER(): unicidade case-insensitive sem duplicar dados'),
    bullet('PgBouncer recomendado em produção de alta escala como connection pooler externo'),
    h3('Nginx'),
    p('Serve arquivos estáticos do React. Configuração try_files $uri /index.html garante funcionamento de rotas do React Router no reload da página. Pode atuar como reverse proxy com TLS termination.'),
    h3('JWT Bearer Authentication'),
    p('Validação stateless: (1) Bearer token → (2) JwtBearerAuthentication valida assinatura com SecretKey compartilhada → (3) Valida issuer/audience/expiração → (4) Claims injetadas no HttpContext.User → (5) ICurrentUserService extrai userId para auditoria.'),

    // 3.4
    sp(200),
    h2('3.4 Manual de Implementação'),
    pBold('1. Clonar:'), code('git clone <url> SDI.Micro.Produto && cd SDI.Micro.Produto'),
    pBold('2. Rede Docker (uma vez, compartilhada por todos os microsserviços):'), code('docker network create portal-b2b-network'),
    pBold('3. Variáveis de ambiente:'), code('cp .env.example .env  # editar POSTGRESQL_*, Jwt__SecretKey, KAFKA_BOOTSTRAP_SERVERS'),
    pBold('4. Banco de dados:'), code('docker exec -i postgres psql -U postgres -d portal_b2b < Scripts/ScriptDeCriacao.sql'),
    pBold('5. Build e execução:'), code('docker compose up -d --build'),
    pBold('6. Verificação:'), code('curl http://localhost:5002/saude  # OK = {"statusHttp":200,"mensagem":"Healthy"}'),

    // 3.5
    sp(200),
    h2('3.5 Manual de Integração dos Microsserviços'),
    h3('Integração via JWT'),
    p('A SecretKey deve ser idêntica em todos os microsserviços (variável Jwt__SecretKey). O auth-service é o único emissor; os demais apenas validam. Mesmo issuer (portal-autenticacao) e audience (portal-b2b) obrigatórios.'),
    h3('Eventos Kafka Publicados'),
    tbl(
      ['Evento (Tópico)', 'Gatilho'],
      [
        ['produto_cadastrado', 'POST /produtos'],
        ['produto_atualizado', 'PUT /produtos/{id}'],
        ['produto_status_alterado', 'PATCH /produtos/{id}/ativar ou /inativar'],
        ['categoria_cadastrada / atualizada / status', 'POST/PUT/PATCH em /categorias'],
        ['transporte_cadastrado / atualizado / status', 'POST/PUT/PATCH em /transportes'],
        ['unidade_medida_cadastrada / atualizada / status', 'POST/PUT/PATCH em /unidades-medida'],
      ],
      c2(0.45, 0.55)
    ),
    sp(80),
    p('Proxy sugerido: /api/produtos/* → produtos-service:5002 | /produtos/* → produtos-front:80'),

    // 3.6
    sp(200),
    h2('3.6 Jornada do Usuário (Macro)'),
    p('Visão de alto nível atravessando todo o sistema sem entrar nos detalhes internos dos microsserviços.'),
    // DIAGRAMA 7 — Jornada Macro
    sp(80),
    ...imgParagraph(imgs.macro, 700, 900, 'Figura 7 — Jornada Macro do Usuário: visão completa do sistema'),

    // 3.7
    sp(200),
    h2('3.7 Decisões Técnicas'),
    tbl(
      ['Decisão', 'Justificativa'],
      [
        ['Microsserviços vs. Monolito', 'Equipes independentes por domínio, deploy independente, escalabilidade granular.'],
        ['ASP.NET Core 10', 'Alta performance, DI e JWT nativos, familiaridade da equipe com C#.'],
        ['Dapper vs. EF Core', 'Controle sobre SQL, performance em JOINs complexos, debugging facilitado.'],
        ['Kafka vs. REST síncrono', 'Desacoplamento temporal, múltiplos consumidores, histórico para replay.'],
        ['Schema compartilhado vs. BD por serviço', 'Simplicidade operacional adequada ao contexto acadêmico.'],
        ['JWT stateless vs. sessions', 'Sem Redis para sessões; token carrega todas as informações necessárias.'],
        ['Soft Delete vs. Delete físico', 'Preserva integridade referencial e histórico para auditoria.'],
        ['React + Vite vs. Next.js', 'SSR não agrega valor para dashboard autenticado; SPA adequada ao caso de uso.'],
        ['Redpanda vs. Kafka original', 'C++ sem JVM: menor latência, sem ZooKeeper, 100% compatível com API Kafka.'],
      ],
      c2(0.30, 0.70)
    ),

    sp(400),
    divider(),
    new Paragraph({
      alignment: AlignmentType.CENTER, spacing: { before: 200 },
      children: [new TextRun({ text: 'Relatório Técnico — SDI.Micro.Produto — Junho de 2026', font: 'Arial', size: 18, color: '888888', italics: true })]
    })
  ];
}

// ───────────────────────────────────────────
// Main
// ───────────────────────────────────────────
async function main() {
  const diagDir = path.join(__dirname, 'diagramas');

  console.log('Baixando diagramas PlantUML...');
  const imgs = {
    arquitetura: await loadDiagram(path.join(diagDir, '01-arquitetura-geral.puml')),
    er:          await loadDiagram(path.join(diagDir, '02-modelo-dados-er.puml')),
    jornada:     await loadDiagram(path.join(diagDir, '03-jornada-usuario.puml')),
    componentes: await loadDiagram(path.join(diagDir, '04-componentes-microservico.puml')),
    kafka:       await loadDiagram(path.join(diagDir, '05-fluxo-kafka.puml')),
    deployment:  await loadDiagram(path.join(diagDir, '06-deployment.puml')),
    macro:       await loadDiagram(path.join(diagDir, '07-jornada-macro.puml')),
  };
  console.log('Todos os diagramas baixados.\n');

  const doc = new Document({
    numbering: {
      config: [{
        reference: 'bullets',
        levels: [{ level: 0, format: LevelFormat.BULLET, text: '•', alignment: AlignmentType.LEFT,
          style: { paragraph: { indent: { left: 480, hanging: 240 } } } }]
      }]
    },
    styles: {
      default: { document: { run: { font: 'Arial', size: 22 } } },
      paragraphStyles: [
        { id: 'Heading1', name: 'Heading 1', basedOn: 'Normal', next: 'Normal', quickFormat: true,
          run: { size: 32, bold: true, font: 'Arial', color: BLUE },
          paragraph: { spacing: { before: 360, after: 120 }, outlineLevel: 0 } },
        { id: 'Heading2', name: 'Heading 2', basedOn: 'Normal', next: 'Normal', quickFormat: true,
          run: { size: 26, bold: true, font: 'Arial', color: BLUE_MID },
          paragraph: { spacing: { before: 280, after: 80 }, outlineLevel: 1 } },
        { id: 'Heading3', name: 'Heading 3', basedOn: 'Normal', next: 'Normal', quickFormat: true,
          run: { size: 24, bold: true, font: 'Arial', color: BLUE_MID },
          paragraph: { spacing: { before: 200, after: 60 }, outlineLevel: 2 } }
      ]
    },
    sections: [{
      properties: {
        page: {
          size: { width: PAGE_W, height: 16838 },
          margin: { top: MARGIN, right: MARGIN, bottom: MARGIN, left: MARGIN }
        }
      },
      headers: {
        default: new Header({
          children: [new Paragraph({
            border: { bottom: { style: BorderStyle.SINGLE, size: 4, color: BLUE_MID, space: 4 } },
            tabStops: [{ type: 'right', position: CONTENT_W }],
            children: [
              new TextRun({ text: 'Relatório Técnico — Portal B2B — SDI.Micro.Produto', font: 'Arial', size: 18, color: '555555' }),
              new TextRun({ text: '\t', font: 'Arial' }),
              new TextRun({ children: [PageNumber.CURRENT], font: 'Arial', size: 18, color: '555555' })
            ]
          })]
        })
      },
      children: [
        ...coverPage(),
        ...sec1(imgs),
        ...sec2(imgs),
        ...sec3(imgs)
      ]
    }]
  });

  const buf = await Packer.toBuffer(doc);
  const out = path.join(__dirname, 'relatorio-tecnico.docx');
  fs.writeFileSync(out, buf);
  console.log(`\nDocumento gerado: ${out} (${(buf.length/1024).toFixed(1)} KB)`);
}

main().catch(e => { console.error(e); process.exit(1); });
