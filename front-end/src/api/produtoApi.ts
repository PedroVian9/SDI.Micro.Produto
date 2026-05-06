import axios, { AxiosError } from "axios";
import type {
  ApiResponse,
  Categoria,
  PagedResult,
  Produto,
  Transporte,
  UnidadeMedida,
} from "@/types/produto";

const baseURL =
  (import.meta.env.VITE_PRODUTO_API_URL as string | undefined) ??
  "/api/produtos";

export const produtoHttp = axios.create({
  baseURL,
  headers: { "Content-Type": "application/json" },
});

export function extractApiError(err: unknown, fallback = "Erro inesperado"): string {
  if (err instanceof AxiosError) {
    const data = err.response?.data as ApiResponse<unknown> | undefined;
    if (data?.erros && data.erros.length > 0) return data.erros.join(", ");
    if (data?.mensagem) return data.mensagem;
    return err.message || fallback;
  }
  if (err instanceof Error) return err.message;
  return fallback;
}

type ListarParams = {
  pagina?: number;
  tamanhoPagina?: number;
  ativo?: boolean | null;
  busca?: string | null;
};

function buildParams(params: Record<string, unknown>) {
  const out: Record<string, unknown> = {};
  for (const [k, v] of Object.entries(params)) {
    if (v === undefined || v === null || v === "") continue;
    out[k] = v;
  }
  return out;
}

async function unwrap<T>(p: Promise<{ data: ApiResponse<T> }>): Promise<T> {
  const res = await p;
  return res.data.resultado;
}

// ===== Transportes =====
export const transportesApi = {
  listar: (params: ListarParams) =>
    unwrap<PagedResult<Transporte>>(
      produtoHttp.get("/transportes", { params: buildParams(params) }),
    ),
  obter: (id: string) =>
    unwrap<Transporte>(produtoHttp.get(`/transportes/${id}`)),
  criar: (input: { nome: string; descricao?: string | null }) =>
    unwrap<Transporte>(
      produtoHttp.post("/transportes", { ...input, usuarioCadastro: null }),
    ),
  atualizar: (id: string, input: { nome: string; descricao?: string | null }) =>
    unwrap<Transporte>(
      produtoHttp.put(`/transportes/${id}`, { ...input, usuarioAlteracao: null }),
    ),
  ativar: (id: string) => produtoHttp.patch(`/transportes/${id}/ativar`),
  inativar: (id: string) => produtoHttp.patch(`/transportes/${id}/inativar`),
};

// ===== Categorias =====
export const categoriasApi = {
  listar: (params: ListarParams & { categoriaPaiId?: string | null }) =>
    unwrap<PagedResult<Categoria>>(
      produtoHttp.get("/categorias", { params: buildParams(params) }),
    ),
  obter: (id: string) => unwrap<Categoria>(produtoHttp.get(`/categorias/${id}`)),
  criar: (input: { categoriaPaiId?: string | null; nome: string; descricao?: string | null }) =>
    unwrap<Categoria>(
      produtoHttp.post("/categorias", { ...input, usuarioCadastro: null }),
    ),
  atualizar: (
    id: string,
    input: { categoriaPaiId?: string | null; nome: string; descricao?: string | null },
  ) =>
    unwrap<Categoria>(
      produtoHttp.put(`/categorias/${id}`, { ...input, usuarioAlteracao: null }),
    ),
  ativar: (id: string) => produtoHttp.patch(`/categorias/${id}/ativar`),
  inativar: (id: string) => produtoHttp.patch(`/categorias/${id}/inativar`),
};

// ===== Unidades de Medida =====
export const unidadesMedidaApi = {
  listar: (params: ListarParams) =>
    unwrap<PagedResult<UnidadeMedida>>(
      produtoHttp.get("/unidades-medida", { params: buildParams(params) }),
    ),
  obter: (id: string) =>
    unwrap<UnidadeMedida>(produtoHttp.get(`/unidades-medida/${id}`)),
  criar: (input: { nome: string; sigla: string; descricao?: string | null }) =>
    unwrap<UnidadeMedida>(
      produtoHttp.post("/unidades-medida", { ...input, usuarioCadastro: null }),
    ),
  atualizar: (
    id: string,
    input: { nome: string; sigla: string; descricao?: string | null },
  ) =>
    unwrap<UnidadeMedida>(
      produtoHttp.put(`/unidades-medida/${id}`, { ...input, usuarioAlteracao: null }),
    ),
  ativar: (id: string) => produtoHttp.patch(`/unidades-medida/${id}/ativar`),
  inativar: (id: string) => produtoHttp.patch(`/unidades-medida/${id}/inativar`),
};

// ===== Produtos =====
export const produtosApi = {
  listar: (
    params: ListarParams & {
      categoriaId?: string | null;
      transporteId?: string | null;
      unidadeMedidaId?: string | null;
    },
  ) =>
    unwrap<PagedResult<Produto>>(
      produtoHttp.get("/produtos", { params: buildParams(params) }),
    ),
  obter: (id: string) => unwrap<Produto>(produtoHttp.get(`/produtos/${id}`)),
  criar: (input: {
    transporteId: string;
    categoriaId: string;
    unidadeMedidaId: string;
    codigo: string;
    nome: string;
    descricao?: string | null;
  }) =>
    unwrap<Produto>(
      produtoHttp.post("/produtos", { ...input, usuarioCadastro: null }),
    ),
  atualizar: (
    id: string,
    input: {
      transporteId: string;
      categoriaId: string;
      unidadeMedidaId: string;
      codigo: string;
      nome: string;
      descricao?: string | null;
    },
  ) =>
    unwrap<Produto>(
      produtoHttp.put(`/produtos/${id}`, { ...input, usuarioAlteracao: null }),
    ),
  ativar: (id: string) => produtoHttp.patch(`/produtos/${id}/ativar`),
  inativar: (id: string) => produtoHttp.patch(`/produtos/${id}/inativar`),
};
