// Shared vendor messaging modal (B030/B044/B052).
// Single implementation — never delegate openMessageModal to window.openMessageModal.
(function () {
    'use strict';

    function requireCustomerLogin() {
        if (!window.isUserLoggedIn) {
            window.location.href = (window.basePath || '') + 'Account/Login?returnUrl=' +
                encodeURIComponent(window.location.pathname + window.location.search);
            return false;
        }
        return true;
    }

    function openMessageModal(vendorId, vendorName) {
        if (!requireCustomerLogin()) {
            return;
        }

        const modalEl = document.getElementById('message-modal');
        if (!modalEl) {
            console.error('Message modal (#message-modal) not found on page.');
            return;
        }

        const vendorIdEl = document.getElementById('msg-vendor-id');
        const vendorNameEl = document.getElementById('msg-vendor-name');
        const contentEl = document.getElementById('msg-content');

        if (vendorIdEl) {
            vendorIdEl.value = vendorId;
        }
        if (vendorNameEl) {
            vendorNameEl.textContent = vendorName;
        }
        if (contentEl) {
            contentEl.value = '';
        }

        bootstrap.Modal.getOrCreateInstance(modalEl).show();
    }

    async function submitMessage() {
        const vendorIdEl = document.getElementById('msg-vendor-id');
        const contentEl = document.getElementById('msg-content');
        const vendorId = vendorIdEl ? vendorIdEl.value : '';
        const content = contentEl ? contentEl.value : '';

        if (!content.trim()) {
            window.SmartPop.toast('Please enter a message.', 'error');
            return;
        }

        try {
            const response = await fetch((window.basePath || '') + 'Message/SendInitialMessage', {
                method: 'POST',
                headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
                body: 'vendorId=' + encodeURIComponent(vendorId) + '&content=' + encodeURIComponent(content)
            });
            const result = await response.json();

            if (result.success) {
                window.SmartPop.toast(result.message, 'success');
                const modalEl = document.getElementById('message-modal');
                const instance = modalEl ? bootstrap.Modal.getInstance(modalEl) : null;
                if (instance) {
                    instance.hide();
                }
                if (result.redirectUrl) {
                    setTimeout(function () {
                        window.location.href = result.redirectUrl;
                    }, 700);
                }
            } else {
                window.SmartPop.toast(result.message, 'error');
            }
        } catch (error) {
            window.SmartPop.toast('Failed to send message.', 'error');
        }
    }

    function initMessageModal(scope) {
        const root = scope || document;

        root.querySelectorAll('.btn-send-message[data-vendor-id]').forEach(function (btn) {
            if (btn.dataset.messageBound === 'true') {
                return;
            }
            btn.dataset.messageBound = 'true';
            btn.addEventListener('click', function () {
                openMessageModal(this.dataset.vendorId, this.dataset.vendorName);
            });
        });

        const submitBtn = root.getElementById ? root.getElementById('btn-submit-message') : document.getElementById('btn-submit-message');
        if (submitBtn && submitBtn.dataset.messageBound !== 'true') {
            submitBtn.dataset.messageBound = 'true';
            submitBtn.addEventListener('click', submitMessage);
        }
    }

    window.PartyClapMessaging = {
        openMessageModal: openMessageModal,
        submitMessage: submitMessage,
        initMessageModal: initMessageModal
    };

    // Legacy global names for pages that still reference them in inline scripts.
    window.openMessageModal = openMessageModal;
    window.submitMessage = submitMessage;

    document.addEventListener('DOMContentLoaded', function () {
        initMessageModal(document);
    });
})();
