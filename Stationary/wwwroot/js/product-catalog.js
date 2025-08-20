document.addEventListener('DOMContentLoaded', function () {
    // Add staggered animation to product cards
    const productCards = document.querySelectorAll('.product-card');
    productCards.forEach((card, index) => {
        card.style.animationDelay = `${index * 0.1}s`;
    });

    // Enhanced search form functionality
    const searchForm = document.getElementById('searchForm');
    const loadingContainer = document.querySelector('.loading-container');

    if (searchForm) {
        searchForm.addEventListener('submit', function (e) {
            // Show loading state
            if (loadingContainer) {
                loadingContainer.style.display = 'block';
            }

            // Hide products temporarily
            const productsGrid = document.querySelector('.products-grid');
            if (productsGrid) {
                productsGrid.style.opacity = '0.5';
            }

            // In a real application, this would be handled by the server
            // This is just for visual feedback
        });
    }

    // Add smooth hover effects to search inputs
    const searchInputs = document.querySelectorAll('.search-input');
    searchInputs.forEach(input => {
        input.addEventListener('focus', function () {
            this.parentElement.style.transform = 'translateY(-2px)';
        });

        input.addEventListener('blur', function () {
            this.parentElement.style.transform = 'translateY(0)';
        });
    });

    // Add to cart button animations
    const addToCartBtns = document.querySelectorAll('.add-to-cart-btn');
    addToCartBtns.forEach(btn => {
        btn.addEventListener('click', function (e) {
            // Add click animation
            this.style.transform = 'scale(0.95)';
            setTimeout(() => {
                this.style.transform = '';
            }, 150);

            // Optional: Add to cart feedback (you can customize this)
            const originalText = this.innerHTML;
            this.innerHTML = '<div class="loading-spinner" style="width: 18px; height: 18px; border: 2px solid white; border-top: 2px solid transparent; border-radius: 50%; animation: spin 1s linear infinite; margin: 0 auto;"></div>';

            setTimeout(() => {
                this.innerHTML = '✓ Added!';
                this.style.background = 'linear-gradient(135deg, #48bb78 0%, #38a169 100%)';

                setTimeout(() => {
                    this.innerHTML = originalText;
                }, 1500);
            }, 800);
        });
    });

    // Image error handling
    const productImages = document.querySelectorAll('.product-image');
    productImages.forEach(img => {
        img.addEventListener('error', function () {
            this.style.display = 'none';
            const placeholder = this.parentElement.querySelector('.product-image-placeholder');
            if (placeholder) {
                placeholder.style.display = 'flex';
            }
        });

        img.addEventListener('load', function () {
            const placeholder = this.parentElement.querySelector('.product-image-placeholder');
            if (placeholder) {
                placeholder.style.display = 'none';
            }
        });
    });

    // Search input enhancements
    const searchInputName = document.querySelector('input[name="search"]');
    const searchInputCategory = document.querySelector('input[name="category"]');

    if (searchInputName) {
        searchInputName.addEventListener('input', function () {
            if (this.value.length > 0) {
                this.style.borderColor = '#48bb78';
            } else {
                this.style.borderColor = '#e2e8f0';
            }
        });
    }

    if (searchInputCategory) {
        searchInputCategory.addEventListener('input', function () {
            if (this.value.length > 0) {
                this.style.borderColor = '#48bb78';
            } else {
                this.style.borderColor = '#e2e8f0';
            }
        });
    }

    // Intersection Observer for scroll animations
    const observerOptions = {
        threshold: 0.1,
        rootMargin: '0px 0px -50px 0px'
    };

    const observer = new IntersectionObserver((entries) => {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                entry.target.style.opacity = '1';
                entry.target.style.transform = 'translateY(0)';
            }
        });
    }, observerOptions);

    // Observe product cards for scroll animations
    productCards.forEach(card => {
        card.style.opacity = '0';
        card.style.transform = 'translateY(20px)';
        card.style.transition = 'opacity 0.6s ease, transform 0.6s ease';
        observer.observe(card);
    });
});


    $(document).ready(function() {

        // Increase quantity
        $('.increase-btn').click(function () {
            var id = $(this).data('id');
            var qtySpan = $('#qty-' + id);
            var qty = parseInt(qtySpan.text());
            qtySpan.text(qty + 1);
        });

    // Decrease quantity
    $('.decrease-btn').click(function() {
        var id = $(this).data('id');
    var qtySpan = $('#qty-' + id);
    var qty = parseInt(qtySpan.text());
        if(qty > 1) qtySpan.text(qty - 1);
    });

    // Add to Cart with selected quantity
    $('.add-to-cart-btn').click(function() {
        var id = $(this).data('id');
    var qty = parseInt($('#qty-' + id).text());

    $.ajax({
        url: '@Url.Action("AddToCart", "User")',
    type: 'POST',
    data: {id: id, quantity: qty },
    success: function(response) {
                if(response.success) {
        alert("Product added to cart!");
                    // Optional: update #cart-count here
                } else {
        alert(response.message);
    if(response.redirect) window.location.href = '/User/Login';
                }
            },
    error: function() {
        alert("Something went wrong.");
            }
        });
    });

});


