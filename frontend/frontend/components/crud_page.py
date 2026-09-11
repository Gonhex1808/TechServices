"""
CrudPage - V2

Evolução didática:
    V0 = LISTAR + CONSULTAR
    V1 = EDITAR + EXCLUIR + interface clara
    V2 = INCLUIR NOVO CLIENTE (POST)

Na V2 acrescentamos:
    - botão "+ Novo Cliente" no topo;
    - formulário em branco no painel direito;
    - validação simples dos campos;
    - POST /api/clientes;
    - atualização automática da lista após inserir;
    - reutilização dos mesmos campos para inserir e editar.

A ideia continua simples:
    CrudPage = interface comum
    CrudApi  = comunicação HTTP
"""

import flet as ft


# ==================================================================
# V1 - CORES DO TEMA CLARO
# ==================================================================
COR_FUNDO = "#FFFFFF"
COR_CARD = "#FFFFFF"
COR_BORDA = "#DDE3EA"
COR_TEXTO = "#111827"
COR_TEXTO_SECUNDARIO = "#6B7280"
COR_AZUL = "#0B63CE"
COR_AZUL_CLARO = "#EAF2FF"
COR_VERMELHO = "#E52222"
COR_SUCESSO = "#1F9254"
COR_ERRO = "#D92D20"


