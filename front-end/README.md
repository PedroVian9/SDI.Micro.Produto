# sdi-front-produtos

Frontend independente para gerenciamento do catálogo do microserviço `SDI.Micro.Produto`.

## Como rodar

```powershell
npm install
npm run dev
```

Por padrão, o app usa:

```text
VITE_PRODUTO_API_URL=/api
VITE_PRODUTO_API_TARGET=https://localhost:7112
VITE_MAIN_FRONT_URL=http://localhost:8080
```

Em desenvolvimento, o navegador chama `/api` no próprio Vite e o Vite encaminha para o backend HTTPS. Isso evita erro de CORS e problemas com o redirecionamento do backend de `http://localhost:5242` para `https://localhost:7112`.

Ao fechar o modal, o app redireciona para `VITE_MAIN_FRONT_URL`.
