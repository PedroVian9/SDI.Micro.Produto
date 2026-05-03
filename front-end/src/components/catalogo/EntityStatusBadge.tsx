import { Badge } from "@/components/ui/badge";

export default function EntityStatusBadge({ ativo }: { ativo: boolean }) {
  return ativo ? (
    <Badge className="border-transparent bg-emerald-100 text-emerald-700 hover:bg-emerald-100 dark:bg-emerald-500/15 dark:text-emerald-300 dark:hover:bg-emerald-500/20">
      Ativo
    </Badge>
  ) : (
    <Badge variant="secondary" className="bg-muted text-muted-foreground">Inativo</Badge>
  );
}
