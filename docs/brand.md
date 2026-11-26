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

## Identidade do projeto

O nome público e técnico é **EscalaEasy**: extensão `.escalaeasy`, chaves `escalaeasy-state` e `escalaeasy-recovery`, namespaces e assemblies `EscalaEasy.*`, solução `EscalaEasy.slnx` e projetos/pastas correspondentes. Não há aliases ou migração de armazenamento de uma identidade anterior; o lançamento será feito exclusivamente como EscalaEasy.

Repositório oficial: [github.com/argemon/escalaeasy](https://github.com/argemon/escalaeasy). Nenhum domínio de hospedagem é presumido e esta atualização não publica uma versão.
