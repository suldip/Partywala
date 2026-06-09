// State-City Data
const stateCityData = {
    "Maharashtra": ["Mumbai", "Pune", "Nagpur", "Nashik", "Aurangabad", "Thane", "Solapur"],
    "Delhi": ["New Delhi", "Central Delhi", "North Delhi", "South Delhi", "East Delhi", "West Delhi"],
    "Karnataka": ["Bangalore", "Mysore", "Mangalore", "Hubli", "Belgaum", "Gulbarga"],
    "Tamil Nadu": ["Chennai", "Coimbatore", "Madurai", "Tiruchirappalli", "Salem", "Tirunelveli"],
    "Gujarat": ["Ahmedabad", "Surat", "Vadodara", "Rajkot", "Bhavnagar", "Jamnagar"],
    "Rajasthan": ["Jaipur", "Jodhpur", "Udaipur", "Kota", "Ajmer", "Bikaner"],
    "Uttar Pradesh": ["Lucknow", "Kanpur", "Ghaziabad", "Agra", "Varanasi", "Meerut"],
    "West Bengal": ["Kolkata", "Howrah", "Durgapur", "Asansol", "Siliguri"],
    "Telangana": ["Hyderabad", "Warangal", "Nizamabad", "Karimnagar", "Khammam"],
    "Andhra Pradesh": ["Visakhapatnam", "Vijayawada", "Guntur", "Nellore", "Kurnool"],
    "Kerala": ["Thiruvananthapuram", "Kochi", "Kozhikode", "Thrissur", "Kollam"],
    "Madhya Pradesh": ["Bhopal", "Indore", "Gwalior", "Jabalpur", "Ujjain"],
    "Punjab": ["Chandigarh", "Ludhiana", "Amritsar", "Jalandhar", "Patiala"],
    "Haryana": ["Gurugram", "Faridabad", "Panipat", "Ambala", "Karnal"],
    "Bihar": ["Patna", "Gaya", "Bhagalpur", "Muzaffarpur", "Darbhanga"]
};

// Service Categories
const serviceCategories = [
    { id: 'all', name: 'All Services', icon: '🎉' },
    { id: 'singer', name: 'Singers', icon: '🎵' },
    { id: 'chef', name: 'Chefs & Caterers', icon: '🍽️' },
    { id: 'decorator', name: 'Decorators', icon: '🎨' },
    { id: 'magician', name: 'Magicians', icon: '✨' },
    { id: 'photographer', name: 'Photographers', icon: '📸' },
    { id: 'dj', name: 'DJs', icon: '🎧' },
    { id: 'event-manager', name: 'Event Managers', icon: '👥' }
];

// Global state
let currentFilters = {
    category: 'all',
    searchQuery: '',
    state: 'all-states',
    city: 'all-cities',
    minPrice: null,
    maxPrice: null,
    minRating: null,
    sortBy: 'rating'
};

let expandedVendors = new Set();
let viewMode = 'detailed';

// Pagination state
const PAGE_SIZE = 9;
let currentPage = 1;
let filteredCards = [];

// Initialize on page load
// Initialize on page load
document.addEventListener('DOMContentLoaded', function () {
    hydrateFiltersFromServer();
    initializeFilters();
    initializeLocationSearch();
    initializeViewToggle();
    initializeCategoryButtons();
    initializePartyDateSearch();
    initializeRequestServiceButtons();

    applyFilters();
});

function initializeRequestServiceButtons() {
    document.querySelectorAll('.btn-request-service').forEach(function (btn) {
        btn.addEventListener('click', function () {
            openServiceRequestDialog(
                this.dataset.vendorId,
                this.dataset.serviceId,
                this.dataset.vendorName,
                this);
        });
    });
}

