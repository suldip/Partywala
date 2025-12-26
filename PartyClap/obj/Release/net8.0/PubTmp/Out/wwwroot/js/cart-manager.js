// Cart Management System
class CartManager {
    constructor() {
        // Check if user is logged in
        this.isLoggedIn = window.isUserLoggedIn || false;

        if (this.isLoggedIn) {
            this.items = this.loadCart();
            this.syncWithServer(); // Sync with server on load
        } else {
            // Clear cart if user is not logged in
            this.items = [];
            this.clearLocalStorage();
        }
        this.updateCartUI();
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
                    // Map server items to local format - use serviceId as unique identifier
                    // since a vendor can have multiple services
                    this.items = data.items.map(item => ({
                        id: item.vendorId || item.VendorId, // UUID string
                        serviceId: item.serviceId || item.ServiceId, // Unique service identifier
                        name: item.vendorName || item.VendorName,
                        category: item.serviceType || item.ServiceType,
                        basePrice: item.cost || item.Cost,
                        // Add other fields if available from server or keep minimal
                    }));

                    this.saveCart();
                    this.updateCartUI();
                    this.updateAddToCartButtons();
                } else {
                    // If sync fails, clear items to avoid showing wrong state
                    this.items = [];
                    this.saveCart();
                    this.updateAddToCartButtons();
                }
            })
            .catch(err => {
                console.error('Failed to sync cart:', err);
                // If error, don't show items as added
                this.items = [];
                this.updateAddToCartButtons();
            });
    }

    // Save cart to localStorage
    saveCart() {
        localStorage.setItem('partyClap_cart', JSON.stringify(this.items));
        this.updateCartUI();
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

        // Send to server
        const formData = new FormData();
        formData.append('serviceId', vendor.serviceId);
        formData.append('vendorId', vendor.id);

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

                    // Update badge with server count if available
                    if (data.count !== undefined) {
                        this.updateCartBadge(data.count);
                    }

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

    updateCartBadge(count) {
        const cartBadge = document.getElementById('cart-count');
        const mobileBadge = document.getElementById('mobile-cart-badge');

        if (cartBadge) {
            cartBadge.textContent = count;
            cartBadge.style.display = count > 0 ? 'inline-block' : 'none';
        }
        if (mobileBadge) {
            mobileBadge.textContent = count;
            mobileBadge.style.display = count > 0 ? 'inline-block' : 'none';
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

    // Get cart count
    getCount() {
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

    // Update cart UI (badge count)
    updateCartUI() {
        const cartBadge = document.getElementById('cart-count');
        const mobileBadge = document.getElementById('mobile-cart-badge');
        const count = this.getCount();

        if (cartBadge) {
            cartBadge.textContent = count;
            cartBadge.style.display = count > 0 ? 'inline-block' : 'none';
        }
        if (mobileBadge) {
            mobileBadge.textContent = count;
            mobileBadge.style.display = count > 0 ? 'inline-block' : 'none';
        }
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
                cartManager.updateAddToCartButtons();
            }
        }
    }, 2000); // Check every 2 seconds
});

// Export for use in other scripts
window.cartManager = cartManager;
