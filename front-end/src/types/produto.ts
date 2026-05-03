export type ApiResponse<T> = {
  statusHttp: number;
  mensagem: string;
  resultado: T;
  erros: string[];
};

export type PagedResult<T> = {
  itens: T[];
  pagina: number;
  tamanhoPagina: number;
  total: number;
};

export type Transporte = {
  id: string;
  nome: string;
  descricao?: string | null;
  ativo: boolean;
  dataCadastro: string;
  ultimaAlteracao?: string | null;
};

export type Categoria = {
  id: string;
  categoriaPaiId?: string | null;
  nome: string;
  descricao?: string | null;
  ativo: boolean;
  dataCadastro: string;
  ultimaAlteracao?: string | null;
};

export type UnidadeMedida = {
  id: string;
  nome: string;
  sigla: string;
  descricao?: string | null;
  ativo: boolean;
  dataCadastro: string;
  ultimaAlteracao?: string | null;
};

export type Produto = {
  id: string;
  transporteId: string;
  categoriaId: string;
  unidadeMedidaId: string;
  codigo: string;
  nome: string;
  descricao?: string | null;
  ativo: boolean;
  dataCadastro: string;
  ultimaAlteracao?: string | null;
};

export type StatusFiltro = "ativos" | "inativos" | "todos";
