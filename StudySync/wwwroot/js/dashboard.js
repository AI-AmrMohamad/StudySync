document.addEventListener('DOMContentLoaded', function () {
    const roleToggle = document.getElementById('roleToggleSwitch');
    const heroCtaText = document.getElementById('heroCtaText');
    const centerTitleMarketplace = document.getElementById('centerTitleMarketplace');
    
    // Role Toggle Logic
    if (roleToggle) {
        roleToggle.addEventListener('change', function () {
            if (this.checked) {
                // Teaching Mode enabled
                document.body.classList.add('teaching-mode');
                if (heroCtaText) heroCtaText.innerText = "Ready to share your expertise today?";
                if (centerTitleMarketplace) centerTitleMarketplace.innerText = "Open Student Requests";
            } else {
                // Learning Mode enabled
                document.body.classList.remove('teaching-mode');
                if (heroCtaText) heroCtaText.innerText = "Ready to master something new today?";
                if (centerTitleMarketplace) centerTitleMarketplace.innerText = "Available Tutors & Experts";
            }
        });
    }

    // Omni Search Logic (AJAX)
    const omniSearch = document.getElementById('omniSearch');
    const resultsContainer = document.getElementById('omniSearchResults');
    let searchTimeout;

    if (omniSearch && resultsContainer) {
        omniSearch.addEventListener('input', function () {
            clearTimeout(searchTimeout);
            const query = this.value.trim();

            if (query.length < 2) {
                resultsContainer.style.display = 'none';
                return;
            }

            searchTimeout = setTimeout(() => {
                fetch(`/Home/Search?query=${encodeURIComponent(query)}`)
                    .then(res => res.json())
                    .then(data => {
                        resultsContainer.innerHTML = '';
                        let hasResults = false;

                        // Render Rooms
                        if (data.rooms && data.rooms.length > 0) {
                            hasResults = true;
                            resultsContainer.innerHTML += `<li><h6 class="dropdown-header text-primary fw-bold">Live Rooms</h6></li>`;
                            data.rooms.forEach(room => {
                                resultsContainer.innerHTML += `<li><a class="dropdown-item py-2 hover-lift" href="/DeepWork/Room/${room.id}"><i class="bi bi-broadcast text-danger me-2"></i>${room.title}</a></li>`;
                            });
                        }

                        // Render Jobs
                        if (data.jobs && data.jobs.length > 0) {
                            if (hasResults) resultsContainer.innerHTML += `<li><hr class="dropdown-divider"></li>`;
                            hasResults = true;
                            resultsContainer.innerHTML += `<li><h6 class="dropdown-header text-success fw-bold">Open Bounties</h6></li>`;
                            data.jobs.forEach(job => {
                                resultsContainer.innerHTML += `<li><a class="dropdown-item py-2 hover-lift" href="/Bounty/Details/${job.id}"><i class="bi bi-briefcase text-success me-2"></i>${job.title}</a></li>`;
                            });
                        }

                        if (hasResults) {
                            resultsContainer.style.display = 'block';
                        } else {
                            resultsContainer.innerHTML = `<li><span class="dropdown-item text-muted">No results found</span></li>`;
                            resultsContainer.style.display = 'block';
                        }
                    });
            }, 300); // 300ms debounce
        });

        // Hide when clicking outside
        document.addEventListener('click', function(e) {
            if (!omniSearch.contains(e.target) && !resultsContainer.contains(e.target)) {
                resultsContainer.style.display = 'none';
            }
        });
    }
});
