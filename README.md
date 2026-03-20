# ServiçosPRO — Assistente Virtual com IA

Chat funcional integrado com IA para atendimento ao cliente da **ServiçosPRO**, empresa de serviços gerais de Mogi das Cruzes — SP.

---

## Tecnologias

- **Backend:** ASP.NET Core (.NET 10) — Minimal API
- **Frontend:** HTML + CSS + JavaScript (single file, sem frameworks)
- **IA:** [Groq API](https://console.groq.com) com modelo `llama-3.3-70b-versatile` (gratuito)

---

## Arquitetura

```
Browser
  │
  │  POST /api/chat  (JSON com histórico de mensagens)
  ▼
ASP.NET Core (localhost:5070)
  │  serve wwwroot/index.html (frontend estático)
  │  recebe POST /api/chat
  │
  │  POST https://api.groq.com/openai/v1/chat/completions
  │  Authorization: Bearer {GROQ_API_KEY}   ← chave fica só no servidor
  ▼
Groq API (LLaMA 3.3 70B)
  │
  └─ resposta normalizada → { content: [{ text: "..." }] }
  ▼
Browser renderiza balão de mensagem
```

**Por que backend?** A chave de API nunca é exposta ao browser. Sem o proxy, qualquer pessoa que inspecionasse o tráfego da página conseguiria sua chave.

---

## Estrutura do Projeto

```
servicospro-chat/
├── wwwroot/
│   └── index.html           # Frontend completo (chat UI)
├── Program.cs               # Minimal API + proxy para Groq
├── ServicosPro.Chat.csproj  # Projeto .NET 10
├── appsettings.json         # Configurações (sem chave — seguro commitar)
├── appsettings.Development.json  # Chave local (gitignored)
└── .gitignore
```

---

## Como obter a chave da Groq (gratuita)

1. Acesse **[console.groq.com](https://console.groq.com)**
2. Faça login com sua conta Google
3. No menu lateral, clique em **API Keys**
4. Clique em **Create API Key**, dê um nome e confirme
5. Copie a chave gerada — ela começa com `gsk_...`

> A Groq oferece um plano gratuito com **14.400 requisições/dia** e **30 req/min** no modelo `llama-3.3-70b-versatile`. Sem necessidade de cartão de crédito.

---

## Como rodar localmente

### Pré-requisitos

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Chave da Groq API (veja seção acima)

### 1. Clone o repositório

```bash
git clone https://github.com/AlessonBarauna/servicospro-chat.git
cd servicospro-chat
```

### 2. Configure a chave

**Opção A — Variável de ambiente (recomendado):**

```cmd
# Windows (cmd)
set Groq__ApiKey=gsk_SUA_CHAVE_AQUI && dotnet run

# Windows (PowerShell)
$env:Groq__ApiKey="gsk_SUA_CHAVE_AQUI"; dotnet run

# Linux / macOS
Groq__ApiKey=gsk_SUA_CHAVE_AQUI dotnet run
```

**Opção B — Arquivo local (nunca commitado):**

Crie o arquivo `appsettings.Development.json` na raiz do projeto:

```json
{
  "Groq": {
    "ApiKey": "gsk_SUA_CHAVE_AQUI"
  }
}
```

Depois rode com o ambiente de desenvolvimento ativo:

```cmd
# Windows (cmd)
set ASPNETCORE_ENVIRONMENT=Development && dotnet run
```

### 3. Acesse o chat

Abra o navegador em **[http://localhost:5070](http://localhost:5070)**

---

## Como funciona o fluxo completo

```
1. Usuário abre http://localhost:5070
      │
      └─ .NET serve wwwroot/index.html

2. Página carrega → mensagem de boas-vindas aparece (local, sem chamada à API)

3. Usuário digita uma mensagem e clica em Enviar
      │
      └─ JavaScript monta o histórico de mensagens e faz:
         POST /api/chat
         Body: { system: "...", messages: [{role, content}, ...] }

4. Program.cs recebe a requisição
      │
      ├─ Valida se Groq:ApiKey está configurada
      ├─ Converte para formato OpenAI (Groq é compatível)
      └─ Chama https://api.groq.com/openai/v1/chat/completions
         com o modelo llama-3.3-70b-versatile

5. Groq retorna a resposta do LLaMA 3.3
      │
      └─ Program.cs extrai o texto e normaliza:
         { content: [{ text: "resposta aqui" }] }

6. JavaScript recebe a resposta e renderiza o balão de mensagem do bot
```

---

## Variáveis de configuração

| Variável | Arquivo | Descrição |
|---|---|---|
| `Groq:ApiKey` | `appsettings.Development.json` ou env var `Groq__ApiKey` | Chave da Groq API |
| `Urls` | `appsettings.json` | Porta do servidor (padrão: `http://localhost:5070`) |

---

## Informações do Assistente (System Prompt)

O assistente está configurado para atender como ServiçosPRO com as seguintes informações:

| Campo | Valor |
|---|---|
| Serviços | Elétrica, hidráulica, pintura, pequenos reparos, instalações |
| Atendimento | Mogi das Cruzes e região (Alto Tietê) |
| Horário | Segunda a sábado, 8h–18h |
| WhatsApp | (11) 96442-1841 |
| Orçamentos | Gratuitos e sem compromisso |
| Pagamento | Pix, dinheiro ou cartão |

Para alterar qualquer informação, edite a constante `SYSTEM_PROMPT` em `wwwroot/index.html`.
