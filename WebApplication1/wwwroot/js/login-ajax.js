document.addEventListener('DOMContentLoaded', function () {
    const loginForm = document.querySelector('.login-form');
    const submitButton = loginForm.querySelector('.sign-in-btn');
    const emailInput = document.getElementById('email');
    const passwordInput = document.getElementById('password');

    loginForm.addEventListener('submit', async function (e) {
        e.preventDefault();

        // Clear previous errors
        clearErrors();

        // Disable submit button
        submitButton.disabled = true;
        submitButton.textContent = 'Signing in...';

        const formData = new FormData(loginForm);
        const token = document.querySelector('input[name="__RequestVerificationToken"]').value;

        try {
            const response = await fetch(loginForm.action, {
                method: 'POST',
                headers: {
                    'X-Requested-With': 'XMLHttpRequest',
                    'RequestVerificationToken': token
                },
                body: formData
            });

            const result = await response.json();

            if (result.success) {
                // Show success message
                showSuccessMessage('Login successful! Redirecting...');

                // Set flag to trigger fingerprint prompt after redirect
                sessionStorage.setItem('justLoggedIn', 'true');

                // Redirect after a short delay
                setTimeout(() => {
                    window.location.href = result.redirectUrl;
                }, 1000);
            } else {
                // Show errors
                if (result.errors && Array.isArray(result.errors)) {
                    showGeneralErrors(result.errors);
                }

                // Re-enable submit button
                submitButton.disabled = false;
                submitButton.textContent = 'Sign In';
            }
        } catch (error) {
            console.error('Login error:', error);
            showGeneralErrors(['An unexpected error occurred. Please try again.']);

            // Re-enable submit button
            submitButton.disabled = false;
            submitButton.textContent = 'Sign In';
        }
    });

    function clearErrors() {
        // Remove all error messages
        const errorElements = document.querySelectorAll('.text-danger, .alert-danger');
        errorElements.forEach(el => el.remove());

        // Remove error styling from inputs
        [emailInput, passwordInput].forEach(input => {
            input.classList.remove('input-error');
        });
    }

    function showGeneralErrors(errors) {
        const formContainer = document.querySelector('.login-form-container');
        const errorDiv = document.createElement('div');
        errorDiv.className = 'alert alert-danger';
        errorDiv.style.marginBottom = '1rem';

        if (errors.length === 1) {
            errorDiv.textContent = errors[0];
        } else {
            const ul = document.createElement('ul');
            ul.style.margin = '0';
            ul.style.paddingLeft = '1.5rem';
            errors.forEach(error => {
                const li = document.createElement('li');
                li.textContent = error;
                ul.appendChild(li);
            });
            errorDiv.appendChild(ul);
        }

        formContainer.insertBefore(errorDiv, loginForm);
    }

    function showSuccessMessage(message) {
        const formContainer = document.querySelector('.login-form-container');
        const successDiv = document.createElement('div');
        successDiv.className = 'alert alert-success';
        successDiv.style.marginBottom = '1rem';
        successDiv.textContent = message;

        formContainer.insertBefore(successDiv, loginForm);
    }
});