class CrudPage:
    def __init__(
        self,
        page: ft.Page,
        api,
        titulo: str,
        id_campo: str,
        colunas: list,
    ):
        self.page = page
        self.api = api
        self.titulo = titulo
        self.id_campo = id_campo
        self.colunas = colunas

        # Guarda o registo atualmente escolhido para edição.
        self.item_em_edicao = None

        # V2 - indica se o formulário está em modo de inclusão.
        # False = consulta/edição normal.
        # True  = novo cliente.
        self.modo_inclusao = False

        self.status = ft.Text(
            "Aguardando...",
            size=11,
            color=COR_TEXTO_SECUNDARIO,
        )

        self.total = ft.Text(
            "Total: 0 registos",
            size=11,
            color=COR_TEXTO,
        )

        self.tabela = self._criar_tabela()

        self.id_field = ft.TextField(
            label="ID",
            height=42,
            text_size=12,
            keyboard_type=ft.KeyboardType.NUMBER,
            expand=True,
            border_color=COR_BORDA,
        )

        self.resultado = ft.Column(
            controls=[
                ft.Text(
                    "Informe um ID para consultar.",
                    size=11,
                    color=COR_TEXTO_SECUNDARIO,
                )
            ],
            spacing=5,
        )

        # V1 - campos do formulário de edição.
        # São criados apenas para as colunas diferentes do ID.
        self.campos_edicao = {}

        for titulo_coluna, chave_json in self.colunas:
            if chave_json == self.id_campo:
                continue

            self.campos_edicao[chave_json] = ft.TextField(
                label=titulo_coluna.title(),
                text_size=12,
                border_color=COR_BORDA,
            )

        self.painel_direito = ft.Container(
            width=330,
            content=self._painel_consulta(),
        )

    # ==============================================================
    # CONSTRUIR A PÁGINA
    # ==============================================================
    def build(self):
        return ft.Container(
            padding=20,
            expand=True,
            bgcolor=COR_FUNDO,
            content=ft.Column(
                controls=[
                    ft.Row(
                        controls=[
                            ft.Column(
                                controls=[
                                    ft.Text(
                                        "Lista de Clientes",
                                        size=24,
                                        weight=ft.FontWeight.BOLD,
                                        color=COR_TEXTO,
                                    ),
                                    self.total,
                                ],
                                spacing=2,
                                expand=True,
                            ),
                            # V2 - novo botão para abrir o formulário vazio.
                            ft.FilledButton(
                                content="Novo Cliente",
                                icon=ft.Icons.ADD,
                                on_click=self._abrir_inclusao,
                                style=ft.ButtonStyle(
                                    bgcolor=COR_SUCESSO,
                                    color="#FFFFFF",
                                ),
                            ),
                            ft.FilledButton(
                                content="Atualizar",
                                icon=ft.Icons.REFRESH,
                                on_click=self._on_atualizar,
                                style=ft.ButtonStyle(
                                    bgcolor=COR_AZUL,
                                    color="#FFFFFF",
                                ),
                            ),
                        ]
                    ),
                    self.status,
                    ft.Row(
                        controls=[
                            ft.Container(
                                content=self._painel_lista(),
                                expand=True,
                            ),
                            self.painel_direito,
                        ],
                        spacing=14,
                        vertical_alignment=ft.CrossAxisAlignment.START,
                    ),
                ],
                spacing=10,
            ),
        )

    # ==============================================================
    # LISTAGEM
    # ==============================================================
    def _painel_lista(self):
        return self._card(
            ft.Container(
                # Permite rolagem horizontal quando a janela for menor.
                content=ft.Row(
                    controls=[self.tabela],
                    scroll=ft.ScrollMode.AUTO,
                ),
                height=460,
            )
        )

    def carregar(self):
        """GET - lista todos os registos."""
        try:
            dados = self.api.listar()
            self.tabela.rows.clear()

            for item in dados:
                self._adicionar_linha(item)

            self.total.value = f"Total: {len(dados)} cliente(s)"
            self.status.value = f"{len(dados)} registo(s) carregado(s)."
            self.status.color = COR_SUCESSO

        except Exception as erro:
            self.status.value = f"Erro: {erro}"
            self.status.color = COR_ERRO

        self.page.update()

    def _on_atualizar(self, e=None):
        self.carregar()

    # ==============================================================
    # V1 - LINHA COM BOTÕES EDITAR E EXCLUIR
    # ==============================================================
    def _adicionar_linha(self, item: dict):
        cells = []

        # Cria as células normais usando a configuração de colunas.
        for titulo, chave_json in self.colunas:
            valor = item.get(chave_json, "")
            valor = valor if valor not in (None, "") else "-"

            cells.append(
                ft.DataCell(
                    ft.Text(
                        str(valor),
                        size=11,
                        color=COR_TEXTO,
                    )
                )
            )

        # V1 - uma célula extra chamada AÇÕES.
        # Usamos lambdas com item=item para cada botão guardar
        # o cliente correto da sua própria linha.
        cells.append(
            ft.DataCell(
                ft.Row(
                    controls=[
                        ft.FilledButton(
                            content="Editar",
                            icon=ft.Icons.EDIT_OUTLINED,
                            on_click=lambda e, item=item: self._abrir_edicao(item),
                            style=ft.ButtonStyle(
                                bgcolor=COR_AZUL,
                                color="#FFFFFF",
                                padding=ft.Padding.symmetric(
                                    horizontal=12,
                                    vertical=8,
                                ),
                            ),
                        ),
                        ft.FilledButton(
                            content="Excluir",
                            icon=ft.Icons.DELETE_OUTLINE,
                            on_click=lambda e, item=item: self._confirmar_exclusao(item),
                            style=ft.ButtonStyle(
                                bgcolor=COR_VERMELHO,
                                color="#FFFFFF",
                                padding=ft.Padding.symmetric(
                                    horizontal=12,
                                    vertical=8,
                                ),
                            ),
                        ),
                    ],
                    spacing=8,
                )
            )
        )

        self.tabela.rows.append(ft.DataRow(cells=cells))

    # ==============================================================
    # V1 - EDITAR
    # ==============================================================
    def _abrir_edicao(self, item: dict):
        """Mostra o formulário com os dados do cliente selecionado."""
        self.item_em_edicao = item
        self.modo_inclusao = False

        for chave_json, campo in self.campos_edicao.items():
            valor = item.get(chave_json, "")
            campo.value = "" if valor is None else str(valor)

        id_registro = item.get(self.id_campo, "")

        self.painel_direito.content = self._card(
            ft.Column(
                controls=[
                    ft.Row(
                        controls=[
                            ft.Icon(ft.Icons.EDIT_OUTLINED, color=COR_AZUL),
                            ft.Text(
                                f"Editar cliente #{id_registro}",
                                size=15,
                                weight=ft.FontWeight.BOLD,
                                color=COR_TEXTO,
                            ),
                        ],
                        spacing=8,
                    ),
                    ft.Text(
                        f"PUT {self.api.endpoint}/{id_registro}",
                        size=9,
                        color=COR_TEXTO_SECUNDARIO,
                    ),
                    *self.campos_edicao.values(),
                    ft.Row(
                        controls=[
                            ft.OutlinedButton(
                                content="Cancelar",
                                on_click=self._cancelar_edicao,
                            ),
                            ft.FilledButton(
                                content="Guardar",
                                icon=ft.Icons.SAVE_OUTLINED,
                                on_click=self._guardar_edicao,
                                style=ft.ButtonStyle(
                                    bgcolor=COR_AZUL,
                                    color="#FFFFFF",
                                ),
                            ),
                        ],
                        alignment=ft.MainAxisAlignment.END,
                    ),
                ],
                spacing=10,
            )
        )

        self.page.update()

    def _guardar_edicao(self, e=None):
        """Envia PUT para a API e atualiza a listagem."""
        if not self.item_em_edicao:
            return

        id_registro = self.item_em_edicao.get(self.id_campo)

        # Começamos com uma cópia do objeto recebido da API.
        # Assim preservamos propriedades que não aparecem na tabela,
        # por exemplo: ativo, dataCadastro, etc.
        dados = dict(self.item_em_edicao)

        # Substituímos somente os campos que o utilizador editou.
        for chave_json, campo in self.campos_edicao.items():
            dados[chave_json] = (campo.value or "").strip()

        # O ID vai na URL. Removê-lo do JSON evita problemas em APIs
        # que não aceitam alteração da chave primária.
        dados.pop(self.id_campo, None)

        try:
            self.api.atualizar(
                int(id_registro),
                dados,
            )

            self.status.value = f"Cliente {id_registro} atualizado com sucesso."
            self.status.color = COR_SUCESSO

            self._cancelar_edicao()
            self.carregar()

        except Exception as erro:
            self.status.value = f"Erro ao atualizar: {erro}"
            self.status.color = COR_ERRO
            self.page.update()

    def _cancelar_edicao(self, e=None):
        # V2 - inclusão e edição voltam ao mesmo painel de consulta.
        self._cancelar_formulario(e)

    # ==============================================================
    # V2 - INCLUIR NOVO CLIENTE
    # ==============================================================
    def _abrir_inclusao(self, e=None):
        """Abre o mesmo formulário, mas com os campos em branco."""
        self.item_em_edicao = None
        self.modo_inclusao = True

        # V2 - limpa os campos porque agora queremos cadastrar.
        for campo in self.campos_edicao.values():
            campo.value = ""

        self.painel_direito.content = self._card(
            ft.Column(
                controls=[
                    ft.Row(
                        controls=[
                            ft.Icon(ft.Icons.PERSON_ADD_OUTLINED, color=COR_SUCESSO),
                            ft.Text(
                                "Novo Cliente",
                                size=15,
                                weight=ft.FontWeight.BOLD,
                                color=COR_TEXTO,
                            ),
                        ],
                        spacing=8,
                    ),
                    ft.Text(
                        f"POST {self.api.endpoint}",
                        size=9,
                        color=COR_TEXTO_SECUNDARIO,
                    ),
                    *self.campos_edicao.values(),
                    ft.Row(
                        controls=[
                            ft.OutlinedButton(
                                content="Cancelar",
                                on_click=self._cancelar_formulario,
                            ),
                            ft.FilledButton(
                                content="Salvar",
                                icon=ft.Icons.SAVE_OUTLINED,
                                on_click=self._salvar_novo,
                                style=ft.ButtonStyle(
                                    bgcolor=COR_SUCESSO,
                                    color="#FFFFFF",
                                ),
                            ),
                        ],
                        alignment=ft.MainAxisAlignment.END,
                    ),
                ],
                spacing=10,
            )
        )

        self.page.update()

    def _salvar_novo(self, e=None):
        """V2 - lê os campos e envia POST /api/clientes."""
        dados = {}

        for chave_json, campo in self.campos_edicao.items():
            dados[chave_json] = (campo.value or "").strip()

        # Validação simples para a aula:
        # nenhum campo do formulário pode ficar vazio.
        if any(not valor for valor in dados.values()):
            self.status.value = "Preencha Nome, Telefone e Email."
            self.status.color = COR_ERRO
            self.page.update()
            return

        try:
            self.api.inserir(dados)

            self.status.value = "Cliente cadastrado com sucesso."
            self.status.color = COR_SUCESSO

            self._cancelar_formulario()
            self.carregar()

        except Exception as erro:
            self.status.value = f"Erro ao cadastrar: {erro}"
            self.status.color = COR_ERRO
            self.page.update()

    def _cancelar_formulario(self, e=None):
        """Fecha inclusão/edição e volta ao painel Consultar Cliente."""
        self.item_em_edicao = None
        self.modo_inclusao = False
        self.painel_direito.content = self._painel_consulta()
        self.page.update()

    # ==============================================================
    # V1 - EXCLUIR / DESATIVAR
    # ==============================================================
    def _confirmar_exclusao(self, item: dict):
        """Abre confirmação antes de chamar DELETE na API."""
        id_registro = item.get(self.id_campo)

        # Tenta mostrar o nome quando existir.
        nome = item.get("nome") or f"ID {id_registro}"

        dialogo = ft.AlertDialog(
            modal=True,
            title=ft.Text("Confirmar exclusão"),
            content=ft.Text(
                f"Deseja realmente excluir/desativar o cliente '{nome}'?"
            ),
            actions=[
                ft.TextButton(
                    content="Cancelar",
                    on_click=lambda e: self._fechar_dialogo(dialogo),
                ),
                ft.FilledButton(
                    content="Excluir",
                    icon=ft.Icons.DELETE_OUTLINE,
                    on_click=lambda e: self._excluir(item, dialogo),
                    style=ft.ButtonStyle(
                        bgcolor=COR_VERMELHO,
                        color="#FFFFFF",
                    ),
                ),
            ],
            actions_alignment=ft.MainAxisAlignment.END,
        )

        # Flet 0.86.x permite abrir um diálogo por page.show_dialog().
        self.page.show_dialog(dialogo)

    def _excluir(self, item: dict, dialogo):
        """DELETE - desativa/exclui o registo e recarrega a lista."""
        id_registro = item.get(self.id_campo)

        try:
            self.api.desativar(int(id_registro))
            self._fechar_dialogo(dialogo)

            self.status.value = f"Cliente {id_registro} excluído/desativado."
            self.status.color = COR_SUCESSO

            self.carregar()

        except Exception as erro:
            self._fechar_dialogo(dialogo)
            self.status.value = f"Erro ao excluir: {erro}"
            self.status.color = COR_ERRO
            self.page.update()

    def _fechar_dialogo(self, dialogo):
        # Compatibilidade simples com a API moderna do Flet.
        try:
            self.page.pop_dialog()
        except Exception:
            dialogo.open = False
            self.page.update()

    # ==============================================================
    # CONSULTAR POR ID - já existia na V0
    # ==============================================================
    def _painel_consulta(self):
        return self._card(
            ft.Column(
                controls=[
                    ft.Row(
                        controls=[
                            ft.Icon(ft.Icons.SEARCH, color=COR_AZUL),
                            ft.Text(
                                "Consultar Cliente",
                                size=15,
                                weight=ft.FontWeight.BOLD,
                                color=COR_TEXTO,
                            ),
                        ],
                        spacing=8,
                    ),
                    ft.Text(
                        f"GET {self.api.endpoint}/{{id}}",
                        size=9,
                        color=COR_TEXTO_SECUNDARIO,
                    ),
                    ft.Row(
                        controls=[
                            self.id_field,
                            ft.FilledButton(
                                content="Consultar",
                                icon=ft.Icons.SEARCH,
                                on_click=self._on_consultar,
                                style=ft.ButtonStyle(
                                    bgcolor=COR_AZUL,
                                    color="#FFFFFF",
                                ),
                            ),
                        ],
                        spacing=6,
                    ),
                    ft.Divider(color=COR_BORDA),
                    self.resultado,
                ],
                spacing=9,
            )
        )

    def _on_consultar(self, e=None):
        valor = (self.id_field.value or "").strip()

        if not valor.isdigit():
            self._mostrar_erro("Informe um ID numérico válido.")
            return

        try:
            item = self.api.buscar_por_id(int(valor))

            if item is None:
                self._mostrar_erro("Registo não encontrado.")
                return

            self._mostrar_item(item)

        except Exception as erro:
            self._mostrar_erro(str(erro))

    def _mostrar_item(self, item: dict):
        """Mostra o cliente consultado de forma mais organizada.

        V1 - MELHORIA:
        Na primeira versão do painel, cada valor aparecia numa pequena
        caixa do tamanho do texto. Agora os campos ocupam toda a largura
        disponível e ficam visualmente alinhados.
        """
        controles = [
            ft.Row(
                controls=[
                    ft.Icon(
                        ft.Icons.CHECK_CIRCLE_OUTLINE,
                        color=COR_SUCESSO,
                        size=18,
                    ),
                    ft.Text(
                        "Cliente encontrado",
                        size=12,
                        color=COR_SUCESSO,
                        weight=ft.FontWeight.BOLD,
                    ),
                ],
                spacing=6,
            )
        ]

        # V1 - cada informação do cliente ocupa a largura do painel.
        for titulo, chave_json in self.colunas:
            controles.append(
                self._campo_resultado(
                    titulo,
                    item.get(chave_json, "") or "-",
                )
            )

        # V1 - reutilizamos o mesmo método de edição usado na tabela.
        # Assim, depois de consultar um cliente, podemos editá-lo sem
        # criar uma nova operação ou duplicar código.
        controles.append(
            ft.Row(
                controls=[
                    ft.FilledButton(
                        content="Editar",
                        icon=ft.Icons.EDIT_OUTLINED,
                        on_click=lambda e, item=item: self._abrir_edicao(item),
                        style=ft.ButtonStyle(
                            bgcolor=COR_AZUL,
                            color="#FFFFFF",
                        ),
                    )
                ],
                alignment=ft.MainAxisAlignment.END,
            )
        )

        self.resultado.controls = controles
        self.page.update()

    def _mostrar_erro(self, texto):
        self.resultado.controls = [
            ft.Text(
                texto,
                size=11,
                color=COR_ERRO,
            )
        ]
        self.page.update()

    # ==============================================================
    # PEQUENOS COMPONENTES VISUAIS
    # ==============================================================
    def _criar_tabela(self):
        # V1 - adicionamos a coluna AÇÕES ao final.
        colunas_tabela = [
            ft.DataColumn(
                ft.Text(
                    titulo,
                    size=10,
                    weight=ft.FontWeight.BOLD,
                    color=COR_AZUL,
                )
            )
            for titulo, chave in self.colunas
        ]

        colunas_tabela.append(
            ft.DataColumn(
                ft.Text(
                    "AÇÕES",
                    size=10,
                    weight=ft.FontWeight.BOLD,
                    color=COR_AZUL,
                )
            )
        )

        return ft.DataTable(
            heading_row_height=42,
            data_row_min_height=54,
            data_row_max_height=62,
            horizontal_margin=12,
            column_spacing=22,
            border=ft.Border.all(1, COR_BORDA),
            heading_row_color="#F8FAFC",
            columns=colunas_tabela,
            rows=[],
        )

    @staticmethod
    def _card(conteudo):
        return ft.Container(
            bgcolor=COR_CARD,
            border=ft.Border.all(
                width=1,
                color=COR_BORDA,
            ),
            border_radius=10,
            padding=14,
            content=conteudo,
        )

    @staticmethod
    def _campo_resultado(titulo, valor):
        # V1 - MELHORIA DO CONSULTAR CLIENTE:
        # width=float("inf") faz cada campo ocupar toda a largura
        # disponível no painel, evitando caixas pequenas e irregulares.
        return ft.Container(
            width=float("inf"),
            bgcolor="#F8FAFC",
            border=ft.Border.all(1, COR_BORDA),
            border_radius=7,
            padding=10,
            content=ft.Column(
                controls=[
                    ft.Text(
                        titulo.upper(),
                        size=8,
                        color=COR_TEXTO_SECUNDARIO,
                        weight=ft.FontWeight.BOLD,
                    ),
                    ft.Text(
                        str(valor),
                        size=11,
                        color=COR_TEXTO,
                    ),
                ],
                spacing=2,
            ),
        )