function initializePartyDateSearch() {
    const exploreForm = document.getElementById('explore-filter-form');
    const partyDateInput = document.getElementById('party-date-input');
    const filterByDateInput = document.getElementById('filter-by-date');

    if (partyDateInput && !partyDateInput.value) {
        partyDateInput.value = new Date().toISOString().slice(0, 10);
    }

    if (exploreForm) {
        exploreForm.addEventListener('submit', function (e) {
            const submitter = e.submitter;
            const applyDateFilter = submitter && (
                submitter.classList.contains('explore-search-btn') ||
                submitter.classList.contains('explore-apply-btn')
            );

            if (filterByDateInput) {
                filterByDateInput.value = applyDateFilter ? '1' : '0';
            }

            if (applyDateFilter && partyDateInput && !partyDateInput.value) {
                partyDateInput.value = new Date().toISOString().slice(0, 10);
            }
        });
    }
}

function hydrateFiltersFromServer() {
    const initial = window.exploreInitialFilters;
    if (!initial) return;

    currentFilters.category = initial.category || 'all';
    currentFilters.searchQuery = (initial.searchQuery || '').toLowerCase();
    currentFilters.sortBy = initial.sortBy || 'rating';
    currentFilters.minPrice = initial.minPrice != null ? Number(initial.minPrice) : null;
    currentFilters.maxPrice = initial.maxPrice != null ? Number(initial.maxPrice) : null;
    currentFilters.minRating = initial.minRating != null ? Number(initial.minRating) : null;

    const searchInput = document.getElementById('search-input');
    if (searchInput && initial.searchQuery) {
        searchInput.value = initial.searchQuery;
    }

    const sortSelect = document.getElementById('filter-sort');
    if (sortSelect && initial.sortBy) {
        sortSelect.value = initial.sortBy;
    }

    const hiddenCategory = document.getElementById('filter-category-hidden');
    if (hiddenCategory) {
        hiddenCategory.value = currentFilters.category;
    }
}

// Initialize filter dropdowns
function initializeFilters() {
    const stateSelect = document.getElementById('filter-state');
    const citySelect = document.getElementById('filter-city');
    const categorySelect = document.getElementById('filter-category');
    const sortSelect = document.getElementById('filter-sort');
    const searchInput = document.getElementById('search-input');
    const minPriceInput = document.getElementById('filter-min-price');
    const maxPriceInput = document.getElementById('filter-max-price');
    const minRatingSelect = document.getElementById('filter-min-rating');

    // State dropdown change
    if (stateSelect) {
        stateSelect.addEventListener('change', function () {
            currentFilters.state = this.value;
            // updateCityDropdown(); // Disabled to preserve server options
            applyFilters();
        });
    }

    // City dropdown change
    if (citySelect) {
        citySelect.addEventListener('change', function () {
            currentFilters.city = this.value || 'all-cities';
            applyFilters();
        });
    }

    // Category dropdown (main)
    const categoryMainSelect = document.getElementById('filter-category-main');
    if (categoryMainSelect) {
        categoryMainSelect.addEventListener('change', function () {
            currentFilters.category = this.value;
            updateCategoryPills();
            applyFilters();
        });
    }

    // Category dropdown (if exists)
    if (categorySelect) {
        categorySelect.addEventListener('change', function () {
            currentFilters.category = this.value;
            applyFilters();
        });
    }

    // Sort dropdown
    if (sortSelect) {
        sortSelect.addEventListener('change', function () {
            currentFilters.sortBy = this.value;
            applyFilters();
        });
    }

    if (minPriceInput) {
        minPriceInput.addEventListener('change', function () {
            currentFilters.minPrice = this.value ? Number(this.value) : null;
            applyFilters();
        });
    }

    if (maxPriceInput) {
        maxPriceInput.addEventListener('change', function () {
            currentFilters.maxPrice = this.value ? Number(this.value) : null;
            applyFilters();
        });
    }

    if (minRatingSelect) {
        minRatingSelect.addEventListener('change', function () {
            currentFilters.minRating = this.value ? Number(this.value) : null;
            applyFilters();
        });
    }

    // Search input
    if (searchInput) {
        let searchTimeout;
        searchInput.addEventListener('input', function () {
            clearTimeout(searchTimeout);
            searchTimeout = setTimeout(() => {
                currentFilters.searchQuery = this.value.toLowerCase();
                applyFilters();
            }, 300);
        });
    }
}

// Update city dropdown - DISABLED
function updateCityDropdown() {
    // Logic removed to allow server-side population
}

