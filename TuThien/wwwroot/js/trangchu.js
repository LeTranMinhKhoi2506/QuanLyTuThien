$(() => {
    
    $(document).on('click', '.category-filter', function (e) {
        e.preventDefault();
        
        const categoryId = $(this).data('category-id');
        
        $.ajax({
            url: '/TrangChu/FilterCategory',
            type: 'GET',
            data: { categoryId: categoryId || null },
            success: function(res) {
                $('#campaign-list-container').html(res);
            },
        });
    });
});