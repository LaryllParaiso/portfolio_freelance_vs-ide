document.addEventListener('DOMContentLoaded', () => {
    document.querySelectorAll('.toast').forEach((el) => {
        try {
            if (typeof bootstrap === 'undefined') return;
            const instance = bootstrap.Toast.getOrCreateInstance(el);

            const progress = el.querySelector('.toast-progress');
            if (progress) {
                progress.style.animation = 'none';
                void progress.offsetHeight;
                progress.style.animation = '';
            }

            el.addEventListener('hidden.bs.toast', () => {
                try {
                    el.remove();
                } catch (e) {
                }
            });
            instance.show();
        } catch (e) {
        }
    });

    document.querySelectorAll('form').forEach((form) => {
        form.addEventListener('submit', (e) => {
            if (form.dataset.noSpinner === '1') return;

            const method = (form.getAttribute('method') || 'get').toLowerCase();
            if (method !== 'post') return;

            const confirmMsg = form.dataset.confirm;
            if (confirmMsg && !window.confirm(confirmMsg)) {
                e.preventDefault();
                return;
            }

            if (e.defaultPrevented) return;

            const submitBtn = form.querySelector('button[type="submit"]');
            if (!submitBtn) return;
            if (submitBtn.disabled) return;

            submitBtn.disabled = true;
            submitBtn.dataset.originalHtml = submitBtn.innerHTML;

            const loadingText = submitBtn.dataset.loadingText || 'Please wait...';
            submitBtn.innerHTML = '<span class="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true"></span>' + loadingText;
        });
    });

    document.querySelectorAll('[data-counter]').forEach((el) => {
        const raw = (el.getAttribute('data-counter') || '').toString();
        const target = raw === '' ? Number(String(el.textContent || '').replace(/[^0-9.]/g, '')) : Number(raw);
        if (!Number.isFinite(target)) return;

        const duration = 700;
        const start = performance.now();

        const tick = (now) => {
            const t = Math.min(1, (now - start) / duration);
            const value = Math.round(target * t);
            el.textContent = String(value);
            if (t < 1) {
                requestAnimationFrame(tick);
            }
        };

        el.textContent = '0';
        requestAnimationFrame(tick);
    });
});