// Initialize location quick search
function initializeLocationSearch() {
    const locationSearch = document.getElementById('location-search');
    const locationResults = document.getElementById('location-results');

    if (locationSearch && locationResults) {
        let searchTimeout;
        locationSearch.addEventListener('input', function () {
            clearTimeout(searchTimeout);
            const query = this.value.toLowerCase().trim();

            if (query.length < 2) {
                locationResults.style.display = 'none';
                return;
            }

            searchTimeout = setTimeout(() => {
                const results = searchLocations(query);
                displayLocationResults(results);
            }, 200);
        });

        // Close results when clicking outside
        document.addEventListener('click', function (e) {
            if (!locationSearch.contains(e.target) && !locationResults.contains(e.target)) {
                locationResults.style.display = 'none';
            }
        });
    }
}

// Search locations by text
function searchLocations(query) {
    const results = [];

    // Search states
    Object.keys(stateCityData).forEach(state => {
        if (state.toLowerCase().includes(query)) {
            results.push({
                type: 'state',
                state: state,
                displayText: state
            });
        }

        // Search cities
        stateCityData[state].forEach(city => {
            if (city.toLowerCase().includes(query)) {
                results.push({
                    type: 'city',
                    state: state,
                    city: city,
                    displayText: `${city}, ${state}`
                });
            }
        });
    });

    return results.slice(0, 6);
}

// Display location search results
function displayLocationResults(results) {
    const locationResults = document.getElementById('location-results');
    if (!locationResults) return;

    if (results.length === 0) {
        locationResults.style.display = 'none';
        return;
    }

    locationResults.innerHTML = results.map(result => `
        <div class="location-result-item" data-state="${result.state}" data-city="${result.city || ''}" data-type="${result.type}">
            <div class="fw-medium">${result.displayText}</div>
            <div class="text-muted small text-capitalize">${result.type}</div>
        </div>
    `).join('');

    locationResults.style.display = 'block';

    // Add click handlers
    locationResults.querySelectorAll('.location-result-item').forEach(item => {
        item.addEventListener('click', function () {
            const state = this.dataset.state;
            const city = this.dataset.city;

            document.getElementById('filter-state').value = state;
            currentFilters.state = state;
            updateCityDropdown();

            if (city) {
                document.getElementById('filter-city').value = city;
                currentFilters.city = city;
            }

            document.getElementById('location-search').value = '';
            locationResults.style.display = 'none';
            applyFilters();
        });
    });
}

// Initialize view mode toggle
function initializeViewToggle() {
    const compactBtn = document.getElementById('view-compact');
    const detailedBtn = document.getElementById('view-detailed');

    if (compactBtn && detailedBtn) {
        compactBtn.addEventListener('click', function () {
            viewMode = 'compact';
            compactBtn.classList.add('active');
            detailedBtn.classList.remove('active');
            updateVendorCards();
        });

        detailedBtn.addEventListener('click', function () {
            viewMode = 'detailed';
            detailedBtn.classList.add('active');
            compactBtn.classList.remove('active');
            updateVendorCards();
        });
    }
}

// Initialize category quick select buttons
function initializeCategoryButtons() {
    const categoryButtons = document.querySelectorAll('.category-quick-btn');
    categoryButtons.forEach(btn => {
        btn.addEventListener('click', function () {
            const category = this.dataset.category;
            currentFilters.category = category;

            categoryButtons.forEach(b => b.classList.remove('active'));
            this.classList.add('active');

            const hiddenCategory = document.getElementById('filter-category-hidden');
            if (hiddenCategory) {
                hiddenCategory.value = category;
            }

            const form = document.getElementById('explore-filter-form');
            if (form) {
                const filterByDateInput = document.getElementById('filter-by-date');
                if (filterByDateInput) {
                    filterByDateInput.value = '0';
                }
                form.submit();
                return;
            }

            applyFilters();
        });
    });
}

// Update category pills to match dropdown
function updateCategoryPills() {
    const categoryButtons = document.querySelectorAll('.category-quick-btn');
    categoryButtons.forEach(btn => {
        if (btn.dataset.category === currentFilters.category) {
            btn.classList.add('active');
            btn.classList.remove('btn-outline-secondary');
            btn.classList.add('btn-primary');
        } else {
            btn.classList.remove('active');
            btn.classList.remove('btn-primary');
            btn.classList.add('btn-outline-secondary');
        }
    });
}

