# sdi-front-produtos

Frontend independente para gerenciamento do catalogo do microservico `SDI.Micro.Produto`.

## Como rodar

```powershell
npm install
npm run dev
```

Por padrao, o app usa:

```text
VITE_BASE_PATH=/produtos/
VITE_PRODUTO_API_URL=/api/produtos
VITE_PRODUTO_API_TARGET=https://localhost:7112
VITE_MAIN_FRONT_URL=http://localhost:8080
```

Em producao, mantenha `VITE_BASE_PATH=/produtos/` e `VITE_PRODUTO_API_URL=/api/produtos` para usar o Load Balancer sem IP fixo da VM.

Em desenvolvimento, o navegador chama `/api` no proprio Vite e o Vite encaminha para o backend HTTPS. Isso evita erro de CORS e problemas com o redirecionamento do backend de `http://localhost:5242` para `https://localhost:7112`.

Ao fechar o modal, o app redireciona para `VITE_MAIN_FRONT_URL`.
