# Marca EscalaEasy

**Sua escala. Sem complicação.**

O conceito **Calendário leve** mantém a geometria original: calendário com abertura lateral e bloco de turno verde. A migração altera o nome e suas cores, sem redesenhar o símbolo.

- **Escala** e calendário: azul-petróleo `#164E63`.
- **Easy** e bloco de turno: verde `#53B58A`.
- Tipografia da marca: **Inter Bold (700), versão 4.1**, incorporada localmente sob SIL Open Font License 1.1.
- No tema escuro, uma base clara preserva as cores oficiais e a legibilidade.
- [Logo horizontal](../frontend/public/brand/logo.svg): texto convertido em curvas Inter Bold, sem depender de fontes instaladas ou rede.
- [Logo no README](images/escalaeasy-logo.png): PNG derivado do SVG, com base branca para leitura nos temas claro e escuro do GitHub. O nome do arquivo identifica a nova marca e evita reutilizar a imagem antiga em cache.
- [Símbolo](../frontend/public/brand/icon.svg) e [favicon](../frontend/public/favicon.svg): geometria original sincronizada com `frontend/src/components/Brand.tsx`.
- Interface e impressão HTML usam `Brand.tsx`, com fonte WOFF2 local em `frontend/public/brand/fonts/`.
- O rodapé do PDF usa a mesma geometria, as duas cores e Inter Bold TTF incorporada. As tabelas continuam com Noto Sans, preservando conteúdo e paginação.

Preserve proporções, fundo transparente e espaço ao redor dos SVGs. Para regenerar as curvas do nome, execute `python tools/generate-brand.py` com `fonttools==4.69.1`; o script usa o Inter Bold versionado no backend. A fonte e sua licença vêm do [release oficial Inter 4.1](https://github.com/rsms/inter/releases/tag/v4.1).

## Compatibilidade e publicação

EscalaEasy é o nome público. Os novos downloads usam `.escalaeasy`; arquivos antigos `.escalalivre` continuam aceitos. As chaves de armazenamento/recuperação, o schema JSON, os namespaces e assemblies `EscalaLivre.*` e os caminhos de projetos continuam válidos. Esses identificadores técnicos preservam arquivos, dados, builds e integrações existentes.

O repositório continua em `https://github.com/argemon/escalalivre`. Esta mudança não registra nem presume domínio, altera hospedagem, renomeia o repositório ou publica uma versão. Evidências históricas e capturas anteriores mantêm a marca da época.