function cardMatchesCategory(card, category) {
    if (category === 'all') return true;
    const slugs = (card.dataset.categories || card.dataset.category || '')
        .split(',')
        .map(s => s.trim().toLowerCase())
        .filter(Boolean);
    return slugs.includes(category);
}

function sortFilteredCards(cards) {
    const sortBy = currentFilters.sortBy || 'rating';
    cards.sort((a, b) => {
        const priceA = parseFloat(a.dataset.price) || 0;
        const priceB = parseFloat(b.dataset.price) || 0;
        const ratingA = parseFloat(a.dataset.rating) || 0;
        const ratingB = parseFloat(b.dataset.rating) || 0;
        const reviewsA = parseInt(a.dataset.reviews, 10) || 0;
        const reviewsB = parseInt(b.dataset.reviews, 10) || 0;

        switch (sortBy) {
            case 'price-low':
                return priceA - priceB;
            case 'price-high':
                return priceB - priceA;
            case 'reviews':
                return reviewsB - reviewsA;
            default:
                return ratingB - ratingA || reviewsB - reviewsA;
        }
    });
}

// Apply all filters to vendor cards, then paginate the matching set.
function applyFilters() {
    const vendorCards = document.querySelectorAll('.vendor-card');
    filteredCards = [];

    vendorCards.forEach(card => {
        const matchesCategory = cardMatchesCategory(card, currentFilters.category);

        const matchesSearch = !currentFilters.searchQuery ||
            card.dataset.searchText.includes(currentFilters.searchQuery);

        const matchesState = currentFilters.state === 'all-states' ||
            card.dataset.state === currentFilters.state;

        const matchesCity = currentFilters.city === 'all-cities' ||
            card.dataset.city === currentFilters.city;

        const price = parseFloat(card.dataset.price) || 0;
        const matchesMinPrice = currentFilters.minPrice == null || price >= currentFilters.minPrice;
        const matchesMaxPrice = currentFilters.maxPrice == null || price <= currentFilters.maxPrice;

        const rating = parseFloat(card.dataset.rating) || 0;
        const matchesRating = currentFilters.minRating == null || rating >= currentFilters.minRating;

        if (matchesCategory && matchesSearch && matchesState && matchesCity &&
            matchesMinPrice && matchesMaxPrice && matchesRating) {
            filteredCards.push(card);
        } else {
            card.style.display = 'none';
        }
    });

    sortFilteredCards(filteredCards);

    // Filters changed -> reset to first page and render.
    currentPage = 1;
    renderPage();

    // Results count reflects the full matching set, not just the current page.
    updateResultsCount(filteredCards.length);
    toggleEmptyState(filteredCards.length === 0);
}

// Show only the cards belonging to the current page; hide the rest.
function renderPage() {
    const totalPages = Math.max(1, Math.ceil(filteredCards.length / PAGE_SIZE));
    if (currentPage > totalPages) currentPage = totalPages;
    if (currentPage < 1) currentPage = 1;

    const start = (currentPage - 1) * PAGE_SIZE;
    const end = start + PAGE_SIZE;

    filteredCards.forEach((card, idx) => {
        card.style.display = (idx >= start && idx < end) ? '' : 'none';
    });

    renderPaginationControls(totalPages);
    updatePaginationSummary(start, Math.min(end, filteredCards.length));
}

// Navigate to a page and smooth-scroll back to the results toolbar.
function goToPage(page) {
    const totalPages = Math.max(1, Math.ceil(filteredCards.length / PAGE_SIZE));
    if (page < 1 || page > totalPages || page === currentPage) return;
    currentPage = page;
    renderPage();

    const toolbar = document.querySelector('.results-toolbar') || document.getElementById('vendor-list');
    if (toolbar) toolbar.scrollIntoView({ behavior: 'smooth', block: 'start' });
}

