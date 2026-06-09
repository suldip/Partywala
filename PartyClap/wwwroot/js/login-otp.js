(function () {
    'use strict';

    function submitSendOtpForm(mobile, loginType, btnGetOtp) {
        var sendForm = document.getElementById('sendOtpForm');
        var mobileField = document.getElementById('sendOtpMobileField');
        var loginTypeField = document.getElementById('sendOtpLoginTypeField');
        if (!sendForm || !mobileField) {
            throw new Error('Could not send OTP. Please refresh the page and try again.');
        }

        var originalText = btnGetOtp.textContent.trim() || 'Get One-Time Password';
        btnGetOtp.innerHTML = '<span class="spinner-border spinner-border-sm me-2"></span>Sending...';
        btnGetOtp.disabled = true;
        mobileField.value = mobile;
        if (loginTypeField) {
            loginTypeField.value = loginType;
        }
        sendForm.submit();
        return originalText;
    }

    function initLoginOtp() {
        var form = document.getElementById('loginForm');
        if (!form || form.dataset.loginOtpBound === 'true') {
            return;
        }
        form.dataset.loginOtpBound = 'true';
        var loginTypeInput = document.getElementById('loginType');
        var loginTypeTabs = document.querySelectorAll('.login-role-tab');
        var mobileInput = document.getElementById('mobile');
        var mobileLabel = document.getElementById('mobile-label');
        var mobileHint = document.getElementById('mobile-hint');
        var otpSection = document.getElementById('otpSection');
        var mobileOtpSection = document.getElementById('mobileOtpSection');
        var adminPasswordSection = document.getElementById('adminPasswordSection');
        var finalOtpInput = document.getElementById('otp');
        var btnGetOtp = document.getElementById('btnGetOtp');
        var btnLogin = document.getElementById('btnLogin');
        var btnResend = document.getElementById('btnResendOtp');
        var adminPassword = document.getElementById('adminPassword');
        var otpInputs = document.querySelectorAll('#loginForm .otp-digit');
        var otpSentText = document.getElementById('otpSentText');
        var statusBox = document.getElementById('loginOtpStatus');
        var googleLoginBtn = document.getElementById('googleLoginBtn');
        var googleLoginDivider = document.getElementById('googleLoginDivider');

        function getLoginType() {
            return loginTypeInput ? loginTypeInput.value : 'Customer';
        }

        function setLoginType(loginType) {
            if (loginTypeInput) {
                loginTypeInput.value = loginType;
            }
            loginTypeTabs.forEach(function (tab) {
                tab.classList.toggle('active', tab.dataset.loginType === loginType);
            });
            updateLoginTypeUi();
        }

        function isAdminLogin() {
            return getLoginType() === 'Admin';
        }

        function isAdminEmail(value) {
            return value && value.indexOf('@') !== -1;
        }

        function resetCredentialStep() {
            otpSection.style.display = 'none';
            mobileOtpSection.style.display = 'none';
            adminPasswordSection.style.display = 'none';
            btnGetOtp.style.display = 'block';
            btnLogin.style.display = 'none';
            if (finalOtpInput) {
                finalOtpInput.value = '';
            }
            if (adminPassword) {
                adminPassword.value = '';
            }
            otpInputs.forEach(function (input) {
                input.value = '';
            });
            clearStatus();
        }

        function updateLoginTypeUi() {
            var loginType = getLoginType();
            var isAdmin = loginType === 'Admin';

            if (googleLoginBtn) {
                googleLoginBtn.style.display = loginType === 'Customer' ? '' : 'none';
            }
            if (googleLoginDivider) {
                googleLoginDivider.style.display = loginType === 'Customer' ? '' : 'none';
            }

            if (mobileLabel) {
                mobileLabel.innerHTML = isAdmin
                    ? '<i class="bi bi-envelope me-2"></i>Admin Email'
                    : '<i class="bi bi-phone me-2"></i>Mobile Number';
            }

            if (mobileHint) {
                if (isAdmin) {
                    mobileHint.textContent = 'Enter your admin email address and password.';
                } else if (loginType === 'Vendor') {
                    mobileHint.textContent = 'Enter the mobile number registered with your vendor account.';
                } else {
                    mobileHint.textContent = 'Enter your registered 10-digit Indian mobile number (starts with 6–9).';
                }
            }

            if (mobileInput) {
                if (isAdmin) {
                    mobileInput.maxLength = 254;
                    mobileInput.inputMode = 'email';
                    mobileInput.type = 'email';
                    mobileInput.placeholder = 'Admin Email';
                } else {
                    mobileInput.maxLength = 10;
                    mobileInput.inputMode = 'numeric';
                    mobileInput.type = 'text';
                    mobileInput.placeholder = 'Mobile Number';
                    mobileInput.value = normalizeIndianMobile(mobileInput.value).slice(0, 10);
                }
            }

            if (btnGetOtp) {
                btnGetOtp.textContent = isAdmin ? 'Continue' : 'Get One-Time Password';
            }

            resetCredentialStep();
        }

        function showStatus(message, type) {
            if (!statusBox) {
                return;
            }
            statusBox.className = 'alert border-0 rounded-3 mb-3';
            if (type === 'error') {
                statusBox.classList.add('alert-danger');
            } else if (type === 'success') {
                statusBox.classList.add('alert-success');
            } else {
                statusBox.classList.add('alert-warning');
            }
            statusBox.textContent = message;
            statusBox.style.display = 'block';
        }

        function clearStatus() {
            if (statusBox) {
                statusBox.style.display = 'none';
                statusBox.textContent = '';
            }
        }

        function toast(type, title, message) {
            showStatus(message, type === 'success' ? 'success' : type === 'error' ? 'error' : 'warning');
            if (!window.SmartPop) {
                return;
            }
            if (type === 'success') {
                window.SmartPop.success(title, message);
            } else if (type === 'error') {
                window.SmartPop.error(title, message);
            } else {
                window.SmartPop.warning(title, message);
            }
        }

        function normalizeIndianMobile(value) {
            var digits = (value || '').replace(/\D/g, '');
            if (digits.length === 12 && digits.indexOf('91') === 0) {
                digits = digits.slice(2);
            }
            if (digits.length === 11 && digits.charAt(0) === '0') {
                digits = digits.slice(1);
            }
            return digits;
        }

        function validateIndianMobile(value) {
            var normalized = normalizeIndianMobile(value);
            if (!normalized) {
                return 'Enter a valid 10-digit Indian mobile number.';
            }
            if (normalized.length !== 10) {
                return 'Mobile number must be exactly 10 digits.';
            }
            if (!/^[6-9]\d{9}$/.test(normalized)) {
                return 'Enter a valid Indian mobile number starting with 6, 7, 8, or 9.';
            }
            return null;
        }

        function showOtpUi() {
            otpSection.style.display = 'block';
            mobileOtpSection.style.display = 'block';
            adminPasswordSection.style.display = 'none';
            btnGetOtp.style.display = 'none';
            btnLogin.style.display = 'block';
            showResendButton();
            otpSection.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
        }

        function showAdminPasswordUi() {
            otpSection.style.display = 'block';
            mobileOtpSection.style.display = 'none';
            adminPasswordSection.style.display = 'block';
            btnGetOtp.style.display = 'none';
            btnLogin.style.display = 'block';
            adminPassword.focus();
        }

        function updateHiddenOtp() {
            var otpValue = '';
            otpInputs.forEach(function (input) {
                otpValue += input.value;
            });
            finalOtpInput.value = otpValue;
            return otpValue;
        }

        function showResendButton() {
            if (btnResend) {
                btnResend.style.display = 'inline-block';
                btnResend.disabled = false;
            }
        }

        function sendOtpRequest(mobile, loginType) {
            clearStatus();
            try {
                submitSendOtpForm(mobile, loginType, btnGetOtp);
            } catch (err) {
                btnGetOtp.disabled = false;
                toast('error', 'Could Not Send OTP', err.message || 'Please refresh and try again.');
            }
            return false;
        }

        loginTypeTabs.forEach(function (tab) {
            tab.addEventListener('click', function () {
                setLoginType(tab.dataset.loginType || 'Customer');
            });
        });

        if (loginTypeInput) {
            updateLoginTypeUi();
        }

        if (mobileInput) {
            mobileInput.addEventListener('input', function () {
                if (isAdminLogin()) {
                    return;
                }

                this.maxLength = 10;
                this.inputMode = 'numeric';
                this.value = normalizeIndianMobile(this.value).slice(0, 10);
            });
        }

        if (btnGetOtp) {
            btnGetOtp.addEventListener('click', function () {
                var mobile = mobileInput.value.trim();
                var loginType = getLoginType();

                if (!mobile) {
                    toast('warning', 'Input Required', isAdminLogin()
                        ? 'Please enter your admin email.'
                        : 'Please enter your mobile number.');
                    return;
                }

                if (isAdminLogin()) {
                    if (!isAdminEmail(mobile)) {
                        toast('warning', 'Invalid Email', 'Please enter a valid admin email address.');
                        return;
                    }
                    showAdminPasswordUi();
                    return;
                }

                var phoneError = validateIndianMobile(mobile);
                if (phoneError) {
                    toast('warning', 'Invalid mobile number', phoneError);
                    return;
                }

                mobile = normalizeIndianMobile(mobile);
                mobileInput.value = mobile;
                sendOtpRequest(mobile, loginType);
            });
        }

        if (btnResend) {
            btnResend.addEventListener('click', function () {
                var mobile = normalizeIndianMobile(mobileInput.value.trim());
                if (!mobile) {
                    return;
                }
                sendOtpRequest(mobile, getLoginType());
            });
        }

        otpInputs.forEach(function (input, index) {
            input.addEventListener('input', function (e) {
                if (e.inputType === 'insertText' && e.data && isNaN(e.data)) {
                    e.target.value = '';
                    return;
                }

                if (e.target.value.length === 1 && index < otpInputs.length - 1) {
                    otpInputs[index + 1].focus();
                }

                var otpValue = updateHiddenOtp();
                if (otpValue.length === otpInputs.length && btnLogin) {
                    btnLogin.focus();
                }
            });

            input.addEventListener('keydown', function (e) {
                if (e.key === 'Backspace' && !e.target.value && index > 0) {
                    otpInputs[index - 1].focus();
                }
            });

            input.addEventListener('paste', function (e) {
                e.preventDefault();
                var pasted = (e.clipboardData.getData('text') || '').replace(/\D/g, '').slice(0, 6);
                if (!pasted) {
                    return;
                }
                pasted.split('').forEach(function (char, i) {
                    if (otpInputs[i]) {
                        otpInputs[i].value = char;
                    }
                });
                updateHiddenOtp();
                var focusIndex = Math.min(pasted.length, otpInputs.length) - 1;
                if (focusIndex >= 0) {
                    otpInputs[focusIndex].focus();
                }
            });
        });

        if (adminPassword) {
            adminPassword.addEventListener('input', function (e) {
                finalOtpInput.value = e.target.value;
            });
        }

        form.addEventListener('submit', function (e) {
            if (isAdminLogin()) {
                if (!isAdminEmail(mobileInput.value.trim())) {
                    e.preventDefault();
                    toast('warning', 'Invalid Email', 'Please enter a valid admin email address.');
                    return;
                }
                if (adminPassword && !finalOtpInput.value) {
                    finalOtpInput.value = adminPassword.value;
                }
                if (!finalOtpInput.value) {
                    e.preventDefault();
                    toast('warning', 'Password Required', 'Please enter your admin password.');
                    return;
                }
                return;
            }

            var error = validateIndianMobile(mobileInput.value);
            if (error) {
                e.preventDefault();
                toast('warning', 'Invalid mobile number', error);
                return;
            }
            mobileInput.value = normalizeIndianMobile(mobileInput.value);

            if (mobileOtpSection.style.display !== 'none') {
                var otpValue = updateHiddenOtp();
                if (otpValue.length !== 6) {
                    e.preventDefault();
                    toast('warning', 'OTP Required', 'Enter the 6-digit code sent to your phone.');
                    return;
                }
            }
        });

        if (form.dataset.showOtp === 'true') {
            if (isAdminLogin()) {
                showAdminPasswordUi();
            } else {
                showOtpUi();
                if (otpSentText && mobileInput.value) {
                    otpSentText.textContent = 'A 6-digit verification code has been sent to +91 ' + mobileInput.value + '. It expires in 1 minute.';
                }
                showResendButton();
                if (otpInputs.length > 0) {
                    otpInputs[0].focus();
                }
            }
        }
    }

    window.PartyClapLoginOtp = { init: initLoginOtp };

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initLoginOtp);
    } else {
        initLoginOtp();
    }
})();
