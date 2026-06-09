// Cart Management System — server-backed for signed-in customers (B041).
// localStorage is only a UI cache; GetCartJson is the source of truth.
class CartManager {
    constructor() {
        this.isLoggedIn = window.isUserLoggedIn || false;
        this.badgeCount = 0;

        if (this.isLoggedIn) {
            this.items = [];
            this.syncWithServer();
        } else {
            this.items = [];
            this.clearLocalStorage();
            this.setBadgeCount(0);
        }
    }

    // Load cart from localStorage
    loadCart() {
        const saved = localStorage.getItem('partyClap_cart');
        return saved ? JSON.parse(saved) : [];
    }

    // Sync cart with server
    syncWithServer() {
        // Only sync if user is logged in
        if (!this.isLoggedIn) {
            this.items = [];
            this.updateAddToCartButtons();
            return;
        }

        fetch((window.basePath || '') + 'Customer/GetCartJson')
            .then(response => {
                if (!response.ok) {
                    // If unauthorized (401), user is not logged in
                    if (response.status === 401) {
                        this.isLoggedIn = false;
                        this.items = [];
                        this.clearLocalStorage();
                        this.updateAddToCartButtons();
                        return null;
                    }
                    throw new Error(`HTTP ${response.status}`);
                }
                return response.json();
            })
            .then(data => {
                if (!data) return; // Handled in catch/401 case

                if (data.success && data.items) {
                    const badgeCount = typeof data.totalCount === 'number'
                        ? data.totalCount
                        : (data.items ? data.items.length : 0);
                    this.setBadgeCount(badgeCount);

                    this.items = data.items.map(item => ({
                        id: item.vendorId || item.VendorId,
                        serviceId: item.serviceId || item.ServiceId,
                        name: item.vendorName || item.VendorName,
                        category: item.serviceType || item.ServiceType,
                        basePrice: item.cost || item.Cost,
                        pricingModel: item.unit || item.Unit || 'event',
                        image: item.mediaUrl || item.MediaUrl || '',
                        location: item.address || item.Address || ''
                    }));

                    this.saveCart();
                    this.updateAddToCartButtons();
                } else if (data.success) {
                    const badgeCount = typeof data.totalCount === 'number' ? data.totalCount : 0;
                    this.setBadgeCount(badgeCount);
                    this.items = [];
                    this.saveCart();
                    this.updateAddToCartButtons();
                }
            })
            .catch(err => {
                console.error('Failed to sync cart:', err);
            });
    }

    // Save cart to localStorage
    saveCart() {
        localStorage.setItem('partyClap_cart', JSON.stringify(this.items));
    }

    // Add item to cart
    addToCart(vendor) {
        // Check if user is logged in
        if (!window.isUserLoggedIn) {
            window.location.href = (window.basePath || '') + 'Account/Login?returnUrl=' + encodeURIComponent(window.location.pathname + window.location.search);
            return false;
        }

        // Check if already in cart (prevent duplicate adds)
        if (this.isInCart(vendor.id, vendor.serviceId)) {
            this.showNotification(`${vendor.name} is already in your cart!`, 'info');
            return false;
        }

        // Find and update the button to show loading state
        const button = document.querySelector(`button.btn-add-to-cart[data-vendor-id="${vendor.id}"][data-service-id="${vendor.serviceId}"]`);
        if (button) {
            const originalHTML = button.innerHTML;
            const originalDisabled = button.disabled;
            button.disabled = true;
            button.style.cursor = 'wait';
            button.innerHTML = '<i class="bi bi-hourglass-split"></i> Adding...';

            // Store original state to restore on error
            button.dataset.originalHtml = originalHTML;
            button.dataset.originalDisabled = originalDisabled;
        }

        const eventDate = window.VendorAvailabilityCalendar
            ? window.VendorAvailabilityCalendar.getSelectedDateForButton(button || document.querySelector(
                `button.btn-add-to-cart[data-vendor-id="${vendor.id}"][data-service-id="${vendor.serviceId}"]`
            ))
            : '';

        if (!eventDate) {
            if (button) {
                delete button.dataset.processing;
                button.disabled = button.dataset.originalDisabled === 'true';
                if (button.dataset.originalHtml) {
                    button.innerHTML = button.dataset.originalHtml;
                }
            }
            this.showNotification('Please select a green (available) date on the calendar before booking.', 'warning');
            return Promise.resolve(false);
        }

        return this.submitAddToCart(vendor, button, eventDate, { partyLocation: '', partyPinCode: '', partyLatitude: null, partyLongitude: null });
    }

