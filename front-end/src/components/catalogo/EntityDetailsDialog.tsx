import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogDescription } from "@/components/ui/dialog";
import EntityStatusBadge from "./EntityStatusBadge";
import { ReactNode } from "react";

function formatDate(value?: string | null) {
  if (!value) return "—";
  try {
    return new Date(value).toLocaleString("pt-BR");
  } catch {
    return value;
  }
}

type Field = { label: string; value: ReactNode };

type Props = {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  title: string;
  id?: string;
  ativo?: boolean;
  dataCadastro?: string;
  ultimaAlteracao?: string | null;
  fields: Field[];
};

export default function EntityDetailsDialog({ open, onOpenChange, title, id, ativo, dataCadastro, ultimaAlteracao, fields }: Props) {
  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-lg">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-3">
            {title}
            {ativo !== undefined && <EntityStatusBadge ativo={ativo} />}
          </DialogTitle>
          {id && <DialogDescription className="font-mono text-xs">ID: {id}</DialogDescription>}
        </DialogHeader>

        <div className="space-y-3 text-sm">
          {fields.map((f, i) => (
            <div key={i} className="grid grid-cols-3 gap-2">
              <div className="text-muted-foreground">{f.label}</div>
              <div className="col-span-2 break-words">{f.value || "—"}</div>
            </div>
          ))}
          <div className="border-t pt-3 grid grid-cols-3 gap-2">
            <div className="text-muted-foreground">Cadastro</div>
            <div className="col-span-2">{formatDate(dataCadastro)}</div>
          </div>
          <div className="grid grid-cols-3 gap-2">
            <div className="text-muted-foreground">Última alteração</div>
            <div className="col-span-2">{formatDate(ultimaAlteracao)}</div>
          </div>
        </div>
      </DialogContent>
    </Dialog>
  );
}
