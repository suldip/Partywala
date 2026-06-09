(function () {
    'use strict';

    var pinCheckCache = {};
    var pinCheckTimers = {};

    function getVendorId(root) {
        if (!root) return '';
        var scope = root.closest('[data-vendor-id]');
        return (scope && scope.dataset.vendorId) || root.dataset.vendorId || '';
    }

    function getValues(root) {
        if (!root) {
            return { partyLocation: '', partyPinCode: '', partyLatitude: null, partyLongitude: null };
        }
        var addressInput = root.querySelector('.party-location-address');
        var pinInput = root.querySelector('.party-location-pincode');
        return {
            partyLocation: addressInput ? addressInput.value.trim() : '',
            partyPinCode: pinInput ? pinInput.value.trim() : '',
            partyLatitude: null,
            partyLongitude: null
        };
    }

    function setPinFeedback(root, ok, message) {
        var pinInput = root.querySelector('.party-location-pincode');
        var feedback = root.querySelector('.party-location-pin-feedback');
        root.dataset.pinServed = ok ? 'true' : 'false';
        if (pinInput) pinInput.classList.toggle('is-invalid', !ok);
        if (feedback) {
            if (!ok && message) {
                feedback.textContent = message;
                feedback.style.display = 'block';
            } else {
                feedback.textContent = '';
                feedback.style.display = 'none';
            }
        }
    }

    function checkVendorPincode(root) {
        setPinFeedback(root, true, '');
        return Promise.resolve(true);
    }

    function validateFormat(root) {
        return true;
    }

    function validate(root) {
        return true;
    }

    function validateAsync(root) {
        return Promise.resolve(true);
    }

    function schedulePinCheck(root) {
    }

    function bindChange(root) {
        if (!root || root.dataset.plBound === 'true') return;
        root.dataset.plBound = 'true';

        root.querySelectorAll('.party-location-address').forEach(function (input) {
            input.addEventListener('input', function () {
                this.classList.remove('is-invalid');
                root.dispatchEvent(new CustomEvent('party-location-change', { bubbles: true }));
            });
        });

        var pinInput = root.querySelector('.party-location-pincode');
        if (pinInput) {
            pinInput.addEventListener('input', function () {
                root.dataset.pinServed = '';
                var feedback = root.querySelector('.party-location-pin-feedback');
                if (feedback) {
                    feedback.textContent = '';
                    feedback.style.display = 'none';
                }
                if (/^\d{6}$/.test(this.value.trim())) {
                    schedulePinCheck(root);
                } else {
                    this.classList.remove('is-invalid');
                }
                root.dispatchEvent(new CustomEvent('party-location-change', { bubbles: true }));
            });
            pinInput.addEventListener('blur', function () {
                if (/^\d{6}$/.test(this.value.trim())) {
                    checkVendorPincode(root);
                }
            });
        }
    }

    function initPicker(root) {
        bindChange(root);
        var pin = root.querySelector('.party-location-pincode');
        if (pin && /^\d{6}$/.test(pin.value.trim()) && getVendorId(root)) {
            checkVendorPincode(root);
        }
    }

    function initAll() {
        document.querySelectorAll('.party-location-picker').forEach(initPicker);
    }

    window.PartyLocation = {
        init: initPicker,
        initAll: initAll,
        getValues: getValues,
        validate: validate,
        validateAsync: validateAsync,
        checkVendorPincode: checkVendorPincode,
        setVendorId: function (root, vendorId) {
            if (!root) return;
            var scope = root.closest('[data-vendor-id]') || root;
            scope.dataset.vendorId = vendorId || '';
        }
    };

    document.addEventListener('DOMContentLoaded', initAll);
})();
