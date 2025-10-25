let dataTable;

$(document).ready(function () {
    loadDataTable();
});

function loadDataTable() {
    dataTable = $('#tblData').DataTable({
        ajax: {
            url: '/Admin/User/GetAll',
            type: 'GET',
            dataType: 'json'
        },
        columns: [
            { data: 'name', width: '20%' },
            { data: 'email', width: '20%' },
            { data: 'phoneNumber', width: '15%' },
            { data: 'role', width: '15%' },
            {
                data: { id: 'id', lockoutEnd: 'lockoutEnd' },
                render: function (data) {
                    // ✅ لو lockoutEnd null أو في الماضي → Unlocked
                    const isLocked = data.lockoutEnd && new Date(data.lockoutEnd) > new Date();

                    const lockClass = isLocked ? 'btn-danger' : 'btn-success';
                    const lockIcon = isLocked ? 'bi-lock-fill' : 'bi-unlock-fill';
                    const lockText = isLocked ? 'Locked' : 'Unlocked';

                    return `
                        <div class="text-center d-flex justify-content-center gap-2">
                            <button onclick="toggleLock('${data.id}', this)" 
                                    class="btn ${lockClass} text-white btn-sm" 
                                    style="width:110px;">
                                <i class="bi ${lockIcon}"></i> ${lockText}
                            </button>
                            <a href="/Admin/User/RoleManagment?userId=${data.id}" 
                               class="btn btn-warning text-white btn-sm" 
                               style="width:140px;">
                                <i class="bi bi-pencil-square"></i> Role
                            </a>
                        </div>
                    `;
                },
                width: '30%'
            }
        ],
        responsive: true,
        language: {
            search: "🔍 Search:",
            lengthMenu: "Show _MENU_ per page",
            info: "Showing _START_ to _END_ of _TOTAL_ users",
            paginate: { previous: "Prev", next: "Next" }
        },
        columnDefs: [{ orderable: false, targets: [4] }]
    });
}

// ✅ دالة التبديل بين Lock / Unlock
function toggleLock(userId, btn) {
    const $btn = $(btn);
    const isLocked = $btn.hasClass('btn-danger'); // true = حالياً مقفول

    Swal.fire({
        title: isLocked ? 'Unlock User?' : 'Lock User?',
        text: isLocked
            ? 'This user will be unlocked and can log in again.'
            : 'This user will be locked and unable to log in.',
        icon: 'question',
        showCancelButton: true,
        confirmButtonColor: isLocked ? '#28a745' : '#d33',
        cancelButtonColor: '#6c757d',
        confirmButtonText: isLocked ? 'Yes, Unlock!' : 'Yes, Lock!'
    }).then(result => {
        if (result.isConfirmed) {

            // ✅ غيّر شكل الزرار مؤقتًا (Feedback سريع)
            if (isLocked) {
                // كان مقفول → هيتفتح
                $btn.removeClass('btn-danger')
                    .addClass('btn-success')
                    .html('<i class="bi bi-unlock-fill"></i> Unlocked');
            } else {
                // كان مفتوح → هيتقفل
                $btn.removeClass('btn-success')
                    .addClass('btn-danger')
                    .html('<i class="bi bi-lock-fill"></i> Locked');
            }

            // 🔄 ابعت الطلب للسيرفر
            $.ajax({
                type: 'POST',
                url: '/Admin/User/LockUnlock',
                data: JSON.stringify(userId),
                contentType: 'application/json',
                success: function (data) {
                    if (data.success) {
                        toastr.success(data.message);
                    } else {
                        toastr.error(data.message || 'Something went wrong.');
                        revertButton();
                    }
                },
                error: function () {
                    toastr.error('Server error occurred.');
                    revertButton();
                }
            });

            // 🔁 دالة لإرجاع الزرار لحالته القديمة لو السيرفر فشل
            function revertButton() {
                if (isLocked) {
                    // رجع لـ Locked
                    $btn.removeClass('btn-success')
                        .addClass('btn-danger')
                        .html('<i class="bi bi-lock-fill"></i> Locked');
                } else {
                    // رجع لـ Unlocked
                    $btn.removeClass('btn-danger')
                        .addClass('btn-success')
                        .html('<i class="bi bi-unlock-fill"></i> Unlocked');
                }
            }
        }
    });
}
