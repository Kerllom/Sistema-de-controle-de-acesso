using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace ControleAcesso
{
    // Níveis de acesso em ordem hierárquica (quanto maior o número, mais permissões)
    public enum NivelAcesso
    {
        Visitante = 1,
        Funcionario = 2,
        Admin = 3
    }

    // Representa um funcionário/usuário do sistema
    public class Usuario
    {
        public string Id { get; set; }
        public string Nome { get; set; }
        public string SenhaHash { get; set; } // nunca armazenamos a senha em texto puro
        public NivelAcesso Nivel { get; set; }
    }

    // Representa uma área do edifício e o nível mínimo exigido para entrar
    public class Area
    {
        public string Nome { get; set; }
        public NivelAcesso NivelMinimo { get; set; }
    }

    // Registro de uma tentativa de acesso (auditoria)
    public class RegistroAcesso
    {
        public DateTime DataHora { get; set; }
        public string UsuarioId { get; set; }
        public string Area { get; set; }
        public bool Autorizado { get; set; }

        public override string ToString()
        {
            string status = Autorizado ? "AUTORIZADO" : "NEGADO";
            return $"[{DataHora:dd/MM/yyyy HH:mm:ss}] Usuário '{UsuarioId}' -> Área '{Area}': {status}";
        }
    }

    public class Sistema
    {
        // "Bancos de dados" em memória
        private List<Usuario> usuarios = new List<Usuario>();
        private List<Area> areas = new List<Area>();
        private List<RegistroAcesso> historico = new List<RegistroAcesso>();

        public Sistema()
        {
            // Áreas padrão do edifício
            areas.Add(new Area { Nome = "Recepcao", NivelMinimo = NivelAcesso.Visitante });
            areas.Add(new Area { Nome = "Escritorio", NivelMinimo = NivelAcesso.Funcionario });
            areas.Add(new Area { Nome = "Sala de Servidores", NivelMinimo = NivelAcesso.Admin });

            // Usuário admin padrão (necessário para o primeiro login)
            CadastrarUsuario("admin", "Administrador", "admin123", NivelAcesso.Admin);
        }

        // ---------- Segurança ----------

        // Gera o hash SHA-256 da senha. Simples e suficiente para fins didáticos.
        private string GerarHash(string senha)
        {
            using (SHA256 sha = SHA256.Create())
            {
                byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(senha));
                return Convert.ToBase64String(bytes);
            }
        }

        // ---------- CRUD de usuários ----------

        public bool CadastrarUsuario(string id, string nome, string senha, NivelAcesso nivel)
        {
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(senha))
            {
                Console.WriteLine("ID e senha não podem estar vazios.");
                return false;
            }
            if (usuarios.Any(u => u.Id == id))
            {
                Console.WriteLine($"Já existe um usuário com o ID '{id}'.");
                return false;
            }

            usuarios.Add(new Usuario
            {
                Id = id,
                Nome = nome,
                SenhaHash = GerarHash(senha),
                Nivel = nivel
            });
            Console.WriteLine($"Usuário '{id}' cadastrado com sucesso.");
            return true;
        }

        public bool EditarUsuario(string id, string novoNome, NivelAcesso novoNivel)
        {
            var u = usuarios.FirstOrDefault(x => x.Id == id);
            if (u == null)
            {
                Console.WriteLine($"Usuário '{id}' não encontrado.");
                return false;
            }
            u.Nome = novoNome;
            u.Nivel = novoNivel;
            Console.WriteLine($"Usuário '{id}' atualizado.");
            return true;
        }

        public bool RemoverUsuario(string id)
        {
            var u = usuarios.FirstOrDefault(x => x.Id == id);
            if (u == null)
            {
                Console.WriteLine($"Usuário '{id}' não encontrado.");
                return false;
            }
            usuarios.Remove(u);
            Console.WriteLine($"Usuário '{id}' removido.");
            return true;
        }

        public void ListarUsuarios()
        {
            Console.WriteLine("\n--- Usuários cadastrados ---");
            foreach (var u in usuarios)
                Console.WriteLine($"ID: {u.Id} | Nome: {u.Nome} | Nível: {u.Nivel}");
        }

        // ---------- Autenticação ----------

        // Retorna o usuário autenticado, ou null se as credenciais forem inválidas.
        public Usuario Autenticar(string id, string senha)
        {
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(senha))
                return null;

            var u = usuarios.FirstOrDefault(x => x.Id == id);
            if (u == null) return null;
            return u.SenhaHash == GerarHash(senha) ? u : null;
        }

        // ---------- Controle de acesso ----------

        // Verifica permissão, registra no histórico e retorna o resultado.
        public bool TentarAcesso(Usuario usuario, string nomeArea)
        {
            var area = areas.FirstOrDefault(a => a.Nome.Equals(nomeArea, StringComparison.OrdinalIgnoreCase));
            if (area == null)
            {
                Console.WriteLine($"Área '{nomeArea}' não existe.");
                return false;
            }

            bool autorizado = (int)usuario.Nivel >= (int)area.NivelMinimo;

            historico.Add(new RegistroAcesso
            {
                DataHora = DateTime.Now,
                UsuarioId = usuario.Id,
                Area = area.Nome,
                Autorizado = autorizado
            });

            Console.WriteLine(autorizado
                ? $"Acesso AUTORIZADO à área '{area.Nome}'."
                : $"Acesso NEGADO à área '{area.Nome}'. Nível insuficiente.");
            return autorizado;
        }

        public void ListarAreas()
        {
            Console.WriteLine("\n--- Áreas disponíveis ---");
            foreach (var a in areas)
                Console.WriteLine($"- {a.Nome} (mínimo: {a.NivelMinimo})");
        }

        public void MostrarHistorico()
        {
            Console.WriteLine("\n--- Histórico de acessos ---");
            if (historico.Count == 0)
            {
                Console.WriteLine("(vazio)");
                return;
            }
            foreach (var r in historico)
                Console.WriteLine(r);
        }
    }

    public class Program
    {
        static Sistema sistema = new Sistema();
        static Usuario usuarioLogado = null;

        public static void Main(string[] args)
        {
            // Permite rodar os testes automatizados via: dotnet run -- testes
            if (args.Length > 0 && args[0].ToLower() == "testes")
            {
                ExecutarTestes();
                return;
            }

            Console.WriteLine("=== Sistema de Controle de Acesso ===");
            Console.WriteLine("(Use admin / admin123 para o primeiro login)\n");

            while (true)
            {
                try
                {
                    if (usuarioLogado == null) MenuLogin();
                    else MenuPrincipal();
                }
                catch (Exception ex)
                {
                    // Garante que entradas inválidas não derrubem o sistema
                    Console.WriteLine($"Erro: {ex.Message}");
                }
            }
        }

        static void MenuLogin()
        {
            Console.WriteLine("\n1) Login");
            Console.WriteLine("2) Sair");
            Console.Write("Escolha: ");
            string op = Console.ReadLine();

            if (op == "1")
            {
                Console.Write("ID: ");
                string id = Console.ReadLine();
                Console.Write("Senha: ");
                string senha = Console.ReadLine();

                var u = sistema.Autenticar(id, senha);
                if (u != null)
                {
                    usuarioLogado = u;
                    Console.WriteLine($"Bem-vindo, {u.Nome} ({u.Nivel})!");
                }
                else
                {
                    Console.WriteLine("Credenciais inválidas.");
                }
            }
            else if (op == "2")
            {
                Environment.Exit(0);
            }
        }

        static void MenuPrincipal()
        {
            Console.WriteLine($"\n--- Logado como {usuarioLogado.Id} ({usuarioLogado.Nivel}) ---");
            Console.WriteLine("1) Tentar acessar área");
            Console.WriteLine("2) Listar áreas");
            if (usuarioLogado.Nivel == NivelAcesso.Admin)
            {
                Console.WriteLine("3) Cadastrar usuário");
                Console.WriteLine("4) Editar usuário");
                Console.WriteLine("5) Remover usuário");
                Console.WriteLine("6) Listar usuários");
                Console.WriteLine("7) Ver histórico");
            }
            Console.WriteLine("0) Logout");
            Console.Write("Escolha: ");
            string op = Console.ReadLine();

            switch (op)
            {
                case "1":
                    Console.Write("Nome da área: ");
                    sistema.TentarAcesso(usuarioLogado, Console.ReadLine());
                    break;
                case "2":
                    sistema.ListarAreas();
                    break;
                case "3":
                    if (usuarioLogado.Nivel == NivelAcesso.Admin) CadastrarUsuarioInterativo();
                    break;
                case "4":
                    if (usuarioLogado.Nivel == NivelAcesso.Admin) EditarUsuarioInterativo();
                    break;
                case "5":
                    if (usuarioLogado.Nivel == NivelAcesso.Admin)
                    {
                        Console.Write("ID a remover: ");
                        sistema.RemoverUsuario(Console.ReadLine());
                    }
                    break;
                case "6":
                    if (usuarioLogado.Nivel == NivelAcesso.Admin) sistema.ListarUsuarios();
                    break;
                case "7":
                    if (usuarioLogado.Nivel == NivelAcesso.Admin) sistema.MostrarHistorico();
                    break;
                case "0":
                    usuarioLogado = null;
                    Console.WriteLine("Logout efetuado.");
                    break;
                default:
                    Console.WriteLine("Opção inválida.");
                    break;
            }
        }

        static void CadastrarUsuarioInterativo()
        {
            Console.Write("ID: ");
            string id = Console.ReadLine();
            Console.Write("Nome: ");
            string nome = Console.ReadLine();
            Console.Write("Senha: ");
            string senha = Console.ReadLine();
            Console.Write("Nível (1=Visitante, 2=Funcionario, 3=Admin): ");
            if (!int.TryParse(Console.ReadLine(), out int n) || n < 1 || n > 3)
            {
                Console.WriteLine("Nível inválido.");
                return;
            }
            sistema.CadastrarUsuario(id, nome, senha, (NivelAcesso)n);
        }

        static void EditarUsuarioInterativo()
        {
            Console.Write("ID a editar: ");
            string id = Console.ReadLine();
            Console.Write("Novo nome: ");
            string nome = Console.ReadLine();
            Console.Write("Novo nível (1=Visitante, 2=Funcionario, 3=Admin): ");
            if (!int.TryParse(Console.ReadLine(), out int n) || n < 1 || n > 3)
            {
                Console.WriteLine("Nível inválido.");
                return;
            }
            sistema.EditarUsuario(id, nome, (NivelAcesso)n);
        }

        // ---------- Roteiro de testes ----------
        static void ExecutarTestes()
        {
            Console.WriteLine("=== EXECUÇÃO DOS TESTES ===\n");
            var s = new Sistema();

            // Cenário 1: Login do admin e acesso autorizado a área restrita
            Console.WriteLine("[Teste 1] Admin acessa Sala de Servidores");
            var admin = s.Autenticar("admin", "admin123");
            s.TentarAcesso(admin, "Sala de Servidores");

            // Cenário 2: Visitante tenta acessar área de funcionário (deve negar)
            Console.WriteLine("\n[Teste 2] Visitante tenta acessar Escritorio");
            s.CadastrarUsuario("visit01", "Joao Visitante", "1234", NivelAcesso.Visitante);
            var visitante = s.Autenticar("visit01", "1234");
            s.TentarAcesso(visitante, "Escritorio");

            // Cenário 3: Tentativa de login com senha errada
            Console.WriteLine("\n[Teste 3] Login com senha errada");
            var falho = s.Autenticar("admin", "senhaErrada");
            Console.WriteLine(falho == null ? "Autenticação corretamente rejeitada." : "ERRO: deveria falhar.");

            // Cenário 4: Admin cadastra funcionário, ele loga e acessa escritório
            Console.WriteLine("\n[Teste 4] Cadastro de funcionário e acesso autorizado");
            s.CadastrarUsuario("func01", "Maria Func", "abc123", NivelAcesso.Funcionario);
            var func = s.Autenticar("func01", "abc123");
            s.TentarAcesso(func, "Escritorio");

            // Cenário 5: Admin remove usuário e tentativa de login posterior falha
            Console.WriteLine("\n[Teste 5] Remoção de usuário");
            s.RemoverUsuario("func01");
            var removido = s.Autenticar("func01", "abc123");
            Console.WriteLine(removido == null ? "Usuário removido não consegue mais logar." : "ERRO: ainda autenticou.");

            // Cenário 6 (bônus): Entrada inválida não derruba o sistema
            Console.WriteLine("\n[Teste 6] Entrada inválida (vazia)");
            var vazio = s.Autenticar("", "");
            Console.WriteLine(vazio == null ? "Entrada vazia tratada com segurança." : "ERRO.");

            // Mostra o histórico final
            s.MostrarHistorico();
        }
    }
}