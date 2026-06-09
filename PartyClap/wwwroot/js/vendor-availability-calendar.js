(function () {
    'use strict';

    var WEEKDAYS = ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'];
    var SLOT_REOPEN_BUFFER_MS = 60 * 60 * 1000;
    var BLOCKING_SLOT_TYPES = { booked: true, pending: true, blocked: true, partyclap: true };

    function parseIso(iso) {
        var p = iso.split('-');
        return new Date(parseInt(p[0], 10), parseInt(p[1], 10) - 1, parseInt(p[2], 10));
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
            if (slot.isHourly) return new Date(start.getTime() + 60 * 60 * 1000);
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

    function resolveDayAvailability(iso, item, now) {
        if (!item) return { booked: false, underProcess: false, label: '' };
        now = now || new Date();
        var slots = Array.isArray(item.slots) ? item.slots : [];
        if (!slots.length) {
            return {
                booked: !!item.booked,
                underProcess: !!item.underProcess,
                label: item.label || ''
            };
        }

        var activeBlocking = slots.filter(function (slot) {
            return BLOCKING_SLOT_TYPES[slot.slotType] && !isSlotExpired(iso, slot, now);
        });

        return {
            booked: activeBlocking.some(function (s) {
                return s.slotType === 'booked' || s.slotType === 'blocked' || s.slotType === 'partyclap';
            }),
            underProcess: activeBlocking.some(function (s) { return s.slotType === 'pending'; }),
            label: item.label || ''
        };
    }

    function parseSchedule(raw) {
        if (!raw) return {};
        try {
            var list = JSON.parse(raw);
            var map = {};
            var now = new Date();
            if (Array.isArray(list)) {
                list.forEach(function (item) {
                    if (item && item.date) {
                        map[item.date] = resolveDayAvailability(item.date, item, now);
                    }
                });
            }
            return map;
        } catch (e) {
            return {};
        }
    }

    function formatMonthYear(date) {
        return date.toLocaleDateString(undefined, { month: 'long', year: 'numeric' });
    }

    function formatDisplayDate(iso) {
        var parts = iso.split('-');
        var d = new Date(parseInt(parts[0], 10), parseInt(parts[1], 10) - 1, parseInt(parts[2], 10));
        return d.toLocaleDateString(undefined, { weekday: 'short', month: 'short', day: 'numeric' });
    }

    function toIso(date) {
        var y = date.getFullYear();
        var m = String(date.getMonth() + 1).padStart(2, '0');
        var d = String(date.getDate()).padStart(2, '0');
        return y + '-' + m + '-' + d;
    }

    function todayIso() {
        return toIso(new Date());
    }

    function getScheduleFromContainer(container) {
        var scriptEl = container.querySelector('.vac-schedule-json');
        if (scriptEl && scriptEl.textContent) {
            return parseSchedule(scriptEl.textContent);
        }
        return parseSchedule(container.dataset.schedule);
    }

    function renderCalendar(container) {
        var schedule = getScheduleFromContainer(container);
        var viewMonth = container._viewMonth || new Date();
        if (!(viewMonth instanceof Date)) {
            viewMonth = new Date();
        }
        viewMonth = new Date(viewMonth.getFullYear(), viewMonth.getMonth(), 1);
        container._viewMonth = viewMonth;

        var monthLabel = container.querySelector('.vac-month-label');
        var weekdaysEl = container.querySelector('.vac-weekdays');
        var gridEl = container.querySelector('.vac-grid');
        if (!gridEl) return;

        if (monthLabel) {
            monthLabel.textContent = formatMonthYear(viewMonth);
        }

        if (weekdaysEl) {
            weekdaysEl.innerHTML = WEEKDAYS.map(function (d) {
                return '<span>' + d + '</span>';
            }).join('');
        }

        var year = viewMonth.getFullYear();
        var month = viewMonth.getMonth();
        var firstDay = new Date(year, month, 1).getDay();
        var daysInMonth = new Date(year, month + 1, 0).getDate();
        var today = todayIso();
        var selected = container.dataset.selectedDate || '';
        var html = '';

        for (var i = 0; i < firstDay; i++) {
            html += '<span class="vac-day vac-empty"></span>';
        }

        for (var day = 1; day <= daysInMonth; day++) {
            var cellDate = new Date(year, month, day);
            var iso = toIso(cellDate);
            var isPast = iso < today;
            var slot = schedule[iso];
            var isBooked = slot && slot.booked === true;
            var isUnderProcess = slot && slot.underProcess === true;
            var isAvailable = schedule.hasOwnProperty(iso) && !isBooked && !isUnderProcess && !isPast;
            var classes = ['vac-day'];

            if (isPast) {
                classes.push('vac-past');
            } else if (isBooked) {
                classes.push('vac-booked');
            } else if (isUnderProcess) {
                classes.push('vac-under-process');
            } else if (isAvailable) {
                classes.push('vac-available');
            } else {
                classes.push('vac-past');
            }

            if (selected === iso) {
                classes.push('vac-selected');
            }

            var attrs = 'data-date="' + iso + '"';
            if (isAvailable) {
                attrs += ' tabindex="0" role="button"';
            }

            html += '<span class="' + classes.join(' ') + '" ' + attrs + '>' + day + '</span>';
        }

        gridEl.innerHTML = html;
        updateSelectedLabel(container);
    }

    function updateSelectedLabel(container) {
        var label = container.querySelector('.vac-selected-label');
        if (!label) return;

        var selected = container.dataset.selectedDate;
        if (selected) {
            label.style.display = 'block';
            label.textContent = 'Booking date: ' + formatDisplayDate(selected);
        } else {
            label.style.display = 'none';
            label.textContent = '';
        }

        var card = container.closest('.vendor-card');
        if (card) {
            card.dataset.selectedPartyDate = selected || '';
        }
    }

    function selectDate(container, iso) {
        container.dataset.selectedDate = iso;
        renderCalendar(container);
        container.dispatchEvent(new CustomEvent('vac-date-selected', {
            bubbles: true,
            detail: { vendorId: container.dataset.vendorId, date: iso }
        }));
    }

    function initContainer(container) {
        if (!container || container.dataset.vacInit === 'true') return;
        container.dataset.vacInit = 'true';

        var initial = container.dataset.initialDate;
        var schedule = getScheduleFromContainer(container);
        if (initial && schedule[initial] && !schedule[initial].booked && !schedule[initial].underProcess) {
            container.dataset.selectedDate = initial;
        } else {
            var today = todayIso();
            Object.keys(schedule).sort().some(function (iso) {
                var slot = schedule[iso];
                if (iso >= today && slot && !slot.booked && !slot.underProcess) {
                    container.dataset.selectedDate = iso;
                    return true;
                }
                return false;
            });
        }

        var initialMonth = container.dataset.selectedDate || initial || todayIso();
        var parts = initialMonth.split('-');
        container._viewMonth = new Date(parseInt(parts[0], 10), parseInt(parts[1], 10) - 1, 1);
        renderCalendar(container);

        container.addEventListener('click', function (e) {
            var day = e.target.closest('.vac-day.vac-available');
            if (!day || !container.contains(day)) return;
            selectDate(container, day.dataset.date);
        });

        container.addEventListener('keydown', function (e) {
            if (e.key !== 'Enter' && e.key !== ' ') return;
            var day = e.target.closest('.vac-day.vac-available');
            if (!day) return;
            e.preventDefault();
            selectDate(container, day.dataset.date);
        });

        var prev = container.querySelector('.vac-prev');
        var next = container.querySelector('.vac-next');
        if (prev) {
            prev.addEventListener('click', function () {
                var m = container._viewMonth;
                container._viewMonth = new Date(m.getFullYear(), m.getMonth() - 1, 1);
                renderCalendar(container);
            });
        }
        if (next) {
            next.addEventListener('click', function () {
                var m = container._viewMonth;
                container._viewMonth = new Date(m.getFullYear(), m.getMonth() + 1, 1);
                renderCalendar(container);
            });
        }

        if (!container.dataset.vacExpiryTimer) {
            container.dataset.vacExpiryTimer = 'true';
            setInterval(function () { renderCalendar(container); }, 60 * 1000);
        }
    }

    function initAll(root) {
        (root || document).querySelectorAll('.vendor-availability-calendar').forEach(initContainer);
    }

    window.VendorAvailabilityCalendar = {
        init: initAll,
        getSelectedDate: function (vendorId) {
            var el = document.querySelector('.vendor-availability-calendar[data-vendor-id="' + vendorId + '"]');
            return el ? (el.dataset.selectedDate || '') : '';
        },
        getSelectedDateForButton: function (button) {
            if (!button) return '';
            var scope = button.closest('.vendor-card') || button.closest('.vendor-profile-booking') || document;
            var calendar = scope.querySelector('.vendor-availability-calendar');
            return calendar ? (calendar.dataset.selectedDate || '') : '';
        },
        markDateRangeUnderProcess: function (vendorId, startIso, endIso) {
            if (!startIso) return;
            var start = new Date(startIso);
            var end = new Date(endIso || startIso);
            for (var d = new Date(start); d <= end; d.setDate(d.getDate() + 1)) {
                this.markDateUnderProcess(vendorId, d.toISOString().slice(0, 10));
            }
        },
        markDateUnderProcess: function (vendorId, iso) {
            var el = document.querySelector('.vendor-availability-calendar[data-vendor-id="' + vendorId + '"]');
            if (!el) return;
            var scriptEl = el.querySelector('.vac-schedule-json');
            if (!scriptEl) return;
            try {
                var list = JSON.parse(scriptEl.textContent || '[]');
                var found = false;
                list.forEach(function (item) {
                    if (item.date === iso) {
                        item.underProcess = true;
                        item.booked = false;
                        found = true;
                    }
                });
                if (!found) {
                    list.push({ date: iso, booked: false, underProcess: true });
                }
                scriptEl.textContent = JSON.stringify(list);
                renderCalendar(el);
            } catch (e) { /* ignore */ }
        }
    };

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', function () { initAll(); });
    } else {
        initAll();
    }
})();
