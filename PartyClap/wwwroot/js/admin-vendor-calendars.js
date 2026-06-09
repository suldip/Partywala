(function () {
    'use strict';

    var calendarApi = null;

    function getBasePath() {
        return window.basePath || '';
    }

    function setActiveVendor(vendorId) {
        document.querySelectorAll('.admin-vendor-item').forEach(function (item) {
            item.classList.toggle('active', item.dataset.vendorId === vendorId);
        });
    }

    function isPartyClapLabel(label) {
        if (!label || !String(label).trim()) return true;
        return String(label).trim().toLowerCase() === 'partyclap';
    }

    function formatBlockDate(iso) {
        var d = new Date(iso + 'T12:00:00');
        return d.toLocaleDateString(undefined, { weekday: 'short', month: 'short', day: 'numeric' });
    }

    function updateVendorMeta(vendor) {
        var nameEl = document.getElementById('admin-selected-vendor-name');
        var metaEl = document.getElementById('admin-selected-vendor-meta');
        var profileLink = document.getElementById('admin-vendor-profile-link');

        if (nameEl) nameEl.textContent = vendor?.name || 'Select a vendor';
        if (metaEl) {
            var parts = [];
            if (vendor?.phone) parts.push(vendor.phone);
            if (vendor?.pinCode) parts.push('PIN ' + vendor.pinCode);
            if (vendor?.address) parts.push(vendor.address);
            metaEl.textContent = parts.join(' · ') || '';
        }
        if (profileLink && vendor?.id) {
            profileLink.href = getBasePath() + 'Customer/VendorProfile?vendorId=' + encodeURIComponent(vendor.id);
            profileLink.classList.remove('d-none');
        } else if (profileLink) {
            profileLink.classList.add('d-none');
        }
    }

    function updateVacStats(stats) {
        if (!stats) return;

        var openEl = document.querySelector('[data-vac-stat="open"]');
        var bookedEl = document.querySelector('[data-vac-stat="booked"]');
        var pendingEl = document.querySelector('[data-vac-stat="pending"]');
        var manualEl = document.querySelector('[data-vac-stat="manual"]');

        if (openEl) openEl.textContent = String(stats.available ?? 0);
        if (bookedEl) bookedEl.textContent = String(stats.booked ?? 0);
        if (pendingEl) pendingEl.textContent = String(stats.underProcess ?? 0);
        if (manualEl) manualEl.textContent = String(stats.manual ?? 0);
    }

    function renderManualEntries(blocks) {
        var wrap = document.getElementById('admin-vendor-calendar-manual-entries');
        if (!wrap) return;

        var entries = (blocks || []).filter(function (block) {
            return !block.isAvailable;
        }).slice(0, 12);

        if (!entries.length) {
            wrap.innerHTML =
                '<div class="vac-side-card">' +
                '<div class="vac-side-card-header"><span><i class="bi bi-list-ul"></i> Manual entries</span></div>' +
                '<div class="p-3"><p class="text-muted small mb-0">No manual calendar entries.</p></div>' +
                '</div>';
            return;
        }

        var itemsHtml = entries.map(function (block) {
            var label = (block.label && String(block.label).trim()) ? String(block.label).trim() : 'PartyClap';
            var isPartyClap = isPartyClapLabel(label);
            var timeHtml = (block.startTime && block.endTime)
                ? '<span class="vac-entry-time"><i class="bi bi-clock me-1"></i>' + block.startTime + ' – ' + block.endTime + '</span>'
                : '';
            return (
                '<div class="vac-entry-item ' + (isPartyClap ? 'vac-entry-partyclap' : 'vac-entry-blocked') + '">' +
                '<div class="vac-entry-icon"><i class="bi ' + (isPartyClap ? 'bi-shield-lock-fill' : 'bi-x-lg') + '"></i></div>' +
                '<div class="vac-entry-body">' +
                '<div class="vac-entry-date">' + formatBlockDate(block.date) + '</div>' +
                '<div class="vac-entry-meta">' + timeHtml +
                '<span class="vac-entry-label ' + (isPartyClap ? 'vac-entry-label-partyclap' : '') + '">' + label + '</span>' +
                '</div></div></div>'
            );
        }).join('');

        wrap.innerHTML =
            '<div class="vac-side-card">' +
            '<div class="vac-side-card-header d-flex justify-content-between align-items-center">' +
            '<span><i class="bi bi-list-ul"></i> Manual entries</span>' +
            '<span class="badge rounded-pill vac-entry-count">' + entries.length + '</span>' +
            '</div>' +
            '<div class="vac-entry-list vac-entry-list-grid">' + itemsHtml + '</div>' +
            '</div>';
    }

    function loadVendorCalendar(vendorId) {
        if (!vendorId) return;

        setActiveVendor(vendorId);
        var calendarRoot = document.getElementById('admin-vendor-calendar');
        if (calendarRoot) calendarRoot.classList.add('opacity-50');

        fetch(getBasePath() + 'Admin/GetVendorScheduleJson?vendorId=' + encodeURIComponent(vendorId))
            .then(function (r) { return r.json(); })
            .then(function (data) {
                if (calendarRoot) calendarRoot.classList.remove('opacity-50');
                if (!data.success) return;

                updateVendorMeta(data.vendor);
                updateVacStats(data.stats);
                renderManualEntries(data.blocks);

                if (!calendarApi && calendarRoot && window.VendorCalendar) {
                    calendarApi = window.VendorCalendar.init(calendarRoot, { readOnly: true });
                }
                if (calendarApi) {
                    calendarApi.setSchedule(data.schedule);
                } else if (calendarRoot && window.VendorCalendar) {
                    calendarRoot.dataset.schedule = JSON.stringify(data.schedule);
                    calendarApi = window.VendorCalendar.init(calendarRoot, { readOnly: true });
                }

                if (window.history && window.history.replaceState) {
                    var url = new URL(window.location.href);
                    url.searchParams.set('vendorId', vendorId);
                    window.history.replaceState({}, '', url.toString());
                }
            })
            .catch(function () {
                if (calendarRoot) calendarRoot.classList.remove('opacity-50');
            });
    }

    function initSearch() {
        var input = document.getElementById('admin-vendor-search');
        if (!input) return;

        input.addEventListener('input', function () {
            var q = this.value.trim().toLowerCase();
            document.querySelectorAll('.admin-vendor-item').forEach(function (item) {
                var text = (item.dataset.search || '').toLowerCase();
                item.style.display = !q || text.indexOf(q) !== -1 ? '' : 'none';
            });
        });
    }

    document.addEventListener('DOMContentLoaded', function () {
        var panel = document.getElementById('admin-vendor-calendars');
        if (!panel) return;

        initSearch();

        document.querySelectorAll('.admin-vendor-item').forEach(function (item) {
            item.addEventListener('click', function () {
                loadVendorCalendar(this.dataset.vendorId);
            });
        });

        var initialId = panel.dataset.selectedVendorId;
        var calendarRoot = document.getElementById('admin-vendor-calendar');
        if (calendarRoot && window.VendorCalendar) {
            calendarApi = window.VendorCalendar.init(calendarRoot, { readOnly: true });
        }

        if (initialId) {
            loadVendorCalendar(initialId);
        }
    });

    window.AdminVendorCalendars = { load: loadVendorCalendar };
})();
