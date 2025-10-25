var dataTable;

$(document).ready(function () {
    // Load all orders initially
    loadDataTable("all");

    // Handle filter button click
    $(".filter-btn").on("click", function () {
        $(".filter-btn").removeClass("active btn-light").addClass("btn-outline-secondary");
        $(this).removeClass("btn-outline-secondary").addClass("btn-light active");

        var status = $(this).data("status");
        $('#tblData').DataTable().destroy(); // destroy old instance
        loadDataTable(status); // reload based on selected status
    });
});

function loadDataTable(status) {
    dataTable = $('#tblData').DataTable({
        "ajax": {
            "url": `/Admin/Order/GetAll?status=${status}`,
            "dataSrc": function (json) {
                // Handles both { data: [...] } and [...] structures
                if (json.data) {
                    return json.data;
                }
                return json;
            }
        },
        "columns": [
            { "data": "id", "width": "10%" },
            {
                "data": "applicationUser.name",
                "render": function (data, type, row) {
                    return data ?? row.applicationUser?.email ?? "Unknown";
                },
                "width": "15%"
            },
            {
                "data": "orderDate",
                "render": function (data) {
                    if (!data) return "";
                    let date = new Date(data);
                    return date.toLocaleDateString();
                },
                "width": "15%"
            },
            {
                "data": "orderTotal",
                "render": function (data) {
                    return `$${parseFloat(data || 0).toFixed(2)}`;
                },
                "width": "10%"
            },
            {
                "data": "orderStatus",
                "render": function (data) {
                    let color = "secondary";
                    switch (data) {
                        case "Pending": color = "warning"; break;
                        case "Approved": color = "primary"; break;
                        case "In Process": color = "info"; break;
                        case "Shipped": color = "success"; break;
                        case "Cancelled": color = "danger"; break;
                        case "Refunded": color = "secondary"; break;
                    }
                    return `<span class="badge bg-${color}">${data}</span>`;
                },
                "width": "15%"
            },
            {
                "data": "paymentStatus",
                "render": function (data) {
                    return `<span class="badge bg-light text-dark">${data}</span>`;
                },
                "width": "15%"
            },
            {
                "data": "id",
                "render": function (data) {
                    return `
                        <div class="btn-group" role="group">
                            <a href="/Admin/Order/Details?id=${data}" 
                               class="btn btn-sm" style="background-color:#F8BBD0; color:#6B4226;">
                                <i class="bi bi-eye"></i> Details
                            </a>
                        </div>
                    `;
                },
                "width": "20%"
            }
        ],
        "language": {
            "emptyTable": "No orders found for this status."
        },
        "width": "100%",
        "responsive": true,
        "order": [[0, "desc"]]
    });
}
