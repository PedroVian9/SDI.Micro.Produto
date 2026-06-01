# Relatório Técnico — Microsserviço de Produtos
## Portal B2B — SDI.Micro.Produto

**Instituição:** [Nome da Instituição]  
**Disciplina:** Sistemas Distribuídos e Integração  
**Professor:** [Nome do Professor]  
**Equipe:** Pedro Viana  
**Data:** Junho de 2026  
**Repositório:** `SDI.Micro.Produto`

---

## Sumário

1. [Microsserviços (incluindo autenticação)](#1-microsserviços)
   1. [Requisitos de Negócio](#11-requisitos-de-negócio)
   2. [Jornada do Usuário](#12-jornada-do-usuário)
   3. [Protótipo (UX)](#13-protótipo-ux)
   4. [Requisitos Funcionais](#14-requisitos-funcionais)
   5. [Requisitos Não Funcionais](#15-requisitos-não-funcionais)
   6. [Tecnologias Utilizadas](#16-tecnologias-utilizadas)
   7. [Considerações Gerais](#17-considerações-gerais)
   8. [Instruções de Implantação](#18-instruções-de-implantação)
2. [Banco de Dados](#2-banco-de-dados)
   1. [Modelo de Dados Geral](#21-modelo-de-dados-geral)
   2. [Padrões de Nomenclatura](#22-padrões-de-nomenclatura)
   3. [Dicionário de Dados](#23-dicionário-de-dados)
   4. [Requisitos Não Funcionais (BD)](#24-requisitos-não-funcionais)
   5. [Modelo por Microsserviço](#25-modelo-por-microsserviço)
   6. [Tecnologias e Decisões Técnicas](#26-tecnologias-e-decisões-técnicas)
   7. [Conexão dos Microsserviços com o BD](#27-conexão-dos-microsserviços-com-o-banco-de-dados)
3. [Arquitetura e Infraestrutura](#3-arquitetura-e-infraestrutura)
   1. [Desenho da Arquitetura](#31-desenho-da-arquitetura-atual)
   2. [Requisitos Funcionais](#32-requisitos-funcionais)
   3. [Requisitos Não Funcionais](#33-requisitos-não-funcionais)
   4. [Manual de Implementação](#34-manual-de-implementação)
   5. [Manual de Integração](#35-manual-de-integração-dos-microsserviços)
   6. [Jornada do Usuário (Macro)](#36-jornada-do-usuário-macro)
   7. [Decisões Técnicas](#37-decisões-técnicas)

---

# 1. Microsserviços

## 1.1 Requisitos de Negócio

### Contexto do Domínio

O Portal B2B é uma plataforma de comércio eletrônico voltada à transação entre empresas (Business-to-Business). Nesse contexto, fornecedores disponibilizam seus catálogos de produtos para compradores empresariais, que realizam pedidos com base nesses catálogos.

O **microsserviço de Produtos** (`SDI.Micro.Produto`) é responsável pelo **domínio de catálogo de produtos**: a fonte de verdade centralizada para todas as informações descritivas de um produto comercializado na plataforma. Ele não armazena preços nem quantidades em estoque — essas responsabilidades pertencem, respectivamente, ao domínio de Fornecedores e ao domínio de Inventário, seguindo o princípio de Bounded Context do Domain-Driven Design (DDD).

### Problemas de Negócio Resolvidos

| Problema | Solução pelo Microsserviço |
|---|---|
| Ausência de um catálogo centralizado e padronizado de produtos | Cadastro único de produtos com código e nome únicos em todo o sistema |
| Necessidade de classificar produtos por finalidade | Gerenciamento hierárquico de categorias com N níveis de profundidade |
| Diferentes métodos de transporte para tipos de produto | Cadastro de modalidades de transporte associadas ao produto |
| Inconsistência de unidades de medida entre fornecedores | Registro padronizado de unidades de medida com sigla normalizada (UPPERCASE) |
| Propagação de mudanças no catálogo para outros serviços | Publicação de eventos via Kafka para sincronização assíncrona |

### Stakeholders

- **Gestor de Produtos:** usuário interno responsável por manter o catálogo atualizado, cadastrar categorias, transportes e unidades de medida.
- **Fornecedor:** consulta o catálogo para associar seus preços aos produtos cadastrados.
- **Comprador:** navega pelo catálogo para realizar pedidos.
- **Outros Microsserviços:** consomem eventos para sincronizar dados (ex.: Pedidos, Fornecedores, Analytics).

### Regras de Negócio Centrais

1. **Unicidade:** nome de produto, código de produto, nome de categoria, nome de transporte, nome e sigla de unidade de medida são únicos no sistema (case-insensitive).
2. **Catálogo puro:** o microsserviço de produtos não armazena preço nem quantidade — esses dados pertencem a outros domínios.
3. **Soft Delete:** produtos não são excluídos fisicamente. O campo `ativo` controla a disponibilidade, preservando integridade referencial e histórico.
4. **Hierarquia de Categorias:** uma categoria pode ter subcategorias de forma recursiva, sem limite de profundidade.
5. **Rastreabilidade de Auditoria:** todo registro possui campos de auditoria (`data_cadastro`, `usuario_cadastro`, `ultima_alteracao`, `usuario_alteracao`) populados automaticamente.
6. **Propagação via Eventos:** qualquer alteração no catálogo gera um evento Kafka padronizado, consumido pelos serviços dependentes de forma assíncrona.

---

## 1.2 Jornada do Usuário

A jornada completa do usuário gestor segue o fluxo abaixo. O diagrama de sequência completo está em `diagramas/03-jornada-usuario.puml`.

```plantuml
@startuml
' Veja o arquivo: docs/diagramas/03-jornada-usuario.puml
' (resumo abaixo)
@enduml
```

> **Diagrama completo:** `docs/diagramas/03-jornada-usuario.puml`

### Etapas da Jornada

#### Etapa 1 — Autenticação
O usuário acessa o portal e faz login. O serviço de autenticação valida as credenciais e emite um JWT Token com as claims do usuário (incluindo o `userId`). O frontend armazena o token no state manager (Zustand) e o envia em toda requisição subsequente via header `Authorization: Bearer <token>`.

#### Etapa 2 — Configuração de Pré-requisitos
Antes de cadastrar um produto, o gestor precisa garantir que os dados de suporte existam:

1. **Transportes:** Cadastra as modalidades de transporte disponíveis (ex.: "Frete Aéreo", "Frete Rodoviário").
2. **Categorias:** Cria a estrutura hierárquica de categorias (ex.: Eletrônicos > Celulares > Smartphones).
3. **Unidades de Medida:** Registra as unidades utilizadas (ex.: "Quilograma" com sigla "KG", "Unidade" com sigla "UN").

#### Etapa 3 — Cadastro do Produto
Com os dados de suporte criados, o gestor:
1. Navega até "Produtos > Novo".
2. Preenche o formulário: código único, nome, descrição, seleciona categoria, transporte e unidade de medida.
3. O frontend valida os campos via Zod antes de enviar.
4. A API valida, persiste no banco e publica o evento `produto_cadastrado` no Kafka.
5. Confirmação exibida ao usuário.

#### Etapa 4 — Consulta e Filtros
O usuário pode listar produtos com filtros por nome (busca textual), categoria, transporte, unidade de medida e status (ativo/inativo). A listagem é paginada (padrão 20 itens por página).

#### Etapa 5 — Edição
O usuário seleciona um produto, edita os campos desejados e salva. O evento `produto_atualizado` é publicado.

#### Etapa 6 — Inativação
O produto é inativado (soft delete via `PATCH /produtos/{id}/inativar`). O evento `produto_status_alterado` é publicado, permitindo que outros serviços atualizem seus estados.

---

## 1.3 Protótipo (UX)

O frontend foi desenvolvido em React com TypeScript e Tailwind CSS, seguindo os padrões do shadcn/ui. As principais telas são:

### Tela 1 — Login
**Título:** Autenticação no Portal B2B  
**Descrição:** Formulário com campos de e-mail e senha. Ao submeter, o sistema autentica via microsserviço de auth e redireciona para o dashboard. Exibe mensagens de erro claras em caso de credenciais inválidas.

### Tela 2 — Dashboard / Lista de Produtos
**Título:** Catálogo de Produtos  
**Descrição:** Tabela paginada exibindo código, nome, categoria, transporte, unidade de medida e status (ativo/inativo) de cada produto. Inclui barra de busca textual, filtros laterais por categoria e status, e botões de ação (Editar, Inativar/Ativar).

### Tela 3 — Formulário de Produto (Criar/Editar)
**Título:** Novo Produto / Editar Produto  
**Descrição:** Formulário com campos: código (obrigatório, único), nome (obrigatório), descrição (opcional), seleção de categoria (dropdown com busca), seleção de transporte, seleção de unidade de medida. Validação em tempo real com React Hook Form + Zod. Feedback visual de campos inválidos.

### Tela 4 — Gestão de Categorias
**Título:** Categorias de Produtos  
**Descrição:** Lista hierárquica de categorias com suporte a subcategorias. Permite criar, editar e inativar categorias. Exibe a hierarquia pai → filho de forma visual.

### Tela 5 — Gestão de Transportes
**Título:** Tipos de Transporte  
**Descrição:** Listagem das modalidades de transporte disponíveis para associação aos produtos. CRUD completo com ativação/inativação.

### Tela 6 — Gestão de Unidades de Medida
**Título:** Unidades de Medida  
**Descrição:** Gerenciamento das unidades de medida com nome completo e sigla. A sigla é automaticamente convertida para UPPERCASE. CRUD completo com ativação/inativação.

---

## 1.4 Requisitos Funcionais

### RF-01 — Gerenciamento de Produtos
| Código | Requisito |
|---|---|
| RF-01.1 | O sistema deve permitir o cadastro de produtos com código único (UPPERCASE), nome, descrição, categoria, transporte e unidade de medida |
| RF-01.2 | O sistema deve permitir a listagem paginada de produtos com filtros por busca textual, categoria, transporte, unidade de medida e status |
| RF-01.3 | O sistema deve permitir a consulta de um produto específico por ID |
| RF-01.4 | O sistema deve permitir a atualização completa dos dados de um produto |
| RF-01.5 | O sistema deve permitir a ativação e inativação de produtos (soft delete) |
| RF-01.6 | O sistema deve publicar eventos Kafka em toda operação de criação, atualização e mudança de status |

### RF-02 — Gerenciamento de Categorias
| Código | Requisito |
|---|---|
| RF-02.1 | O sistema deve suportar hierarquia de categorias com N níveis via auto-referência |
| RF-02.2 | O nome de categoria deve ser único em todo o sistema (case-insensitive) |
| RF-02.3 | O sistema deve permitir filtrar categorias por categoria-pai |
| RF-02.4 | CRUD completo com ativação/inativação |

### RF-03 — Gerenciamento de Transportes
| Código | Requisito |
|---|---|
| RF-03.1 | O sistema deve permitir o cadastro de modalidades de transporte com nome único |
| RF-03.2 | CRUD completo com ativação/inativação |
| RF-03.3 | Não é possível excluir um transporte associado a um produto ativo |

### RF-04 — Gerenciamento de Unidades de Medida
| Código | Requisito |
|---|---|
| RF-04.1 | O sistema deve cadastrar unidades com nome e sigla, sendo a sigla normalizada para UPPERCASE |
| RF-04.2 | Nome e sigla devem ser únicos em todo o sistema (case-insensitive) |
| RF-04.3 | CRUD completo com ativação/inativação |

### RF-05 — Autenticação e Autorização
| Código | Requisito |
|---|---|
| RF-05.1 | Todas as rotas (exceto `/saude`) exigem autenticação via JWT Bearer |
| RF-05.2 | O token JWT é emitido pelo microsserviço de autenticação (issuer: `portal-autenticacao`) |
| RF-05.3 | O `userId` extraído do JWT é registrado nas operações de auditoria |

### RF-06 — Health Check
| Código | Requisito |
|---|---|
| RF-06.1 | O endpoint `GET /saude` deve validar a saúde da API e a conectividade com o PostgreSQL |
| RF-06.2 | Retorna HTTP 200 quando saudável e HTTP 503 quando indisponível |

### RF-07 — Formato de Resposta Padronizado
Todas as respostas da API seguem o envelope:

```json
{
  "statusHttp": 200,
  "mensagem": "Operacao realizada com sucesso.",
  "resultado": { },
  "erros": []
}
```

Respostas paginadas incluem metadados:

```json
{
  "resultado": {
    "itens": [],
    "pagina": 1,
    "tamanhoPagina": 20,
    "total": 150
  }
}
```

---

## 1.5 Requisitos Não Funcionais

### RNF-01 — Desempenho
| Código | Requisito |
|---|---|
| RNF-01.1 | As queries ao banco de dados devem utilizar índices otimizados para garantir latência < 100ms em operações de leitura |
| RNF-01.2 | O pool de conexões com o PostgreSQL deve suportar até 100 conexões simultâneas |
| RNF-01.3 | Paginação obrigatória em todas as listagens, com máximo de 100 itens por página |

### RNF-02 — Segurança
| Código | Requisito |
|---|---|
| RNF-02.1 | Autenticação JWT Bearer com validação de issuer, audience, expiração e clock skew |
| RNF-02.2 | Chave secreta JWT compartilhada entre todos os microsserviços via variável de ambiente |
| RNF-02.3 | Prevenção de SQL Injection via queries parametrizadas (Dapper) |
| RNF-02.4 | HTTPS habilitado em ambiente de produção |

### RNF-03 — Disponibilidade
| Código | Requisito |
|---|---|
| RNF-03.1 | Política de restart `unless-stopped` no Docker Compose |
| RNF-03.2 | Health check exposto para monitoramento externo (orquestradores, load balancers) |
| RNF-03.3 | Falha na publicação de evento Kafka não deve falhar a operação HTTP (KAFKA_FAIL_ON_PUBLISH_ERROR=false) |

### RNF-04 — Observabilidade
| Código | Requisito |
|---|---|
| RNF-04.1 | Logging estruturado via Serilog (JSON) em todos os níveis |
| RNF-04.2 | Rastreabilidade de operações via `correlationId` nos eventos Kafka |
| RNF-04.3 | Auditoria completa: `data_cadastro`, `usuario_cadastro`, `ultima_alteracao`, `usuario_alteracao` |

### RNF-05 — Tecnologias NÃO Utilizadas e Justificativas

| Tecnologia | Motivo da Não Utilização |
|---|---|
| Entity Framework Core | Substituído por **Dapper** para maior controle sobre SQL e melhor performance em queries complexas com múltiplos JOINs e paginação |
| Redis (Cache) | Não justificado nesta fase dado o volume de dados e a necessidade de dados sempre atualizados no catálogo |
| gRPC | Não utilizado pois a comunicação entre serviços é feita de forma assíncrona via Kafka, e a comunicação frontend↔backend é REST, adequada para o contexto |
| RabbitMQ | Substituído por **Apache Kafka (Redpanda)** pela equipe de infraestrutura, para consistência com os demais serviços do sistema |
| Oracle / SQL Server | Substituído por **PostgreSQL** por ser open-source, ter melhor suporte a UUIDs, e ser a tecnologia de banco definida pela equipe de infraestrutura |
| Autenticação própria | Delegada ao **microsserviço de autenticação** da equipe de auth, consumindo apenas a validação de JWT |

---

## 1.6 Tecnologias Utilizadas

### Backend — ASP.NET Core 10

**ASP.NET Core 10.0** é o framework principal para construção da API REST. Escolhido por:
- Suporte nativo a Dependency Injection, Middleware Pipeline, e JWT Bearer Authentication.
- Alta performance (um dos frameworks web mais rápidos em benchmarks TechEmpower).
- Ecossistema maduro com suporte de longo prazo (LTS).

**Dapper 2.1.66** — Micro-ORM para mapeamento objeto-relacional:
- Executa SQL puro com mapeamento automático de resultados para POCOs (via `DefaultTypeMap.MatchNamesWithUnderscores`).
- Suporte a `QueryMultiple` para buscar dados paginados e contagem total em uma única round-trip ao banco.
- Queries parametrizadas eliminam risco de SQL Injection.

**Confluent.Kafka 2.8.0** — Cliente Kafka oficial:
- Publica eventos de integração de forma assíncrona.
- Configuração de `acks`, `timeout` e tratamento de erros desacoplado da resposta HTTP.

**Npgsql 9.0.3** — Driver PostgreSQL para .NET:
- Connection pooling nativo com suporte a `NpgsqlDataSource`.
- Suporte a tipos nativos do PostgreSQL (UUID, TIMESTAMPTZ, etc.).

**Serilog 8.0.1** — Logging estruturado:
- Saída em JSON para integração com ferramentas de observabilidade (ELK Stack, Grafana Loki).
- Enriquecimento automático com timestamp, nível e contexto.

**Swashbuckle (Swagger) 6.6.2** — Documentação da API:
- Geração automática de documentação OpenAPI 3.0 a partir das anotações dos controllers.
- Interface Swagger UI disponível em `/swagger` em ambiente de desenvolvimento.

### Frontend — React + Vite

**React 18.3.1** — Biblioteca de componentes declarativos:
- Hooks modernos (`useState`, `useEffect`, `useCallback`) para gerenciamento de estado local.
- Composição de componentes reutilizáveis com Radix UI.

**Vite 5.4.19** — Build tool ultra-rápida:
- HMR (Hot Module Replacement) para desenvolvimento ágil.
- Build production otimizada com tree-shaking e code-splitting.
- Proxy reverso configurado para redirecionar `/api` ao backend durante o desenvolvimento.

**TypeScript 5.8** — Tipagem estática:
- Tipos compartilhados entre componentes e chamadas de API.
- Detecção de erros em tempo de compilação.

**Tailwind CSS 3.4** — Framework CSS utilitário:
- Estilização sem sair do HTML/JSX.
- Purge automático de classes não utilizadas no build de produção.

**Radix UI + shadcn/ui** — Componentes acessíveis:
- Primitivos headless sem estilo padrão, totalmente customizáveis com Tailwind.
- Acessibilidade (WAI-ARIA) embutida nos componentes.

**TanStack React Query 5.83** — Gerenciamento de cache de dados remotos:
- Cache automático com revalidação, retry e stale-while-revalidate.
- Mutations com invalidação automática de cache.

**Zustand 5.0.8** — Gerenciamento de estado global:
- Store simples para tokens JWT e dados do usuário autenticado.
- Sem boilerplate excessivo (sem Redux).

**React Hook Form 7.61.1 + Zod** — Formulários e validação:
- Validação de schema declarativa com mensagens de erro em português.
- Integração com componentes Radix UI.

**Axios 1.12.2** — Cliente HTTP:
- Interceptors configurados para injetar o Bearer Token automaticamente.
- Tratamento centralizado de erros (401 → redirect ao login).

### DevOps e Infraestrutura

**Docker** — Containerização:
- Multi-stage build para imagens enxutas (apenas artefatos de produção).
- Backend: `mcr.microsoft.com/dotnet/aspnet:10.0` (~200MB).
- Frontend: `nginx:1.27-alpine` (~50MB) servindo os arquivos estáticos.

**Docker Compose** — Orquestração local:
- Define todos os serviços, redes e dependências do sistema.
- Rede `portal-b2b-network` compartilhada entre todos os microsserviços.

---

## 1.7 Considerações Gerais

### Arquitetura do Microsserviço

O `produtos-service` segue uma **arquitetura em camadas** com separação clara de responsabilidades:

```
Controller (HTTP binding e roteamento)
    ↓
Service (lógica de negócio, validações, orquestração)
    ↓
Repository (acesso a dados via Dapper/SQL)
    ↓
PostgreSQL (persistência)
    ↓
Kafka (eventos de integração, assíncrono)
```

Essa separação garante testabilidade (cada camada pode ser testada isoladamente), coesão (cada classe tem uma única responsabilidade) e facilidade de manutenção.

### Bounded Context

O serviço opera dentro do **Bounded Context de Catálogo**, deliberadamente excluindo:
- Preços (domínio Fornecedor)
- Estoque/quantidade (domínio Inventário)
- Pedidos (domínio Pedidos)

Isso evita acoplamento e garante que mudanças em outros domínios não impactem o catálogo.

### Integração Assíncrona

Toda operação de escrita publica um evento no Kafka. Os consumidores (outros microsserviços) processam esses eventos em seu próprio ritmo, sem acoplamento síncrono com o serviço de produtos. Isso garece:
- **Resiliência:** falha em um consumidor não afeta o produtor.
- **Escalabilidade:** consumidores podem ser escalados independentemente.
- **Auditabilidade:** histórico completo de eventos persistido no Kafka.

### Tratamento de Erros

O `GlobalExceptionMiddleware` intercepta todas as exceções e as converte em respostas HTTP padronizadas:
- `DomainException` → HTTP 400 com mensagem de negócio
- `PostgresException (23505)` → HTTP 409 Conflict (violação de unicidade)
- `PostgresException (23503)` → HTTP 400 Bad Request (FK violada)
- Exceções não tratadas → HTTP 500 com log completo

---

## 1.8 Instruções de Implantação

### Pré-requisitos

- Docker Engine 24+ e Docker Compose 2+
- Rede Docker `portal-b2b-network` criada pela equipe de infraestrutura
- PostgreSQL 16 acessível na rede (container `postgres` ou externo)
- Redpanda/Kafka acessível na rede (container `redpanda:9092`)
- Microsserviço de autenticação (`auth-service`) em execução com a mesma `SecretKey` JWT

### Passo 1 — Criar a rede (se não existir)

```bash
docker network create portal-b2b-network
```

### Passo 2 — Configurar variáveis de ambiente

Crie um arquivo `.env` na raiz do projeto:

```dotenv
# Banco de dados
POSTGRESQL_HOST=postgres
POSTGRESQL_PORT=5432
POSTGRESQL_DATABASE=portal_b2b
POSTGRESQL_USER=seu_usuario
POSTGRESQL_PASSWORD=sua_senha
DB_SCHEMA=portal_b2b

# JWT (deve ser a mesma chave do auth-service)
Jwt__Issuer=portal-autenticacao
Jwt__Audience=portal-b2b
Jwt__SecretKey=SUA_CHAVE_SECRETA_COMPARTILHADA
Jwt__ClockSkewSeconds=60

# Kafka
KAFKA_BOOTSTRAP_SERVERS=redpanda:9092
KAFKA_ENABLED=true
KAFKA_FAIL_ON_PUBLISH_ERROR=false

# Frontend
FRONTEND_PORT=8081
VITE_BASE_PATH=/produtos/
VITE_PRODUTO_API_URL=/api/produtos
```

### Passo 3 — Executar o script de banco de dados

```bash
# Conecte ao PostgreSQL e execute o script de criação
psql -h localhost -U seu_usuario -d portal_b2b -f Scripts/ScriptDeCriacao.sql
```

O script cria:
- Schema `portal_b2b`
- Tabelas: `produtos_transporte`, `produtos_categoria`, `produtos_unidade_medida`, `produtos_produto`
- Índices de performance e unicidade
- Triggers de auditoria (`ultima_alteracao`)
- Permissões necessárias

### Passo 4 — Subir os containers

```bash
docker compose up -d --build
```

### Passo 5 — Verificar a saúde do serviço

```bash
curl http://localhost:5002/saude
# Resposta esperada: {"statusHttp": 200, "mensagem": "Healthy"}
```

### Passo 6 — Acessar o Swagger (desenvolvimento)

```
http://localhost:5002/swagger
```

### Integração com o Gateway

Configure o gateway/proxy para rotear:
- `/api/produtos/*` → `http://produtos-service:5002`
- `/produtos/*` → `http://produtos-front:80`

---

# 2. Banco de Dados

## 2.1 Modelo de Dados Geral

O sistema utiliza um banco de dados PostgreSQL compartilhado (`portal_b2b`), com isolamento por schemas. O microsserviço de Produtos utiliza o schema `portal_b2b`.

> **Diagrama ER completo:** `docs/diagramas/02-modelo-dados-er.puml`

```
┌──────────────────────────────────────────────────────────┐
│                    Schema: portal_b2b                    │
│                                                          │
│  produtos_transporte   produtos_categoria (hierárquica)  │
│         │                      │                         │
│         └──────────┬───────────┘                         │
│                    │                                     │
│           produtos_produto                               │
│                    │                                     │
│                    └── produtos_unidade_medida            │
└──────────────────────────────────────────────────────────┘
```

### Relacionamentos

| Tabela Origem | Coluna | Tabela Destino | Tipo |
|---|---|---|---|
| `produtos_produto` | `transporte_id` | `produtos_transporte.id` | N:1 |
| `produtos_produto` | `categoria_id` | `produtos_categoria.id` | N:1 |
| `produtos_produto` | `unidade_medida_id` | `produtos_unidade_medida.id` | N:1 |
| `produtos_categoria` | `categoria_pai_id` | `produtos_categoria.id` | N:1 (self) |

---

## 2.2 Padrões de Nomenclatura

### Convenção Adotada: `snake_case`

Todos os campos, tabelas e índices do banco de dados seguem o padrão **`snake_case`** (minúsculas com underscores separando palavras). Esse padrão foi definido pela equipe de infraestrutura e repassado a todas as equipes de microsserviços.

**Razão técnica:** PostgreSQL é case-insensitive por padrão e converte identificadores para minúsculas. O uso de snake_case evita a necessidade de aspas duplas no SQL e é consistente com convenções da comunidade PostgreSQL.

**Mapeamento no .NET:** O Dapper foi configurado com `DefaultTypeMap.MatchNamesWithUnderscores = true` para mapear automaticamente colunas `snake_case` para propriedades `PascalCase` das entidades C#, sem necessidade de mapeamento manual.

```csharp
// Configuração no Program.cs
DefaultTypeMap.MatchNamesWithUnderscores = true;
```

### Padrões de Prefixo de Tabelas

Todas as tabelas do microsserviço de produtos são prefixadas com `produtos_`, facilitando a identificação do contexto:

| Prefixo | Contexto |
|---|---|
| `produtos_` | Microsserviço de Produtos |
| `auth_` | Microsserviço de Autenticação |
| `fornecedores_` | Microsserviço de Fornecedores |

### Padrões de Nomeação de Campos

| Padrão | Exemplo | Descrição |
|---|---|---|
| Chave primária | `id` | UUID gerado automaticamente |
| Chave estrangeira | `categoria_pai_id` | `{tabela_referenciada}_id` |
| Campo de status | `ativo` | Booleano para soft delete |
| Data de criação | `data_cadastro` | TIMESTAMPTZ com `DEFAULT NOW()` |
| Usuário criador | `usuario_cadastro` | UUID do usuário autenticado |
| Data de atualização | `ultima_alteracao` | TIMESTAMPTZ atualizado por trigger |
| Usuário atualizador | `usuario_alteracao` | UUID do usuário autenticado |

---

## 2.3 Dicionário de Dados

### Tabela: `produtos_transporte`

**Contexto:** Modalidades de transporte que podem ser associadas a produtos.

| Campo | Tipo | Null | Descrição |
|---|---|---|---|
| `id` | UUID | NOT NULL | Identificador único gerado automaticamente (gen_random_uuid()) |
| `nome` | VARCHAR(150) | NOT NULL | Nome da modalidade de transporte. Único no sistema (case-insensitive via índice LOWER) |
| `descricao` | VARCHAR(500) | NULL | Descrição opcional da modalidade |
| `ativo` | BOOLEAN | NOT NULL | Indica se o registro está ativo. Padrão: TRUE. FALSE representa soft delete |
| `data_cadastro` | TIMESTAMPTZ | NOT NULL | Data e hora de criação. Preenchido automaticamente (DEFAULT NOW()) |
| `usuario_cadastro` | UUID | NULL | ID do usuário que criou o registro, extraído do JWT |
| `ultima_alteracao` | TIMESTAMPTZ | NULL | Data e hora da última alteração. Atualizado automaticamente por trigger |
| `usuario_alteracao` | UUID | NULL | ID do usuário que realizou a última alteração |

### Tabela: `produtos_categoria`

**Contexto:** Categorias de produtos com suporte à hierarquia de N níveis.

| Campo | Tipo | Null | Descrição |
|---|---|---|---|
| `id` | UUID | NOT NULL | Identificador único |
| `categoria_pai_id` | UUID | NULL | FK auto-referencial para `produtos_categoria.id`. NULL indica categoria raiz |
| `nome` | VARCHAR(150) | NOT NULL | Nome único da categoria (case-insensitive) |
| `descricao` | VARCHAR(500) | NULL | Descrição opcional |
| `ativo` | BOOLEAN | NOT NULL | Status de ativação. Padrão: TRUE |
| `data_cadastro` | TIMESTAMPTZ | NOT NULL | Timestamp de criação |
| `usuario_cadastro` | UUID | NULL | ID do criador |
| `ultima_alteracao` | TIMESTAMPTZ | NULL | Timestamp da última alteração (trigger) |
| `usuario_alteracao` | UUID | NULL | ID do último alterador |

### Tabela: `produtos_unidade_medida`

**Contexto:** Unidades de medida utilizadas nos produtos do catálogo.

| Campo | Tipo | Null | Descrição |
|---|---|---|---|
| `id` | UUID | NOT NULL | Identificador único |
| `nome` | VARCHAR(150) | NOT NULL | Nome completo da unidade (ex.: "Quilograma"). Único (case-insensitive) |
| `sigla` | VARCHAR(20) | NOT NULL | Sigla abreviada (ex.: "KG"). Armazenada em UPPERCASE. Única (case-insensitive) |
| `descricao` | VARCHAR(500) | NULL | Descrição opcional |
| `ativo` | BOOLEAN | NOT NULL | Status de ativação |
| `data_cadastro` | TIMESTAMPTZ | NOT NULL | Timestamp de criação |
| `usuario_cadastro` | UUID | NULL | ID do criador |
| `ultima_alteracao` | TIMESTAMPTZ | NULL | Timestamp da última alteração |
| `usuario_alteracao` | UUID | NULL | ID do último alterador |

### Tabela: `produtos_produto`

**Contexto:** Entidade central do microsserviço. Representa um produto no catálogo B2B.

| Campo | Tipo | Null | Descrição |
|---|---|---|---|
| `id` | UUID | NOT NULL | Identificador único do produto |
| `transporte_id` | UUID | NOT NULL | FK para `produtos_transporte.id`. Define a modalidade de transporte do produto |
| `categoria_id` | UUID | NOT NULL | FK para `produtos_categoria.id`. Categoria à qual o produto pertence |
| `unidade_medida_id` | UUID | NOT NULL | FK para `produtos_unidade_medida.id`. Unidade de medida do produto |
| `codigo` | VARCHAR(60) | NOT NULL | Código único do produto. Armazenado em UPPERCASE. Único no sistema (case-insensitive) |
| `nome` | VARCHAR(150) | NOT NULL | Nome descritivo do produto |
| `descricao` | VARCHAR(1000) | NULL | Descrição detalhada opcional |
| `ativo` | BOOLEAN | NOT NULL | Status de ativação. Padrão: TRUE |
| `data_cadastro` | TIMESTAMPTZ | NOT NULL | Timestamp de criação |
| `usuario_cadastro` | UUID | NULL | ID do criador |
| `ultima_alteracao` | TIMESTAMPTZ | NULL | Timestamp da última alteração (trigger) |
| `usuario_alteracao` | UUID | NULL | ID do último alterador |

**Nota:** O produto não possui campos de preço ou quantidade. Esses dados pertencem a outros domínios do sistema.

---

## 2.4 Requisitos Não Funcionais

### Banco de Dados Distribuído

O Portal B2B adota a estratégia de **banco de dados compartilhado com isolamento por schema** (Shared Database, Separate Schema), uma abordagem intermediária entre banco totalmente isolado por serviço e banco completamente compartilhado.

**Estratégia adotada:** Schema separado por contexto dentro do mesmo banco PostgreSQL.

| Vantagem | Detalhe |
|---|---|
| Simplicidade operacional | Um único banco para gerenciar, monitorar e fazer backup |
| Isolamento lógico | Cada microsserviço acessa apenas seu schema via permissões |
| Joins cross-schema | Possível em consultas de relatório, sem overhead de rede |
| Custo | Infraestrutura única reduz custo operacional |

**Isolamento de acesso:** Cada microsserviço conecta com um usuário PostgreSQL com permissões restritas ao seu schema:

```sql
-- Usuário do microsserviço de produtos
GRANT USAGE ON SCHEMA portal_b2b TO usuario_produtos;
GRANT SELECT, INSERT, UPDATE ON ALL TABLES IN SCHEMA portal_b2b TO usuario_produtos;
```

**Connection Pooling:** O Npgsql gerencia um pool com até 100 conexões simultâneas por instância do serviço. Isso evita a sobrecarga de criação de conexão a cada requisição.

**Resiliência:** O serviço não implementa retry automático para operações de banco. Em caso de falha transiente de conexão, o middleware global captura a exceção e retorna HTTP 500. A resiliência de longo prazo é garantida pela política `restart: unless-stopped` do Docker Compose.

---

## 2.5 Modelo por Microsserviço

### Microsserviço de Produtos — Schema `portal_b2b`

```
produtos_transporte (1) ──────── (N) produtos_produto
produtos_categoria  (1) ──────── (N) produtos_produto
produtos_unidade_medida (1) ──── (N) produtos_produto
produtos_categoria (1) ─────── (N) produtos_categoria (hierarquia)
```

> **Diagrama completo:** `docs/diagramas/02-modelo-dados-er.puml`

---

## 2.6 Tecnologias e Decisões Técnicas

### PostgreSQL 16

Escolhido como banco de dados principal pelos seguintes motivos:
1. **Suporte nativo a UUID:** O tipo `UUID` com `gen_random_uuid()` elimina a necessidade de geração de IDs na aplicação.
2. **TIMESTAMPTZ:** Armazena datas com timezone, evitando ambiguidades em sistemas distribuídos.
3. **Índices funcionais:** Permite criar índices em expressões como `LOWER(nome)` para unicidade case-insensitive eficiente.
4. **ACID compliance:** Garante consistência transacional em operações de escrita.
5. **Open-source:** Sem custos de licenciamento.
6. **Triggers:** Permite lógica de auditoria automática no banco, sem dependência da aplicação.

### Dapper

Escolhido em detrimento do Entity Framework Core:
- **Performance:** Dapper gera SQL próximo ao ótimo, sem overhead de tradução LINQ.
- **Controle:** Queries complexas com múltiplos JOINs, subqueries e paginação são mais legíveis em SQL puro.
- **Debugging:** SQL visível no código facilita análise de performance e diagnóstico.
- **Trade-off:** Mais código boilerplate para mapeamento, compensado pelo `MatchNamesWithUnderscores`.

### Triggers de Auditoria

```sql
CREATE FUNCTION fn_atualiza_ultima_alteracao()
RETURNS TRIGGER AS $$
BEGIN
    NEW.ultima_alteracao = NOW();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;
```

A função é chamada por triggers `BEFORE UPDATE` em cada tabela. Isso garante que `ultima_alteracao` seja sempre atualizado, mesmo se a aplicação esquecer de atribuir o valor.

---

## 2.7 Conexão dos Microsserviços com o Banco de Dados

### Fluxo de Conexão

```
NpgsqlDataSource (Singleton)
    │
    ├── Connection String (via env ou appsettings)
    ├── Max Pool Size: 100
    ├── Connection Lifetime: padrão Npgsql
    │
IDbConnectionFactory (Scoped)
    │
    └── OpenConnectionAsync() → NpgsqlConnection
            │
            └── Dapper: Query<T>(), Execute(), QueryMultipleAsync()
                        │
                        └── PostgreSQL: porta 5432
```

### Resolução da Connection String

O sistema resolve a connection string na seguinte ordem de prioridade:

1. **Variáveis de ambiente individuais** (preferida em produção):
   ```
   POSTGRESQL_HOST, POSTGRESQL_PORT, POSTGRESQL_DATABASE,
   POSTGRESQL_USER, POSTGRESQL_PASSWORD
   ```

2. **ConnectionStrings:DefaultConnection** no `appsettings.{Environment}.json` (desenvolvimento).

### Configuração do Pool

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=...;Port=5432;Database=portal_b2b;
      Username=...;Password=...;
      Search Path=portal_b2b;
      Maximum Pool Size=100;
      Application Name=produtos-service"
  }
}
```

O parâmetro `Search Path=portal_b2b` define o schema padrão, eliminando a necessidade de prefixo `schema.tabela` no SQL.

---

# 3. Arquitetura e Infraestrutura

## 3.1 Desenho da Arquitetura Atual

> **Diagrama completo:** `docs/diagramas/01-arquitetura-geral.puml`  
> **Diagrama de deployment:** `docs/diagramas/06-deployment.puml`

```
┌─────────────────────────────────────────────────────────────┐
│               Docker Network: portal-b2b-network            │
│                                                             │
│  [Usuário]                                                  │
│      │ HTTPS                                                │
│      ▼                                                      │
│  [Nginx/Gateway :80]  ──── roteia ──►  [auth-service :5001] │
│      │                              ▲                       │
│      │ /api/produtos                │ valida JWT            │
│      ▼                              │                       │
│  [produtos-service :5002] ──────────┘                       │
│      │                 │                                    │
│      │ Dapper/Npgsql   │ Confluent.Kafka                    │
│      ▼                 ▼                                    │
│  [PostgreSQL :5432]  [Redpanda :9092]                       │
│                                │                            │
│              ┌─────────────────┤                            │
│              ▼                 ▼                            │
│  [fornecedores-service]  [pedidos-service]                  │
│                                                             │
│  [produtos-front :8081]  ──► [produtos-service :5002]       │
└─────────────────────────────────────────────────────────────┘
```

---

## 3.2 Requisitos Funcionais

| RF | Descrição |
|---|---|
| RF-ARQ-01 | O sistema deve ser composto por microsserviços independentes, cada um com responsabilidade de domínio única |
| RF-ARQ-02 | Cada microsserviço deve expor uma API REST com documentação OpenAPI |
| RF-ARQ-03 | A comunicação entre microsserviços deve ser realizada via eventos assíncronos (Kafka) |
| RF-ARQ-04 | A autenticação deve ser centralizada e stateless (JWT) |
| RF-ARQ-05 | Cada microsserviço deve expor endpoint de health check |
| RF-ARQ-06 | O frontend deve ser servido por container dedicado separado do backend |
| RF-ARQ-07 | Todo o ambiente deve poder ser inicializado via Docker Compose com um único comando |

---

## 3.3 Requisitos Não Funcionais

### Tecnologias de Infraestrutura

#### Docker & Docker Compose

**Função:** Containerização e orquestração do ambiente local de desenvolvimento e produção.

- **Multi-stage builds:** Reduzem o tamanho final das imagens. O backend compila em `sdk:10.0` (~800MB) e executa em `aspnet:10.0` (~200MB). O frontend compila em `node:20-alpine` e serve via `nginx:1.27-alpine` (~50MB).
- **Restart policy (`unless-stopped`):** Garante que os containers reiniciem automaticamente após falhas ou reinicialização do host.
- **Rede bridge compartilhada:** Todos os serviços comunicam via DNS interno do Docker (nome do container como hostname).

#### Apache Kafka (Redpanda)

**Função:** Message broker para comunicação assíncrona entre microsserviços.

**Redpanda** é uma implementação de Kafka em C++ compatível com a API Kafka, oferecendo:
- Latência menor que o Kafka Java original (sem JVM).
- Simplicidade operacional (sem necessidade de ZooKeeper).
- 100% compatível com clientes Kafka existentes (Confluent.Kafka).

**Garantias de entrega:** O produtor (produtos-service) usa `acks=all` para garantir que o evento seja persistido antes de considerar a publicação bem-sucedida. A configuração `KAFKA_FAIL_ON_PUBLISH_ERROR=false` garante que uma falha Kafka não derrube a operação HTTP.

#### PostgreSQL 16

**Função:** Banco de dados relacional principal do sistema.

Detalhes técnicos:
- **WAL (Write-Ahead Logging):** Garante durabilidade e suporte a replicação.
- **MVCC (Multi-Version Concurrency Control):** Leituras não bloqueiam escritas.
- **Índices GIN/GiST:** Disponíveis para pesquisa full-text (não utilizado nesta versão, mas suportado).
- **Connection Pooling externo:** Recomendado PgBouncer em produção de alta escala.

#### Nginx

**Função:** Servidor web para o frontend e potencial reverse proxy.

- Serve arquivos estáticos do React (HTML, JS, CSS) com cache otimizado.
- Configuração `try_files $uri /index.html` garante que rotas do React Router funcionem ao recarregar a página.
- Pode atuar como reverse proxy para os microsserviços, adicionando TLS termination.

#### JWT Bearer Authentication

**Função:** Autenticação stateless entre o frontend e os microsserviços.

O fluxo de validação:
1. Cliente envia `Authorization: Bearer <token>`.
2. O middleware `JwtBearerAuthentication` do ASP.NET valida a assinatura com a `SecretKey`.
3. Valida issuer (`portal-autenticacao`), audience (`portal-b2b`) e expiração.
4. Injeta as claims no `HttpContext.User`.
5. `ICurrentUserService` extrai o `userId` das claims para auditoria.

---

## 3.4 Manual de Implementação

### Requisitos de Sistema

| Componente | Versão Mínima |
|---|---|
| Docker Engine | 24.0+ |
| Docker Compose | 2.20+ |
| Git | 2.40+ |

### Passo a Passo Completo

#### 1. Clonar o repositório

```bash
git clone <url-do-repositorio> SDI.Micro.Produto
cd SDI.Micro.Produto
```

#### 2. Criar a rede Docker compartilhada

```bash
docker network create portal-b2b-network
```

> Esta rede é criada uma única vez e compartilhada por todos os microsserviços do projeto.

#### 3. Configurar variáveis de ambiente

```bash
cp .env.example .env
# Editar .env com as configurações do ambiente
```

#### 4. Inicializar o banco de dados

```bash
# Caso esteja usando o postgres do projeto integrado:
docker exec -i postgres psql -U postgres -d portal_b2b < Scripts/ScriptDeCriacao.sql
```

#### 5. Build e inicialização dos containers

```bash
docker compose up -d --build
```

#### 6. Verificação

```bash
# Verificar containers em execução
docker compose ps

# Verificar logs do backend
docker compose logs produtos-service -f

# Health check
curl http://localhost:5002/saude

# Acessar frontend
# http://localhost:8081
```

### Execução em Desenvolvimento (sem Docker)

#### Backend

```bash
cd back-end/SDI.Back.API

# Configurar variáveis de ambiente
$env:ASPNETCORE_ENVIRONMENT="Development"
$env:DATABASE_URL="postgresql://usuario:senha@localhost:5432/portal_b2b"

# Executar
dotnet run
# API disponível em: http://localhost:5002
# Swagger: http://localhost:5002/swagger
```

#### Frontend

```bash
cd front-end

# Instalar dependências
npm install

# Configurar variáveis de ambiente
cp .env.example .env.local
# Editar VITE_PRODUTO_API_URL=http://localhost:5002

# Executar
npm run dev
# Frontend disponível em: http://localhost:5173
```

---

## 3.5 Manual de Integração dos Microsserviços

### Integração via JWT

**Chave Compartilhada:** A `SecretKey` do JWT deve ser a mesma em todos os microsserviços que validam tokens. Ela é configurada via variável de ambiente `Jwt__SecretKey`.

```dotenv
# Em todos os microsserviços
Jwt__Issuer=portal-autenticacao
Jwt__Audience=portal-b2b
Jwt__SecretKey=CHAVE_IDENTICA_EM_TODOS
```

O microsserviço de autenticação é o único responsável por **emitir** tokens. Os demais apenas **validam**.

### Integração via Kafka

**Consumindo eventos do microsserviço de produtos:**

```csharp
// Exemplo de consumer em outro microsserviço
var config = new ConsumerConfig
{
    BootstrapServers = "redpanda:9092",
    GroupId = "pedidos-service-group",
    AutoOffsetReset = AutoOffsetReset.Earliest
};

using var consumer = new ConsumerBuilder<string, string>(config).Build();
consumer.Subscribe(new[] { "produto_cadastrado", "produto_atualizado" });

while (true)
{
    var message = consumer.Consume();
    var evento = JsonSerializer.Deserialize<IntegrationEvent>(message.Value);
    // processar evento...
}
```

**Formato do evento recebido:**

```json
{
  "eventId": "550e8400-e29b-41d4-a716-446655440000",
  "eventType": "produto_cadastrado",
  "eventVersion": "1.0",
  "timestamp": "2026-06-01T14:00:00Z",
  "source": "produtos-service",
  "correlationId": "660e8400-e29b-41d4-a716-446655441111",
  "aggregateType": "produto",
  "aggregateId": "770e8400-e29b-41d4-a716-446655442222",
  "userId": "880e8400-e29b-41d4-a716-446655443333",
  "payload": {
    "id": "770e8400-e29b-41d4-a716-446655442222",
    "codigo": "PROD-001",
    "nome": "Notebook Dell Inspiron",
    "categoriaId": "...",
    "transporteId": "...",
    "unidadeMedidaId": "...",
    "ativo": true
  }
}
```

### Integração via API REST

Para microsserviços que precisam consultar o catálogo de produtos sincronamente:

```bash
# Autenticação obrigatória
curl -H "Authorization: Bearer <token>" \
     http://produtos-service:5002/produtos/{id}

# Listagem com filtros
curl -H "Authorization: Bearer <token>" \
     "http://produtos-service:5002/produtos?pagina=1&tamanhoPagina=20&busca=notebook"
```

### Configuração do Proxy (nginx.conf sugerido)

```nginx
upstream produtos_api {
    server produtos-service:5002;
}

upstream produtos_front {
    server produtos-front:80;
}

server {
    listen 80;

    location /api/produtos/ {
        proxy_pass http://produtos_api/;
        proxy_set_header Authorization $http_authorization;
    }

    location /produtos/ {
        proxy_pass http://produtos_front/;
    }
}
```

---

## 3.6 Jornada do Usuário (Macro)

> **Diagrama completo:** `docs/diagramas/07-jornada-macro.puml`

A jornada macro descreve o caminho do usuário pelo sistema sem entrar nos detalhes internos de cada microsserviço:

```
[Usuário acessa o Portal]
        │
        ▼
[Auth Service: Login e emissão de JWT]
        │
        ▼
[Gateway: roteamento por módulo]
        │
    ┌───┴───────────────────┐
    │                       │
    ▼                       ▼
[Microsserviço Produtos]  [Microsserviço Fornecedores]
 - Gerencia catálogo       - Gerencia cotações
        │                       │
        └───────────┬───────────┘
                    │ Eventos Kafka
                    ▼
        [Microsserviço Pedidos]
         - Utiliza catálogo
         - Gera pedidos
                    │
                    ▼
        [Usuário recebe confirmação]
```

---

## 3.7 Decisões Técnicas

### 1. Microsserviços vs. Monolito

**Decisão:** Arquitetura de microsserviços.  
**Justificativa:** O escopo do projeto pedagógico exige que equipes independentes trabalhem em domínios separados (Autenticação, Produtos, Fornecedores, Pedidos) sem interdependência de código. Microsserviços permitem deploy independente, tecnologias distintas por serviço e escalabilidade granular.

### 2. ASP.NET Core vs. outras tecnologias de backend

**Decisão:** ASP.NET Core 10.0.  
**Justificativa:** Alta performance, ecossistema maduro, suporte a .NET 10 LTS, integração nativa com JWT, e familiaridade da equipe com C#.

### 3. Dapper vs. Entity Framework Core

**Decisão:** Dapper.  
**Justificativa:** Maior controle sobre SQL, performance superior em queries com JOINs complexos, e facilidade de debug. O overhead de configuração do EF Core não se justifica para a escala do projeto.

### 4. Kafka (Redpanda) vs. REST síncrono

**Decisão:** Kafka para comunicação entre microsserviços.  
**Justificativa:** Desacoplamento temporal — um microsserviço pode estar indisponível sem afetar o produtor. Suporte a múltiplos consumidores do mesmo evento sem coordenação. Histórico de eventos para auditoria e replay.

### 5. Schema compartilhado vs. banco por serviço

**Decisão:** Schema separado no mesmo banco PostgreSQL.  
**Justificativa:** Simplicidade operacional adequada ao contexto acadêmico/de desenvolvimento. Banco separado por serviço seria ideal em produção de alta escala, mas adiciona complexidade de gerenciamento desnecessária nesta fase.

### 6. JWT stateless vs. sessions

**Decisão:** JWT stateless.  
**Justificativa:** Sem necessidade de sessão centralizada (Redis, banco), reduzindo dependências. O token carrega todas as informações necessárias (userId, roles). Adequado para APIs REST distribuídas.

### 7. Soft Delete vs. Delete físico

**Decisão:** Soft delete (campo `ativo`).  
**Justificativa:** Preserva integridade referencial (produtos inativados ainda existem como FK em pedidos históricos). Permite reativação. Mantém histórico para auditoria.

### 8. React + Vite vs. Next.js

**Decisão:** React puro com Vite.  
**Justificativa:** Next.js adiciona SSR/SSG que não agrega valor para uma aplicação de gestão (dashboard) que requer autenticação. Vite oferece DX superior com HMR ultrarrápido. SPA adequada para o caso de uso.

---

*Relatório gerado em Junho de 2026*  
*Repositório: SDI.Micro.Produto*
