import { Button } from "@/components/ui/button";
import { Eye, Pencil, Power, PowerOff } from "lucide-react";
import { ReactNode } from "react";

type Props = {
  ativo: boolean;
  onView: () => void;
  onEdit: () => void;
  onToggleStatus: () => void;
};

export default function RowActions({ ativo, onView, onEdit, onToggleStatus }: Props): ReactNode {
  return (
    <div className="flex items-center justify-end gap-1">
      <Button variant="ghost" size="icon" className="h-8 w-8" onClick={onView} title="Visualizar">
        <Eye className="h-4 w-4" />
      </Button>
      <Button variant="ghost" size="icon" className="h-8 w-8" onClick={onEdit} title="Editar">
        <Pencil className="h-4 w-4" />
      </Button>
      <Button
        variant="ghost"
        size="icon"
        className="h-8 w-8"
        onClick={onToggleStatus}
        title={ativo ? "Inativar" : "Ativar"}
      >
        {ativo ? (
          <PowerOff className="h-4 w-4 text-destructive" />
        ) : (
          <Power className="h-4 w-4 text-emerald-600 dark:text-emerald-300" />
        )}
      </Button>
    </div>
  );
}
