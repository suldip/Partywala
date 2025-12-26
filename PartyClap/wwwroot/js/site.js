// SmartPop Utility based on SweetAlert2
window.SmartPop = {
    // Success Alert
    success: function (title, message) {
        return Swal.fire({
            icon: 'success',
            title: title || 'Success!',
            text: message,
            confirmButtonColor: '#8B5CF6',
            timer: 3000,
            timerProgressBar: true
        });
    },

    // Error Alert
    error: function (title, message) {
        return Swal.fire({
            icon: 'error',
            title: title || 'Error!',
            text: message || 'Something went wrong.',
            confirmButtonColor: '#111827'
        });
    },

    // Warning Alert
    warning: function (title, message) {
        return Swal.fire({
            icon: 'warning',
            title: title || 'Warning!',
            text: message,
            confirmButtonColor: '#f59e0b'
        });
    },

    // Confirmation Dialog
    confirm: function (title, message, confirmText = 'Yes', cancelText = 'Cancel') {
        return Swal.fire({
            title: title || 'Are you sure?',
            text: message,
            icon: 'question',
            showCancelButton: true,
            confirmButtonColor: '#8B5CF6',
            cancelButtonColor: '#6b7280',
            confirmButtonText: confirmText,
            cancelButtonText: cancelText,
            reverseButtons: true
        });
    },

    // Toast Notification (Top Right)
    toast: function (message, type = 'success') {
        const Toast = Swal.mixin({
            toast: true,
            position: 'top-end',
            showConfirmButton: false,
            timer: 3000,
            timerProgressBar: true,
            didOpen: (toast) => {
                toast.addEventListener('mouseenter', Swal.stopTimer)
                toast.addEventListener('mouseleave', Swal.resumeTimer)
            }
        });
        return Toast.fire({
            icon: type,
            title: message
        });
    }
};

// Write your JavaScript code.

// Handle data-confirm attributes automatically
document.addEventListener('click', function (e) {
    const confirmBtn = e.target.closest('[data-confirm]');
    if (confirmBtn) {
        e.preventDefault();
        const message = confirmBtn.getAttribute('data-confirm');
        const title = confirmBtn.getAttribute('data-confirm-title') || 'Are you sure?';
        const form = confirmBtn.closest('form');

        window.SmartPop.confirm(title, message).then((result) => {
            if (result.isConfirmed) {
                if (form) {
                    // Create a hidden input to ensure form submission works correctly if needed
                    form.submit();
                } else if (confirmBtn.tagName === 'A') {
                    window.location.href = confirmBtn.href;
                }
            }
        });
    }
});

// Fix for navbar links - ensure they work and close navbar collapse on mobile
(function () {
    function initNavbarCollapse() {
        const navbarCollapse = document.getElementById('navbarNav');

        if (navbarCollapse) {
            // Close navbar collapse when clicking on any link inside it (except dropdown toggles)
            navbarCollapse.addEventListener('click', function (e) {
                const target = e.target.closest('a');

                // Only close if it's a navigation link (not a dropdown toggle or anchor with href="#")
                if (target && target.getAttribute('href') &&
                    target.getAttribute('href') !== '#' &&
                    !target.hasAttribute('data-bs-toggle')) {

                    // Close the navbar collapse on mobile
                    if (navbarCollapse.classList.contains('show')) {
                        // Try using Bootstrap API first
                        if (typeof bootstrap !== 'undefined' && bootstrap.Collapse) {
                            const bsCollapse = bootstrap.Collapse.getInstance(navbarCollapse);
                            if (bsCollapse) {
                                setTimeout(function () {
                                    bsCollapse.hide();
                                }, 100);
                                return;
                            }
                        }

                        // Fallback: directly manipulate classes if Bootstrap API not available
                        setTimeout(function () {
                            navbarCollapse.classList.remove('show');
                            navbarCollapse.classList.add('collapse');
                        }, 100);
                    }
                }
            });
        }
    }

    // Initialize when DOM is ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initNavbarCollapse);
    } else {
        initNavbarCollapse();
    }
})();