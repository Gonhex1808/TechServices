"""
TechService Frontend - V2 IEFP

Objetivo da V2:
    - manter tudo o que já funciona na V1;
    - acrescentar o botão Novo Cliente;
    - abrir formulário em branco;
    - enviar POST /api/clientes;
    - atualizar a lista após o cadastro.

Regra:
    main.py continua pequeno.

Executar:
    python main.py
"""

import flet as ft

from config import API_URL
from crud_api import CrudApi
from menu import AppMenu
from pages.clientes_page import criar_clientes_page


def main(page: ft.Page):
    # ==============================================================
    # V2 - mantém o TEMA CLARO da V1
    # ==============================================================
    # Na V0 utilizávamos ThemeMode.DARK e fundo escuro.
    # Na V1 usamos uma tela branca, conforme o protótipo da aula.
    page.title = "TechService - Clientes"
    page.theme_mode = ft.ThemeMode.LIGHT
    page.padding = 0
    page.spacing = 0
    page.bgcolor = "#FFFFFF"

    # Área que recebe a página escolhida no menu.
    conteudo = ft.Container(
        expand=True,
        bgcolor="#FFFFFF",
    )

    # ==============================================================
    # API DE CLIENTES
    # ==============================================================
    # CrudApi continua igual à V0.
    # A interface chama os métodos que já existiam:
    #   listar()
    #   buscar_por_id()
    #   atualizar()
    #   desativar()\n    # V2 acrescenta:\n    #   inserir()
    clientes_api = CrudApi(
        base_url=API_URL,
        endpoint="/api/clientes",
    )

    # ==============================================================
    # NAVEGAÇÃO
    # ==============================================================
    def abrir_clientes(e=None):
        pagina = criar_clientes_page(
            page=page,
            api=clientes_api,
        )

        conteudo.content = pagina.build()
        page.update()

        # Depois de montar a tela, busca os clientes na API.
        pagina.carregar()

    def pagina_futura(titulo):
        """Página temporária para módulos das próximas versões."""

        def abrir(e=None):
            conteudo.content = ft.Container(
                padding=30,
                bgcolor="#FFFFFF",
                content=ft.Column(
                    controls=[
                        ft.Text(
                            titulo,
                            size=24,
                            weight=ft.FontWeight.BOLD,
                            color="#111827",
                        ),
                        ft.Text(
                            "Módulo será desenvolvido nas próximas aulas.",
                            size=12,
                            color="#6B7280",
                        ),
                    ],
                    spacing=6,
                ),
            )
            page.update()

        return abrir

    # ==============================================================
    # MENU
    # ==============================================================
    menu = AppMenu(
        abrir_clientes=abrir_clientes,
        abrir_produtos=pagina_futura("Produtos"),
        abrir_ordens=pagina_futura("Ordens de Serviço"),
    )

    # ==============================================================
    # LAYOUT PRINCIPAL
    # ==============================================================
    page.add(
        ft.Column(
            controls=[
                # V2 - mantém o cabeçalho claro da V1.
                ft.Container(
                    bgcolor="#FFFFFF",
                    border=ft.Border(
                        bottom=ft.BorderSide(
                            width=1,
                            color="#E5E7EB",
                        )
                    ),
                    padding=ft.Padding.symmetric(
                        horizontal=20,
                        vertical=12,
                    ),
                    content=ft.Row(
                        controls=[
                            ft.Icon(
                                ft.Icons.HUB_OUTLINED,
                                color="#0B63CE",
                                size=24,
                            ),
                            ft.Text(
                                "Tech Service - Clientes",
                                size=16,
                                weight=ft.FontWeight.W_600,
                                color="#111827",
                            ),
                        ],
                        spacing=10,
                    ),
                ),
                ft.Row(
                    controls=[
                        menu.build(),
                        conteudo,
                    ],
                    spacing=0,
                    expand=True,
                    vertical_alignment=ft.CrossAxisAlignment.START,
                ),
            ],
            spacing=0,
            expand=True,
        )
    )

    # Abre Clientes quando o programa inicia.
    abrir_clientes()


if __name__ == "__main__":
    ft.run(main)
