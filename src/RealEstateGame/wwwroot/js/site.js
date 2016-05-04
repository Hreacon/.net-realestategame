// Write your Javascript code.
$(document)
    .ready(function () {
        function ajax(href, target) {
            // use ajax call to homecontroller method
            // when data returns put it in #rightside
            console.log("ajax");
            window.history.pushState('RealEstateGame', 'RealEstateGame', href);
            $.ajax({
                type: 'GET',
                url: href + '?ajax=true',
                success: function (result) {
                    $(target).html(result);
                }
            });
        }
        $("a.ajax")
            .click(function (event) {
                ajax($(this).attr('href'), $(this).attr('data-target'));
                event.preventDefault();
                event.stopImmediatePropagation();
                return false;
            });
    });