    submitAddToCart(vendor, button, eventDate, location) {
        const formData = new FormData();
        formData.append('serviceId', vendor.serviceId);
        formData.append('vendorId', vendor.id);
        formData.append('eventDate', eventDate);
        formData.append('partyLocation', location.partyLocation || '');
        formData.append('partyPinCode', location.partyPinCode || '');

        return fetch((window.basePath || '') + 'Customer/AddToCartJson', {
            method: 'POST',
            body: formData
        })
            .then(response => {
                if (!response.ok) {
                    throw new Error(`HTTP ${response.status}`);
                }
                return response.json();
            })
            .then(data => {
                if (data.success) {
                    // Add to local items array (will be synced on next sync)
                    const newItem = {
                        id: vendor.id,
                        serviceId: vendor.serviceId,
                        name: vendor.name,
                        category: vendor.category,
                        basePrice: vendor.basePrice,
                        pricingModel: vendor.pricingModel,
                        image: vendor.image,
                        location: vendor.location,
                        addedAt: new Date().toISOString()
                    };

                    // Check for duplicates before adding
                    const exists = this.items.some(item =>
                        String(item.id || '') === String(vendor.id) &&
                        String(item.serviceId || '') === String(vendor.serviceId)
                    );

                    if (!exists) {
                        this.items.push(newItem);
                        this.saveCart();
                    }

                    this.showNotification(`${vendor.name} added to cart!`, 'success');

                    // Update button state after a short delay to prevent flicker
                    setTimeout(() => {
                        this.updateAddToCartButtons();
                    }, 100);

                    // Sync with server to get latest state
                    this.syncWithServer();
                } else {
                    // Restore button state on error
                    const button = document.querySelector(`button.btn-add-to-cart[data-vendor-id="${vendor.id}"][data-service-id="${vendor.serviceId}"]`);
                    if (button && button.dataset.originalHtml) {
                        button.innerHTML = button.dataset.originalHtml;
                        button.disabled = button.dataset.originalDisabled === 'true';
                        button.style.cursor = '';
                        delete button.dataset.originalHtml;
                        delete button.dataset.originalDisabled;
                    }
                    this.showNotification('Failed to add to cart: ' + (data.message || 'Unknown error'), 'danger');
                }
            })
            .catch(error => {
                console.error('Error adding to cart:', error);
                // Restore button state on error
                const button = document.querySelector(`button.btn-add-to-cart[data-vendor-id="${vendor.id}"][data-service-id="${vendor.serviceId}"]`);
                if (button && button.dataset.originalHtml) {
                    button.innerHTML = button.dataset.originalHtml;
                    button.disabled = button.dataset.originalDisabled === 'true';
                    button.style.cursor = '';
                    delete button.dataset.originalHtml;
                    delete button.dataset.originalDisabled;
                }
                this.showNotification('Error adding to cart. Please try again.', 'danger');
            });
    }

    setBadgeCount(count) {
        this.badgeCount = Math.max(0, Number(count) || 0);
        this.updateCartBadge(this.badgeCount);
    }

    updateCartBadge(count) {
        const cartBadge = document.getElementById('cart-count');
        const mobileBadge = document.getElementById('mobile-cart-badge');
        const safeCount = Math.max(0, Number(count) || 0);

        if (cartBadge) {
            cartBadge.textContent = safeCount;
            cartBadge.style.display = safeCount > 0 ? 'inline-block' : 'none';
        }
        if (mobileBadge) {
            mobileBadge.textContent = safeCount;
            mobileBadge.style.display = safeCount > 0 ? 'inline-block' : 'none';
        }
    }

    // Remove item from cart
    removeFromCart(vendorId, serviceId) {
        if (serviceId) {
            // Remove specific service
            this.items = this.items.filter(item =>
                !(String(item.id || '') === String(vendorId) && String(item.serviceId || '') === String(serviceId))
            );
        } else {
            // Remove all items for this vendor
            this.items = this.items.filter(item => String(item.id || '') !== String(vendorId));
        }
        this.saveCart();
        this.showNotification('Removed from cart', 'info');
        this.updateAddToCartButtons();
    }

    // Check if vendor/service is in cart
    isInCart(vendorId, serviceId) {
        if (!vendorId) return false;

        // Check by serviceId if provided (more specific)
        if (serviceId) {
            return this.items.some(item =>
                String(item.serviceId || '') === String(serviceId) &&
                String(item.id || '') === String(vendorId)
            );
        }

        // Fallback to vendorId only (less specific, but works)
        return this.items.some(item => String(item.id || '') === String(vendorId));
    }

    // Navbar badge: new cart + pending + accepted requests
    getCount() {
        return this.badgeCount;
    }

    getNewCartCount() {
        return this.items.length;
    }

    // Get cart total
    getTotal() {
        return this.items.reduce((total, item) => {
            const price = item.pricingModel === 'per person'
                ? item.basePrice * 20 // Assume minimum 20 people
                : item.basePrice;
            return total + price;
        }, 0);
    }