// Build a Bootstrap pagination control with a windowed page range.
function renderPaginationControls(totalPages) {
    const container = document.getElementById('pagination-controls');
    if (!container) return;

    if (totalPages <= 1) {
        container.innerHTML = '';
        return;
    }

    const pages = [];
    const window = 1; // pages shown on each side of the current page
    pages.push(1);
    for (let p = currentPage - window; p <= currentPage + window; p++) {
        if (p > 1 && p < totalPages) pages.push(p);
    }
    if (totalPages > 1) pages.push(totalPages);

    const unique = [...new Set(pages)].sort((a, b) => a - b);

    let html = '';
    html += `<li class="page-item ${currentPage === 1 ? 'disabled' : ''}">
                <a class="page-link" href="#" data-page="${currentPage - 1}" aria-label="Previous">&laquo;</a>
             </li>`;

    let prev = 0;
    unique.forEach(p => {
        if (prev && p - prev > 1) {
            html += `<li class="page-item disabled"><span class="page-link">&hellip;</span></li>`;
        }
        html += `<li class="page-item ${p === currentPage ? 'active' : ''}">
                    <a class="page-link" href="#" data-page="${p}">${p}</a>
                 </li>`;
        prev = p;
    });

    html += `<li class="page-item ${currentPage === totalPages ? 'disabled' : ''}">
                <a class="page-link" href="#" data-page="${currentPage + 1}" aria-label="Next">&raquo;</a>
             </li>`;

    container.innerHTML = html;

    container.querySelectorAll('a.page-link[data-page]').forEach(link => {
        link.addEventListener('click', function (e) {
            e.preventDefault();
            const target = parseInt(this.dataset.page, 10);
            if (!isNaN(target)) goToPage(target);
        });
    });
}

// "Showing 1–9 of 202 professionals"
function updatePaginationSummary(startIndex, endIndex) {
    const summary = document.getElementById('pagination-summary');
    if (!summary) return;
    const total = filteredCards.length;
    if (total === 0) {
        summary.textContent = '';
        return;
    }
    summary.textContent = `Showing ${startIndex + 1}\u2013${endIndex} of ${total} professionals`;
}

// Update results count display
function updateResultsCount(count) {
    const resultsCount = document.getElementById('results-count');
    if (resultsCount) {
        resultsCount.textContent = count;
    }
}

// Toggle empty state
function toggleEmptyState(show) {
    const emptyState = document.getElementById('empty-state');
    const vendorList = document.getElementById('vendor-list');

    if (emptyState && vendorList) {
        emptyState.style.display = show ? 'block' : 'none';
        vendorList.style.display = show ? 'none' : 'block';
    }
}

// Toggle vendor portfolio expansion
function togglePortfolio(vendorId) {
    const portfolioFull = document.getElementById(`portfolio-full-${vendorId}`);
    const portfolioCompact = document.getElementById(`portfolio-compact-${vendorId}`);
    const toggleBtn = document.getElementById(`portfolio-toggle-${vendorId}`);

    if (expandedVendors.has(vendorId)) {
        expandedVendors.delete(vendorId);
        if (portfolioFull) portfolioFull.style.display = 'none';
        if (portfolioCompact) portfolioCompact.style.display = 'block';
        if (toggleBtn) toggleBtn.textContent = 'View Portfolio';
    } else {
        expandedVendors.add(vendorId);
        if (portfolioFull) portfolioFull.style.display = 'block';
        if (portfolioCompact) portfolioCompact.style.display = 'none';
        if (toggleBtn) toggleBtn.textContent = 'Show Less';
    }
}

// Update vendor cards based on view mode
function updateVendorCards() {
    const vendorCards = document.querySelectorAll('.vendor-card');
    vendorCards.forEach(card => {
        if (viewMode === 'compact') {
            card.classList.add('compact-view');
            card.classList.remove('detailed-view');
        } else {
            card.classList.add('detailed-view');
            card.classList.remove('compact-view');
        }
    });
}

// Clear all filters
function clearAllFilters() {
    const basePath = window.basePath || '';
    window.location.href = basePath + 'Customer/Explore';
}

