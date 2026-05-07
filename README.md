# Sistema de Controle de Acesso

Sistema simples de controle de acesso a áreas de um edifício, desenvolvido em C# (.NET 8) como aplicação de console. Gerencia usuários, permissões e mantém histórico de tentativas de acesso.

## Funcionalidades

- Autenticação por ID e senha (senhas armazenadas com hash SHA-256, nunca em texto puro)
- Três níveis hierárquicos de acesso: **Visitante**, **Funcionário** e **Administrador**
- Validação de permissão antes de liberar qualquer área
- Registro automático de todas as tentativas de acesso (autorizadas e negadas) com data, hora, usuário e resultado
- CRUD de usuários restrito ao administrador
- Consulta do histórico completo de acessos (apenas admin)
- Tratamento de entradas inválidas sem encerramento abrupto

## Requisitos

- [.NET SDK 8.0](https://dotnet.microsoft.com/download) ou superior

## Como executar

Clone o repositório e crie um projeto console:

```bash
git clone <url-do-repositorio>
cd <pasta-do-projeto>
dotnet new console -n ControleAcesso
```

Substitua o `Program.cs` gerado pelo arquivo `ControleAcesso.cs` deste repositório e rode:

```bash
dotnet run
```

Para executar o roteiro de testes automatizados:

```bash
dotnet run -- testes
```

## Login padrão

O sistema cria um administrador padrão na primeira execução:

- **ID:** `admin`
- **Senha:** `admin123`

## Estrutura do código

Todo o sistema está em um único arquivo (`ControleAcesso.cs`) para facilitar a leitura, organizado nas seguintes seções:

| Componente | Descrição |
|------------|-----------|
| `enum NivelAcesso` | Define os três níveis hierárquicos (1=Visitante, 2=Funcionario, 3=Admin) |
| `class Usuario` | Representa um usuário cadastrado |
| `class Area` | Representa uma área do edifício e o nível mínimo exigido |
| `class RegistroAcesso` | Representa um registro de auditoria |
| `class Sistema` | Lógica principal: CRUD, autenticação, validação e histórico |
| `class Program` | Interface de menu via console e roteiro de testes |

## Áreas pré-cadastradas

| Área | Nível mínimo |
|------|--------------|
| Recepção | Visitante |
| Escritório | Funcionário |
| Sala de Servidores | Admin |

## Decisões de projeto

- **Persistência em memória:** os dados não são gravados em arquivo, simplificando o código. Para uso real, basta adicionar serialização via `System.Text.Json`.
- **Hash SHA-256:** suficiente para fins didáticos. Em produção, recomenda-se PBKDF2, bcrypt ou Argon2 com salt único por usuário.
- **Permissões hierárquicas:** o uso de enum numérico permite comparação direta (`usuario.Nivel >= area.NivelMinimo`), mais simples que listas de controle de acesso (ACL).
- **Arquivo único:** facilita análise e correção; em projetos maiores o ideal seria separar em pastas `Models/` e `Services/`.

## Roteiro de testes

O comando `dotnet run -- testes` executa seis cenários:

1. Admin acessa Sala de Servidores → **autorizado**
2. Visitante tenta acessar Escritório → **negado**
3. Login com senha errada → **rejeitado**
4. Admin cadastra funcionário e ele acessa Escritório → **autorizado**
5. Admin remove usuário e nova tentativa de login → **falha**
6. Entrada vazia no login → **tratada com segurança**

Ao final, o histórico completo de acessos é exibido.

## Possíveis melhorias futuras

- Persistência em arquivo JSON ou banco de dados (SQLite com Entity Framework)
- Migração do hash para PBKDF2 ou bcrypt com salt
- Bloqueio de conta após N tentativas de login falhas
- Autenticação em dois fatores (2FA)
- Interface gráfica (WPF, Avalonia) ou web (ASP.NET)
- Exportação de relatórios de auditoria em CSV ou PDF
- Logs em arquivo separado para conformidade com LGPD
