
# SDI.Micro.Produto

Microservico de catalogo de produtos do ecossistema SDI, desenvolvido em .NET C# com ASP.NET Core, Dapper, PostgreSQL e Kafka.

Este servico permite o cadastro e manutencao de:

- Categorias, incluindo hierarquia por categoria pai.
- Transportes.
- Unidades de medida.
- Produtos, vinculados obrigatoriamente a uma categoria, um transporte e uma unidade de medida, sem preco e sem quantidade no catalogo.

O microservico foi implementado a partir da analise do script [`Scripts/ScriptDeCriacao.sql`](Scripts/ScriptDeCriacao.sql), respeitando o schema `sdi`, as tabelas, constraints, indices, auditoria e relacionamentos definidos no banco.

## Sumario

- [Objetivo](#objetivo)
- [Tecnologias](#tecnologias)
- [Estrutura do repositorio](#estrutura-do-repositorio)
- [Analise detalhada do banco](#analise-detalhada-do-banco)
- [Arquitetura da aplicacao](#arquitetura-da-aplicacao)
- [Configuracao do ambiente](#configuracao-do-ambiente)
- [Criacao do banco](#criacao-do-banco)
- [Como executar](#como-executar)
- [Swagger](#swagger)
- [Padrao de resposta](#padrao-de-resposta)
- [Endpoints](#endpoints)
- [Payloads de exemplo](#payloads-de-exemplo)
- [Regras de negocio](#regras-de-negocio)
- [Tratamento de erros](#tratamento-de-erros)
- [Integracao Kafka](#integracao-kafka)
- [Health check](#health-check)
- [Mapeamento Dapper](#mapeamento-dapper)
- [Troubleshooting](#troubleshooting)
- [Evolucoes recomendadas](#evolucoes-recomendadas)

## Objetivo

O objetivo deste microservico e centralizar o cadastro de produtos e suas entidades auxiliares em uma API REST simples, consistente e preparada para integracao com outros servicos distribuidos.

O banco indica que produto nao e uma entidade isolada. Para cadastrar um produto valido, o sistema precisa conhecer:

- Como o produto sera transportado.
- A qual categoria ele pertence.
- Qual unidade de medida caracteriza o produto.
- Um codigo unico de identificacao.

Preco e quantidade nao pertencem ao catalogo de produtos. Esses dados devem ficar nos dominios de fornecimentos, estoque, demandas ou pedidos, conforme a arquitetura distribuida.

Por isso, a API foi dividida em quatro dominios principais:

- `transportes`
- `categorias`
- `unidades-medida`
- `produtos`

## Tecnologias

| Tecnologia | Uso |
| --- | --- |
| .NET 10 | Plataforma principal da API |
| ASP.NET Core | Controllers REST, DI, pipeline HTTP e Swagger |
| C# com nullable enable | Codigo mais seguro contra nulos |
| PostgreSQL | Banco relacional |
| Dapper | Acesso a dados e mapeamento leve |
| Npgsql | Driver PostgreSQL para .NET |
| Apache Kafka | Comunicacao assincrona com outros microservicos |
| Confluent.Kafka | Producer Kafka usado pela API |
| Serilog | Logging estruturado em console |
| Swashbuckle | Geracao de documentacao Swagger/OpenAPI |

Pacotes NuGet usados no projeto:

```xml
<PackageReference Include="Confluent.Kafka" Version="2.8.0" />
<PackageReference Include="Dapper" Version="2.1.66" />
<PackageReference Include="Npgsql" Version="9.0.3" />
<PackageReference Include="Serilog.AspNetCore" Version="8.0.1" />
<PackageReference Include="Swashbuckle.AspNetCore" Version="6.6.2" />
```

## Estrutura do repositorio

```text
SDI.Micro.Produto/
  README.md
  SDI.Back.Template.slnx
  Scripts/
    ScriptDeCriacao.sql
  SDI.Back.Template/
    Program.cs
    appsettings.json
    Properties/
      launchSettings.json
    Controllers/
      CategoriasController.cs
      HealthController.cs
      ProdutosController.cs
      TransportesController.cs
      UnidadesMedidaController.cs
    Configuration/
      KafkaOptions.cs
    Data/
      IDbConnectionFactory.cs
      NpgsqlConnectionFactory.cs
      PostgresConnectionStringResolver.cs
    Exceptions/
      DomainException.cs
    HealthChecks/
      PostgresHealthCheck.cs
    Middlewares/
      GlobalExceptionHandlerMiddleware.cs
    Models/
      Dto/
        Input/
        Output/
      Entity/
      Messaging/
      Responses/
    Messaging/
      EventTypes.cs
      IKafkaEventPublisher.cs
      KafkaEventPublisher.cs
      KafkaServiceCollectionExtensions.cs
    Repositories/
      Interfaces/
      CategoriaRepository.cs
      ProdutoRepository.cs
      TransporteRepository.cs
      UnidadeMedidaRepository.cs
    Services/
      Interfaces/
      CategoriaService.cs
      ProdutoService.cs
      TransporteService.cs
      UnidadeMedidaService.cs
      MappingExtensions.cs
      ServiceValidation.cs
```

### Observacao sobre nomenclatura

A estrutura original usava o prefixo `Alta`. A pedido do projeto, as pastas, solution, projeto, assembly e namespace foram ajustados para `SDI`.

Exemplos:

- `Alta.Back.Template` virou `SDI.Back.Template`
- `Alta.Back.Template.slnx` virou `SDI.Back.Template.slnx`
- `Alta.Back.Template.csproj` virou `SDI.Back.Template.csproj`
- `namespace Alta.Back.Template` virou `namespace SDI.Back.Template`

## Analise detalhada do banco

O script de criacao define uma base com foco em cadastro e auditoria.

Arquivo analisado:

```text
Scripts/ScriptDeCriacao.sql
```

### Extensao pgcrypto

```sql
CREATE EXTENSION IF NOT EXISTS pgcrypto;
```

Essa extensao e usada para habilitar a funcao `gen_random_uuid()`, responsavel por gerar UUIDs diretamente no PostgreSQL.

Impacto na API:

- A API nao precisa gerar `id` no C# ao criar registros.
- O banco gera o `id` automaticamente.
- Os repositories usam `returning *` apos `insert` para devolver o registro completo ja com `id`, `data_cadastro` e valores default.

### Schema sdi

```sql
CREATE SCHEMA IF NOT EXISTS sdi;
```

Todas as tabelas do dominio ficam no schema `sdi`.

Impacto na API:

- Todos os SQLs Dapper usam nomes totalmente qualificados:

```sql
sdi.transporte
sdi.categoria
sdi.unidade_medida
sdi.produto
```

Isso evita ambiguidade com tabelas de outros schemas.

### Funcao de ultima alteracao

```sql
CREATE OR REPLACE FUNCTION sdi.fn_atualizar_ultima_alteracao()
RETURNS TRIGGER AS $$
BEGIN
    NEW.ultima_alteracao = NOW();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;
```

Essa funcao atualiza automaticamente o campo `ultima_alteracao` sempre que um registro sofre `UPDATE`.

Impacto na API:

- A API nao seta manualmente `ultima_alteracao`.
- Os repositories informam apenas `usuario_alteracao`.
- O banco garante a data correta da alteracao.

### Auditoria comum

As tabelas principais seguem o mesmo padrao:

```sql
ativo BOOLEAN NOT NULL DEFAULT TRUE,
data_cadastro TIMESTAMPTZ NOT NULL DEFAULT NOW(),
usuario_cadastro UUID NULL,
ultima_alteracao TIMESTAMPTZ NULL,
usuario_alteracao UUID NULL
```

Impacto na API:

- Todo cadastro nasce ativo por default.
- A data de cadastro e controlada pelo banco.
- Alteracoes de status usam endpoints especificos de ativacao e inativacao.
- `usuario_cadastro` e `usuario_alteracao` sao opcionais, pois o banco permite `NULL`.

### Tabela sdi.transporte

Campos:

| Campo | Tipo | Obrigatorio | Observacao |
| --- | --- | --- | --- |
| `id` | UUID | Sim | Gerado por `gen_random_uuid()` |
| `nome` | VARCHAR(150) | Sim | Nome unico, case-insensitive |
| `descricao` | VARCHAR(500) | Nao | Texto livre |
| `ativo` | BOOLEAN | Sim | Default `true` |
| `data_cadastro` | TIMESTAMPTZ | Sim | Default `now()` |
| `usuario_cadastro` | UUID | Nao | Usuario criador |
| `ultima_alteracao` | TIMESTAMPTZ | Nao | Atualizado por trigger |
| `usuario_alteracao` | UUID | Nao | Usuario alterador |

Indice unico:

```sql
CREATE UNIQUE INDEX IF NOT EXISTS ux_sdi_transporte_nome
ON sdi.transporte (LOWER(nome));
```

Consequencia:

- Nao e permitido cadastrar dois transportes com nomes iguais, mesmo mudando maiusculas/minusculas.
- `Correios` e `correios` sao considerados duplicados.

Endpoints implementados:

- `GET /transportes`
- `GET /transportes/{id}`
- `POST /transportes`
- `PUT /transportes/{id}`
- `PATCH /transportes/{id}/ativar`
- `PATCH /transportes/{id}/inativar`

### Tabela sdi.categoria

Campos:

| Campo | Tipo | Obrigatorio | Observacao |
| --- | --- | --- | --- |
| `id` | UUID | Sim | Gerado por `gen_random_uuid()` |
| `categoria_pai_id` | UUID | Nao | FK para `sdi.categoria(id)` |
| `nome` | VARCHAR(150) | Sim | Nome unico, case-insensitive |
| `descricao` | VARCHAR(500) | Nao | Texto livre |
| `ativo` | BOOLEAN | Sim | Default `true` |
| `data_cadastro` | TIMESTAMPTZ | Sim | Default `now()` |
| `usuario_cadastro` | UUID | Nao | Usuario criador |
| `ultima_alteracao` | TIMESTAMPTZ | Nao | Atualizado por trigger |
| `usuario_alteracao` | UUID | Nao | Usuario alterador |

Relacionamento:

```sql
CONSTRAINT fk_categoria_categoria_pai
    FOREIGN KEY (categoria_pai_id)
    REFERENCES sdi.categoria(id)
```

Isso permite categorias hierarquicas, por exemplo:

```text
Eletronicos
  Celulares
  Notebooks
Alimentos
  Bebidas
  Graos
```

Indice unico:

```sql
CREATE UNIQUE INDEX IF NOT EXISTS ux_sdi_categoria_nome
ON sdi.categoria (LOWER(nome));
```

Indice auxiliar:

```sql
CREATE INDEX IF NOT EXISTS ix_sdi_categoria_categoria_pai_id
ON sdi.categoria (categoria_pai_id);
```

Impacto na API:

- A API valida que uma categoria nao pode ser pai dela mesma.
- A API valida que a categoria pai existe e esta ativa.
- A listagem permite filtrar por `categoriaPaiId`.

Endpoints implementados:

- `GET /categorias`
- `GET /categorias/{id}`
- `POST /categorias`
- `PUT /categorias/{id}`
- `PATCH /categorias/{id}/ativar`
- `PATCH /categorias/{id}/inativar`

### Tabela sdi.unidade_medida

Campos:

| Campo | Tipo | Obrigatorio | Observacao |
| --- | --- | --- | --- |
| `id` | UUID | Sim | Gerado por `gen_random_uuid()` |
| `nome` | VARCHAR(150) | Sim | Nome unico, case-insensitive |
| `sigla` | VARCHAR(20) | Sim | Sigla unica, case-insensitive |
| `descricao` | VARCHAR(500) | Nao | Texto livre |
| `ativo` | BOOLEAN | Sim | Default `true` |
| `data_cadastro` | TIMESTAMPTZ | Sim | Default `now()` |
| `usuario_cadastro` | UUID | Nao | Usuario criador |
| `ultima_alteracao` | TIMESTAMPTZ | Nao | Atualizado por trigger |
| `usuario_alteracao` | UUID | Nao | Usuario alterador |

Indices unicos:

```sql
CREATE UNIQUE INDEX IF NOT EXISTS ux_sdi_unidade_medida_nome
ON sdi.unidade_medida (LOWER(nome));

CREATE UNIQUE INDEX IF NOT EXISTS ux_sdi_unidade_medida_sigla
ON sdi.unidade_medida (LOWER(sigla));
```

Impacto na API:

- Nao podem existir duas unidades de medida com o mesmo nome.
- Nao podem existir duas unidades de medida com a mesma sigla.
- A API normaliza `sigla` para maiusculo antes de gravar.

Exemplos:

- `Unidade` / `UN`
- `Quilograma` / `KG`
- `Litro` / `L`
- `Metro` / `M`

Endpoints implementados:

- `GET /unidades-medida`
- `GET /unidades-medida/{id}`
- `POST /unidades-medida`
- `PUT /unidades-medida/{id}`
- `PATCH /unidades-medida/{id}/ativar`
- `PATCH /unidades-medida/{id}/inativar`

### Tabela sdi.produto

Campos:

| Campo | Tipo | Obrigatorio | Observacao |
| --- | --- | --- | --- |
| `id` | UUID | Sim | Gerado por `gen_random_uuid()` |
| `transporte_id` | UUID | Sim | FK para transporte |
| `categoria_id` | UUID | Sim | FK para categoria |
| `unidade_medida_id` | UUID | Sim | FK para unidade de medida |
| `codigo` | VARCHAR(60) | Sim | Codigo unico, case-insensitive |
| `nome` | VARCHAR(150) | Sim | Nome do produto |
| `descricao` | VARCHAR(1000) | Nao | Texto livre |
| `ativo` | BOOLEAN | Sim | Default `true` |
| `data_cadastro` | TIMESTAMPTZ | Sim | Default `now()` |
| `usuario_cadastro` | UUID | Nao | Usuario criador |
| `ultima_alteracao` | TIMESTAMPTZ | Nao | Atualizado por trigger |
| `usuario_alteracao` | UUID | Nao | Usuario alterador |

Relacionamentos:

```sql
CONSTRAINT fk_produto_transporte
    FOREIGN KEY (transporte_id)
    REFERENCES sdi.transporte(id)

CONSTRAINT fk_produto_categoria
    FOREIGN KEY (categoria_id)
    REFERENCES sdi.categoria(id)

CONSTRAINT fk_produto_unidade_medida
    FOREIGN KEY (unidade_medida_id)
    REFERENCES sdi.unidade_medida(id)
```

Indice unico:

```sql
CREATE UNIQUE INDEX IF NOT EXISTS ux_sdi_produto_codigo
ON sdi.produto (LOWER(codigo));
```

Indices auxiliares:

```sql
CREATE INDEX IF NOT EXISTS ix_sdi_produto_transporte_id
ON sdi.produto (transporte_id);

CREATE INDEX IF NOT EXISTS ix_sdi_produto_categoria_id
ON sdi.produto (categoria_id);

CREATE INDEX IF NOT EXISTS ix_sdi_produto_unidade_medida_id
ON sdi.produto (unidade_medida_id);

CREATE INDEX IF NOT EXISTS ix_sdi_produto_nome
ON sdi.produto (nome);
```

Impacto na API:

- Produto exige transporte, categoria e unidade de medida.
- A API valida se os relacionamentos existem e estao ativos antes de inserir ou atualizar.
- Produto nao possui preco no catalogo.
- Produto nao possui quantidade no catalogo.
- Produto nao possui vinculo direto com fornecedor; a associacao fornecedor/produto pertence ao microsservico de fornecimentos, permitindo reutilizar o mesmo catalogo por multiplos fornecedores.
- `codigo` e normalizado para maiusculo antes de gravar.
- A listagem permite filtros por categoria, transporte e unidade de medida.

Endpoints implementados:

- `GET /produtos`
- `GET /produtos/{id}`
- `POST /produtos`
- `PUT /produtos/{id}`
- `PATCH /produtos/{id}/ativar`
- `PATCH /produtos/{id}/inativar`

## Arquitetura da aplicacao

A API foi organizada em camadas simples:

```text
Controller -> Service -> Repository -> PostgreSQL
```

### Controllers

Responsaveis por:

- Expor endpoints HTTP.
- Receber parametros de rota, query string e corpo da requisicao.
- Chamar os services.
- Retornar respostas padronizadas.

Controllers existentes:

- `TransportesController`
- `CategoriasController`
- `UnidadesMedidaController`
- `ProdutosController`
- `HealthController`

### Services

Responsaveis por:

- Regras de negocio.
- Validacao de campos obrigatorios.
- Validacao de tamanho maximo.
- Normalizacao de dados, como sigla e codigo em maiusculo.
- Validacao de relacionamentos.
- Conversao de entidades para DTOs de saida.
- Publicacao de eventos Kafka apos criacao, atualizacao e alteracao de status bem-sucedidas.

Services existentes:

- `TransporteService`
- `CategoriaService`
- `UnidadeMedidaService`
- `ProdutoService`

### Repositories

Responsaveis por:

- Executar SQL com Dapper.
- Abrir conexoes via factory.
- Mapear resultados do PostgreSQL para entidades C#.
- Retornar dados paginados.

Repositories existentes:

- `TransporteRepository`
- `CategoriaRepository`
- `UnidadeMedidaRepository`
- `ProdutoRepository`

### Data

Responsavel por configuracao e abertura de conexoes:

- `PostgresConnectionStringResolver`
- `IDbConnectionFactory`
- `NpgsqlConnectionFactory`

### Middleware de erro

O middleware `GlobalExceptionHandlerMiddleware` centraliza respostas de erro:

- Erros de dominio.
- Violacoes de unicidade do PostgreSQL.
- Violacoes de FK do PostgreSQL.
- Erros internos inesperados.

## Configuracao do ambiente

### Pre-requisitos

Instale:

- .NET SDK 10 ou superior.
- PostgreSQL com suporte a extensao `pgcrypto`.
- Um editor como Visual Studio, Rider ou VS Code.

Verifique o SDK:

```powershell
dotnet --info
```

Verifique o PostgreSQL:

```powershell
psql --version
```

### Connection string

O projeto tenta resolver a conexao com PostgreSQL de duas formas.

Primeiro, pelas variaveis de ambiente:

```text
POSTGRESQL_HOST
POSTGRESQL_PORT
POSTGRESQL_DATABASE
POSTGRESQL_USER
POSTGRESQL_PASSWORD
```

Se essas variaveis nao existirem, usa:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=sdi_produto;Username=postgres;Password=SUA_SENHA;Pooling=true;Maximum Pool Size=100;Include Error Detail=true"
  }
}
```

Recomendacao:

- Em desenvolvimento local, pode usar `appsettings.json`.
- Em homologacao/producao, prefira variaveis de ambiente.
- Nao versionar senhas reais em repositorios compartilhados.

Exemplo PowerShell:

```powershell
$env:POSTGRESQL_HOST="localhost"
$env:POSTGRESQL_PORT="5432"
$env:POSTGRESQL_DATABASE="sdi_produto"
$env:POSTGRESQL_USER="postgres"
$env:POSTGRESQL_PASSWORD="SUA_SENHA"
```

### Kafka

A integracao Kafka fica na secao:

```json
{
  "Kafka": {
    "Enabled": true,
    "FailOnPublishError": false,
    "BootstrapServers": "localhost:9092",
    "ClientId": "sdi-micro-produto",
    "EventsTopic": "sdi.produto.events",
    "PublishTimeoutMs": 5000
  }
}
```

Parametros:

| Parametro | Descricao |
| --- | --- |
| `Enabled` | Liga ou desliga a publicacao Kafka. |
| `FailOnPublishError` | Quando `true`, falhas ao publicar evento retornam erro na requisicao. Quando `false`, a API mantem a resposta HTTP e registra log de erro. |
| `BootstrapServers` | Endereco dos brokers Kafka. |
| `ClientId` | Identificacao deste producer no Kafka. |
| `EventsTopic` | Topico onde os eventos do microservico de produto sao publicados. |
| `PublishTimeoutMs` | Tempo maximo de tentativa de entrega antes de registrar falha. |

Tambem e possivel sobrescrever por variaveis de ambiente usando o padrao do ASP.NET Core:

```powershell
$env:Kafka__BootstrapServers="localhost:9092"
$env:Kafka__EventsTopic="sdi.produto.events"
$env:Kafka__Enabled="true"
```

## Criacao do banco

### Criar database

Entre no PostgreSQL e crie o banco:

```sql
CREATE DATABASE sdi_produto;
```

### Executar o script

Execute o script:

```powershell
psql -h localhost -p 5432 -U postgres -d sdi_produto -f Scripts\ScriptDeCriacao.sql
```

O script cria:

- Extensao `pgcrypto`.
- Schema `sdi`.
- Funcao `sdi.fn_atualizar_ultima_alteracao`.
- Tabelas `transporte`, `categoria`, `unidade_medida` e `produto`.
- Constraints de FK.
- Indices unicos.
- Indices auxiliares.
- Triggers de atualizacao de `ultima_alteracao`.

### Ordem recomendada de cadastro

Como produto depende das outras tabelas, a ordem natural de uso e:

1. Criar transportes.
2. Criar categorias.
3. Criar unidades de medida.
4. Criar produtos.

## Como executar

### Restaurar pacotes

```powershell
dotnet restore SDI.Back.Template.slnx
```

### Compilar

```powershell
dotnet build SDI.Back.Template.slnx
```

### Executar API

```powershell
dotnet run --project SDI.Back.Template\SDI.Back.Template.csproj
```

Ou informando uma URL especifica:

```powershell
dotnet run --project SDI.Back.Template\SDI.Back.Template.csproj --urls http://localhost:5242
```

### Parar API

No terminal onde a API esta rodando:

```text
Ctrl+C
```

Isso e importante para evitar lock no arquivo `.exe` durante proximos builds.

## Swagger

Em ambientes diferentes de producao, o Swagger fica disponivel em:

```text
http://localhost:5242/swagger
```

ou na porta configurada pelo `launchSettings.json`.

O Swagger permite:

- Visualizar todos os endpoints.
- Testar cadastros.
- Testar listagens.
- Consultar modelos de entrada e saida.
- Verificar respostas HTTP.

## Padrao de resposta

A API usa o wrapper:

```csharp
ApiResponse<T>
```

Formato:

```json
{
  "statusHttp": 200,
  "mensagem": "Operacao realizada com sucesso.",
  "resultado": {},
  "erros": []
}
```

### Resposta paginada

Listagens usam:

```csharp
PagedResult<T>
```

Formato:

```json
{
  "statusHttp": 200,
  "mensagem": "Operacao realizada com sucesso.",
  "resultado": {
    "itens": [],
    "pagina": 1,
    "tamanhoPagina": 20,
    "total": 0
  },
  "erros": []
}
```

### Paginacao

Parametros padrao:

| Parametro | Default | Observacao |
| --- | --- | --- |
| `pagina` | `1` | Valores menores que 1 viram 1 |
| `tamanhoPagina` | `20` | Limitado entre 1 e 100 |

## Endpoints

### Health

| Metodo | Rota | Descricao |
| --- | --- | --- |
| GET | `/saude` | Verifica saude da aplicacao e conexao com PostgreSQL |

### Transportes

| Metodo | Rota | Descricao |
| --- | --- | --- |
| GET | `/transportes` | Lista transportes |
| GET | `/transportes/{id}` | Busca transporte por id |
| POST | `/transportes` | Cria transporte |
| PUT | `/transportes/{id}` | Atualiza transporte |
| PATCH | `/transportes/{id}/ativar` | Ativa transporte |
| PATCH | `/transportes/{id}/inativar` | Inativa transporte |

Query params de listagem:

| Parametro | Tipo | Default | Descricao |
| --- | --- | --- | --- |
| `pagina` | int | 1 | Pagina atual |
| `tamanhoPagina` | int | 20 | Tamanho da pagina |
| `ativo` | bool? | true | Filtra por status |
| `busca` | string? | null | Busca em nome/descricao |

### Categorias

| Metodo | Rota | Descricao |
| --- | --- | --- |
| GET | `/categorias` | Lista categorias |
| GET | `/categorias/{id}` | Busca categoria por id |
| POST | `/categorias` | Cria categoria |
| PUT | `/categorias/{id}` | Atualiza categoria |
| PATCH | `/categorias/{id}/ativar` | Ativa categoria |
| PATCH | `/categorias/{id}/inativar` | Inativa categoria |

Query params de listagem:

| Parametro | Tipo | Default | Descricao |
| --- | --- | --- | --- |
| `pagina` | int | 1 | Pagina atual |
| `tamanhoPagina` | int | 20 | Tamanho da pagina |
| `ativo` | bool? | true | Filtra por status |
| `busca` | string? | null | Busca em nome/descricao |
| `categoriaPaiId` | guid? | null | Filtra por categoria pai |

### Unidades de medida

| Metodo | Rota | Descricao |
| --- | --- | --- |
| GET | `/unidades-medida` | Lista unidades de medida |
| GET | `/unidades-medida/{id}` | Busca unidade por id |
| POST | `/unidades-medida` | Cria unidade de medida |
| PUT | `/unidades-medida/{id}` | Atualiza unidade de medida |
| PATCH | `/unidades-medida/{id}/ativar` | Ativa unidade de medida |
| PATCH | `/unidades-medida/{id}/inativar` | Inativa unidade de medida |

Query params de listagem:

| Parametro | Tipo | Default | Descricao |
| --- | --- | --- | --- |
| `pagina` | int | 1 | Pagina atual |
| `tamanhoPagina` | int | 20 | Tamanho da pagina |
| `ativo` | bool? | true | Filtra por status |
| `busca` | string? | null | Busca em nome/sigla/descricao |

### Produtos

| Metodo | Rota | Descricao |
| --- | --- | --- |
| GET | `/produtos` | Lista produtos |
| GET | `/produtos/{id}` | Busca produto por id |
| POST | `/produtos` | Cria produto |
| PUT | `/produtos/{id}` | Atualiza produto |
| PATCH | `/produtos/{id}/ativar` | Ativa produto |
| PATCH | `/produtos/{id}/inativar` | Inativa produto |

Query params de listagem:

| Parametro | Tipo | Default | Descricao |
| --- | --- | --- | --- |
| `pagina` | int | 1 | Pagina atual |
| `tamanhoPagina` | int | 20 | Tamanho da pagina |
| `ativo` | bool? | true | Filtra por status |
| `busca` | string? | null | Busca em codigo/nome/descricao |
| `categoriaId` | guid? | null | Filtra por categoria |
| `transporteId` | guid? | null | Filtra por transporte |
| `unidadeMedidaId` | guid? | null | Filtra por unidade de medida |

## Payloads de exemplo

### Criar transporte

Requisicao:

```http
POST /transportes
Content-Type: application/json
```

```json
{
  "nome": "Correios",
  "descricao": "Transporte via Correios",
  "usuarioCadastro": null
}
```

Resposta:

```json
{
  "statusHttp": 201,
  "mensagem": "Registro criado com sucesso.",
  "resultado": {
    "id": "00000000-0000-0000-0000-000000000000",
    "nome": "Correios",
    "descricao": "Transporte via Correios",
    "ativo": true,
    "dataCadastro": "2026-04-27T21:00:00-03:00",
    "ultimaAlteracao": null
  },
  "erros": []
}
```

### Criar categoria raiz

```http
POST /categorias
Content-Type: application/json
```

```json
{
  "categoriaPaiId": null,
  "nome": "Eletronicos",
  "descricao": "Produtos eletronicos em geral",
  "usuarioCadastro": null
}
```

### Criar subcategoria

```http
POST /categorias
Content-Type: application/json
```

```json
{
  "categoriaPaiId": "ID_DA_CATEGORIA_PAI",
  "nome": "Notebooks",
  "descricao": "Computadores portateis",
  "usuarioCadastro": null
}
```

### Criar unidade de medida

```http
POST /unidades-medida
Content-Type: application/json
```

```json
{
  "nome": "Unidade",
  "sigla": "un",
  "descricao": "Controle por unidade individual",
  "usuarioCadastro": null
}
```

Observacao:

- A API gravara `sigla` como `UN`.

### Criar produto

```http
POST /produtos
Content-Type: application/json
```

```json
{
  "transporteId": "ID_DO_TRANSPORTE",
  "categoriaId": "ID_DA_CATEGORIA",
  "unidadeMedidaId": "ID_DA_UNIDADE_MEDIDA",
  "codigo": "note-001",
  "nome": "Notebook Dell Inspiron",
  "descricao": "Notebook para uso academico e profissional",
  "usuarioCadastro": null
}
```

Observacao:

- A API gravara `codigo` como `NOTE-001`.

### Atualizar produto

```http
PUT /produtos/{id}
Content-Type: application/json
```

```json
{
  "transporteId": "ID_DO_TRANSPORTE",
  "categoriaId": "ID_DA_CATEGORIA",
  "unidadeMedidaId": "ID_DA_UNIDADE_MEDIDA",
  "codigo": "NOTE-001",
  "nome": "Notebook Dell Inspiron 15",
  "descricao": "Notebook atualizado",
  "usuarioAlteracao": null
}
```

### Inativar produto

```http
PATCH /produtos/{id}/inativar
```

Opcionalmente:

```http
PATCH /produtos/{id}/inativar?usuarioAlteracao=00000000-0000-0000-0000-000000000000
```

Resposta:

```http
204 No Content
```

## Regras de negocio

### Gerais

- Registros sao criados ativos por padrao.
- A API nao remove registros fisicamente.
- Inativacao e feita alterando o campo `ativo`.
- Listagens retornam apenas ativos por padrao.
- Para listar ativos e inativos, envie `ativo` vazio ou remova o filtro conforme cliente HTTP permitir.
- Campos de texto sao aparados com `Trim()`.
- Campos opcionais vazios viram `null`.
- IDs sao UUID.

### Transporte

- `nome` e obrigatorio.
- `nome` tem limite de 150 caracteres.
- `descricao` tem limite de 500 caracteres.
- `nome` deve ser unico ignorando maiusculas/minusculas.

### Categoria

- `nome` e obrigatorio.
- `nome` tem limite de 150 caracteres.
- `descricao` tem limite de 500 caracteres.
- `nome` deve ser unico ignorando maiusculas/minusculas.
- `categoriaPaiId` e opcional.
- Uma categoria nao pode ser pai dela mesma.
- Categoria pai precisa existir e estar ativa.

### Unidade de medida

- `nome` e obrigatorio.
- `sigla` e obrigatoria.
- `nome` tem limite de 150 caracteres.
- `sigla` tem limite de 20 caracteres.
- `descricao` tem limite de 500 caracteres.
- `nome` deve ser unico ignorando maiusculas/minusculas.
- `sigla` deve ser unica ignorando maiusculas/minusculas.
- `sigla` e gravada em maiusculo.

### Produto

- `transporteId` e obrigatorio.
- `categoriaId` e obrigatorio.
- `unidadeMedidaId` e obrigatorio.
- `codigo` e obrigatorio.
- `nome` e obrigatorio.
- `codigo` tem limite de 60 caracteres.
- `nome` tem limite de 150 caracteres.
- `descricao` tem limite de 1000 caracteres.
- `codigo` deve ser unico ignorando maiusculas/minusculas.
- `codigo` e gravado em maiusculo.
- Transporte precisa existir e estar ativo.
- Categoria precisa existir e estar ativa.
- Unidade de medida precisa existir e estar ativa.
- Produto nao possui `preco` nem `quantidadeTotal`.
- Produto e reutilizavel por multiplos fornecedores, portanto nao possui `fornecedorId`.

## Tratamento de erros

Erros sao centralizados no middleware:

```text
GlobalExceptionHandlerMiddleware
```

### Erro de validacao/regra de negocio

Exemplo:

```json
{
  "statusHttp": 400,
  "mensagem": "Transporte e obrigatorio.",
  "resultado": null,
  "erros": [
    "Transporte e obrigatorio."
  ]
}
```

### Registro nao encontrado

Exemplo:

```json
{
  "statusHttp": 404,
  "mensagem": "Produto nao encontrado.",
  "resultado": null,
  "erros": [
    "Produto nao encontrado."
  ]
}
```

### Violacao de unicidade

Quando o PostgreSQL retorna `23505`, a API responde:

```json
{
  "statusHttp": 409,
  "mensagem": "Ja existe um registro com os dados informados.",
  "resultado": null,
  "erros": []
}
```

Casos comuns:

- Transporte com nome repetido.
- Categoria com nome repetido.
- Unidade de medida com nome repetido.
- Unidade de medida com sigla repetida.
- Produto com codigo repetido.

### Violacao de relacionamento

Quando o PostgreSQL retorna erro de FK, a API responde:

```json
{
  "statusHttp": 400,
  "mensagem": "Um relacionamento informado nao existe no banco de dados.",
  "resultado": null,
  "erros": []
}
```

Na maior parte dos casos, os services validam relacionamentos antes do banco.

## Integracao Kafka

O microservico publica eventos de integracao no topico configurado em `Kafka:EventsTopic` sempre que uma operacao de escrita e concluida no banco.

Eventos publicados:

| Evento | Quando ocorre |
| --- | --- |
| `produto_cadastrado` | Produto criado |
| `produto_atualizado` | Produto atualizado |
| `produto_status_alterado` | Produto ativado ou inativado |
| `categoria_cadastrada` | Categoria criada |
| `categoria_atualizada` | Categoria atualizada |
| `categoria_status_alterado` | Categoria ativada ou inativada |
| `transporte_cadastrado` | Transporte criado |
| `transporte_atualizado` | Transporte atualizado |
| `transporte_status_alterado` | Transporte ativado ou inativado |
| `unidade_medida_cadastrada` | Unidade de medida criada |
| `unidade_medida_atualizada` | Unidade de medida atualizada |
| `unidade_medida_status_alterado` | Unidade de medida ativada ou inativada |

Formato do envelope:

```json
{
  "eventId": "00000000-0000-0000-0000-000000000000",
  "eventType": "produto_cadastrado",
  "eventVersion": "1.0",
  "timestamp": "2026-04-28T14:00:00Z",
  "source": "produtos-service",
  "correlationId": "00000000-0000-0000-0000-000000000000",
  "payload": {}
}
```

Observacao sobre autenticacao:

- Como a autenticacao ainda sera definida por outro microservico, nao existe dependencia de token/JWT nesta implementacao.
- `usuarioCadastro` e `usuarioAlteracao` continuam opcionais nos endpoints REST para uso futuro, mas nao entram no envelope Kafka atual porque o contrato sugerido centraliza dados de integracao em `payload`.
- Quando o contrato de autenticacao estiver definido, basta trocar a origem do usuario nos controllers/services sem quebrar o contrato do evento.

## Health check

Endpoint:

```http
GET /saude
```

O health check valida:

- Status geral da API.
- Conectividade com PostgreSQL via `select 1`.

Resposta saudavel:

```json
{
  "statusHttp": 200,
  "mensagem": "Sistema operando normalmente.",
  "resultado": {
    "status": "Healthy",
    "totalDuration": "00:00:00.0100000",
    "entries": {
      "postgres": {
        "status": "Healthy",
        "description": "Postgres respondendo.",
        "duration": "00:00:00.0050000",
        "data": {}
      }
    }
  },
  "erros": []
}
```

Resposta com instabilidade:

```json
{
  "statusHttp": 503,
  "mensagem": "Sistema com instabilidade.",
  "resultado": null,
  "erros": []
}
```

## Mapeamento Dapper

No `Program.cs`, a aplicacao configura:

```csharp
DefaultTypeMap.MatchNamesWithUnderscores = true;
```

Isso permite mapear automaticamente colunas `snake_case` do PostgreSQL para propriedades `PascalCase` do C#.

Exemplos:

| PostgreSQL | C# |
| --- | --- |
| `data_cadastro` | `DataCadastro` |
| `usuario_cadastro` | `UsuarioCadastro` |
| `ultima_alteracao` | `UltimaAlteracao` |
| `usuario_alteracao` | `UsuarioAlteracao` |
| `categoria_pai_id` | `CategoriaPaiId` |
| `unidade_medida_id` | `UnidadeMedidaId` |

Os repositories usam:

- `QueryMultipleAsync` para listagem + total.
- `QuerySingleAsync` para insert com retorno.
- `QuerySingleOrDefaultAsync` para busca e update.
- `ExecuteAsync` para ativacao/inativacao.
- `ExecuteScalarAsync` para verificacoes de existencia.

## Fluxo de cadastro recomendado

Exemplo completo de uso:

1. Criar transporte:

```http
POST /transportes
```

2. Criar categoria:

```http
POST /categorias
```

3. Criar unidade de medida:

```http
POST /unidades-medida
```

4. Criar produto usando os IDs retornados:

```http
POST /produtos
```

5. Consultar produtos:

```http
GET /produtos?pagina=1&tamanhoPagina=20&ativo=true
```

## Exemplos de consultas

### Listar produtos ativos

```http
GET /produtos
```

### Buscar produto por texto

```http
GET /produtos?busca=notebook
```

### Filtrar produtos por categoria

```http
GET /produtos?categoriaId=00000000-0000-0000-0000-000000000000
```

### Filtrar produtos por transporte

```http
GET /produtos?transporteId=00000000-0000-0000-0000-000000000000
```

### Filtrar produtos por unidade de medida

```http
GET /produtos?unidadeMedidaId=00000000-0000-0000-0000-000000000000
```

### Listar subcategorias de uma categoria

```http
GET /categorias?categoriaPaiId=00000000-0000-0000-0000-000000000000
```

## Troubleshooting

### Erro: arquivo `.exe` em uso

Mensagem comum:

```text
The process cannot access the file 'SDI.Back.Template.exe' because it is being used by another process.
```

Causa:

- A API ainda esta rodando em outro terminal ou processo.

Solucao:

```powershell
Get-Process SDI.Back.Template -ErrorAction SilentlyContinue
Stop-Process -Name SDI.Back.Template
```

Depois:

```powershell
dotnet build SDI.Back.Template.slnx
```

### Erro de conexao com PostgreSQL

Verifique:

- PostgreSQL esta rodando.
- Host e porta estao corretos.
- Database existe.
- Usuario e senha estao corretos.
- O script `Scripts/ScriptDeCriacao.sql` foi executado.

Teste:

```powershell
psql -h localhost -p 5432 -U postgres -d sdi_produto
```

### Erro: schema `sdi` nao existe

Causa:

- Script de criacao nao foi executado.

Solucao:

```powershell
psql -h localhost -p 5432 -U postgres -d sdi_produto -f Scripts\ScriptDeCriacao.sql
```

### Erro: funcao `gen_random_uuid` nao existe

Causa:

- Extensao `pgcrypto` nao foi criada.

Solucao:

```sql
CREATE EXTENSION IF NOT EXISTS pgcrypto;
```

Ou execute novamente o script completo.

### Erro 409 ao cadastrar

Causa provavel:

- Campo unico duplicado.

Verifique:

- `transporte.nome`
- `categoria.nome`
- `unidade_medida.nome`
- `unidade_medida.sigla`
- `produto.codigo`

### Erro ao criar produto

Verifique:

- `transporteId` existe e esta ativo.
- `categoriaId` existe e esta ativa.
- `unidadeMedidaId` existe e esta ativa.
- `codigo` nao esta duplicado.

## Evolucoes recomendadas

Possiveis proximos passos:

- Adicionar autenticacao e autorizacao JWT.
- Extrair `usuarioCadastro` e `usuarioAlteracao` automaticamente do usuario autenticado.
- Adicionar testes unitarios para services.
- Adicionar testes de integracao com PostgreSQL em container.
- Criar migrations ou versionamento formal dos scripts.
- Adicionar logs com correlation id.
- Adicionar endpoint detalhado de produto com nomes de categoria, transporte e unidade de medida.
- Implementar soft delete semantico como endpoint `DELETE` que apenas inativa.
- Adicionar filtros avancados de estoque, codigo e periodo de cadastro.
- Adicionar OpenTelemetry para traces distribuidos.
- Adicionar Dockerfile e docker-compose para API + PostgreSQL.

## Checklist rapido para subir do zero

1. Criar banco:

```sql
CREATE DATABASE sdi_produto;
```

2. Executar script:

```powershell
psql -h localhost -p 5432 -U postgres -d sdi_produto -f Scripts\ScriptDeCriacao.sql
```

3. Configurar connection string:

```json
"DefaultConnection": "Host=localhost;Port=5432;Database=sdi_produto;Username=postgres;Password=SUA_SENHA"
```

4. Restaurar pacotes:

```powershell
dotnet restore SDI.Back.Template.slnx
```

5. Compilar:

```powershell
dotnet build SDI.Back.Template.slnx
```

6. Executar:

```powershell
dotnet run --project SDI.Back.Template\SDI.Back.Template.csproj
```

7. Abrir Swagger:

```text
http://localhost:5242/swagger
```

8. Cadastrar nesta ordem:

```text
transportes -> categorias -> unidades-medida -> produtos
```
