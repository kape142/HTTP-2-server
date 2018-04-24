$(document).ready(function () {
    $("#idbtn").on('click', function () {
        getTest();
    });
});

function getTest() {
    $.ajax({
        url: '/testurl',
        type: 'GET',
        dataType: 'text/plain',
        complete: function (xhr) {
            switch (xhr.status) {
                default:
                    console.log("Status get " + xhr.status);
                    break;
            }
        }
    });
}