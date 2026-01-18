document.addEventListener('DOMContentLoaded', () => {
    const navbar = document.getElementById('publicNavbar');
    const collapseEl = document.getElementById('navbarPublicCollapse');
    const scrollIndicator = document.getElementById('scrollIndicator');
    const navOverlay = document.getElementById('navOverlay');
    const scrollTopBtn = document.getElementById('scrollTopBtn');
    const contactSection = document.getElementById('contact');
    const homeSection = document.getElementById('home');
    const filterBtns = Array.from(document.querySelectorAll('.project-filter-btn'));
    const projectItems = Array.from(document.querySelectorAll('.project-item'));
    const journeyEl = document.querySelector('.journey');
    const journeyItems = Array.from(document.querySelectorAll('.journey-item'));
    const skillsEl = document.querySelector('.about-skills');

    const getScrollTop = () => window.scrollY || window.pageYOffset || document.documentElement.scrollTop || 0;

    const setNavbarState = () => {
        if (!navbar) return;

        let isHero = false;
        if (homeSection) {
            const navH = navbar.getBoundingClientRect().height || 0;
            const heroBottom = homeSection.getBoundingClientRect().top + getScrollTop() + homeSection.getBoundingClientRect().height;
            isHero = getScrollTop() < Math.max(0, heroBottom - navH - 8);
            navbar.classList.toggle('is-hero', isHero);
        }

        if (!isHero && window.scrollY > 80) {
            navbar.classList.add('is-scrolled');
        } else {
            navbar.classList.remove('is-scrolled');
        }

        if (scrollIndicator) {
            if (window.scrollY > 60) {
                scrollIndicator.classList.add('is-hidden');
            } else {
                scrollIndicator.classList.remove('is-hidden');
            }
        }

        if (scrollTopBtn && contactSection) {
            const contactTop = contactSection.getBoundingClientRect().top + getScrollTop();
            const showAt = Math.max(0, contactTop - (window.innerHeight * 0.65));
            if (getScrollTop() >= showAt) {
                scrollTopBtn.classList.add('is-visible');
            } else {
                scrollTopBtn.classList.remove('is-visible');
            }
        }

        if (journeyEl) {
            const rect = journeyEl.getBoundingClientRect();
            const docTop = rect.top + getScrollTop();
            const start = docTop - (window.innerHeight * 0.2);
            const end = docTop + rect.height - (window.innerHeight * 0.45);
            const denom = Math.max(1, end - start);
            const progress = Math.max(0, Math.min(1, (getScrollTop() - start) / denom));
            journeyEl.style.setProperty('--journey-progress', String(progress));
        }
    };

    setNavbarState();
    window.addEventListener('scroll', setNavbarState, { passive: true });

    const smoothScrollTo = (targetId) => {
        const target = document.getElementById(targetId);
        if (!target || !navbar) return;

        const navHeight = navbar.getBoundingClientRect().height;
        const y = target.getBoundingClientRect().top + getScrollTop() - navHeight - 16;
        window.scrollTo({ top: y, behavior: 'smooth' });
    };

    document.querySelectorAll('a.js-scroll[href^="#"]').forEach((link) => {
        link.addEventListener('click', (e) => {
            const href = link.getAttribute('href');
            if (!href || href.length < 2) return;

            e.preventDefault();
            smoothScrollTo(href.substring(1));

            if (collapseEl && typeof bootstrap !== 'undefined') {
                const instance = bootstrap.Collapse.getInstance(collapseEl) || new bootstrap.Collapse(collapseEl, { toggle: false });
                instance.hide();
            }
        });
    });

    if (collapseEl) {
        collapseEl.addEventListener('shown.bs.collapse', () => {
            document.body.classList.add('nav-open');
        });
        collapseEl.addEventListener('hidden.bs.collapse', () => {
            document.body.classList.remove('nav-open');
        });
    }

    if (navOverlay && collapseEl && typeof bootstrap !== 'undefined') {
        navOverlay.addEventListener('click', () => {
            const instance = bootstrap.Collapse.getInstance(collapseEl) || new bootstrap.Collapse(collapseEl, { toggle: false });
            instance.hide();
        });
    }

    if (scrollTopBtn) {
        scrollTopBtn.addEventListener('click', () => {
            window.scrollTo({ top: 0, behavior: 'smooth' });
        });
    }

    const contactAlert = document.querySelector('#contact .contact-form-card .alert');
    if (contactAlert) {
        window.setTimeout(() => {
            contactAlert.classList.add('is-hiding');
            const remove = () => {
                if (contactAlert && contactAlert.parentNode) {
                    contactAlert.parentNode.removeChild(contactAlert);
                }
            };
            contactAlert.addEventListener('transitionend', remove, { once: true });
            window.setTimeout(remove, 400);
        }, 3000);
    }

    const animateNumber = (el, to, duration = 900) => {
        const start = performance.now();
        const from = 0;

        const tick = (now) => {
            const t = Math.min(1, (now - start) / duration);
            const v = Math.round(from + (to - from) * t);
            el.textContent = `${v}%`;
            if (t < 1) requestAnimationFrame(tick);
        };
        requestAnimationFrame(tick);
    };

    const runSkillsAnimation = () => {
        if (!skillsEl) return;
        const pctEls = Array.from(skillsEl.querySelectorAll('.skill-pct'));
        skillsEl.classList.add('is-animated');
        pctEls.forEach((el, idx) => {
            const target = Number(el.dataset.target || '0');
            if (!Number.isFinite(target)) return;
            window.setTimeout(() => animateNumber(el, Math.max(0, Math.min(100, target))), idx * 120);
        });
    };

    if (skillsEl && 'IntersectionObserver' in window) {
        const obs = new IntersectionObserver(
            (entries) => {
                entries.forEach((entry) => {
                    if (!entry.isIntersecting) return;
                    runSkillsAnimation();
                    obs.disconnect();
                });
            },
            { threshold: 0.25 }
        );

        obs.observe(skillsEl);
    } else if (skillsEl) {
        runSkillsAnimation();
    }

    if (journeyItems.length && 'IntersectionObserver' in window) {
        const shown = new WeakSet();
        const obs = new IntersectionObserver(
            (entries) => {
                entries.forEach((entry) => {
                    if (!entry.isIntersecting) return;
                    const el = entry.target;
                    if (shown.has(el)) return;
                    shown.add(el);
                    const idx = journeyItems.indexOf(el);
                    const delay = idx >= 0 ? idx * 120 : 0;
                    el.style.transitionDelay = `${Math.min(600, delay)}ms`;
                    el.classList.add('is-visible');
                });
            },
            { threshold: 0.2, rootMargin: '0px 0px -10% 0px' }
        );
        journeyItems.forEach((it) => obs.observe(it));
    } else if (journeyItems.length) {
        journeyItems.forEach((it) => it.classList.add('is-visible'));
        if (journeyEl) {
            journeyEl.style.setProperty('--journey-progress', '1');
        }
    }

    const applyProjectFilter = (filter) => {
        if (!projectItems.length) return;

        projectItems.forEach((item) => {
            const cat = item.getAttribute('data-category') || '';
            const show = filter === 'all' || cat === filter;
            item.classList.toggle('is-hidden', !show);
        });
    };

    if (filterBtns.length && projectItems.length) {
        filterBtns.forEach((btn) => {
            btn.addEventListener('click', () => {
                const filter = btn.getAttribute('data-filter') || 'all';
                filterBtns.forEach((b) => b.classList.toggle('is-active', b === btn));
                applyProjectFilter(filter);

                if (typeof AOS !== 'undefined' && typeof AOS.refresh === 'function') {
                    try {
                        AOS.refresh();
                    } catch (e) {
                    }
                }
            });
        });

        const active = filterBtns.find((b) => b.classList.contains('is-active'));
        applyProjectFilter(active ? (active.getAttribute('data-filter') || 'all') : 'all');
    }

    if (typeof AOS !== 'undefined' && typeof AOS.refreshHard === 'function') {
        window.setTimeout(() => {
            try {
                AOS.refreshHard();
            } catch (e) {
            }
        }, 250);
    }
});
