// Popup de detalhes do perfil: cada item clicável aponta, em data-detalhe, para o <template> com o conteúdo.
(() => {
    const elemento = document.getElementById('detalhe-modal');
    if (!elemento || !window.bootstrap) return;
    const modal = new bootstrap.Modal(elemento);
    const rotulo = document.getElementById('detalhe-modal-rotulo');
    const titulo = document.getElementById('detalhe-modal-titulo');
    const corpo = document.getElementById('detalhe-modal-corpo');
    let origem = null;

    function abrir(gatilho) {
        const modelo = document.getElementById(gatilho.dataset.detalhe);
        if (!modelo) return;
        origem = gatilho;
        rotulo.textContent = gatilho.dataset.detalheRotulo ?? '';
        titulo.textContent = gatilho.dataset.detalheTitulo ?? '';
        corpo.replaceChildren(modelo.content.cloneNode(true));
        modal.show();
    }

    document.addEventListener('click', event => {
        const gatilho = event.target.closest('[data-detalhe]');
        if (gatilho) abrir(gatilho);
    });
    // Ao fechar, o foco volta para o item que abriu o popup.
    elemento.addEventListener('hidden.bs.modal', () => origem?.focus());

    // Link direto para um item: /Clientes/Detalhe/{id}#ver=<id do item>
    const direto = location.hash.match(/^#ver=([\w-]+)$/);
    if (direto) {
        const gatilho = document.querySelector(`[data-detalhe="${direto[1]}"]`);
        if (gatilho) abrir(gatilho);
    }
})();
