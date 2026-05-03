import { ReactNode } from "react";
import { Plus, RefreshCw, Search } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import type { StatusFiltro } from "@/types/produto";

type Props = {
  title: string;
  description: string;
  busca: string;
  onBuscaChange: (v: string) => void;
  status: StatusFiltro;
  onStatusChange: (v: StatusFiltro) => void;
  onNovo: () => void;
  onRefresh: () => void;
  extraFilters?: ReactNode;
};

export default function TabHeader({
  title,
  description,
  busca,
  onBuscaChange,
  status,
  onStatusChange,
  onNovo,
  onRefresh,
  extraFilters,
}: Props) {
  return (
    <div className="space-y-4 rounded-lg bg-card px-4 py-4 shadow-card ring-1 ring-border/30">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
        <div>
          <h2 className="text-lg font-semibold leading-tight text-primary">{title}</h2>
          <p className="text-sm text-muted-foreground">{description}</p>
        </div>
        <Button onClick={onNovo} className="w-full shadow-sm sm:w-auto">
          <Plus className="h-4 w-4" /> Novo
        </Button>
      </div>

      <div className="flex flex-col gap-2 md:flex-row md:items-center">
        <div className="relative min-w-0 flex-1">
          <Search className="absolute left-2.5 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
          <Input
            value={busca}
            onChange={(e) => onBuscaChange(e.target.value)}
            placeholder="Buscar..."
            className="bg-background pl-8"
          />
        </div>
        <div className="flex flex-wrap gap-2">
          {extraFilters}
          <Select value={status} onValueChange={(v) => onStatusChange(v as StatusFiltro)}>
            <SelectTrigger className="w-[150px] bg-background">
              <SelectValue placeholder="Status" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="ativos">Ativos</SelectItem>
              <SelectItem value="inativos">Inativos</SelectItem>
              <SelectItem value="todos">Todos</SelectItem>
            </SelectContent>
          </Select>
          <Button variant="outline" size="icon" onClick={onRefresh} title="Atualizar">
            <RefreshCw className="h-4 w-4" />
          </Button>
        </div>
      </div>
    </div>
  );
}

export function statusToAtivo(s: StatusFiltro): boolean | null {
  if (s === "ativos") return true;
  if (s === "inativos") return false;
  return null;
}
