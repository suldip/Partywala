// Cart page: schedule editing and checkout (B051/B054).

(function () {

    'use strict';



    document.addEventListener('DOMContentLoaded', function () {

        const checkoutForm = document.getElementById('checkout-form');

        if (!checkoutForm) {

            return;

        }



        const checkoutBtn = document.getElementById('checkout-btn');

        const dateValidationError = document.getElementById('date-validation-error');

        const dateInputs = document.querySelectorAll('.event-date-input');

        const endDateInputs = document.querySelectorAll('.event-end-date-input');

        const startInputs = document.querySelectorAll('.event-start-input');

        const endInputs = document.querySelectorAll('.event-end-input');

        let isUpdatingDate = false;



        function getInput(selector, cartItemId) {

            return document.querySelector(selector + '[data-cart-item-id="' + cartItemId + '"]');

        }

        function getPartyLocationPicker(cartItemId) {
            const wrap = document.querySelector('.party-location-picker-wrap[data-cart-item-id="' + cartItemId + '"]');
            return wrap ? wrap.querySelector('.party-location-picker') : null;
        }

        function getPartyLocationValues(cartItemId) {
            const picker = getPartyLocationPicker(cartItemId);
            if (!picker || !window.PartyLocation) {
                return { partyLocation: '', partyPinCode: '', partyLatitude: null, partyLongitude: null };
            }
            return window.PartyLocation.getValues(picker);
        }

        function countDays(startIso, endIso) {

            if (!startIso || !endIso) return 1;

            const start = new Date(startIso + 'T12:00:00');

            const end = new Date(endIso + 'T12:00:00');

            if (end < start) return 0;

            const diff = Math.round((end - start) / (1000 * 60 * 60 * 24));

            return diff + 1;

        }



        function validateDateRange(cartItemId) {

            const startDate = getInput('.event-date-input', cartItemId);

            const endDate = getInput('.event-end-date-input', cartItemId);

            const err = document.querySelector('.date-range-error[data-cart-item-id="' + cartItemId + '"]');

            let ok = true;

            if (startDate && endDate && startDate.value && endDate.value && endDate.value < startDate.value) {

                ok = false;

                endDate.classList.add('is-invalid');

                if (err) err.style.display = 'block';

            } else {

                if (endDate) endDate.classList.remove('is-invalid');

                if (err) err.style.display = 'none';

            }

            return ok;

        }



        function validateTimeOrder(cartItemId) {

            const startDate = getInput('.event-date-input', cartItemId);

            const endDate = getInput('.event-end-date-input', cartItemId);

            const start = getInput('.event-start-input', cartItemId);

            const end = getInput('.event-end-input', cartItemId);

            const err = document.querySelector('.time-error[data-cart-item-id="' + cartItemId + '"]');

            let ok = true;



            const sameDay = startDate?.value && endDate?.value && startDate.value === endDate.value;

            if (sameDay && start && end && start.value && end.value && end.value <= start.value) {

                ok = false;

                end.classList.add('is-invalid');

                if (err) err.style.display = 'block';

            } else {

                if (end) end.classList.remove('is-invalid');

                if (err) err.style.display = 'none';

            }

            return ok;

        }



        function validateAllDates() {

            if (!dateInputs || dateInputs.length === 0) {

                return true;

            }



            let allValid = true;

            [dateInputs, endDateInputs, startInputs, endInputs].forEach(function (group) {

                group.forEach(function (input) {

                    if (!input.value || input.value.trim() === '') {

                        allValid = false;

                        input.classList.add('is-invalid');

                    } else {

                        input.classList.remove('is-invalid');

                    }

                });

            });



            dateInputs.forEach(function (input) {

                const cartItemId = input.dataset.cartItemId;

                if (!validateDateRange(cartItemId)) allValid = false;

                if (!validateTimeOrder(cartItemId)) allValid = false;

            });



            if (allValid && dateValidationError) {

                dateValidationError.style.display = 'none';

            }



            return allValid;

        }



        function scrollToFirstInvalidSchedule() {

            const invalid = document.querySelector('.event-date-input.is-invalid, .event-end-date-input.is-invalid, .event-start-input.is-invalid, .event-end-input.is-invalid');

            if (invalid) {

                invalid.closest('.event-date-picker')?.scrollIntoView({ behavior: 'smooth', block: 'center' });

                invalid.focus();

            }

        }



        function showValidationError() {

            if (dateValidationError) {

                dateValidationError.style.display = 'block';

            }

            scrollToFirstInvalidSchedule();

            window.SmartPop.toast('Set event dates and times for every item before sending.', 'warning');

        }



        function formatDateBadge(startIso, endIso, startTime, endTime) {

            if (!startIso) return '';

            const start = new Date(startIso + 'T12:00:00');

            const startStr = start.toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });

            let text = startStr;

            if (endIso && endIso !== startIso) {

                const end = new Date(endIso + 'T12:00:00');

                text += ' – ' + end.toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });

            }

            const days = countDays(startIso, endIso || startIso);

            if (days > 1) text += ' (' + days + ' days)';

            if (startTime) {

                text += ' · ' + startTime + (endTime ? '\u2013' + endTime : '');

            }

            return text;

        }



        function updateDayCountDisplay(cartItemId, dayCount, lineTotal) {

            const dayLabel = document.querySelector('.day-count-label[data-cart-item-id="' + cartItemId + '"] .day-count-value');

            if (dayLabel) dayLabel.textContent = dayCount;

            const daySummary = document.querySelector('.day-count-summary[data-cart-item-id="' + cartItemId + '"]');

            if (daySummary) daySummary.textContent = dayCount + (dayCount === 1 ? ' day' : ' days');

            const lineTotalEl = document.querySelector('.line-total-value[data-cart-item-id="' + cartItemId + '"]');

            if (lineTotalEl && lineTotal != null) {

                lineTotalEl.textContent = '₹' + Math.round(lineTotal).toLocaleString('en-IN');

            }

        }



        function onScheduleChange(cartItemId) {

            const dateEl = getInput('.event-date-input', cartItemId);

            const endDateEl = getInput('.event-end-date-input', cartItemId);

            const startEl = getInput('.event-start-input', cartItemId);

            const endEl = getInput('.event-end-input', cartItemId);

            const selectedDate = dateEl ? dateEl.value : '';

            const selectedEndDate = endDateEl ? endDateEl.value : '';



            if (dateEl && endDateEl && selectedDate && !selectedEndDate) {

                endDateEl.value = selectedDate;

            }

            if (dateEl && endDateEl && selectedDate && selectedEndDate && selectedEndDate < selectedDate) {

                endDateEl.value = selectedDate;

            }



            const badge = document.getElementById('event-date-badge-' + cartItemId);

            const badgeText = document.getElementById('event-date-text-' + cartItemId);

            const effectiveEnd = endDateEl ? endDateEl.value : selectedDate;

            if (selectedDate && badge) {

                badge.style.display = 'inline-block';

                badge.classList.remove('bg-warning', 'text-dark');

                badge.classList.add('bg-primary', 'text-white');

                if (badgeText) {

                    badgeText.textContent = formatDateBadge(selectedDate, effectiveEnd, startEl?.value, endEl?.value);

                }

            } else if (badge) {

                badge.style.display = 'none';

            }



            validateDateRange(cartItemId);

            validateTimeOrder(cartItemId);

            const location = getPartyLocationValues(cartItemId);

            updateCartItemSchedule(

                cartItemId,

                selectedDate,

                effectiveEnd,

                startEl ? startEl.value : '',

                endEl ? endEl.value : '',

                location.partyLocation,

                location.partyPinCode,

                location.partyLatitude,

                location.partyLongitude

            );

            validateAllDates();

        }



        function updateCartItemSchedule(cartItemId, eventDate, eventEndDate, startTime, endTime, partyLocation, partyPinCode, partyLatitude, partyLongitude) {

            if (isUpdatingDate) return;



            isUpdatingDate = true;



            fetch((window.basePath || '') + 'Customer/UpdateCartItemDate', {

                method: 'POST',

                headers: { 'Content-Type': 'application/json' },

                credentials: 'same-origin',

                body: JSON.stringify({

                    cartItemId: parseInt(cartItemId, 10),

                    eventDate: eventDate,

                    eventEndDate: eventEndDate,

                    eventStartTime: startTime || null,

                    eventEndTime: endTime || null,

                    partyLocation: partyLocation || null,

                    partyPinCode: partyPinCode || null,

                    partyLatitude: partyLatitude,

                    partyLongitude: partyLongitude

                })

            })

                .then(function (response) { return response.json(); })

                .then(function (data) {

                    isUpdatingDate = false;

                    if (data.success) {

                        if (data.dayCount != null) {

                            updateDayCountDisplay(cartItemId, data.dayCount, data.lineTotal);

                        }

                        if (data.orderTotal != null) {

                            const fmt = '₹' + Math.round(data.orderTotal).toLocaleString('en-IN');

                            document.querySelectorAll('.order-subtotal-value, .order-total-value').forEach(function (el) {

                                el.textContent = fmt;

                            });

                        }

                        if (data.recalculate) {

                            const lastReload = sessionStorage.getItem('lastCartReload');

                            const now = Date.now();

                            if (!lastReload || (now - parseInt(lastReload, 10)) > 2000) {

                                sessionStorage.setItem('lastCartReload', now.toString());

                                setTimeout(function () { location.reload(); }, 500);

                            }

                        }

                    }

                })

                .catch(function () {

                    isUpdatingDate = false;

                });

        }



        async function saveAllSchedules() {

            const saves = [];

            dateInputs.forEach(function (input) {

                const cartItemId = input.dataset.cartItemId;

                const dateEl = getInput('.event-date-input', cartItemId);

                const endDateEl = getInput('.event-end-date-input', cartItemId);

                const startEl = getInput('.event-start-input', cartItemId);

                const endEl = getInput('.event-end-input', cartItemId);

                if (!dateEl?.value || !endDateEl?.value || !startEl?.value || !endEl?.value) {

                    return;

                }

                const location = getPartyLocationValues(cartItemId);

                saves.push(

                    fetch((window.basePath || '') + 'Customer/UpdateCartItemDate', {

                        method: 'POST',

                        headers: { 'Content-Type': 'application/json' },

                        credentials: 'same-origin',

                        body: JSON.stringify({

                            cartItemId: parseInt(cartItemId, 10),

                            eventDate: dateEl.value,

                            eventEndDate: endDateEl.value,

                            eventStartTime: startEl.value,

                            eventEndTime: endEl.value,

                            partyLocation: location.partyLocation || null,

                            partyPinCode: location.partyPinCode || null,

                            partyLatitude: location.partyLatitude,

                            partyLongitude: location.partyLongitude

                        })

                    }).then(function (response) { return response.json(); })

                );

            });

            if (saves.length === 0) {

                return true;

            }

            const results = await Promise.all(saves);

            return results.every(function (r) { return r && r.success; });

        }



        // Default times if empty

        startInputs.forEach(function (input) {

            if (!input.value) input.value = '10:00';

        });

        endInputs.forEach(function (input) {

            if (!input.value) input.value = '18:00';

        });



        [dateInputs, endDateInputs, startInputs, endInputs].forEach(function (group) {

            group.forEach(function (input) {

                input.addEventListener('change', function () {

                    onScheduleChange(this.dataset.cartItemId);

                });

            });

        });



        dateInputs.forEach(function (input) {

            validateDateRange(input.dataset.cartItemId);

            validateTimeOrder(input.dataset.cartItemId);

        });

        if (window.PartyLocation) {
            window.PartyLocation.initAll();
        }

        document.querySelectorAll('.party-location-picker-wrap[data-cart-item-id]').forEach(function (wrap) {
            const picker = wrap.querySelector('.party-location-picker');
            if (!picker) return;
            picker.addEventListener('party-location-change', function () {
                onScheduleChange(wrap.dataset.cartItemId);
            });
        });



        checkoutForm.addEventListener('submit', async function (e) {

            if (checkoutForm.dataset.checkoutConfirmed === 'true') {

                return;

            }



            e.preventDefault();



            if (!validateAllDates()) {

                showValidationError();

                return;

            }

            if (!checkoutBtn) {

                checkoutForm.dataset.checkoutConfirmed = 'true';

                checkoutForm.submit();

                return;

            }



            const originalHtml = checkoutBtn.innerHTML;

            checkoutBtn.disabled = true;

            checkoutBtn.innerHTML = '<span class="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true"></span>Processing...';



            try {

                const saved = await saveAllSchedules();

                if (!saved) {

                    window.SmartPop.toast('Could not save your event schedule. Please try again.', 'error');

                    checkoutBtn.disabled = false;

                    checkoutBtn.innerHTML = originalHtml;

                    return;

                }



                checkoutForm.dataset.checkoutConfirmed = 'true';

                checkoutForm.submit();

            } catch (err) {

                console.error('Checkout failed:', err);

                window.SmartPop.toast('Something went wrong. Please try again.', 'error');

                checkoutBtn.disabled = false;

                checkoutBtn.innerHTML = originalHtml;

            }

        });

    });

})();

