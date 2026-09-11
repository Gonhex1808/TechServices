"""
CrudApi

Esta classe concentra as chamadas HTTP.

Ela NÃO sabe o que é:
    Cliente
    Produto
    Ordem de Serviço

Ela só conhece:
    endpoint
    GET
    POST
    PUT
    DELETE

Exemplo:

    clientes_api = CrudApi(
        API_URL,
        "/api/clientes"
    )

    produtos_api = CrudApi(
        API_URL,
        "/api/produtos"
    )
"""

import requests

from config import API_KEY, REQUEST_TIMEOUT


class CrudApi:
    def __init__(
        self,
        base_url: str,
        endpoint: str,
    ):
        self.base_url = base_url.rstrip("/")
        self.endpoint = endpoint.rstrip("/")

        # ==========================================================
        # SEGURANÇA - API KEY
        # ==========================================================
        # O cabeçalho é criado uma única vez e reutilizado em todas
        # as requisições GET, POST, PUT e DELETE.
        self.headers = {
            "X-API-Key": API_KEY,
        }

    # ==============================================================
    # GET - LISTAR TODOS
    # ==============================================================
    def listar(self):
        response = requests.get(
            self._url(),
            headers=self.headers,  # NOVO: envia a API Key para o backend.
            timeout=REQUEST_TIMEOUT,
        )

        self._verificar_erro(response)

        return response.json()

    # ==============================================================
    # GET - BUSCAR POR ID
    # ==============================================================
    def buscar_por_id(
        self,
        id_registro: int,
    ):
        response = requests.get(
            self._url(id_registro),
            headers=self.headers,  # NOVO: envia a API Key para o backend.
            timeout=REQUEST_TIMEOUT,
        )

        if response.status_code == 404:
            return None

        self._verificar_erro(response)

        return response.json()

    # ==============================================================
    # POST - INSERIR
    # ==============================================================
    def inserir(
        self,
        dados: dict,
    ):
        response = requests.post(
            self._url(),
            json=dados,
            headers=self.headers,  # NOVO: envia a API Key para o backend.
            timeout=REQUEST_TIMEOUT,
        )

        self._verificar_erro(response)

        return self._ler_resposta(response)

    # ==============================================================
    # PUT - ATUALIZAR
    # ==============================================================
    def atualizar(
        self,
        id_registro: int,
        dados: dict,
    ):
        response = requests.put(
            self._url(id_registro),
            json=dados,
            headers=self.headers,  # NOVO: envia a API Key para o backend.
            timeout=REQUEST_TIMEOUT,
        )

        self._verificar_erro(response)

        return self._ler_resposta(response)

    # ==============================================================
    # DELETE - DESATIVAR
    # ==============================================================
    def desativar(
        self,
        id_registro: int,
    ):
        response = requests.delete(
            self._url(id_registro),
            headers=self.headers,  # NOVO: envia a API Key para o backend.
            timeout=REQUEST_TIMEOUT,
        )

        self._verificar_erro(response)

        return self._ler_resposta(response)

    # ==============================================================
    # AUXILIARES
    # ==============================================================

    def _url(
        self,
        id_registro: int | None = None,
    ):
        """
        Monta a URL.

        Sem ID:
            /api/clientes

        Com ID:
            /api/clientes/5
        """

        url = (
            f"{self.base_url}"
            f"{self.endpoint}"
        )

        if id_registro is not None:
            url += f"/{id_registro}"

        return url

    @staticmethod
    def _ler_resposta(response):
        """
        Algumas respostas não possuem JSON.

        Exemplo:
            DELETE pode retornar 204 No Content.
        """

        if not response.content:
            return None

        try:
            return response.json()

        except ValueError:
            return response.text

    @staticmethod
    def _verificar_erro(response):
        """
        Tratamento simples de erros HTTP.
        """

        if response.ok:
            return

        mensagem = None

        try:
            dados = response.json()

            if isinstance(dados, dict):
                mensagem = (
                    dados.get("mensagem")
                    or dados.get("message")
                    or dados.get("detail")
                    or dados.get("title")
                )

        except ValueError:
            mensagem = response.text

        if not mensagem:
            mensagem = (
                f"Erro HTTP "
                f"{response.status_code}"
            )

        raise Exception(mensagem)
