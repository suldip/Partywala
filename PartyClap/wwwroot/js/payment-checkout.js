(function () {
    'use strict';

    function initPaymentMethods(root) {
        if (!root || root.dataset.payMethodsBound) return;
        root.dataset.payMethodsBound = 'true';

        var options = root.querySelectorAll('.pay-method-option');
        var panels = root.querySelectorAll('.pay-method-panel');

        options.forEach(function (option) {
            option.addEventListener('click', function () {
                var method = option.dataset.method;
                options.forEach(function (o) {
                    o.classList.toggle('active', o === option);
                    var radio = o.querySelector('input[type="radio"]');
                    if (radio) radio.checked = o === option;
                });
                panels.forEach(function (p) {
                    p.classList.toggle('active', p.dataset.method === method);
                });
            });
        });
    }

    document.addEventListener('DOMContentLoaded', function () {
        document.querySelectorAll('[data-pay-checkout]').forEach(initPaymentMethods);
    });

    window.PaymentCheckout = { init: initPaymentMethods };
})();
