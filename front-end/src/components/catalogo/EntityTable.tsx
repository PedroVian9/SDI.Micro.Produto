import { ReactNode } from "react";
import { AlertCircle, ChevronLeft, ChevronRight, Inbox } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";

export type Column<T> = {
  key: string;
  header: string;
  render: (row: T) => ReactNode;
  className?: string;
};

type Props<T> = {
  columns: Column<T>[];
  data: T[];
  loading?: boolean;
  error?: string | null;
  page: number;
  pageSize: number;
  total: number;
  onPageChange: (p: number) => void;
  rowKey: (row: T) => string;
  actions?: (row: T) => ReactNode;
  onRetry?: () => void;
};

export default function EntityTable<T>({
  columns,
  data,
  loading,
  error,
  page,
  pageSize,
  total,
  onPageChange,
  rowKey,
  actions,
  onRetry,
}: Props<T>) {
  const totalPages = Math.max(1, Math.ceil(total / pageSize));

  return (
    <div className="space-y-3">
      <div className="overflow-hidden rounded-lg bg-card shadow-card ring-1 ring-border/40">
        <Table>
          <TableHeader>
            <TableRow className="bg-accent/60 hover:bg-accent/60">
              {columns.map((c) => (
                <TableHead key={c.key} className={c.className}>
                  {c.header}
                </TableHead>
              ))}
              {actions && <TableHead className="w-[1%] whitespace-nowrap text-right">Ações</TableHead>}
            </TableRow>
          </TableHeader>
          <TableBody>
            {loading ? (
              Array.from({ length: 5 }).map((_, i) => (
                <TableRow key={i}>
                  {columns.map((c) => (
                    <TableCell key={c.key}>
                      <Skeleton className="h-4 w-full max-w-[160px]" />
                    </TableCell>
                  ))}
                  {actions && (
                    <TableCell>
                      <Skeleton className="ml-auto h-8 w-24" />
                    </TableCell>
                  )}
                </TableRow>
              ))
            ) : error ? (
              <TableRow>
                <TableCell colSpan={columns.length + (actions ? 1 : 0)} className="py-12 text-center">
                  <div className="flex flex-col items-center gap-2 text-destructive">
                    <AlertCircle className="h-8 w-8" />
                    <p className="font-medium">Falha ao carregar dados</p>
                    <p className="text-sm text-muted-foreground">{error}</p>
                    {onRetry && (
                      <Button variant="outline" size="sm" onClick={onRetry}>
                        Tentar novamente
                      </Button>
                    )}
                  </div>
                </TableCell>
              </TableRow>
            ) : data.length === 0 ? (
              <TableRow>
                <TableCell colSpan={columns.length + (actions ? 1 : 0)} className="py-12 text-center">
                  <div className="flex flex-col items-center gap-2 text-muted-foreground">
                    <Inbox className="h-8 w-8" />
                    <p className="font-medium">Nenhum registro encontrado</p>
                    <p className="text-sm">Ajuste os filtros ou cadastre um novo item.</p>
                  </div>
                </TableCell>
              </TableRow>
            ) : (
              data.map((row) => (
                <TableRow key={rowKey(row)} className="hover:bg-accent/35">
                  {columns.map((c) => (
                    <TableCell key={c.key} className={c.className}>
                      {c.render(row)}
                    </TableCell>
                  ))}
                  {actions && <TableCell className="text-right">{actions(row)}</TableCell>}
                </TableRow>
              ))
            )}
          </TableBody>
        </Table>
      </div>

      <div className="flex flex-col items-start justify-between gap-2 text-sm text-muted-foreground sm:flex-row sm:items-center">
        <div>
          {total > 0 ? (
            <>
              Exibindo página <strong>{page}</strong> de <strong>{totalPages}</strong> · {total} registro(s)
            </>
          ) : (
            <>0 registros</>
          )}
        </div>
        <div className="flex items-center gap-1">
          <Button variant="outline" size="sm" disabled={page <= 1 || loading} onClick={() => onPageChange(page - 1)}>
            <ChevronLeft className="h-4 w-4" /> Anterior
          </Button>
          <Button
            variant="outline"
            size="sm"
            disabled={page >= totalPages || loading}
            onClick={() => onPageChange(page + 1)}
          >
            Próxima <ChevronRight className="h-4 w-4" />
          </Button>
        </div>
      </div>
    </div>
  );
}
