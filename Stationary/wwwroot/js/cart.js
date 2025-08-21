

// Increase quantity
$(document).on('click', '.increase-btn', function () {
    const id = $(this).data('id');
    const qtySpan = $(`#qty-${id}`);
    let qty = parseInt(qtySpan.text());
    updateCartQuantity(id, qty + 1, qtySpan);
});

// Decrease quantity
$(document).on('click', '.decrease-btn', function () {
    const id = $(this).data('id');
    const qtySpan = $(`#qty-${id}`);
    let qty = parseInt(qtySpan.text());
    if (qty > 1) {
        updateCartQuantity(id, qty - 1, qtySpan);
    }
});

// Remove item
$(document).on('click', '.remove-btn', function () {
    const id = $(this).data('id');
    $.ajax({
        url: '/User/RemoveFromCart',
        type: 'POST',
        data: { id: id },
        success: function (response) {
            if (response.success) {
                location.reload();
            } else {
                alert(response.message);
                if (response.redirect) {
                    window.location.href = '/User/Login';
                }
            }
        },
        error: function () {
            alert('Something went wrong.');
        }
    });
});

// Update cart quantity
function updateCartQuantity(id, quantity, qtySpan) {
    $.ajax({
        url: '/User/UpdateCartQuantity',
        type: 'POST',
        data: { id: id, quantity: quantity },
        success: function (response) {
            if (response.success) {
                qtySpan.text(quantity);
                updateCartCount();
            } else {
                alert(response.message);
                if (response.redirect) {
                    window.location.href = '/User/Login';
                }
            }
        },
        error: function () {
            alert('Something went wrong.');
        }
    });
}

// ✅ Update cart count
function updateCartCount() {
    $.ajax({
        url: '/User/GetCartCount',
        type: 'GET',
        success: function (response) {
            $('#cart-count').text(response.count || 0);
        },
        error: function () {
            console.error('Failed to update cart count.');
        }
    });
};
