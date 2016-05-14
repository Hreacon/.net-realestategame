﻿function ajax(href, target) {
    // scalable ajax function. Use html attributes to get the target. Turn everything into an ajax call with a class and a data-target.
    // Also "breakproof". Site works with refreshing and even if there's no javascript.
    console.log("ajax");
    window.history.pushState('RealEstateGame', 'RealEstateGame', href);
    var tag = '';
    if (href.includes("#")) {
        tag = href.substring(href.indexOf("#"));
        href = href.substring(0, href.indexOf("#"));
    }
    if (href.includes("?")) {
        href = href + "&ajax=true";
    } else {
        href = href + "?ajax=true";
    }

    $.ajax({
        type: 'GET',
        url: href,
        success: function (result) {
            ajaxReturn(target, result, tag);
        }
    });
}
function ajaxPost(form, target) {
    // ajax post requests
    // get the href
    var href = $(form).attr('action');
    console.log("ajaxpost");
    // do the ajax call
    $.ajax({
        type: 'POST',
        url: href,
        data: $(form).serialize(),
        success: function(result) {
            ajaxReturn(target, result, 'no');
        }
    });
}
function ajaxReturn(target, result, tag) {
    console.log("post success");
    if (result.trim().substr(0, 1) == "<") {
        $(target).html(result);
        updatePlayer();
        ajaxInit();
        if (tag.length > 1 && tag != "no") {
            console.log('scroll');
            $(window).scrollTop($(tag).offset().top-100);
        } else if(tag != "no") {
            $(window).scrollTop(0);
        }
    } else message(result);
}
function updatePlayer() {
    console.log("Update Player");
    $.ajax({
        type: 'GET',
        url: '/Home/GetPlayer?ajax=true',
        success: function (result) {
            console.log("Update Player Success");
            $("#viewplayer").html(result);
        }
    });
}
function message(text) {
    console.log("message '" + text+"'");
    var message = $(".message");
    message.text(text);
    message.fadeIn();
    window.setTimeout(function() {
            message.fadeOut();
        },
        1500);
}
function ajaxInit() {
    // initialize the ajax calls in place of the standard event handling
    console.log('ajax init');
    $("a.ajax")
    .click(function (event) {
        // remove the class ajax to prevent additional event handlers
        $(this).removeClass("ajax");
        ajax($(this).attr('href'), $(this).attr('data-target'));
        event.preventDefault();
        event.stopImmediatePropagation();
        return false;
    });
    $("button.ajax")
        .parent()
        .submit(function (event) {
            event.preventDefault();
            var button = $(this).find("button.ajax");
            button.removeClass("ajax");
            var flag = $("<input>").attr('name', 'ajax').attr('type', 'hidden').val('true');
            $(this).append(flag);
            console.log(this);
            ajaxPost(this, button.attr("data-target"));
            return false;
        });
    var slidertotal = 0;
    $("input[type=range]")
        .each(function () {
            $(this).on('mousemove', function () {
                    var slider = Number($(this).val());
                    console.log(slider, money, slidertotal);
                    var val = '$' + parseFloat(slider, 10).toFixed(2).replace(/(\d)(?=(\d{3})+\.)/g, "$1,");
                    var tget = "output" + $(this).attr('id');
                    $("#"+tget).html(val);
                }).on('change',function() {
                    var slider = Number($(this).val());
                    var money = Number($("#money").text());
                    slidertotal = setSliderTotal(this);
                    if (slider > money - slidertotal) {
                        slider = money - slidertotal;
                        $(this).val(money-slidertotal);
                    }
                    var val = '$' + parseFloat(slider, 10).toFixed(2).replace(/(\d)(?=(\d{3})+\.)/g, "$1,");
                    var tget = "output" + $(this).attr('id');
                    $("#" + tget).html(val);
            });
        });
}
function setSliderTotal(except) {
    var total = 0;
    $("input[type=range]")
        .each(function () {
            if(!(this == except))
                total += Number($(this).val());
        });
    return total;
}
$(document)
    .ready(function () {
        ajaxInit();
        var $window = $(window);
        var player = $("#viewplayer");
        $window.scroll(function () {
            if($window.width() > 992)
                player.css('margin-top', window.pageYOffset);
        });
    });

