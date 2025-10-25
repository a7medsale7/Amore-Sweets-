$(document).ready(function () {
    var table = $('#tblData').DataTable({
        "pageLength": 5,
        "lengthMenu": [5, 10, 25, 50],
        "autoWidth": false,
        "responsive": true,
        "language": {
            "search": "🔍 Search:",
            "lengthMenu": "Show _MENU_ per page",
            "info": "Showing _START_ to _END_ of _TOTAL_ products",
            "paginate": { "previous": "Prev", "next": "Next" }
        },
        "columnDefs": [
            { "orderable": false, "targets": [7, 8] } // 🧩 الأعمدة الأخيرة (Actions, Special)
        ]

    });

    // SweetAlert Delete Confirmation
    $('#tblData').on('click', '.btn-delete', function (e) {
        e.preventDefault();

        var id = $(this).data('id'); // تأكد إن عندك data-id="@item.Id" في الزرار
        var row = $(this).closest('tr');

        Swal.fire({
            title: 'Are you sure?',
            text: "This product will be permanently deleted!",
            icon: 'warning',
            showCancelButton: true,
            confirmButtonColor: '#d33',
            cancelButtonColor: '#6B4226',
            confirmButtonText: 'Yes, delete it!',
            cancelButtonText: 'Cancel'
        }).then((result) => {
            if (result.isConfirmed) {
                $.ajax({
                    url: `${window.location.origin}/Admin/Product/Delete/${id}`,
                    type: 'POST',
                    success: function (response) {
                        if (response.success) {
                            Swal.fire({
                                title: 'Deleted!',
                                text: 'Product has been deleted successfully.',
                                icon: 'success',
                                timer: 1500,
                                showConfirmButton: false
                            });
                            table.row(row).remove().draw(false);
                        } else {
                            Swal.fire('Error!', response.message || 'Something went wrong.', 'error');
                        }
                    },
                    error: function () {
                        Swal.fire('Error!', 'Server error occurred.', 'error');
                    }
                });
            }
        });
    });
});
