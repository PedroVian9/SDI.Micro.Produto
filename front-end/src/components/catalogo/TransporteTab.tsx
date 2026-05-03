import { useEffect, useState } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { useQuery, useMutation, useQueryClient, keepPreviousData } from "@tanstack/react-query";
import { toast } from "sonner";
import { Dialog, DialogContent, DialogFooter, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Textarea } from "@/components/ui/textarea";
import { Button } from "@/components/ui/button";
import { Label } from "@/components/ui/label";
import { transportesApi, extractApiError } from "@/api/produtoApi";
import type { Transporte, StatusFiltro } from "@/types/produto";
import EntityTable, { Column } from "./EntityTable";
import EntityStatusBadge from "./EntityStatusBadge";
import EntityDetailsDialog from "./EntityDetailsDialog";
import ConfirmStatusDialog from "./ConfirmStatusDialog";
import RowActions from "./RowActions";
import TabHeader, { statusToAtivo } from "./TabHeader";

const schema = z.object({
  nome: z.string().min(1, "Informe o nome").max(150, "Máximo 150 caracteres"),
  descricao: z.string().max(500, "Máximo 500 caracteres").optional().nullable(),
});
type FormData = z.infer<typeof schema>;

const PAGE_SIZE = 20;

export default function TransporteTab() {
  const qc = useQueryClient();
  const [busca, setBusca] = useState("");
  const [buscaDebounced, setBuscaDebounced] = useState("");
  const [status, setStatus] = useState<StatusFiltro>("ativos");
  const [page, setPage] = useState(1);

  const [formOpen, setFormOpen] = useState(false);
  const [editing, setEditing] = useState<Transporte | null>(null);
  const [viewing, setViewing] = useState<Transporte | null>(null);
  const [confirming, setConfirming] = useState<Transporte | null>(null);

  useEffect(() => {
    const t = setTimeout(() => setBuscaDebounced(busca), 350);
    return () => clearTimeout(t);
  }, [busca]);
  useEffect(() => { setPage(1); }, [buscaDebounced, status]);

  const ativo = statusToAtivo(status);
  const query = useQuery({
    queryKey: ["transportes", { page, buscaDebounced, ativo }],
    queryFn: () => transportesApi.listar({
      pagina: page, tamanhoPagina: PAGE_SIZE,
      busca: buscaDebounced || null, ativo,
    }),
    placeholderData: keepPreviousData,
  });

  const form = useForm<FormData>({
    resolver: zodResolver(schema),
    defaultValues: { nome: "", descricao: "" },
  });

  function openNovo() {
    setEditing(null);
    form.reset({ nome: "", descricao: "" });
    setFormOpen(true);
  }
  function openEdit(t: Transporte) {
    setEditing(t);
    form.reset({ nome: t.nome, descricao: t.descricao ?? "" });
    setFormOpen(true);
  }

  const saveMutation = useMutation({
    mutationFn: async (data: FormData) => {
      const payload = { nome: data.nome, descricao: data.descricao || null };
      if (editing) return transportesApi.atualizar(editing.id, payload);
      return transportesApi.criar(payload);
    },
    onSuccess: () => {
      toast.success(editing ? "Transporte atualizado" : "Transporte criado");
      setFormOpen(false);
      qc.invalidateQueries({ queryKey: ["transportes"] });
    },
    onError: (err) => toast.error(extractApiError(err, "Erro ao salvar transporte")),
  });

  const statusMutation = useMutation({
    mutationFn: async (t: Transporte) =>
      t.ativo ? transportesApi.inativar(t.id) : transportesApi.ativar(t.id),
    onSuccess: (_d, t) => {
      toast.success(t.ativo ? "Transporte inativado" : "Transporte ativado");
      setConfirming(null);
      qc.invalidateQueries({ queryKey: ["transportes"] });
    },
    onError: (err) => toast.error(extractApiError(err, "Erro ao alterar status")),
  });

  const columns: Column<Transporte>[] = [
    { key: "nome", header: "Nome", render: (r) => <span className="font-medium">{r.nome}</span> },
    { key: "descricao", header: "Descrição", render: (r) => <span className="text-muted-foreground">{r.descricao || "—"}</span> },
    { key: "status", header: "Status", render: (r) => <EntityStatusBadge ativo={r.ativo} /> },
    { key: "cadastro", header: "Cadastro", render: (r) => new Date(r.dataCadastro).toLocaleDateString("pt-BR") },
  ];

  const error = query.isError ? extractApiError(query.error, "Falha ao carregar transportes") : null;

  return (
    <div className="space-y-4">
      <TabHeader
        title="Transportes"
        description="Cadastre as modalidades de transporte usadas pelos produtos."
        busca={busca} onBuscaChange={setBusca}
        status={status} onStatusChange={setStatus}
        onNovo={openNovo}
        onRefresh={() => qc.invalidateQueries({ queryKey: ["transportes"] })}
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
        actions={(r) => (
          <RowActions
            ativo={r.ativo}
            onView={() => setViewing(r)}
            onEdit={() => openEdit(r)}
            onToggleStatus={() => setConfirming(r)}
          />
        )}
      />

      <Dialog open={formOpen} onOpenChange={setFormOpen}>
        <DialogContent className="max-w-md">
          <DialogHeader>
            <DialogTitle>{editing ? "Editar transporte" : "Novo transporte"}</DialogTitle>
          </DialogHeader>
          <form onSubmit={form.handleSubmit((d) => saveMutation.mutate(d))} className="space-y-3">
            <div className="space-y-1.5">
              <Label htmlFor="nome">Nome *</Label>
              <Input id="nome" {...form.register("nome")} maxLength={150} />
              {form.formState.errors.nome && <p className="text-xs text-destructive">{form.formState.errors.nome.message}</p>}
            </div>
            <div className="space-y-1.5">
              <Label htmlFor="descricao">Descrição</Label>
              <Textarea id="descricao" rows={3} {...form.register("descricao")} maxLength={500} />
              {form.formState.errors.descricao && <p className="text-xs text-destructive">{form.formState.errors.descricao.message}</p>}
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
