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
import { categoriasApi, extractApiError } from "@/api/produtoApi";
import type { Categoria, StatusFiltro } from "@/types/produto";
import EntityTable, { Column } from "./EntityTable";
import EntityStatusBadge from "./EntityStatusBadge";
import EntityDetailsDialog from "./EntityDetailsDialog";
import ConfirmStatusDialog from "./ConfirmStatusDialog";
import RowActions from "./RowActions";
import TabHeader, { statusToAtivo } from "./TabHeader";

const SEM_PAI = "__none__";

const schema = z.object({
  nome: z.string().min(1, "Informe o nome").max(150),
  descricao: z.string().max(500).optional().nullable(),
  categoriaPaiId: z.string().optional().nullable(),
});
type FormData = z.infer<typeof schema>;
const PAGE_SIZE = 20;

export default function CategoriaTab() {
  const qc = useQueryClient();
  const [busca, setBusca] = useState("");
  const [buscaDebounced, setBuscaDebounced] = useState("");
  const [status, setStatus] = useState<StatusFiltro>("ativos");
  const [page, setPage] = useState(1);
  const [formOpen, setFormOpen] = useState(false);
  const [editing, setEditing] = useState<Categoria | null>(null);
  const [viewing, setViewing] = useState<Categoria | null>(null);
  const [confirming, setConfirming] = useState<Categoria | null>(null);

  useEffect(() => { const t = setTimeout(() => setBuscaDebounced(busca), 350); return () => clearTimeout(t); }, [busca]);
  useEffect(() => { setPage(1); }, [buscaDebounced, status]);

  const ativo = statusToAtivo(status);
  const query = useQuery({
    queryKey: ["categorias", { page, buscaDebounced, ativo }],
    queryFn: () => categoriasApi.listar({ pagina: page, tamanhoPagina: PAGE_SIZE, busca: buscaDebounced || null, ativo }),
    placeholderData: keepPreviousData,
  });

  // Categorias pais (ativas) para o select
  const paisQuery = useQuery({
    queryKey: ["categorias", "ativas-todas"],
    queryFn: () => categoriasApi.listar({ pagina: 1, tamanhoPagina: 500, ativo: true }),
  });
  const paiMap = useMemo(() => {
    const m = new Map<string, string>();
    paisQuery.data?.itens.forEach((c) => m.set(c.id, c.nome));
    query.data?.itens.forEach((c) => { if (!m.has(c.id)) m.set(c.id, c.nome); });
    return m;
  }, [paisQuery.data, query.data]);

  const form = useForm<FormData>({
    resolver: zodResolver(schema),
    defaultValues: { nome: "", descricao: "", categoriaPaiId: null },
  });

  function openNovo() { setEditing(null); form.reset({ nome: "", descricao: "", categoriaPaiId: null }); setFormOpen(true); }
  function openEdit(c: Categoria) { setEditing(c); form.reset({ nome: c.nome, descricao: c.descricao ?? "", categoriaPaiId: c.categoriaPaiId ?? null }); setFormOpen(true); }

  const saveMutation = useMutation({
    mutationFn: async (data: FormData) => {
      const payload = {
        nome: data.nome,
        descricao: data.descricao || null,
        categoriaPaiId: data.categoriaPaiId || null,
      };
      if (editing) return categoriasApi.atualizar(editing.id, payload);
      return categoriasApi.criar(payload);
    },
    onSuccess: () => { toast.success(editing ? "Categoria atualizada" : "Categoria criada"); setFormOpen(false); qc.invalidateQueries({ queryKey: ["categorias"] }); },
    onError: (err) => toast.error(extractApiError(err, "Erro ao salvar categoria")),
  });

  const statusMutation = useMutation({
    mutationFn: async (c: Categoria) => c.ativo ? categoriasApi.inativar(c.id) : categoriasApi.ativar(c.id),
    onSuccess: (_d, c) => { toast.success(c.ativo ? "Categoria inativada" : "Categoria ativada"); setConfirming(null); qc.invalidateQueries({ queryKey: ["categorias"] }); },
    onError: (err) => toast.error(extractApiError(err, "Erro ao alterar status")),
  });

  const columns: Column<Categoria>[] = [
    { key: "nome", header: "Nome", render: (r) => <span className="font-medium">{r.nome}</span> },
    { key: "pai", header: "Categoria pai", render: (r) => r.categoriaPaiId ? (paiMap.get(r.categoriaPaiId) ?? "—") : <span className="text-muted-foreground">—</span> },
    { key: "descricao", header: "Descrição", render: (r) => <span className="text-muted-foreground">{r.descricao || "—"}</span> },
    { key: "status", header: "Status", render: (r) => <EntityStatusBadge ativo={r.ativo} /> },
    { key: "cadastro", header: "Cadastro", render: (r) => new Date(r.dataCadastro).toLocaleDateString("pt-BR") },
  ];

  const error = query.isError ? extractApiError(query.error, "Falha ao carregar categorias") : null;
  const paisOptions = (paisQuery.data?.itens ?? []).filter((c) => !editing || c.id !== editing.id);

  return (
    <div className="space-y-4">
      <TabHeader
        title="Categorias"
        description="Organize os produtos em categorias hierárquicas."
        busca={busca} onBuscaChange={setBusca}
        status={status} onStatusChange={setStatus}
        onNovo={openNovo}
        onRefresh={() => qc.invalidateQueries({ queryKey: ["categorias"] })}
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
        <DialogContent className="max-w-md">
          <DialogHeader><DialogTitle>{editing ? "Editar categoria" : "Nova categoria"}</DialogTitle></DialogHeader>
          <form onSubmit={form.handleSubmit((d) => saveMutation.mutate(d))} className="space-y-3">
            <div className="space-y-1.5">
              <Label htmlFor="nome">Nome *</Label>
              <Input id="nome" {...form.register("nome")} maxLength={150} />
              {form.formState.errors.nome && <p className="text-xs text-destructive">{form.formState.errors.nome.message}</p>}
            </div>
            <div className="space-y-1.5">
              <Label>Categoria pai</Label>
              <Controller
                control={form.control}
                name="categoriaPaiId"
                render={({ field }) => (
                  <Select
                    value={field.value ?? SEM_PAI}
                    onValueChange={(v) => field.onChange(v === SEM_PAI ? null : v)}
                  >
                    <SelectTrigger><SelectValue placeholder="Sem categoria pai" /></SelectTrigger>
                    <SelectContent>
                      <SelectItem value={SEM_PAI}>Sem categoria pai</SelectItem>
                      {paisOptions.map((c) => <SelectItem key={c.id} value={c.id}>{c.nome}</SelectItem>)}
                    </SelectContent>
                  </Select>
                )}
              />
            </div>
            <div className="space-y-1.5">
              <Label htmlFor="descricao">Descrição</Label>
              <Textarea id="descricao" rows={3} {...form.register("descricao")} maxLength={500} />
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
          { label: "Nome", value: viewing?.nome },
          { label: "Categoria pai", value: viewing?.categoriaPaiId ? paiMap.get(viewing.categoriaPaiId) : "—" },
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