// Pricing calculator for per-person vendors
function initPricingCalculator(vendorId, basePrice, minimumOrders) {
    const calculatorToggle = document.getElementById(`calc-toggle-${vendorId}`);
    const calculatorContent = document.getElementById(`calc-content-${vendorId}`);
    const guestInput = document.getElementById(`guest-count-${vendorId}`);
    const decreaseBtn = document.getElementById(`decrease-${vendorId}`);
    const increaseBtn = document.getElementById(`increase-${vendorId}`);

    if (calculatorToggle && calculatorContent) {
        calculatorToggle.addEventListener('click', function () {
            const isVisible = calculatorContent.style.display === 'block';
            calculatorContent.style.display = isVisible ? 'none' : 'block';
            this.textContent = isVisible ? '+ Calculate for Your Party Size' : '- Hide Calculator';
        });
    }

    if (guestInput && decreaseBtn && increaseBtn) {
        const updateCalculation = () => {
            const guestCount = parseInt(guestInput.value) || minimumOrders;
            const baseTotal = guestCount * basePrice;
            const advance = Math.round(baseTotal * 0.2);

            document.getElementById(`total-${vendorId}`).textContent = `₹${baseTotal.toLocaleString()}`;
            document.getElementById(`advance-${vendorId}`).textContent = `₹${advance.toLocaleString()}`;
        };

        decreaseBtn.addEventListener('click', () => {
            const current = parseInt(guestInput.value) || minimumOrders;
            guestInput.value = Math.max(minimumOrders, current - 5);
            updateCalculation();
        });

        increaseBtn.addEventListener('click', () => {
            const current = parseInt(guestInput.value) || minimumOrders;
            guestInput.value = current + 5;
            updateCalculation();
        });

        guestInput.addEventListener('input', updateCalculation);
    }
}

function isWeekendDate(iso) {
    if (!iso) return false;
    const d = new Date(iso);
    const day = d.getDay();
    return day === 0 || day === 6;
}

function countEventDays(startIso, endIso) {
    if (!startIso) return 1;
    const end = endIso || startIso;
    const start = new Date(startIso);
    const finish = new Date(end);
    if (finish < start) return 0;
    return Math.round((finish - start) / (1000 * 60 * 60 * 24)) + 1;
}

function calculateRequestTotal(dailyCost, weekendCost, startIso, endIso) {
    const daily = parseFloat(dailyCost) || 0;
    const weekend = weekendCost ? parseFloat(weekendCost) : null;
    if (!startIso) return daily;
    const end = endIso || startIso;
    let total = 0;
    const start = new Date(startIso);
    const finish = new Date(end);
    for (let d = new Date(start); d <= finish; d.setDate(d.getDate() + 1)) {
        const iso = d.toISOString().slice(0, 10);
        total += (weekend && isWeekendDate(iso)) ? weekend : daily;
    }
    return total;
}

function updateRequestPricing() {
    const dailyInput = document.getElementById('request-daily-cost');
    const weekendInput = document.getElementById('request-weekend-cost');
    const startInput = document.getElementById('request-event-start');
    const endInput = document.getElementById('request-event-end');
    const summary = document.getElementById('request-pricing-summary');
    const dayCountEl = document.getElementById('request-day-count');
    const totalEl = document.getElementById('request-total-cost');
    const rateLabel = document.getElementById('request-daily-rate-label');

    if (!dailyInput || !startInput || !summary) return;

    const dailyCost = parseFloat(dailyInput.value) || 0;
    if (dailyCost <= 0) {
        summary.style.display = 'none';
        return;
    }

    let startIso = startInput.value;
    let endIso = endInput ? endInput.value : startIso;
    if (startIso && endInput && (!endIso || endIso < startIso)) {
        endInput.value = startIso;
        endIso = startIso;
    }

    const days = countEventDays(startIso, endIso);
    const total = calculateRequestTotal(dailyCost, weekendInput?.value, startIso, endIso);

    summary.style.display = 'block';
    if (dayCountEl) dayCountEl.textContent = days;
    if (totalEl) totalEl.textContent = '₹' + Math.round(total).toLocaleString('en-IN');
    if (rateLabel) {
        rateLabel.textContent = '₹' + Math.round(dailyCost).toLocaleString('en-IN') + ' per day' +
            (weekendInput?.value ? ' (weekend ₹' + Math.round(parseFloat(weekendInput.value)).toLocaleString('en-IN') + ')' : '');
    }
}

