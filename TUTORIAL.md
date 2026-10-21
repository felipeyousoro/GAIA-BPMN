# Tutorial rápido

## 1. Subir API, PostgreSQL e MinIO

Na raiz do projeto:

```powershell
docker compose -f deploy/compose.yaml up --build -d
```

Em uma instalação nova, o PostgreSQL aplica automaticamente as migrações inicial e 0002. Se já houver um volume antigo, remova-o somente se puder descartar os dados locais:

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
3. Clique em uma task no diagrama para abrir o painel.
4. Edite **Input** e **Output**, adicione anexos e clique em **Salvar**.
5. Use **Baixar BPMN** para obter o XML com os valores `gaia:input` e `gaia:output`.

O desenho BPMN não é editável. A API preserva extensões existentes e grava apenas os valores GAIA. Para desenvolvimento web, `npm.cmd run dev` usa CORS em `http://localhost:5173`; o Electron usa a origem `null`.

## Comandos úteis

```powershell
docker compose -f deploy/compose.yaml logs -f api
docker compose -f deploy/compose.yaml down
```