    // Update cart UI (badge count from server total)
    updateCartUI() {
        this.updateCartBadge(this.badgeCount);
    }

    // Update all "Add to Cart" buttons
    updateAddToCartButtons() {
        // Only target the actual buttons, not the card containers
        document.querySelectorAll('button.btn-add-to-cart[data-vendor-id]').forEach(btn => {
            // Skip if button is currently processing
            if (btn.dataset.processing === 'true') {
                return;
            }

            const vendorId = btn.dataset.vendorId;
            const serviceId = btn.dataset.serviceId;

            // Only show "Added" if user is logged in AND item is in cart
            const isLoggedIn = window.isUserLoggedIn || this.isLoggedIn;
            const inCart = isLoggedIn && this.isInCart(vendorId, serviceId);

            // Use requestAnimationFrame to prevent visual flicker
            requestAnimationFrame(() => {
                if (inCart) {
                    btn.classList.remove('btn-light-purple');
                    btn.classList.add('btn-success');
                    btn.innerHTML = '<i class="bi bi-check-lg"></i> Added';
                    btn.disabled = true;
                    btn.style.cursor = 'not-allowed';
                } else {
                    btn.classList.remove('btn-success');
                    btn.classList.add('btn-light-purple');
                    btn.innerHTML = '<i class="bi bi-cart-plus"></i> Add to Cart';
                    btn.disabled = false;
                    btn.style.cursor = 'pointer';
                }
            });
        });
    }

    // Show notification
    showNotification(message, type = 'info') {
        // Create notification element
        const notification = document.createElement('div');
        notification.className = `alert alert-${type} position-fixed top-0 end-0 m-3 fade show`;
        notification.style.zIndex = '9999';
        notification.style.minWidth = '250px';
        notification.innerHTML = `
            <div class="d-flex align-items-center">
                <span>${message}</span>
                <button type="button" class="btn-close ms-auto" data-bs-dismiss="alert"></button>
            </div>
        `;

        document.body.appendChild(notification);

        // Auto remove after 3 seconds
        setTimeout(() => {
            notification.classList.remove('show');
            setTimeout(() => notification.remove(), 150);
        }, 3000);
    }

    // Clear cart
    clearCart() {
        this.items = [];
        this.saveCart();
        this.updateAddToCartButtons();
    }

    // Clear localStorage
    clearLocalStorage() {
        localStorage.removeItem('partyClap_cart');
    }
}

// Initialize cart manager
const cartManager = new CartManager();

// Add to cart button handler
document.addEventListener('DOMContentLoaded', function () {
    // Check login status and update cart accordingly
    const currentLoginStatus = window.isUserLoggedIn || false;
    if (!currentLoginStatus && cartManager.items.length > 0) {
        // User is not logged in but has items in cart - clear them
        cartManager.items = [];
        cartManager.clearLocalStorage();
        cartManager.updateAddToCartButtons();
    }

    // Handle add to cart buttons
    document.querySelectorAll('.btn-add-to-cart').forEach(btn => {
        btn.addEventListener('click', function (e) {
            // Prevent default and stop propagation to avoid multiple triggers
            e.preventDefault();
            e.stopPropagation();

            // Prevent multiple rapid clicks
            if (this.disabled || this.dataset.processing === 'true') {
                return false;
            }

            this.dataset.processing = 'true';

            const vendorData = {
                id: this.dataset.vendorId, // UUID string, do not parse as int
                serviceId: this.dataset.serviceId,
                name: this.dataset.vendorName,
                category: this.dataset.vendorCategory,
                basePrice: parseFloat(this.dataset.vendorPrice),
                pricingModel: this.dataset.vendorPricing,
                image: this.dataset.vendorImage,
                location: this.dataset.vendorLocation
            };

            cartManager.addToCart(vendorData).finally(() => {
                // Remove processing flag after operation completes
                setTimeout(() => {
                    delete this.dataset.processing;
                }, 500);
            });

            return false;
        });
    });

    // Update button states on page load
    cartManager.updateAddToCartButtons();

    // Re-check login status periodically (in case user logs in/out in another tab)
    setInterval(function () {
        const newLoginStatus = window.isUserLoggedIn || false;
        if (newLoginStatus !== cartManager.isLoggedIn) {
            cartManager.isLoggedIn = newLoginStatus;
            if (newLoginStatus) {
                // User just logged in - sync with server
                cartManager.syncWithServer();
            } else {
                // User just logged out - clear cart
                cartManager.items = [];
                cartManager.clearLocalStorage();
                cartManager.setBadgeCount(0);
                cartManager.updateAddToCartButtons();
            }
        }
    }, 2000); // Check every 2 seconds
});

// Export for use in other scripts
window.cartManager = cartManager;
