"""
Menu lateral - V1.

Responsabilidade:
    mostrar as opções de navegação.

Nada de HTTP aqui.
Nada de CRUD aqui.
"""

import flet as ft


AZUL = "#0B63CE"
TEXTO = "#111827"
TEXTO_SECUNDARIO = "#6B7280"
BORDA = "#E5E7EB"
FUNDO_SELECIONADO = "#EAF2FF"


class AppMenu:
    def __init__(
        self,
        abrir_clientes,
        abrir_produtos,
        abrir_ordens,
    ):
        self.abrir_clientes = abrir_clientes
        self.abrir_produtos = abrir_produtos
        self.abrir_ordens = abrir_ordens

    def build(self):
        # ==========================================================
        # V1 - MENU BRANCO
        # ==========================================================
        # O menu da V0 era escuro.
        # Agora usamos fundo branco e destaque azul para Clientes.
        return ft.Container(
            width=220,
            bgcolor="#FFFFFF",
            border=ft.Border(
                right=ft.BorderSide(
                    width=1,
                    color=BORDA,
                )
            ),
            padding=14,
            content=ft.Column(
                controls=[
                    ft.Row(
                        controls=[
                            ft.Icon(
                                ft.Icons.PEOPLE,
                                color=AZUL,
                                size=28,
                            ),
                            ft.Text(
                                "CLIENTES",
                                size=16,
                                color=AZUL,
                                weight=ft.FontWeight.BOLD,
                            ),
                        ],
                        spacing=8,
                    ),
                    ft.Divider(color=BORDA),
                    # V1 - opção selecionada parecida com a imagem.
                    ft.Container(
                        bgcolor=FUNDO_SELECIONADO,
                        border_radius=8,
                        content=ft.TextButton(
                            content="Listar Clientes",
                            icon=ft.Icons.LIST_ALT,
                            on_click=self.abrir_clientes,
                            style=ft.ButtonStyle(
                                color=AZUL,
                            ),
                        ),
                    ),
                    ft.TextButton(
                        content="Consultar Cliente",
                        icon=ft.Icons.SEARCH,
                        on_click=self.abrir_clientes,
                        style=ft.ButtonStyle(
                            color=TEXTO,
                        ),
                    ),
                    ft.Divider(color=BORDA),
                    ft.Text(
                        "PRÓXIMOS MÓDULOS",
                        size=9,
                        color=TEXTO_SECUNDARIO,
                        weight=ft.FontWeight.BOLD,
                    ),
                    ft.TextButton(
                        content="Produtos",
                        icon=ft.Icons.INVENTORY_2_OUTLINED,
                        on_click=self.abrir_produtos,
                        style=ft.ButtonStyle(color=TEXTO_SECUNDARIO),
                    ),
                    ft.TextButton(
                        content="Ordens de Serviço",
                        icon=ft.Icons.BUILD_OUTLINED,
                        on_click=self.abrir_ordens,
                        style=ft.ButtonStyle(color=TEXTO_SECUNDARIO),
                    ),
                ],
                spacing=5,
            ),
        )