function validateRequestTime() {
    const startDate = document.getElementById('request-event-start');
    const endDate = document.getElementById('request-event-end');
    const startTime = document.getElementById('request-event-start-time');
    const endTime = document.getElementById('request-event-end-time');
    const err = document.getElementById('request-time-error');
    let ok = true;

    if (startTime) startTime.classList.remove('is-invalid');
    if (endTime) endTime.classList.remove('is-invalid');

    if (startDate && endDate && startTime && endTime
        && startDate.value && endDate.value && startTime.value && endTime.value
        && startDate.value === endDate.value
        && endTime.value <= startTime.value) {
        ok = false;
        endTime.classList.add('is-invalid');
    }

    if (err) err.style.display = ok ? 'none' : 'block';
    return ok;
}

function bindRequestPricingListeners() {
    ['request-event-start', 'request-event-end', 'request-event-start-time', 'request-event-end-time'].forEach(function (id) {
        const el = document.getElementById(id);
        if (el && !el.dataset.pricingBound) {
            el.dataset.pricingBound = '1';
            el.addEventListener('change', function () {
                updateRequestPricing();
                validateRequestTime();
            });
        }
    });
}

// Open service request dialog
function openServiceRequestDialog(vendorId, serviceId, vendorName, triggerBtn) {
    // Check if user is logged in
    if (!window.isUserLoggedIn) {
        window.location.href = (window.basePath || '') + 'Account/Login?returnUrl=' + encodeURIComponent(window.location.pathname);
        return;
    }

    const vendorIdInput = document.getElementById('request-vendor-id');
    const serviceIdInput = document.getElementById('request-service-id');
    const vendorNameElement = document.getElementById('request-vendor-name');

    if (vendorIdInput) vendorIdInput.value = vendorId;
    if (serviceIdInput) serviceIdInput.value = serviceId;
    if (vendorNameElement) vendorNameElement.textContent = vendorName;

    const form = document.getElementById('service-request-modal').querySelector('form');
    if (form) {
        form.reset();
        document.getElementById('request-vendor-id').value = vendorId;
        document.getElementById('request-service-id').value = serviceId;
    }

    const dailyCostInput = document.getElementById('request-daily-cost');
    const weekendCostInput = document.getElementById('request-weekend-cost');
    if (triggerBtn && dailyCostInput) {
        dailyCostInput.value = triggerBtn.dataset.serviceCost || '0';
    }
    if (triggerBtn && weekendCostInput) {
        weekendCostInput.value = triggerBtn.dataset.weekendCost || '';
    }

    const eventDateInput = form ? form.querySelector('input[name="eventDate"]') : null;
    const eventEndInput = document.getElementById('request-event-end');
    const selectedDate = triggerBtn && window.VendorAvailabilityCalendar
        ? window.VendorAvailabilityCalendar.getSelectedDateForButton(triggerBtn)
        : '';
    if (eventDateInput && selectedDate) {
        eventDateInput.value = selectedDate;
    }
    if (eventEndInput && selectedDate) {
        eventEndInput.value = selectedDate;
    }

    const startTimeInput = document.getElementById('request-event-start-time');
    const endTimeInput = document.getElementById('request-event-end-time');
    if (startTimeInput && !startTimeInput.value) startTimeInput.value = '10:00';
    if (endTimeInput && !endTimeInput.value) endTimeInput.value = '18:00';

    bindRequestPricingListeners();
    updateRequestPricing();
    validateRequestTime();

    const requestWrap = document.getElementById('request-party-location-wrap');
    const requestPicker = document.querySelector('#service-request-modal .party-location-picker');
    if (requestWrap) {
        requestWrap.dataset.vendorId = vendorId || '';
    }
    if (requestPicker && window.PartyLocation) {
        const addressInput = requestPicker.querySelector('.party-location-address');
        const pinInput = requestPicker.querySelector('.party-location-pincode');
        if (addressInput) addressInput.value = '';
        if (pinInput) pinInput.value = '';
        window.PartyLocation.init(requestPicker);
        window.PartyLocation.setVendorId(requestPicker, vendorId);
    }

    // Show modal
    const modal = new bootstrap.Modal(document.getElementById('service-request-modal'));
    modal.show();
}

