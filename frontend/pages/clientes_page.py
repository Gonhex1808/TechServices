"""
Configuração da página de Clientes - V2.

A lógica continua concentrada em CrudPage.
Clientes informa somente:
    título
    campo ID
    colunas/campos

V2:
    os mesmos campos são reutilizados pelos formulários de edição e inclusão.
"""

from components.crud_page import CrudPage


def criar_clientes_page(page, api):
    return CrudPage(
        page=page,
        api=api,
        titulo="Clientes",
        id_campo="idCliente",
        colunas=[
            ("ID", "idCliente"),
            ("NOME", "nome"),
            ("TELEFONE", "telefone"),
            ("EMAIL", "email"),
        ],
    )
