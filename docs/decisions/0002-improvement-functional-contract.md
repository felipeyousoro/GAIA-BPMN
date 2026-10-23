# 0002 — Contrato funcional para a evolução do GAIA BPMN

## Status

Aceito para implementação incremental.

## Contexto

O GAIA BPMN atualmente possui uma lista global de processos, autenticação
`Admin`/`Guest`, metadados de tasks e anexos persistidos pela API. A melhoria
introduz uma hierarquia organizacional, metadados para todos os elementos BPMN,
upload independente do salvamento dos campos e impressão segura de PDFs.

O inventário `GAIA-Inventario-LGPD` foi usado como referência local. Seus
padrões reutilizáveis são:

- tela de cadastros com entradas independentes para cada entidade;
- listagens em tabela e cartões com linhas clicáveis;
- página de detalhe para navegar do contexto pai aos registros filhos;
- indicadores visuais para existência de arquivo e outros estados;
- rotas protegidas por autenticação e papel;
- mensagens de erro e validação próximas ao formulário.

Esses padrões serão adaptados ao vocabulário BPMN, sem copiar a estrutura ou
as regras de domínio do inventário LGPD.

## Vocabulário oficial

| Termo | Uso |
| --- | --- |
| Órgão | Unidade organizacional de primeiro nível. |
| Subórgão | Unidade filha de um órgão; contém um ou mais BPMNs. |
| BPMN | Processo importado e seus metadados no GAIA. |
| Task | Elemento executável que possui Entrada, Saída, Recursos necessários, Instruções e anexos. |
| Gateway | Elemento de decisão; possui Descrição quando editável. |
| Evento de início | Evento inicial; possui Descrição quando editável. |
| Evento de fim | Evento final; possui Descrição quando editável. |

Os nomes técnicos de tabelas e propriedades permanecerão em inglês conforme o
código existente; os rótulos da interface usarão os termos acima.

## Decisões de domínio

### Hierarquia e legado

1. Cada BPMN pertence a exatamente um Subórgão.
2. Um Subórgão pertence a exatamente um Órgão e pode conter vários BPMNs.
3. Um BPMN pode ser transferido entre Subórgãos pelo Admin. A transferência
   altera somente o vínculo organizacional e preserva o ID do processo, o
   arquivo, os elementos, os metadados e os anexos.
4. A exclusão de Órgão ou Subórgão com filhos é bloqueada. O fluxo normal é
   inativar o registro; a transferência deve ocorrer antes de uma exclusão
   administrativa futura.
5. Na primeira migração, será criado o contexto `Órgão padrão` →
   `Subórgão padrão` e todos os processos existentes serão vinculados a ele.
   Nenhum ID de processo, elemento ou arquivo será recriado.
6. Rotas antigas de processos continuarão funcionando durante a transição,
   redirecionando a interface para o contexto organizacional.

### Metadados BPMN

1. `Description`, `Resources` e `Instructions` são metadados da aplicação.
2. A extensão `gaia:io` existente continuará sendo usada para Input/Output.
   Os novos campos serão adicionados em uma extensão GAIA versionada somente
   quando a implementação da exportação estiver pronta.
3. Importação e reimportação nunca substituirão metadados GAIA ausentes no XML.
   Um valor somente será atualizado quando a extensão correspondente estiver
   presente e válida.
4. Tasks terão os quatro campos textuais e anexos. Outros elementos suportados
   terão Descrição e anexos conforme a capacidade indicada pelo contrato.
   Elementos não suportados permanecerão somente leitura e exibirão essa
   limitação.

### Arquivos e anexos

1. O upload é confirmado somente depois de o objeto e seus metadados serem
   persistidos. O botão **Salvar** não participa desse fluxo.
2. O limite inicial é 25 MiB por arquivo. São aceitos PDF, PNG, JPEG, SVG,
   DOCX, XLSX e arquivos BPMN; o MIME, a extensão e a assinatura do conteúdo
   devem ser compatíveis. O servidor pode rejeitar um arquivo cujo conteúdo
   não corresponda ao MIME declarado.
3. O nome original é preservado apenas como metadado sanitizado. A chave de
   armazenamento é gerada pelo servidor e nunca é exposta ao cliente.
4. Falha após a gravação do objeto dispara compensação/registro para limpeza;
   o anexo não é retornado como criado. Falha na remoção também é explícita e
   entra na rotina de diagnóstico de órfãos.
5. PDFs oferecem **Imprimir** somente após validação do conteúdo e por uma
   visualização autenticada; **Baixar** permanece disponível.

## Matriz de permissões inicial

| Ação | Admin/Mestre | Guest/Convidado |
| --- | --- | --- |
| Listar órgãos, subórgãos e BPMNs autorizados | Sim | Sim |
| Criar, editar, ativar ou inativar órgão/subórgão | Sim | Não |
| Importar ou criar BPMN | Sim | Não |
| Transferir BPMN entre subórgãos | Sim | Não |
| Ler elementos, metadados e anexos autorizados | Sim | Sim |
| Editar metadados de elementos | Sim | Não |
| Enviar ou remover anexos | Sim | Não |
| Baixar ou imprimir PDF autorizado | Sim | Sim |
| Gerenciar usuários | Sim | Não |

As verificações serão feitas na API pelo vínculo
`anexo → elemento → BPMN → subórgão → órgão`; a interface apenas refletirá o
resultado da autorização. O modelo não cria escopos por órgão nesta primeira
entrega, mas os endpoints serão organizados para permitir essa extensão.

## Mapa de telas

1. **Órgãos**: busca, filtro por ativo/inativo, cadastro e entrada para detalhe.
2. **Detalhe do Órgão**: breadcrumb, resumo e lista de Subórgãos.
3. **Detalhe do Subórgão**: breadcrumb, filtros e lista de BPMNs.
4. **Importar BPMN**: seletor de Órgão/Subórgão obrigatório antes do upload.
5. **Visualizador BPMN**: diagrama, painel discriminado pelo tipo do elemento,
   estado de salvamento e anexos.
6. **Configurações**: usuários e diagnóstico de objetos órfãos restrito ao
   Admin.

## Critérios de aceite derivados

- Todo processo legado aparece no contexto padrão após a migração.
- Um BPMN só pode possuir um `suborgao_id`; um Subórgão pode possuir vários.
- Admin pode transferir um BPMN sem alterar seus IDs ou arquivos.
- Guest não recebe resposta de órgão, subórgão, BPMN, elemento ou anexo fora
  do escopo autorizado.
- Upload confirmado aparece após a requisição e permanece após recarregar a
  tela sem clicar em **Salvar**.
- Reimportação sem extensão GAIA preserva todos os metadados existentes.
- Apenas PDFs validados exibem **Imprimir**.
- Migrações são executadas explicitamente e não são aplicadas automaticamente
  pela API em produção.

## Consequências

As fases seguintes devem começar pela migração organizacional e pelos contratos
de resposta da API. A UI poderá ser implementada sobre breadcrumbs e telas de
detalhe, mantendo a lista global apenas como compatibilidade temporária.
Limites, tipos MIME e política de retenção deverão ser configuráveis antes do
uso em produção; os valores acima são o padrão inicial para desenvolvimento e
homologação.
