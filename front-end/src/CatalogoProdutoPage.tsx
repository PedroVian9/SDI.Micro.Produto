import { useState } from "react";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { Package, Ruler, Tags, Truck } from "lucide-react";
import ProdutoTab from "@/components/catalogo/ProdutoTab";
import CategoriaTab from "@/components/catalogo/CategoriaTab";
import UnidadeMedidaTab from "@/components/catalogo/UnidadeMedidaTab";
import TransporteTab from "@/components/catalogo/TransporteTab";
import { ThemeToggle } from "@/components/theme-toggle";

const mainFrontUrl = import.meta.env.VITE_MAIN_FRONT_URL?.trim() || "/";

export default function CatalogoProdutoPage() {
  const [open, setOpen] = useState(true);

  function handleOpenChange(nextOpen: boolean) {
    if (nextOpen) {
      setOpen(true);
      return;
    }

    setOpen(false);
    window.location.assign(mainFrontUrl);
  }

  return (
    <div className="min-h-screen bg-[linear-gradient(180deg,hsl(var(--accent))_0%,hsl(var(--background))_42%,hsl(var(--muted))_100%)]">
      <Dialog open={open} onOpenChange={handleOpenChange}>
        <DialogContent
          className="flex h-[95vh] w-[98vw] max-w-7xl flex-col gap-0 overflow-hidden border-0 p-0 shadow-elevated sm:h-[92vh] sm:w-[95vw]"
          onPointerDownOutside={(e) => e.preventDefault()}
          onEscapeKeyDown={(e) => e.preventDefault()}
        >
          <DialogHeader className="shrink-0 border-b bg-card px-4 py-4 sm:px-6">
            <div className="flex items-start justify-between gap-4 pr-10">
              <div className="min-w-0">
                <DialogTitle className="text-xl text-primary sm:text-2xl">Catálogo de Produtos</DialogTitle>
                <DialogDescription className="text-xs sm:text-sm">
                  Gestão de produtos, categorias, unidades de medida e transportes do microserviço SDI.Micro.Produto.
                </DialogDescription>
              </div>
              <div className="shrink-0">
                <ThemeToggle />
              </div>
            </div>
          </DialogHeader>

          <Tabs defaultValue="produtos" className="flex min-h-0 flex-1 flex-col">
            <div className="shrink-0 overflow-x-auto border-b bg-card/95 px-4 pt-3 sm:px-6">
              <TabsList className="h-auto gap-1 bg-muted/60 p-1">
                <TabsTrigger value="produtos" className="gap-2 data-[state=active]:bg-card data-[state=active]:text-primary">
                  <Package className="h-4 w-4" /> Produtos
                </TabsTrigger>
                <TabsTrigger value="categorias" className="gap-2 data-[state=active]:bg-card data-[state=active]:text-primary">
                  <Tags className="h-4 w-4" /> Categorias
                </TabsTrigger>
                <TabsTrigger value="unidades" className="gap-2 data-[state=active]:bg-card data-[state=active]:text-primary">
                  <Ruler className="h-4 w-4" /> Unidades de medida
                </TabsTrigger>
                <TabsTrigger value="transportes" className="gap-2 data-[state=active]:bg-card data-[state=active]:text-primary">
                  <Truck className="h-4 w-4" /> Transportes
                </TabsTrigger>
              </TabsList>
            </div>

            <div className="flex-1 overflow-auto bg-muted/30 p-4 sm:p-6">
              <TabsContent value="produtos" className="mt-0">
                <ProdutoTab />
              </TabsContent>
              <TabsContent value="categorias" className="mt-0">
                <CategoriaTab />
              </TabsContent>
              <TabsContent value="unidades" className="mt-0">
                <UnidadeMedidaTab />
              </TabsContent>
              <TabsContent value="transportes" className="mt-0">
                <TransporteTab />
              </TabsContent>
            </div>
          </Tabs>
        </DialogContent>
      </Dialog>
    </div>
  );
}
