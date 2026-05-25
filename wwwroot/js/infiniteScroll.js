window.MiniInstagramInfiniteScroll = (() => {
    const observers = new Map();

    return {
        observe: (element, dotNetRef) => {
            if (!element) {
                return;
            }

            const existing = observers.get(element);
            if (existing) {
                existing.disconnect();
            }

            const observer = new IntersectionObserver((entries) => {
                if (entries.some((entry) => entry.isIntersecting)) {
                    dotNetRef.invokeMethodAsync("LoadMore");
                }
            }, {
                root: null,
                rootMargin: "700px 0px",
                threshold: 0.01
            });

            observer.observe(element);
            observers.set(element, observer);
        },

        unobserve: (element) => {
            const observer = observers.get(element);
            if (!observer) {
                return;
            }

            observer.disconnect();
            observers.delete(element);
        }
    };
})();
