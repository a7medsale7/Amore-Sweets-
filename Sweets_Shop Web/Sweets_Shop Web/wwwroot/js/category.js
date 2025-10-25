$(document).ready(function () {

    // ✅ أولاً: خزّن الجدول في متغير عام
    var table = $('#tblData').DataTable({
        "pageLength": 5,
        "lengthMenu": [5, 10, 25, 50],
        "autoWidth": false,
        "responsive": true,
        "language": {
            "search": "🔍 Search:",
            "lengthMenu": "Show _MENU_ per page",
            "info": "Showing _START_ to _END_ of _TOTAL_ categories",
            "paginate": { "previous": "Prev", "next": "Next" }
        },
        "columnDefs": [
            { "orderable": false, "targets": [7] } // عمود الأكشن رقم 7
        ]
    });

    // ✅ بعد ما الـ table يبقى جاهز، اربط حدث الحذف
    $('#tblData').on('click', '.btn-delete', function (e) {
        e.preventDefault();

        var id = $(this).data('id');
        var row = $(this).parents('tr');

        Swal.fire({
            title: 'Are you sure?',
            text: "This category will be permanently deleted!",
            icon: 'warning',
            showCancelButton: true,
            confirmButtonColor: '#d33',
            cancelButtonColor: '#6B4226',
            confirmButtonText: 'Yes, delete it!',
            cancelButtonText: 'Cancel'
        }).then((result) => {
            if (result.isConfirmed) {
                $.ajax({
                    url: `${window.location.origin}/Admin/Category/Delete/${id}`,
                    type: 'POST',
                    success: function (response) {
                        if (response.success) {
                            Swal.fire({
                                title: 'Deleted!',
                                text: 'Category has been deleted successfully.',
                                icon: 'success',
                                timer: 1500,
                                showConfirmButton: false
                            });

                            // ✅ انتظر لحظة بسيطة قبل إزالة الصف
                            setTimeout(function () {
                                table.row(row).remove().draw(false);
                            }, 300);
                        } else {
                            Swal.fire('Error!', response.message || 'Something went wrong.', 'error');
                        }
                    },
                    error: function (xhr) {
                        Swal.fire('Error!', xhr.responseText || 'Server error occurred.', 'error');
                    }
                });
            }
        });
    });
});
