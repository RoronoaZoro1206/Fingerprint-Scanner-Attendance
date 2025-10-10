document.addEventListener('DOMContentLoaded', function() {
    const registerForm = document.querySelector('.register-form');
    
    if (!registerForm) {
        console.error('Register form not found!');
        return;
    }
    
    const submitButton = registerForm.querySelector('.sign-in-btn');

    // Debug: Log all input names on page load
    console.log('Form inputs:');
    registerForm.querySelectorAll('input').forEach(input => {
        console.log(`  - name: "${input.name}", id: "${input.id}"`);
    });

    registerForm.addEventListener('submit', async function(e) {
        e.preventDefault();

        // Clear previous errors
        clearErrors();

        // Disable submit button
        submitButton.disabled = true;
        submitButton.textContent = 'Creating Account...';

        const formData = new FormData(registerForm);
        const token = document.querySelector('input[name="__RequestVerificationToken"]').value;

        // Debug: Log form data
        console.log('Submitting form data:');
        for (let [key, value] of formData.entries()) {
            console.log(`  ${key}: ${value}`);
        }

        try {
            const response = await fetch(registerForm.action, {
                method: 'POST',
                headers: {
                    'X-Requested-With': 'XMLHttpRequest',
                    'RequestVerificationToken': token
                },
                body: formData
            });

            const result = await response.json();
            console.log('Registration response:', result);

            if (result.success) {
                // Show success message
                showSuccessMessage(result.message || 'Registration successful! Redirecting...');
                
                // Redirect after a short delay
                setTimeout(() => {
                    window.location.href = result.redirectUrl;
                }, 1500);
            } else {
                // Show errors
                if (result.errors) {
                    console.log('Displaying errors:', result.errors);
                    displayErrors(result.errors);
                } else {
                    console.warn('No errors object in response');
                }
                
                // Re-enable submit button
                submitButton.disabled = false;
                submitButton.textContent = 'Create Account';
            }
        } catch (error) {
            console.error('Registration error:', error);
            showGeneralError('An unexpected error occurred. Please try again.');
            
            // Re-enable submit button
            submitButton.disabled = false;
            submitButton.textContent = 'Create Account';
        }
    });

    function clearErrors() {
        // Remove dynamically created alert messages
        const alertElements = document.querySelectorAll('.alert-danger, .alert-success');
        alertElements.forEach(el => el.remove());

        // Clear existing validation spans
        const errorSpans = registerForm.querySelectorAll('.text-danger');
        errorSpans.forEach(span => {
            span.textContent = '';
        });

        // Remove error styling from inputs
        const inputs = registerForm.querySelectorAll('.form-input');
        inputs.forEach(input => {
            input.classList.remove('input-error');
        });
    }

    function displayErrors(errors) {
        // Handle field-specific errors
        for (const [field, message] of Object.entries(errors)) {
            console.log(`Processing error for field: "${field}", message: "${message}"`);
            
            if (field === '' || field === 'general') {
                // General error
                showGeneralError(message);
            } else {
                // Field-specific error - asp-for generates PascalCase names
                // Try exact match first (PascalCase from server)
                let input = registerForm.querySelector(`[name="${field}"]`);
                
                // Try with ID if name doesn't work
                if (!input) {
                    input = document.getElementById(field);
                }
                
                // Try lowercase version
                if (!input) {
                    const lowerField = field.charAt(0).toLowerCase() + field.slice(1);
                    input = registerForm.querySelector(`[name="${lowerField}"]`);
                    if (!input) {
                        input = document.getElementById(lowerField);
                    }
                }
                
                console.log(`Found input for field "${field}":`, input);
                
                if (input) {
                    input.classList.add('input-error');
                    
                    // Find the error span - look in parent form-group
                    let container = input.closest('.form-group');
                    let errorSpan = null;
                    
                    if (container) {
                        errorSpan = container.querySelector('.text-danger');
                    }
                    
                    console.log(`Found error span for field "${field}":`, errorSpan);
                    
                    if (errorSpan) {
                        errorSpan.textContent = message;
                        errorSpan.style.display = 'block';
                    } else {
                        // Create new error span if not found
                        errorSpan = document.createElement('span');
                        errorSpan.className = 'text-danger';
                        errorSpan.style.display = 'block';
                        errorSpan.style.marginTop = '0.25rem';
                        errorSpan.style.fontSize = '0.875rem';
                        errorSpan.style.color = '#dc3545';
                        errorSpan.textContent = message;
                        
                        // Insert after the input or password container
                        const passwordContainer = input.closest('.password-input-container');
                        if (passwordContainer && container) {
                            // For password fields, append to the form-group
                            container.appendChild(errorSpan);
                        } else if (container) {
                            container.appendChild(errorSpan);
                        } else {
                            input.parentElement.appendChild(errorSpan);
                        }
                        
                        console.log('Created new error span:', errorSpan);
                    }
                } else {
                    console.warn(`Could not find input for field: "${field}"`);
                    // Show as general error if we can't find the field
                    showGeneralError(`${field}: ${message}`);
                }
            }
        }
    }

    function showGeneralError(message) {
        const formContainer = document.querySelector('.login-form-container');
        const errorDiv = document.createElement('div');
        errorDiv.className = 'alert alert-danger';
        errorDiv.style.marginBottom = '1rem';
        errorDiv.textContent = message;
        
        formContainer.insertBefore(errorDiv, registerForm);
    }

    function showSuccessMessage(message) {
        const formContainer = document.querySelector('.login-form-container');
        const successDiv = document.createElement('div');
        successDiv.className = 'alert alert-success';
        successDiv.style.marginBottom = '1rem';
        successDiv.textContent = message;
        
        formContainer.insertBefore(successDiv, registerForm);
    }
});
