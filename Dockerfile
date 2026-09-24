# syntax=docker/dockerfile:1

# Spec 008 / T-OPS-1 - imagem única do MVP LoLCoach (API .NET 10 + SPA Angular)
# para a Railway, topology de 1 serviço (spec.md R1, design.md Decisão A/B).
#
# Características exigidas e onde são atendidas:
#   R1.2 multi-stage: Node e SDK .NET existem apenas nos estágios de build.
#   R1.3 usuário não-root: estágio final roda como usuário `app` (base aspnet).
#   R1.4 nenhum secret em ARG/ENV de build nem em camadas da imagem.
#   R2.1 porta via PORT com fallback 8080, no ENTRYPOINT em formato shell.
#   R4.1 bundle Angular publicado em wwwroot ANTES do `dotnet publish`.
#
# Build local (a partir da raiz do repositório):
#   docker build -t lolcoach:local .
# Smoke local:
#   docker run --rm -e ASPNETCORE_ENVIRONMENT=Production -p 8080:8080 lolcoach:local

# ---------------------------------------------------------------------------
# Estágio 1 - build do bundle Angular (Node existe SOMENTE aqui)
# ---------------------------------------------------------------------------
FROM node:24-alpine AS spa

WORKDIR /src/frontend

# Camada de dependências primeiro: cache invalidado apenas quando o lockfile muda.
# `npm ci` é determinístico e exige o package-lock.json versionado.
COPY frontend/package.json frontend/package-lock.json ./
RUN npm ci --include=dev

COPY frontend/ ./

# angular.json define defaultConfiguration=production; a flag explícita mantém o
# build reprodutível mesmo se o default do projeto mudar.
# Saída esperada: dist/frontend/browser (outputHashing: all).
RUN npm run build -- --configuration production

# ---------------------------------------------------------------------------
# Estágio 2 - restore + publish da API, com o SPA já dentro de wwwroot
# ---------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS publish

WORKDIR /src

COPY backend/ ./backend/

# O bundle precisa estar em wwwroot ANTES do publish: assim o Web SDK o inclui
# no artefato publicado sem <Content Include> extra.
COPY --from=spa /src/frontend/dist/frontend/browser/ ./backend/src/LoLCoach.Api/wwwroot/

RUN dotnet restore ./backend/src/LoLCoach.Api/LoLCoach.Api.csproj
RUN dotnet publish ./backend/src/LoLCoach.Api/LoLCoach.Api.csproj \
        --configuration Release \
        --no-restore \
        --output /app/publish \
        /p:UseAppHost=false

# ---------------------------------------------------------------------------
# Estágio 3 - runtime (sem SDK, sem Node, sem código-fonte)
# ---------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

WORKDIR /app

# --chown garante que o usuário não-root possa ler o artefato publicado.
COPY --from=publish --chown=app:app /app/publish ./

# Fallback local apenas: em produção a Railway injeta PORT e usa o mesmo valor
# para roteamento e healthcheck (R2.1). Nenhum valor sensível é definido aqui.
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

# Usuário não-root já presente na imagem base (R1.3).
USER app

# Formato shell para expandir a variável PORT em runtime; `exec` mantém o dotnet
# como PID 1 e preserva o encaminhamento de sinais (SIGTERM no redeploy).
ENTRYPOINT ["sh", "-c", "export ASPNETCORE_HTTP_PORTS=\"${PORT:-8080}\" && exec dotnet LoLCoach.Api.dll"]
