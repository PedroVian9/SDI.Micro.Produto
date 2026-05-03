import { useEffect, useMemo, useState } from "react";
import { useForm, Controller } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { useQuery, useMutation, useQueryClient, keepPreviousData } from "@tanstack/react-query";
import { toast } from "sonner";
import { Dialog, DialogContent, DialogFooter, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Textarea } from "@/components/ui/textarea";
import { Button } from "@/components/ui/button";
import { Label } from "@/components/ui/label";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import {
  produtosApi, categoriasApi, transportesApi, unidadesMedidaApi, extractApiError,
} from "@/api/produtoApi";
import type { Produto, StatusFiltro } from "@/types/produto";
import EntityTable, { Column } from "./EntityTable";
import EntityStatusBadge from "./EntityStatusBadge";
import EntityDetailsDialog from "./EntityDetailsDialog";
import ConfirmStatusDialog from "./ConfirmStatusDialog";
import RowActions from "./RowActions";
import TabHeader, { statusToAtivo } from "./TabHeader";

const TODOS = "__all__";

const schema = z.object({
  codigo: z.string().min(1, "Informe o código").max(60),
  nome: z.string().min(1, "Informe o nome").max(150),
  descricao: z.string().max(1000).optional().nullable(),
  categoriaId: z.string().uuid("Selecione a categoria"),
  transporteId: z.string().uuid("Selecione o transporte"),
  unidadeMedidaId: z.string().uuid("Selecione a unidade"),
});
type FormData = z.infer<typeof schema>;
const PAGE_SIZE = 20;

