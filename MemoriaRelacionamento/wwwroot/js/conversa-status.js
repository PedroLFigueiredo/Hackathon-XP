(() => {
    const labels = { NaFila: 'Na fila', Transcrevendo: 'Transcrevendo áudio…', Analisando: 'Analisando…', Concluida: 'Concluída', Erro: 'Erro' };
    const pending = [];
    document.querySelectorAll('[data-status-id]').forEach(element => {
        element.textContent = labels[element.dataset.status] ?? element.dataset.status;
        if (!['Concluida', 'Erro'].includes(element.dataset.status)) pending.push(element);
    });
    async function poll() {
        if (!pending.length) return;
        try {
            for (const element of pending) {
                const response = await fetch(`/api/conversas/${element.dataset.statusId}/status`);
                if (!response.ok) { element.textContent = 'Não foi possível consultar o andamento.'; continue; }
                const result = await response.json();
                element.textContent = labels[result.status] ?? result.status;
                if (['Concluida', 'Erro'].includes(result.status)) { location.reload(); return; }
            }
        } catch { pending.forEach(element => element.textContent = 'Conexão interrompida. Tentando novamente…'); }
        setTimeout(poll, 2000);
    }
    if (pending.length) setTimeout(poll, 2000);
})();
