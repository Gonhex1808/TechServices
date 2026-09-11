"""
Configuração central do frontend.

Se a porta da API ASP.NET ou a API Key mudar,
alteramos apenas este ficheiro.
"""

API_URL = "http://162.240.171.8:8106"

# ==============================================================
# SEGURANÇA - API KEY
# ==============================================================
# Esta chave será enviada pelo frontend em TODAS as chamadas HTTP.
# O backend compara este valor com a chave configurada no appsettings.json.
#
# IMPORTANTE PARA A AULA:
# Em produção, uma API Key não deve ficar escrita diretamente no código.
# O ideal é usar variáveis de ambiente ou um gestor de segredos.
API_KEY = "TechService-Key-2026"

REQUEST_TIMEOUT = 5
