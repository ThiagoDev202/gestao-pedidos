# Diário de uso do Claude

Alimentado pelo `/retro` a cada PR; vira a seção "Como usei o Claude" do README.

## Bootstrap (2026-11-13)
- **Funcionou bem**: ler só a governança do repositório de referência (sem o código) e adaptar; agentes com papéis rígidos; guardrails executáveis em vez de regra só escrita.
- **Corrigido**: o hook `PreToolUse` bloqueava comandos legítimos porque lia o texto inteiro do comando (corpo de heredoc com `--no-verify` e com a regex de coautoria); passou a ignorar corpos de heredoc (`scripts/hooks/strip-heredocs.awk`). Regex inline com parênteses em `check-pr.sh` deu erro de sintaxe e foi movida para variável.