export default function ProdutoTab() {
  const qc = useQueryClient();
  const [busca, setBusca] = useState("");
  const [buscaDebounced, setBuscaDebounced] = useState("");
  const [status, setStatus] = useState<StatusFiltro>("ativos");
  const [page, setPage] = useState(1);
  const [categoriaId, setCategoriaId] = useState<string | null>(null);
  const [transporteId, setTransporteId] = useState<string | null>(null);
  const [unidadeId, setUnidadeId] = useState<string | null>(null);

  const [formOpen, setFormOpen] = useState(false);
  const [editing, setEditing] = useState<Produto | null>(null);
  const [viewing, setViewing] = useState<Produto | null>(null);
  const [confirming, setConfirming] = useState<Produto | null>(null);

  useEffect(() => { const t = setTimeout(() => setBuscaDebounced(busca), 350); return () => clearTimeout(t); }, [busca]);
  useEffect(() => { setPage(1); }, [buscaDebounced, status, categoriaId, transporteId, unidadeId]);

  const ativo = statusToAtivo(status);

  const categoriasAtivas = useQuery({
    queryKey: ["categorias", "ativas"],
    queryFn: () => categoriasApi.listar({ pagina: 1, tamanhoPagina: 500, ativo: true }),
  });
  const transportesAtivos = useQuery({
    queryKey: ["transportes", "ativos"],
    queryFn: () => transportesApi.listar({ pagina: 1, tamanhoPagina: 500, ativo: true }),
  });
  const unidadesAtivas = useQuery({
    queryKey: ["unidades-medida", "ativas"],
    queryFn: () => unidadesMedidaApi.listar({ pagina: 1, tamanhoPagina: 500, ativo: true }),
  });

  const catMap = useMemo(() => new Map((categoriasAtivas.data?.itens ?? []).map((c) => [c.id, c.nome])), [categoriasAtivas.data]);
  const traMap = useMemo(() => new Map((transportesAtivos.data?.itens ?? []).map((t) => [t.id, t.nome])), [transportesAtivos.data]);
  const uniMap = useMemo(() => new Map((unidadesAtivas.data?.itens ?? []).map((u) => [u.id, u.sigla])), [unidadesAtivas.data]);
  const uniNomeMap = useMemo(() => new Map((unidadesAtivas.data?.itens ?? []).map((u) => [u.id, u.nome])), [unidadesAtivas.data]);

  const query = useQuery({
    queryKey: ["produtos", { page, buscaDebounced, ativo, categoriaId, transporteId, unidadeId }],
    queryFn: () => produtosApi.listar({
      pagina: page, tamanhoPagina: PAGE_SIZE,
      busca: buscaDebounced || null, ativo,
      categoriaId, transporteId, unidadeMedidaId: unidadeId,
    }),
    placeholderData: keepPreviousData,
  });

  const form = useForm<FormData>({
    resolver: zodResolver(schema),
    defaultValues: { codigo: "", nome: "", descricao: "", categoriaId: "", transporteId: "", unidadeMedidaId: "" },
  });

  function openNovo() {
    setEditing(null);
    form.reset({ codigo: "", nome: "", descricao: "", categoriaId: "", transporteId: "", unidadeMedidaId: "" });
    setFormOpen(true);
  }
  function openEdit(p: Produto) {
    setEditing(p);
    form.reset({
      codigo: p.codigo, nome: p.nome, descricao: p.descricao ?? "",
      categoriaId: p.categoriaId, transporteId: p.transporteId, unidadeMedidaId: p.unidadeMedidaId,
    });
    setFormOpen(true);
  }

  const saveMutation = useMutation({
    mutationFn: async (data: FormData) => {
      const payload = {
        codigo: data.codigo.toUpperCase(),
        nome: data.nome,
        descricao: data.descricao || null,
        categoriaId: data.categoriaId,
        transporteId: data.transporteId,
        unidadeMedidaId: data.unidadeMedidaId,
      };
      if (editing) return produtosApi.atualizar(editing.id, payload);
      return produtosApi.criar(payload);
    },
    onSuccess: () => { toast.success(editing ? "Produto atualizado" : "Produto criado"); setFormOpen(false); qc.invalidateQueries({ queryKey: ["produtos"] }); },
    onError: (err) => toast.error(extractApiError(err, "Erro ao salvar produto")),
  });

  const statusMutation = useMutation({
    mutationFn: async (p: Produto) => p.ativo ? produtosApi.inativar(p.id) : produtosApi.ativar(p.id),
    onSuccess: (_d, p) => { toast.success(p.ativo ? "Produto inativado" : "Produto ativado"); setConfirming(null); qc.invalidateQueries({ queryKey: ["produtos"] }); },
    onError: (err) => toast.error(extractApiError(err, "Erro ao alterar status")),
  });

  const columns: Column<Produto>[] = [
    { key: "codigo", header: "Código", render: (r) => <span className="font-mono uppercase font-medium">{r.codigo}</span> },
    { key: "nome", header: "Nome", render: (r) => <span className="font-medium">{r.nome}</span> },
    { key: "categoria", header: "Categoria", render: (r) => catMap.get(r.categoriaId) ?? <span className="text-muted-foreground">—</span> },
    { key: "transporte", header: "Transporte", render: (r) => traMap.get(r.transporteId) ?? <span className="text-muted-foreground">—</span> },
    { key: "unidade", header: "Unidade", render: (r) => <span className="font-mono uppercase">{uniMap.get(r.unidadeMedidaId) ?? "—"}</span> },
    { key: "status", header: "Status", render: (r) => <EntityStatusBadge ativo={r.ativo} /> },
    { key: "cadastro", header: "Cadastro", render: (r) => new Date(r.dataCadastro).toLocaleDateString("pt-BR") },
  ];

  const error = query.isError ? extractApiError(query.error, "Falha ao carregar produtos") : null;

  return (
    <div className="space-y-4">
      <TabHeader
        title="Produtos"
        description="Itens do catálogo. Cada produto pertence a uma categoria, um transporte e uma unidade de medida."
        busca={busca} onBuscaChange={setBusca}
        status={status} onStatusChange={setStatus}
        onNovo={openNovo}
        onRefresh={() => qc.invalidateQueries({ queryKey: ["produtos"] })}
        extraFilters={
          <>
            <Select value={categoriaId ?? TODOS} onValueChange={(v) => setCategoriaId(v === TODOS ? null : v)}>
              <SelectTrigger className="w-[170px]"><SelectValue placeholder="Categoria" /></SelectTrigger>
              <SelectContent>
                <SelectItem value={TODOS}>Todas categorias</SelectItem>
                {(categoriasAtivas.data?.itens ?? []).map((c) => <SelectItem key={c.id} value={c.id}>{c.nome}</SelectItem>)}
              </SelectContent>
            </Select>
            <Select value={transporteId ?? TODOS} onValueChange={(v) => setTransporteId(v === TODOS ? null : v)}>
              <SelectTrigger className="w-[170px]"><SelectValue placeholder="Transporte" /></SelectTrigger>
              <SelectContent>
                <SelectItem value={TODOS}>Todos transportes</SelectItem>
                {(transportesAtivos.data?.itens ?? []).map((t) => <SelectItem key={t.id} value={t.id}>{t.nome}</SelectItem>)}
              </SelectContent>
            </Select>
            <Select value={unidadeId ?? TODOS} onValueChange={(v) => setUnidadeId(v === TODOS ? null : v)}>
              <SelectTrigger className="w-[150px]"><SelectValue placeholder="Unidade" /></SelectTrigger>
              <SelectContent>
                <SelectItem value={TODOS}>Todas unidades</SelectItem>
                {(unidadesAtivas.data?.itens ?? []).map((u) => <SelectItem key={u.id} value={u.id}>{u.nome} ({u.sigla})</SelectItem>)}
              </SelectContent>
            </Select>
          </>
        }
      />

      <EntityTable
        columns={columns}
        data={query.data?.itens ?? []}
        loading={query.isLoading}
        error={error}
        page={page}
        pageSize={PAGE_SIZE}
        total={query.data?.total ?? 0}
        onPageChange={setPage}
        rowKey={(r) => r.id}
        onRetry={() => query.refetch()}
        actions={(r) => <RowActions ativo={r.ativo} onView={() => setViewing(r)} onEdit={() => openEdit(r)} onToggleStatus={() => setConfirming(r)} />}
      />

      <Dialog open={formOpen} onOpenChange={setFormOpen}>
        <DialogContent className="max-w-2xl">
          <DialogHeader><DialogTitle>{editing ? "Editar produto" : "Novo produto"}</DialogTitle></DialogHeader>
          <form onSubmit={form.handleSubmit((d) => saveMutation.mutate(d))} className="space-y-3">
            <div className="grid grid-cols-1 sm:grid-cols-3 gap-3">
              <div className="space-y-1.5">
                <Label htmlFor="codigo">Código *</Label>
                <Input id="codigo" {...form.register("codigo")} maxLength={60} className="uppercase font-mono" />
                {form.formState.errors.codigo && <p className="text-xs text-destructive">{form.formState.errors.codigo.message}</p>}
              </div>
              <div className="space-y-1.5 sm:col-span-2">
                <Label htmlFor="nome">Nome *</Label>
                <Input id="nome" {...form.register("nome")} maxLength={150} />
                {form.formState.errors.nome && <p className="text-xs text-destructive">{form.formState.errors.nome.message}</p>}
              </div>
            </div>

            <div className="grid grid-cols-1 sm:grid-cols-3 gap-3">
              <div className="space-y-1.5">
                <Label>Categoria *</Label>
                <Controller control={form.control} name="categoriaId" render={({ field }) => (
                  <Select value={field.value || ""} onValueChange={field.onChange}>
                    <SelectTrigger><SelectValue placeholder="Selecione..." /></SelectTrigger>
                    <SelectContent>
                      {(categoriasAtivas.data?.itens ?? []).map((c) => <SelectItem key={c.id} value={c.id}>{c.nome}</SelectItem>)}
                    </SelectContent>
                  </Select>
                )} />
                {form.formState.errors.categoriaId && <p className="text-xs text-destructive">{form.formState.errors.categoriaId.message}</p>}
              </div>
              <div className="space-y-1.5">
                <Label>Transporte *</Label>
                <Controller control={form.control} name="transporteId" render={({ field }) => (
                  <Select value={field.value || ""} onValueChange={field.onChange}>
                    <SelectTrigger><SelectValue placeholder="Selecione..." /></SelectTrigger>
                    <SelectContent>
                      {(transportesAtivos.data?.itens ?? []).map((t) => <SelectItem key={t.id} value={t.id}>{t.nome}</SelectItem>)}
                    </SelectContent>
                  </Select>
                )} />
                {form.formState.errors.transporteId && <p className="text-xs text-destructive">{form.formState.errors.transporteId.message}</p>}
              </div>
              <div className="space-y-1.5">
                <Label>Unidade *</Label>
                <Controller control={form.control} name="unidadeMedidaId" render={({ field }) => (
                  <Select value={field.value || ""} onValueChange={field.onChange}>
                    <SelectTrigger><SelectValue placeholder="Selecione..." /></SelectTrigger>
                    <SelectContent>
                      {(unidadesAtivas.data?.itens ?? []).map((u) => <SelectItem key={u.id} value={u.id}>{u.nome} ({u.sigla})</SelectItem>)}
                    </SelectContent>
                  </Select>
                )} />
                {form.formState.errors.unidadeMedidaId && <p className="text-xs text-destructive">{form.formState.errors.unidadeMedidaId.message}</p>}
              </div>
            </div>

            <div className="space-y-1.5">
              <Label htmlFor="descricao">Descrição</Label>
              <Textarea id="descricao" rows={3} {...form.register("descricao")} maxLength={1000} />
            </div>

            <DialogFooter>
              <Button type="button" variant="outline" onClick={() => setFormOpen(false)}>Cancelar</Button>
              <Button type="submit" disabled={saveMutation.isPending}>{editing ? "Salvar" : "Criar"}</Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>

      <EntityDetailsDialog
        open={!!viewing}
        onOpenChange={(o) => !o && setViewing(null)}
        title={viewing?.nome ?? ""}
        id={viewing?.id}
        ativo={viewing?.ativo}
        dataCadastro={viewing?.dataCadastro}
        ultimaAlteracao={viewing?.ultimaAlteracao}
        fields={[
          { label: "Código", value: <span className="font-mono uppercase">{viewing?.codigo}</span> },
          { label: "Nome", value: viewing?.nome },
          { label: "Categoria", value: viewing && (catMap.get(viewing.categoriaId) ?? viewing.categoriaId) },
          { label: "Transporte", value: viewing && (traMap.get(viewing.transporteId) ?? viewing.transporteId) },
          { label: "Unidade", value: viewing && (uniNomeMap.get(viewing.unidadeMedidaId) ?? viewing.unidadeMedidaId) },
          { label: "Descrição", value: viewing?.descricao },
        ]}
      />

      <ConfirmStatusDialog
        open={!!confirming}
        onOpenChange={(o) => !o && setConfirming(null)}
        ativando={!confirming?.ativo}
        loading={statusMutation.isPending}
        onConfirm={() => confirming && statusMutation.mutate(confirming)}
      />
    </div>
  );
}