// Submit service request
async function submitServiceRequest(event) {
    event.preventDefault();

    const form = event.target.id === 'service-request-form'
        ? event.target
        : document.getElementById('service-request-form');
    const submitBtn = document.querySelector('button[form="service-request-form"]')
        || form?.querySelector('button[type="submit"]');
    const spinner = submitBtn ? submitBtn.querySelector('.spinner-border') : null;

    // Disable button and show spinner
    if (submitBtn) {
        submitBtn.disabled = true;
        if (spinner) spinner.classList.remove('d-none');
    }

    const formData = new FormData(form);
    const startDate = formData.get('eventDate');
    const endDate = formData.get('eventEndDate') || startDate;
    const startTime = formData.get('eventStartTime');
    const endTime = formData.get('eventEndTime');

    if (endDate < startDate) {
        SmartPop.error('Invalid dates', 'Event end date cannot be before the start date.');
        if (submitBtn) {
            if (spinner) spinner.classList.add('d-none');
            submitBtn.disabled = false;
        }
        return;
    }

    if (!startTime || !endTime) {
        SmartPop.error('Missing time', 'Please set event start and end time.');
        if (submitBtn) {
            if (spinner) spinner.classList.add('d-none');
            submitBtn.disabled = false;
        }
        return;
    }

    if (startDate === endDate && endTime <= startTime) {
        validateRequestTime();
        SmartPop.error('Invalid time', 'End time must be after start time on the same day.');
        if (submitBtn) {
            if (spinner) spinner.classList.add('d-none');
            submitBtn.disabled = false;
        }
        return;
    }

    const requestPicker = document.querySelector('#service-request-modal .party-location-picker');
    const partyLoc = requestPicker && window.PartyLocation
        ? window.PartyLocation.getValues(requestPicker)
        : { partyLocation: '', partyPinCode: '', partyLatitude: null, partyLongitude: null };

    const requestData = {
        serviceId: formData.get('serviceId'),
        vendorId: formData.get('vendorId'),
        eventDate: startDate,
        eventEndDate: endDate,
        eventStartTime: startTime,
        eventEndTime: endTime,
        eventType: formData.get('eventType'),
        guestCount: parseInt(formData.get('guestCount')),
        additionalDetails: formData.get('details'),
        partyLocation: partyLoc.partyLocation,
        partyPinCode: partyLoc.partyPinCode,
        partyLatitude: partyLoc.partyLatitude,
        partyLongitude: partyLoc.partyLongitude
    };

    fetch((window.basePath || '') + 'Customer/RequestService', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
        },
        body: JSON.stringify(requestData)
    })
        .then(response => response.json())
        .then(data => {
            // Hide spinner and enable button
            if (submitBtn) {
                if (spinner) spinner.classList.add('d-none');
                submitBtn.disabled = false;
            }

            if (data.success) {
                const modal = bootstrap.Modal.getInstance(document.getElementById('service-request-modal'));
                if (modal) {
                    modal.hide();
                }

                SmartPop.success('Request Sent', data.message || 'Service request submitted successfully!');

                if (data.eventDate && requestData.vendorId && window.VendorAvailabilityCalendar) {
                    if (window.VendorAvailabilityCalendar.markDateRangeUnderProcess) {
                        window.VendorAvailabilityCalendar.markDateRangeUnderProcess(
                            requestData.vendorId, data.eventDate, data.eventEndDate || data.eventDate);
                    } else {
                        window.VendorAvailabilityCalendar.markDateUnderProcess(requestData.vendorId, data.eventDate);
                    }
                }

                form.reset();
            } else {
                SmartPop.error('Submission Failed', data.message || 'Error submitting request. Please try again.');
            }
        })
        .catch(error => {
            if (submitBtn) {
                if (spinner) spinner.classList.add('d-none');
                submitBtn.disabled = false;
            }
            console.error('Error:', error);
            SmartPop.error('Network Error', 'Error submitting request. Please try again.');
        });
}
