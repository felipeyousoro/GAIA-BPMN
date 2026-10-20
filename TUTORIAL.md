# Tutorial rápido

## 1. Subir API, PostgreSQL e MinIO

Na raiz do projeto:

```powershell
docker compose -f deploy/compose.yaml up --build -d
```

Em uma instalação nova, o PostgreSQL aplica automaticamente a migração inicial. Se já houver um volume antigo, remova-o somente se puder descartar os dados locais:

```powershell
docker compose -f deploy/compose.yaml down -v
```

Teste a API:

```powershell
curl http://localhost:8080/health
```

Resultado esperado: `{"status":"ok"}`.

## 2. Gerar e abrir o desktop

Em outro terminal:

```powershell
cd apps/desktop
npm.cmd install
npm.cmd run build
npm.cmd run electron
```

Para desenvolvimento sem Electron:

```powershell
npm.cmd run dev
```

Abra o endereço mostrado pelo Vite.

## 3. Usar

1. Informe a URL da API, normalmente `http://localhost:8080`.
2. Clique em **Importar BPMN** e selecione `.bpmn` ou `.xml`.
3. Selecione um elemento na lista.
4. Edite os metadados próprios da aplicação.

O XML original não é regravado. A API não deve ser exposta diretamente à internet sem HTTPS, autenticação OIDC e credenciais fortes.

## Comandos úteis

```powershell
docker compose -f deploy/compose.yaml logs -f api
docker compose -f deploy/compose.yaml down
```
