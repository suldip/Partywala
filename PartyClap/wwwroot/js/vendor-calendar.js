(function () {
    'use strict';

    var WEEKDAYS = ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'];
    var MAX_CELL_CHIPS = 3;
    var SLOT_REOPEN_BUFFER_MS = 60 * 60 * 1000;
    var BLOCKING_SLOT_TYPES = { booked: true, pending: true, blocked: true, partyclap: true };

    function parseSchedule(raw) {
        if (!raw) return [];
        if (Array.isArray(raw)) return raw;
        try {
            var data = JSON.parse(raw);
            return Array.isArray(data) ? data : [];
        } catch (e) {
            return [];
        }
    }

    function toIso(date) {
        var y = date.getFullYear();
        var m = String(date.getMonth() + 1).padStart(2, '0');
        var d = String(date.getDate()).padStart(2, '0');
        return y + '-' + m + '-' + d;
    }

    function parseIso(iso) {
        var p = iso.split('-');
        return new Date(parseInt(p[0], 10), parseInt(p[1], 10) - 1, parseInt(p[2], 10));
    }

    function formatMonthYear(date) {
        return date.toLocaleDateString(undefined, { month: 'long', year: 'numeric' });
    }

    function formatLongDate(iso) {
        return parseIso(iso).toLocaleDateString(undefined, { weekday: 'long', month: 'long', day: 'numeric', year: 'numeric' });
    }

    function formatTeamsWeekday(iso) {
        return parseIso(iso).toLocaleDateString(undefined, { weekday: 'long' });
    }

    function formatTeamsDateLine(iso) {
        return parseIso(iso).toLocaleDateString(undefined, { day: 'numeric', month: 'long', year: 'numeric' });
    }

    function getSlots(item) {
        if (!item) return [];
        return Array.isArray(item.slots) ? item.slots : [];
    }

    function startOfDay(date) {
        return new Date(date.getFullYear(), date.getMonth(), date.getDate());
    }

    function parseTimeOnDate(iso, timeStr) {
        if (!timeStr) return null;
        var parts = timeStr.split(':');
        if (parts.length < 2) return null;
        var d = parseIso(iso);
        d.setHours(parseInt(parts[0], 10), parseInt(parts[1], 10), 0, 0);
        return d;
    }

    function getSlotEndDate(iso, slot) {
        if (!slot) return null;
        if (slot.endTime) return parseTimeOnDate(iso, slot.endTime);
        if (slot.startTime) {
            var start = parseTimeOnDate(iso, slot.startTime);
            if (!start) return null;
            if (slot.isHourly) {
                return new Date(start.getTime() + 60 * 60 * 1000);
            }
            var end = parseIso(iso);
            end.setHours(23, 59, 59, 999);
            return end;
        }
        var endOfDay = parseIso(iso);
        endOfDay.setHours(23, 59, 59, 999);
        return endOfDay;
    }

    function isSlotExpired(iso, slot, now) {
        if (!slot || !BLOCKING_SLOT_TYPES[slot.slotType]) return false;
        now = now || new Date();
        var day = parseIso(iso);
        var today = startOfDay(now);
        if (day > today) return false;
        if (day < today) return true;
        var end = getSlotEndDate(iso, slot);
        if (!end) return false;
        return now.getTime() >= end.getTime() + SLOT_REOPEN_BUFFER_MS;
    }

    function applySlotExpiryToItem(iso, item, now) {
        if (!item) return item;
        now = now || new Date();
        var slots = getSlots(item).map(function (slot) {
            return Object.assign({}, slot);
        });

        slots.forEach(function (slot) {
            if (BLOCKING_SLOT_TYPES[slot.slotType] && isSlotExpired(iso, slot, now)) {
                slot.slotType = 'completed';
                slot.status = 'Completed';
                slot.editable = false;
            }
        });

        var activeBlocking = slots.filter(function (slot) {
            return BLOCKING_SLOT_TYPES[slot.slotType];
        });

        var next = Object.assign({}, item, { slots: slots });

        if (!activeBlocking.length) {
            next.booked = false;
            next.underProcess = false;
            if (iso === toIso(now)) {
                var completed = slots.filter(function (s) { return s.slotType === 'completed'; });
                if (completed.length && !slots.some(function (s) { return s.slotType === 'available'; })) {
                    var latestReopen = null;
                    completed.forEach(function (slot) {
                        var end = getSlotEndDate(iso, slot);
                        if (!end) return;
                        var reopen = new Date(end.getTime() + SLOT_REOPEN_BUFFER_MS);
                        if (!latestReopen || reopen > latestReopen) latestReopen = reopen;
                    });
                    if (latestReopen && now >= latestReopen) {
                        var openFrom = String(latestReopen.getHours()).padStart(2, '0') + ':' +
                            String(latestReopen.getMinutes()).padStart(2, '0');
                        slots.push({
                            slotType: 'available',
                            startTime: openFrom,
                            endTime: '23:59',
                            label: 'Open for booking',
                            status: 'Available',
                            source: 'Auto'
                        });
                        next.startTime = openFrom;
                        next.endTime = '23:59';
                        next.label = 'Open for booking';
                    }
                }
            }
        } else {
            next.booked = activeBlocking.some(function (s) {
                return s.slotType === 'booked' || s.slotType === 'blocked' || s.slotType === 'partyclap';
            });
            next.underProcess = activeBlocking.some(function (s) { return s.slotType === 'pending'; });
        }

        return next;
    }

    function applySlotExpiryToSchedule(schedule, now) {
        now = now || new Date();
        return (schedule || []).map(function (item) {
            if (!item || !item.date) return item;
            return applySlotExpiryToItem(item.date, item, now);
        });
    }

    function slotTimeText(slot) {
        if (slot.startTime && slot.endTime) return slot.startTime + ' – ' + slot.endTime;
        if (slot.startTime) return slot.startTime;
        return 'All day';
    }

    function formatChipTime(slot) {
        if (slot.startTime && slot.endTime) return slot.startTime + '–' + slot.endTime;
        if (slot.startTime) return slot.startTime;
        return '';
    }

    function isPartyClapSlot(slot) {
        if (!slot) return false;
        if (slot.slotType === 'partyclap') return true;
        var label = (slot.label || '').trim().toLowerCase();
        return label === 'partyclap' || label === 'partyclap';
    }

    function slotChipKind(slot) {
        if (slot.slotType === 'partyclap' || (slot.slotType === 'blocked' && isPartyClapSlot(slot))) return 'partyclap';
        if (slot.slotType === 'booked') return 'booked';
        if (slot.slotType === 'blocked') return 'blocked';
        if (slot.slotType === 'pending') return 'pending';
        if (slot.slotType === 'completed') return 'completed';
        if (slot.slotType === 'available') return 'available';
        return 'open';
    }

    function slotChipTitle(slot) {
        var kind = slotChipKind(slot);
        if (kind === 'partyclap') return 'PartyClap';
        if (kind === 'booked') return slot.customer || slot.label || 'Booked';
        if (kind === 'blocked') return slot.label || 'Blocked';
        if (kind === 'pending') return slot.customer || 'Pending';
        if (kind === 'completed') return slot.label || slot.customer || 'Completed';
        if (kind === 'available') return slot.label || 'Available';
        return slot.label || 'Open';
    }

    function slotLabel(slot) {
        var time = formatChipTime(slot);
        var title = slotChipTitle(slot);
        return time ? (time + ' · ' + title) : title;
    }

    function slotChipClass(slot) {
        return 'vtc-chip-' + slotChipKind(slot);
    }

    function meetingCardType(slot) {
        var kind = slotChipKind(slot);
        if (kind === 'available') return 'hours';
        if (kind === 'partyclap') return 'partyclap';
        if (kind === 'booked' || kind === 'blocked') return 'booked';
        if (kind === 'pending') return 'pending';
        if (kind === 'completed') return 'completed';
        return kind || 'open';
    }

    function renderEventChip(slot) {
        var kind = slotChipKind(slot);
        var time = formatChipTime(slot);
        var title = slotChipTitle(slot);
        var tooltip = slotLabel(slot);
        return '<div class="vtc-event-chip vtc-event-chip-' + kind + '" title="' + escapeAttr(tooltip) + '">' +
            (time ? '<span class="vtc-event-chip-time">' + escapeHtml(time) + '</span>' : '') +
            '<span class="vtc-event-chip-title">' + escapeHtml(title) + '</span>' +
            '</div>';
    }

    function dayClass(item, isPast) {
        if (isPast) return 'vtc-past';
        if (!item) return 'vtc-available';
        if (item.underProcess) return 'vtc-under-process';

        var slots = getSlots(item);
        if (slots.length) {
            var active = slots.filter(function (s) {
                return s.slotType !== 'completed' && s.slotType !== 'available';
            });
            if (active.some(function (s) { return s.slotType === 'pending'; })) return 'vtc-under-process';
            if (active.some(function (s) { return s.slotType === 'booked' || s.slotType === 'blocked'; })) return 'vtc-booked';
            if (active.some(function (s) { return s.slotType === 'partyclap' || isPartyClapSlot(s); })) return 'vtc-partyclap';
        }

        if (item.booked) return 'vtc-booked';
        if (item.underProcess) return 'vtc-under-process';
        return 'vtc-available';
    }

    function chipsHtml(item, isPast) {
        if (isPast) return '';
        var slots = getSlots(item);
        if (slots.length) {
            var html = '<div class="vtc-chips-wrap">';
            var shown = slots.slice(0, MAX_CELL_CHIPS);
            shown.forEach(function (slot) {
                html += renderEventChip(slot);
            });
            if (slots.length > MAX_CELL_CHIPS) {
                html += '<span class="vtc-chip vtc-chip-more">+' + (slots.length - MAX_CELL_CHIPS) + ' more</span>';
            }
            html += '</div>';
            return html;
        }
        if (!item) {
            return '<div class="vtc-chips-wrap"><div class="vtc-event-chip vtc-event-chip-open">' +
                '<span class="vtc-event-chip-title">Open</span></div></div>';
        }
        if (item.booked) {
            var time = (item.startTime && item.endTime) ? (item.startTime + '–' + item.endTime) : '';
            return '<div class="vtc-chips-wrap">' + renderEventChip({
                slotType: isPartyClapSlot({ label: item.label }) ? 'partyclap' : 'booked',
                label: item.label,
                customer: item.customer,
                startTime: item.startTime,
                endTime: item.endTime
            }) + '</div>';
        }
        if (item.underProcess) {
            return '<div class="vtc-chips-wrap">' + renderEventChip({
                slotType: 'pending',
                customer: item.customer,
                startTime: item.startTime,
                endTime: item.endTime
            }) + '</div>';
        }
        if (item.startTime && item.endTime) {
            return '<div class="vtc-chips-wrap">' + renderEventChip({
                slotType: 'available',
                startTime: item.startTime,
                endTime: item.endTime,
                label: item.label || 'Available'
            }) + '</div>';
        }
        return '<div class="vtc-chips-wrap"><div class="vtc-event-chip vtc-event-chip-open">' +
            '<span class="vtc-event-chip-title">Open</span></div></div>';
    }

    function statusBadge(item, isPast) {
        if (isPast) {
            return '<span class="vtc-status-pill booked"><i class="bi bi-clock-history"></i> Past</span>';
        }
        if (!item) {
            return '<span class="vtc-status-pill open"><i class="bi bi-check-circle-fill"></i> Available</span>';
        }
        if (item.booked) {
            var slots = getSlots(item);
            var hasPartyClap = slots.some(function (s) { return slotChipKind(s) === 'partyclap'; });
            if (hasPartyClap && !slots.some(function (s) { return s.slotType === 'booked'; })) {
                return '<span class="vtc-status-pill partyclap"><i class="bi bi-shield-lock-fill"></i> PartyClap block</span>';
            }
            return '<span class="vtc-status-pill booked"><i class="bi bi-calendar-x-fill"></i> Booked / Blocked</span>';
        }
        if (item.underProcess) {
            return '<span class="vtc-status-pill pending"><i class="bi bi-hourglass-split"></i> Under process</span>';
        }
        return '<span class="vtc-status-pill open"><i class="bi bi-check-circle-fill"></i> Available</span>';
    }

    function escapeHtml(text) {
        if (text == null || text === '') return '';
        var div = document.createElement('div');
        div.textContent = String(text);
        return div.innerHTML;
    }

    function escapeAttr(text) {
        return escapeHtml(text).replace(/"/g, '&quot;');
    }

    function formatCurrency(amount) {
        if (amount == null || amount === '') return '';
        return '₹' + Math.round(Number(amount)).toLocaleString('en-IN');
    }

    function formatEventRange(item) {
        if (!item || !item.eventRangeStart) return '';
        if (!item.eventRangeEnd || item.eventRangeEnd === item.eventRangeStart) {
            return formatLongDate(item.eventRangeStart);
        }
        return formatLongDate(item.eventRangeStart) + ' – ' + formatLongDate(item.eventRangeEnd) +
            (item.dayCount ? ' (' + item.dayCount + ' day' + (item.dayCount === 1 ? '' : 's') + ')' : '');
    }

    function detailRow(icon, label, value) {
        if (!value) return '';
        return '<div class="vtc-booking-detail-row">' +
            '<div class="vtc-booking-detail-icon"><i class="bi ' + icon + '"></i></div>' +
            '<div class="vtc-booking-detail-content">' +
            '<div class="vtc-booking-detail-label">' + label + '</div>' +
            '<div class="vtc-booking-detail-value">' + escapeHtml(String(value)) + '</div>' +
            '</div></div>';
    }

    function renderBookingDetailsHtml(item) {
        if (!item) return '';

        var isManual = item.source === 'Manual Block';
        var type = item.underProcess ? 'pending' : (item.booked ? 'booked' : 'open');
        var title = isManual ? (item.label || 'Blocked') : (item.customer || item.label || 'Booking');
        var timeText = (item.startTime && item.endTime) ? (item.startTime + ' – ' + item.endTime) : 'All day';
        var rows = [];

        if (item.serviceType) rows.push(detailRow('bi-briefcase-fill', 'Service', item.serviceType));
        if (item.eventType && !isManual) rows.push(detailRow('bi-stars', 'Event type', item.eventType));
        var range = formatEventRange(item);
        if (range) rows.push(detailRow('bi-calendar-range', 'Event dates', range));
        if (item.startTime && item.endTime) rows.push(detailRow('bi-clock-fill', 'Timing', timeText));
        if (item.customer && !isManual) rows.push(detailRow('bi-person-fill', 'Customer', item.customer));
        if (item.customerPhone) rows.push(detailRow('bi-telephone-fill', 'Phone', item.customerPhone));
        if (item.customerEmail) rows.push(detailRow('bi-envelope-fill', 'Email', item.customerEmail));
        if (item.partyLocation) {
            var address = item.partyLocation + (item.partyPinCode ? ', PIN ' + item.partyPinCode : '');
            rows.push(detailRow('bi-geo-alt-fill', 'Party venue', address));
        } else if (item.partyPinCode) {
            rows.push(detailRow('bi-mailbox', 'PIN code', item.partyPinCode));
        }
        if (item.guestCount) rows.push(detailRow('bi-people-fill', 'Guests', item.guestCount));
        if (item.totalCost) rows.push(detailRow('bi-currency-rupee', 'Total', formatCurrency(item.totalCost)));
        if (item.source) rows.push(detailRow('bi-diagram-3-fill', 'Source', item.source));
        if (item.status) rows.push(detailRow('bi-info-circle-fill', 'Status', item.status));

        return '<div class="vtc-booking-details-card vtc-booking-details-' + type + '">' +
            '<div class="vtc-booking-details-header">' +
            '<div class="vtc-booking-details-accent"></div>' +
            '<div class="vtc-booking-details-heading">' +
            '<div class="vtc-booking-details-time">' + escapeHtml(timeText) + '</div>' +
            '<div class="vtc-booking-details-title">' + escapeHtml(title) + '</div>' +
            '</div></div>' +
            '<div class="vtc-booking-details-body">' + (rows.join('') || '<p class="text-muted small mb-0">No extra details.</p>') + '</div>' +
            '</div>';
    }

    function slotTypeBadge(slot) {
        if (slot.slotType === 'booked') return '<span class="vtc-slot-badge booked">Booked</span>';
        if (slot.slotType === 'pending') return '<span class="vtc-slot-badge pending">Pending</span>';
        if (slot.slotType === 'partyclap' || isPartyClapSlot(slot)) return '<span class="vtc-slot-badge partyclap">PartyClap</span>';
        if (slot.slotType === 'blocked') return '<span class="vtc-slot-badge blocked">Blocked</span>';
        if (slot.slotType === 'completed') return '<span class="vtc-slot-badge completed">Completed</span>';
        if (slot.slotType === 'available') return '<span class="vtc-slot-badge available">Available</span>';
        return '';
    }

    function renderSlotListHtml(slots, antiForgeryToken, deleteAction) {
        if (!slots.length) {
            return '<p class="text-muted small mb-0">No slots yet. Add availability or block time below.</p>';
        }

        return slots.map(function (slot, index) {
            var title = slotChipTitle(slot);
            var meta = [];
            if (slot.source) meta.push(slot.source);
            if (slot.isHourly) meta.push('Hourly');
            if (slot.serviceType) meta.push(slot.serviceType);

            var actions = '';
            if (slot.editable && slot.blockId) {
                actions = '<div class="vtc-slot-actions">' +
                    '<button type="button" class="btn btn-sm btn-light vtc-slot-edit" data-slot-index="' + index + '">' +
                    '<i class="bi bi-pencil"></i> Edit</button>' +
                    '<form method="post" action="' + escapeAttr(deleteAction) + '" class="d-inline">' +
                    (antiForgeryToken || '') +
                    '<input type="hidden" name="blockId" value="' + escapeHtml(String(slot.blockId)) + '" />' +
                    '<button type="submit" class="btn btn-sm btn-light text-danger" title="Delete">' +
                    '<i class="bi bi-trash3"></i></button></form></div>';
            } else if (slot.slotType === 'booked' || slot.slotType === 'pending') {
                actions = '<div class="vtc-slot-actions">' +
                    '<button type="button" class="btn btn-sm btn-light vtc-slot-view" data-slot-index="' + index + '">' +
                    '<i class="bi bi-eye"></i> Details</button></div>';
            }

            var slotKind = slotChipKind(slot);
            return '<div class="vtc-slot-item vtc-slot-' + slotKind + '" data-slot-index="' + index + '">' +
                '<div class="vtc-slot-main">' +
                '<div class="vtc-slot-time">' + escapeHtml(slotTimeText(slot)) + '</div>' +
                '<div class="vtc-slot-title">' + escapeHtml(title) + '</div>' +
                (meta.length ? '<div class="vtc-slot-meta">' + escapeHtml(meta.join(' · ')) + '</div>' : '') +
                '</div>' +
                '<div class="vtc-slot-side">' + slotTypeBadge(slot) + actions + '</div>' +
                '</div>';
        }).join('');
    }

    function meetingCard(type, timeText, title, meta) {
        return '<div class="vtc-meeting-card vtc-meeting-' + type + '">' +
            '<div class="vtc-meeting-accent"></div>' +
            '<div class="vtc-meeting-body">' +
            '<div class="vtc-meeting-time">' + escapeHtml(timeText) + '</div>' +
            '<div class="vtc-meeting-title">' + escapeHtml(title) + '</div>' +
            (meta ? '<div class="vtc-meeting-meta">' + escapeHtml(meta) + '</div>' : '') +
            '</div></div>';
    }

    function addMinutesToTime(timeStr, minutes) {
        if (!timeStr) return '';
        var parts = timeStr.split(':');
        var h = parseInt(parts[0], 10);
        var m = parseInt(parts[1], 10);
        var total = h * 60 + m + minutes;
        if (total >= 24 * 60) total = 24 * 60 - 1;
        var nh = Math.floor(total / 60);
        var nm = total % 60;
        return String(nh).padStart(2, '0') + ':' + String(nm).padStart(2, '0');
    }

    function initTabs(container) {
        if (!container || container.dataset.vacTabsBound) return;
        container.dataset.vacTabsBound = 'true';

        var tabs = container.querySelectorAll('.vac-action-tab');
        var panels = container.querySelectorAll('.vac-tab-panel');

        tabs.forEach(function (tab) {
            tab.addEventListener('click', function () {
                var target = tab.dataset.vacTab;
                tabs.forEach(function (t) {
                    t.classList.toggle('active', t === tab);
                    t.setAttribute('aria-selected', t === tab ? 'true' : 'false');
                });
                panels.forEach(function (p) {
                    p.classList.toggle('active', p.dataset.vacPanel === target);
                });
            });
        });
    }

    function initHourlyControls(modalEl) {
        if (!modalEl || modalEl.dataset.hourlyBound) return;
        modalEl.dataset.hourlyBound = 'true';

        modalEl.querySelectorAll('.vtc-hourly-field').forEach(function (field) {
            var form = field.closest('form');
            var hourlyCheck = field.querySelector('.vtc-form-hourly');
            var durationWrap = field.querySelector('.vtc-duration-btns');
            var startInput = form?.querySelector('.vtc-form-start');
            var endInput = form?.querySelector('.vtc-form-end');
            var activeHours = 1;

            function applyHourlyEnd() {
                if (!hourlyCheck?.checked || !startInput || !endInput) return;
                endInput.value = addMinutesToTime(startInput.value, activeHours * 60);
            }

            hourlyCheck?.addEventListener('change', function () {
                if (durationWrap) durationWrap.style.display = hourlyCheck.checked ? 'flex' : 'none';
                if (hourlyCheck.checked) applyHourlyEnd();
            });

            startInput?.addEventListener('change', applyHourlyEnd);
            startInput?.addEventListener('input', applyHourlyEnd);

            field.querySelectorAll('.vtc-duration-btn').forEach(function (btn) {
                btn.addEventListener('click', function () {
                    field.querySelectorAll('.vtc-duration-btn').forEach(function (b) {
                        b.classList.toggle('active', b === btn);
                    });
                    activeHours = parseInt(btn.dataset.hours, 10) || 1;
                    if (hourlyCheck && !hourlyCheck.checked) {
                        hourlyCheck.checked = true;
                        if (durationWrap) durationWrap.style.display = 'flex';
                    }
                    applyHourlyEnd();
                });
            });
        });
    }

    function initCalendar(root, options) {
        if (!root) return null;

        options = options || {};
        var readOnly = options.readOnly === true || root.dataset.readOnly === 'true';
        var panel = root.closest('.vendor-calendar-panel') || root.parentElement;
        var dayPanel = panel?.querySelector('.vtc-teams-day-panel');
        var emptyEl = dayPanel?.querySelector('.vtc-teams-day-empty');
        var contentEl = dayPanel?.querySelector('.vtc-teams-day-content');
        var weekdayEl = dayPanel?.querySelector('.vtc-teams-weekday');
        var dateLineEl = dayPanel?.querySelector('.vtc-teams-date-line');
        var summaryEl = options.summaryEl || panel?.querySelector('.vtc-day-summary');
        var eventsEl = options.eventsEl || panel?.querySelector('.vtc-day-events');
        var badgeEl = panel?.querySelector('.vtc-day-status-badge');
        var manageBtn = panel?.querySelector('#vtc-open-schedule-btn');

        var scheduleModalEl = document.getElementById('vendor-schedule-modal');
        var modalTitle = scheduleModalEl?.querySelector('#vendorScheduleModalLabel');
        var modalDateLabel = scheduleModalEl?.querySelector('.vtc-modal-date-label');
        var modalSlotsSection = scheduleModalEl?.querySelector('.vtc-modal-slots-section');
        var modalSlotsList = scheduleModalEl?.querySelector('.vtc-modal-slots-list');
        var modalSlotCount = scheduleModalEl?.querySelector('.vtc-modal-slot-count');
        var modalBookingDetails = scheduleModalEl?.querySelector('.vtc-modal-booking-details');
        var modalBookedNote = scheduleModalEl?.querySelector('.vtc-modal-booked-note');
        var modalBookedText = scheduleModalEl?.querySelector('.vtc-modal-booked-text');
        var modalFormsWrap = scheduleModalEl?.querySelector('.vtc-modal-schedule-forms');
        var formDates = scheduleModalEl ? scheduleModalEl.querySelectorAll('.vtc-form-date') : [];
        var scheduleModal = scheduleModalEl && window.bootstrap
            ? bootstrap.Modal.getOrCreateInstance(scheduleModalEl)
            : null;

        var deleteAction = scheduleModalEl?.querySelector('form.vac-action-form')?.getAttribute('action') || '';
        var antiForgeryHtml = scheduleModalEl?.querySelector('input[name="__RequestVerificationToken"]')?.outerHTML || '';

        if (scheduleModalEl) {
            initHourlyControls(scheduleModalEl);
        }

        var state = {
            scheduleMap: {},
            viewMonth: new Date(),
            selectedDate: '',
            editingSlot: null
        };

        state.viewMonth = new Date(state.viewMonth.getFullYear(), state.viewMonth.getMonth(), 1);

        var monthLabel = root.querySelector('.vtc-month-label');
        var weekdaysEl = root.querySelector('.vtc-weekdays');
        var gridEl = root.querySelector('.vtc-grid');
        var todayIso = toIso(new Date());

        function refreshScheduleExpiry() {
            var schedule = Object.keys(state.scheduleMap).map(function (iso) {
                var item = applySlotExpiryToItem(iso, state.scheduleMap[iso]);
                item.date = iso;
                return item;
            });
            state.scheduleMap = {};
            schedule.forEach(function (item) {
                if (item && item.date) state.scheduleMap[item.date] = item;
            });
            render();
            if (state.selectedDate) {
                renderDayDetail(state.selectedDate);
                if (scheduleModalEl && scheduleModalEl.classList.contains('show')) {
                    syncModalForms(state.selectedDate, state.scheduleMap[state.selectedDate]);
                }
            }
        }

        function setSchedule(raw) {
            var schedule = applySlotExpiryToSchedule(parseSchedule(raw));
            state.scheduleMap = {};
            schedule.forEach(function (item) {
                if (item && item.date) state.scheduleMap[item.date] = item;
            });
            render();
            selectDay(state.selectedDate || todayIso, { openModal: false });
        }

        function render() {
            if (monthLabel) monthLabel.textContent = formatMonthYear(state.viewMonth);

            if (weekdaysEl) {
                weekdaysEl.innerHTML = WEEKDAYS.map(function (d) { return '<span>' + d + '</span>'; }).join('');
            }

            if (!gridEl) return;

            var year = state.viewMonth.getFullYear();
            var month = state.viewMonth.getMonth();
            var firstDay = new Date(year, month, 1).getDay();
            var daysInMonth = new Date(year, month + 1, 0).getDate();
            var html = '';

            for (var i = 0; i < firstDay; i++) {
                html += '<div class="vtc-day vtc-empty"></div>';
            }

            for (var day = 1; day <= daysInMonth; day++) {
                var cellDate = new Date(year, month, day);
                var iso = toIso(cellDate);
                var item = state.scheduleMap[iso];
                var isPast = iso < todayIso;
                var classes = ['vtc-day', dayClass(item, isPast)];
                if (state.selectedDate === iso) classes.push('vtc-selected');
                if (iso === todayIso) classes.push('vtc-today');
                html += '<div class="' + classes.join(' ') + '" data-date="' + iso + '">';
                html += '<div class="vtc-day-num">' + day + '</div>';
                html += chipsHtml(item, isPast);
                html += '</div>';
            }

            gridEl.innerHTML = html;
        }

        function buildTeamsEvents(item, isPast) {
            var slots = getSlots(item);
            if (slots.length) {
                return slots.map(function (slot) {
                    var type = meetingCardType(slot);
                    var title = slotChipTitle(slot);
                    var meta = [];
                    if (slot.source) meta.push(slot.source);
                    if (slot.isHourly) meta.push('Hourly');
                    if (slot.serviceType) meta.push(slot.serviceType);
                    return meetingCard(type, slotTimeText(slot), title, meta.join(' · '));
                }).join('');
            }

            if (!item && !isPast) {
                return meetingCard('open', 'All day', 'Available', 'Open for customer bookings on Explore');
            }
            if (!item && isPast) {
                return '<p class="vtc-teams-no-events">No events recorded for this day.</p>';
            }

            var type = 'open';
            var timeText = (item.startTime && item.endTime) ? (item.startTime + ' – ' + item.endTime) : 'All day';
            var title = 'Available';
            var metaParts = [];

            if (item.booked) {
                type = 'booked';
                title = item.customer || item.label || 'Booked / Blocked';
                if (item.source) metaParts.push(item.source);
                if (item.status) metaParts.push(item.status);
            } else if (item.underProcess) {
                type = 'pending';
                title = item.customer || 'Request pending';
                metaParts.push('Under process');
                if (item.source) metaParts.push(item.source);
            } else if (item.startTime && item.endTime) {
                type = 'hours';
                title = 'Working hours';
                metaParts.push('Available for bookings');
            }

            if (item.label && !item.booked) metaParts.unshift(item.label);
            if (item.eventType) metaParts.push(item.eventType);
            if (item.serviceType) metaParts.push(item.serviceType);
            if (item.partyLocation) metaParts.push(item.partyLocation);

            return meetingCard(type, timeText, title, metaParts.join(' · '));
        }

        function renderDayDetail(iso) {
            var isPast = iso < todayIso;
            var item = state.scheduleMap[iso];

            if (emptyEl) emptyEl.style.display = iso ? 'none' : 'block';
            if (contentEl) contentEl.style.display = iso ? 'block' : 'none';

            if (summaryEl) {
                summaryEl.textContent = iso ? formatLongDate(iso) : 'Click a date on the calendar.';
            }

            if (weekdayEl) weekdayEl.textContent = iso ? formatTeamsWeekday(iso) : '';
            if (dateLineEl) dateLineEl.textContent = iso ? formatTeamsDateLine(iso) : '';

            if (badgeEl) {
                badgeEl.style.display = iso ? 'block' : 'none';
                badgeEl.innerHTML = iso ? statusBadge(item, isPast) : '';
            }

            if (eventsEl) {
                eventsEl.innerHTML = iso ? buildTeamsEvents(item, isPast) : '';
            }

            if (manageBtn) {
                manageBtn.style.display = iso && !isPast ? 'inline-flex' : 'none';
            }
        }

        function resetEditForms() {
            state.editingSlot = null;
            if (!scheduleModalEl) return;

            scheduleModalEl.querySelectorAll('.vtc-form-block-id').forEach(function (input) {
                input.value = '';
            });
            scheduleModalEl.querySelectorAll('.vtc-cancel-edit').forEach(function (btn) {
                btn.style.display = 'none';
            });
            scheduleModalEl.querySelectorAll('.vtc-form-hourly').forEach(function (cb) {
                cb.checked = false;
            });
            scheduleModalEl.querySelectorAll('.vtc-duration-btns').forEach(function (wrap) {
                wrap.style.display = 'none';
            });
            scheduleModalEl.querySelectorAll('.vtc-block-form .vtc-form-submit').forEach(function (btn) {
                btn.innerHTML = '<i class="bi bi-slash-circle me-1"></i> Block slot';
            });
        }

        function populateEditForm(slot) {
            if (!scheduleModalEl || !slot || !slot.editable || slot.slotType === 'available') return;

            var form = scheduleModalEl.querySelector('.vtc-block-form');
            if (!form) return;

            var blockIdInput = form.querySelector('.vtc-form-block-id');
            if (blockIdInput) blockIdInput.value = slot.blockId || '';

            if (slot.startTime) form.querySelector('.vtc-form-start').value = slot.startTime;
            if (slot.endTime) form.querySelector('.vtc-form-end').value = slot.endTime;

            var labelInput = form.querySelector('.vtc-form-label');
            if (labelInput && slot.label) labelInput.value = slot.label;

            var hourlyCheck = form.querySelector('.vtc-form-hourly');
            var durationWrap = form.querySelector('.vtc-duration-btns');
            if (hourlyCheck) {
                hourlyCheck.checked = !!slot.isHourly;
                if (durationWrap) durationWrap.style.display = slot.isHourly ? 'flex' : 'none';
            }

            var submitBtn = form.querySelector('.vtc-form-submit');
            if (submitBtn) {
                submitBtn.innerHTML = '<i class="bi bi-slash-circle me-1"></i> Update block';
            }

            var cancelBtn = form.querySelector('.vtc-cancel-edit');
            if (cancelBtn) cancelBtn.style.display = 'inline-block';

            state.editingSlot = slot;
        }

        function slotToDetailItem(slot, dayItem) {
            return {
                booked: slot.slotType === 'booked',
                underProcess: slot.slotType === 'pending',
                customer: slot.customer,
                customerPhone: slot.customerPhone,
                customerEmail: slot.customerEmail,
                serviceType: slot.serviceType,
                eventType: slot.eventType,
                startTime: slot.startTime,
                endTime: slot.endTime,
                partyLocation: slot.partyLocation,
                partyPinCode: slot.partyPinCode,
                source: slot.source,
                status: slot.status,
                totalCost: slot.totalCost,
                guestCount: slot.guestCount,
                label: slot.label,
                eventRangeStart: dayItem?.eventRangeStart,
                eventRangeEnd: dayItem?.eventRangeEnd,
                dayCount: dayItem?.dayCount
            };
        }

        function bindSlotListEvents(iso, item) {
            if (!modalSlotsList || !scheduleModalEl) return;

            modalSlotsList.querySelectorAll('.vtc-slot-edit').forEach(function (btn) {
                btn.addEventListener('click', function () {
                    var index = parseInt(btn.dataset.slotIndex, 10);
                    var slots = getSlots(item);
                    if (slots[index]) populateEditForm(slots[index]);
                });
            });

            modalSlotsList.querySelectorAll('.vtc-slot-view').forEach(function (btn) {
                btn.addEventListener('click', function () {
                    var index = parseInt(btn.dataset.slotIndex, 10);
                    var slots = getSlots(item);
                    if (!slots[index] || !modalBookingDetails) return;
                    modalBookingDetails.style.display = 'block';
                    modalBookingDetails.innerHTML = renderBookingDetailsHtml(slotToDetailItem(slots[index], item));
                });
            });
        }

        function syncModalForms(iso, item) {
            formDates.forEach(function (input) { input.value = iso; });
            resetEditForms();

            if (modalDateLabel) {
                modalDateLabel.textContent = formatLongDate(iso);
            }

            var slots = getSlots(item);
            var isPast = iso < todayIso;
            var hasCustomerSlots = slots.some(function (s) {
                return s.slotType === 'booked' || s.slotType === 'pending';
            });

            if (modalTitle) {
                if (hasCustomerSlots && slots.length === 1 && slots[0].slotType === 'pending') {
                    modalTitle.textContent = 'Request details';
                } else if (hasCustomerSlots && !slots.some(function (s) { return s.editable; })) {
                    modalTitle.textContent = 'Booking details';
                } else {
                    modalTitle.textContent = 'Manage schedule';
                }
            }

            if (modalSlotsSection) {
                modalSlotsSection.style.display = iso ? 'block' : 'none';
            }
            if (modalSlotsList) {
                modalSlotsList.innerHTML = renderSlotListHtml(slots, antiForgeryHtml, deleteAction);
                bindSlotListEvents(iso, item);
            }
            if (modalSlotCount) {
                modalSlotCount.textContent = String(slots.length);
            }

            if (modalBookingDetails) {
                modalBookingDetails.style.display = 'none';
                modalBookingDetails.innerHTML = '';
            }

            if (modalBookedNote) {
                modalBookedNote.style.display = isPast ? 'block' : 'none';
            }
            if (modalBookedText) {
                modalBookedText.textContent = 'Past dates cannot be updated.';
            }
            if (modalFormsWrap) {
                modalFormsWrap.style.display = isPast ? 'none' : 'block';
            }
        }

        function openScheduleModal(iso) {
            if (!scheduleModal || !iso) return;
            var item = state.scheduleMap[iso];
            syncModalForms(iso, item);
            scheduleModal.show();
        }

        function selectDay(iso, opts) {
            opts = opts || {};
            if (!iso) return;

            state.selectedDate = iso;
            render();
            renderDayDetail(iso);

            var isPast = iso < todayIso;
            if (!readOnly && opts.openModal !== false && !isPast && scheduleModal) {
                openScheduleModal(iso);
            }
        }

        function goToToday() {
            var now = new Date();
            state.viewMonth = new Date(now.getFullYear(), now.getMonth(), 1);
            selectDay(todayIso, { openModal: !readOnly });
        }

        if (!root.dataset.vtcBound) {
            root.dataset.vtcBound = 'true';

            root.addEventListener('click', function (e) {
                var cell = e.target.closest('.vtc-day[data-date]');
                if (!cell || !root.contains(cell)) return;
                selectDay(cell.dataset.date);
            });

            root.querySelector('.vtc-prev')?.addEventListener('click', function () {
                state.viewMonth = new Date(state.viewMonth.getFullYear(), state.viewMonth.getMonth() - 1, 1);
                render();
            });

            root.querySelector('.vtc-next')?.addEventListener('click', function () {
                state.viewMonth = new Date(state.viewMonth.getFullYear(), state.viewMonth.getMonth() + 1, 1);
                render();
            });

            root.querySelector('.vtc-today')?.addEventListener('click', goToToday);
        }

        if (scheduleModalEl && !scheduleModalEl.dataset.cancelBound) {
            scheduleModalEl.dataset.cancelBound = 'true';
            scheduleModalEl.querySelectorAll('.vtc-cancel-edit').forEach(function (btn) {
                btn.addEventListener('click', function () {
                    var iso = state.selectedDate;
                    resetEditForms();
                    if (iso) syncModalForms(iso, state.scheduleMap[iso]);
                });
            });
        }

        if (manageBtn && !manageBtn.dataset.bound) {
            manageBtn.dataset.bound = 'true';
            manageBtn.addEventListener('click', function () {
                if (state.selectedDate) openScheduleModal(state.selectedDate);
            });
        }

        setSchedule(root.dataset.schedule || '[]');

        if (!root.dataset.vtcExpiryTimer) {
            root.dataset.vtcExpiryTimer = 'true';
            setInterval(refreshScheduleExpiry, 60 * 1000);
        }

        return {
            setSchedule: setSchedule,
            selectDay: selectDay,
            goToToday: goToToday,
            openScheduleModal: openScheduleModal
        };
    }

    window.VendorCalendar = {
        init: initCalendar
    };

    document.addEventListener('DOMContentLoaded', function () {
        var el = document.getElementById('vendor-teams-calendar');
        if (el) initCalendar(el);
    });
})();
