# Tutorial rápido

## 1. Subir API e PostgreSQL

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

1. Na tela de **Login**, caso o servidor não esteja em `http://localhost:8080`, clique em **Configurar servidor API** para informar a URL.
2. Entre com o usuário mestre padrão criado automaticamente no primeiro boot:
   - **Usuário**: `admin`
   - **Senha**: `admin123`
3. Como **Mestre (`Admin`)**:
   - Clique em **+ Importar BPMN** na tela de Processos e selecione um arquivo `.bpmn`.
   - Clique em uma task no diagrama para editar **Input**, **Output**, adicionar/remover anexos e clicar em **Salvar**.
   - Acesse **Configurações (Settings)** na barra lateral para alterar foto de perfil, nome de exibição, senha, URL da API e gerenciar usuários convidados (criar novos usuários, resetar senhas ou remover convidados).
4. Como **Convidado (`Guest`)**:
   - Acesso somente leitura: pode navegar pelos processos importados, inspecionar `Input`/`Output` das tasks e baixar tanto o diagrama BPMN quanto os anexos.
   - Em **Configurações (Settings)**, pode alterar a própria foto de perfil, nome de exibição, senha e URL da API.

O desenho BPMN não é editável. A API preserva extensões existentes e grava apenas os valores GAIA. Para desenvolvimento web, `npm.cmd run dev` usa CORS em `http://localhost:5173`; o Electron usa a origem `null`.

## Comandos úteis

```powershell
docker compose -f deploy/compose.yaml logs -f api
docker compose -f deploy/compose.yaml down
```
