(() => {
    const labels = { NaFila: 'Na fila', Transcrevendo: 'Transcrevendo áudio…', Analisando: 'Analisando…', Concluida: 'Concluída', Erro: 'Erro' };
    const pending = [];
    function tempo(segundos) {
        const total = Math.floor(segundos);
        return `${Math.floor(total / 60)}:${String(total % 60).padStart(2, '0')}`;
    }
    document.querySelectorAll('[data-status-id]').forEach(element => {
        element.textContent = labels[element.dataset.status] ?? element.dataset.status;
        if (!['Concluida', 'Erro'].includes(element.dataset.status)) pending.push(element);
    });
    async function poll() {
        if (!pending.length) return;
        try {
            for (const element of pending) {
                const response = await fetch(`/api/conversas/${element.dataset.statusId}/status`, { cache: 'no-store', signal: AbortSignal.timeout(15000) });
                if (!response.ok) { element.textContent = 'Não foi possível consultar o andamento.'; continue; }
                const result = await response.json();
                element.textContent = labels[result.status] ?? result.status;
                if (result.status === 'Transcrevendo' && result.progresso) {
                    const progresso = result.progresso;
                    element.textContent = progresso.percentual == null
                        ? `${progresso.etapa}…`
                        : `${progresso.etapa}… ${progresso.percentual}% (${tempo(progresso.segundosProcessados)} de ${tempo(progresso.segundosTotais)})`;
                    const texto = document.querySelector(`[data-transcricao-id="${element.dataset.statusId}"]`);
                    if (texto && progresso.transcricao) texto.textContent = progresso.transcricao;
                    else if (texto && !texto.textContent.trim()) texto.textContent = 'Aguardando o primeiro trecho…';
                }
                if (['Concluida', 'Erro'].includes(result.status)) { location.reload(); return; }
            }
        } catch { pending.forEach(element => element.textContent = 'Conexão interrompida. Tentando novamente…'); }
        setTimeout(poll, 2000);
    }
    if (pending.length) setTimeout(poll, 2000);
})();
