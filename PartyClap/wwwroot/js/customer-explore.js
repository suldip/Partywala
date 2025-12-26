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
    sortBy: 'rating'
};

let expandedVendors = new Set();
let viewMode = 'detailed';

// Initialize on page load
// Initialize on page load
document.addEventListener('DOMContentLoaded', function () {
    initializeFilters();
    initializeLocationSearch();
    initializeViewToggle();
    initializeCategoryButtons();

    // Force apply filters to ensure UI is in sync
    applyFilters();
});

// Initialize filter dropdowns
function initializeFilters() {
    const stateSelect = document.getElementById('filter-state');
    const citySelect = document.getElementById('filter-city');
    const categorySelect = document.getElementById('filter-category');
    const sortSelect = document.getElementById('filter-sort');
    const searchInput = document.getElementById('search-input');

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
            currentFilters.city = this.value;
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

            // Update active state
            categoryButtons.forEach(b => b.classList.remove('active'));
            this.classList.add('active');

            // Update dropdown
            const categorySelect = document.getElementById('filter-category-main');
            if (categorySelect) {
                categorySelect.value = category;
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

// Apply all filters to vendor cards
function applyFilters() {
    const vendorCards = document.querySelectorAll('.vendor-card');
    let visibleCount = 0;

    vendorCards.forEach(card => {
        const matchesCategory = currentFilters.category === 'all' ||
            card.dataset.category === currentFilters.category;

        const matchesSearch = !currentFilters.searchQuery ||
            card.dataset.searchText.includes(currentFilters.searchQuery);

        const matchesState = currentFilters.state === 'all-states' ||
            card.dataset.state === currentFilters.state;

        const matchesCity = currentFilters.city === 'all-cities' ||
            card.dataset.city === currentFilters.city;

        if (matchesCategory && matchesSearch && matchesState && matchesCity) {
            card.style.display = '';
            visibleCount++;
        } else {
            card.style.display = 'none';
        }
    });

    // Update results count
    updateResultsCount(visibleCount);

    // Show/hide empty state
    toggleEmptyState(visibleCount === 0);
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
    currentFilters = {
        category: 'all',
        searchQuery: '',
        state: 'all-states',
        city: 'all-cities',
        sortBy: 'rating'
    };

    // Reset UI
    document.getElementById('filter-state').value = 'all-states';
    document.getElementById('filter-city').value = 'all-cities';
    document.getElementById('filter-category').value = 'all';
    document.getElementById('search-input').value = '';
    document.getElementById('location-search').value = '';

    updateCityDropdown();
    applyFilters();
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
            const platformFee = baseTotal * 0.1;
            const grandTotal = baseTotal + platformFee;
            const advance = Math.round(baseTotal * 0.2);

            document.getElementById(`total-${vendorId}`).textContent = `₹${grandTotal.toLocaleString()}`;
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

// Open service request dialog
function openServiceRequestDialog(vendorId, serviceId, vendorName) {
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

    // Reset form
    const form = document.getElementById('service-request-modal').querySelector('form');
    if (form) {
        form.reset();
        document.getElementById('request-vendor-id').value = vendorId;
        document.getElementById('request-service-id').value = serviceId;
    }

    // Show modal
    const modal = new bootstrap.Modal(document.getElementById('service-request-modal'));
    modal.show();
}

// Submit service request
function submitServiceRequest(event) {
    event.preventDefault();

    const form = event.target;
    const submitBtn = form.querySelector('button[type="submit"]');
    const spinner = submitBtn ? submitBtn.querySelector('.spinner-border') : null;

    // Disable button and show spinner
    if (submitBtn) {
        submitBtn.disabled = true;
        if (spinner) spinner.classList.remove('d-none');
    }

    const formData = new FormData(form);
    const requestData = {
        serviceId: formData.get('serviceId'),
        vendorId: formData.get('vendorId'),
        eventDate: formData.get('eventDate'),
        eventType: formData.get('eventType'),
        guestCount: parseInt(formData.get('guestCount')),
        additionalDetails: formData.get('details')
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
                // Close modal
                const modal = bootstrap.Modal.getInstance(document.getElementById('service-request-modal'));
                if (modal) {
                    modal.hide();
                }

                // Show success message
                SmartPop.success('Request Sent', data.message || 'Service request submitted successfully! The vendor will contact you soon.');

                // Reset form
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
