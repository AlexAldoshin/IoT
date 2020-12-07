var c;
var ctx;
var x, y, dx, dy;
var bound;
var img;
var rect;

function start_map_events() {
    load_image('css/img/nb-iot.jpg', '');

    function m_start(e) {
        bound = c[0].getBoundingClientRect();
        if (e.type == 'touchstart') {
            x = (e.touches[0].clientX - bound.left).toFixed(0) * 1;
            y = (e.touches[0].clientY - bound.top).toFixed(0) * 1;
        }
        else {
            x = (e.clientX - bound.left).toFixed(0) * 1;
            y = (e.clientY - bound.top).toFixed(0) * 1;
        }


    }
    function m_end(e) {
        bound = c[0].getBoundingClientRect();
        if (e.type == 'touchend') {
            dx = (e.changedTouches[0].clientX - bound.left).toFixed(0) - x;
            dy = (e.changedTouches[0].clientY - bound.top).toFixed(0) - y;
        }
        else {
            dx = (e.clientX - bound.left).toFixed(0) - x;
            dy = (e.clientY - bound.top).toFixed(0) - y;
        }


        
        if (dx < 0) {
            x = x + dx;
            dx = -dx;
        }
        if (dy < 0) {
            y = y + dy;
            dy = -dy;
        }
        rect = [x, y, dx, dy];
        ctx.drawImage(img, 0, 0);
        ctx.strokeRect(...rect);
        $("#server").text("Send: " + rect);
        c2 = $("#map_sel");
        ctx2 = c2[0].getContext("2d");
        ctx2.clearRect(0, 0, ctx2.canvas.width, ctx2.canvas.height);
        ctx2.drawImage(img, rect[0], rect[1], dx, dy, 0, 0, dx, dy);
        ctx2.strokeRect(0, 0, dx, dy);
    }

    c[0].addEventListener("touchstart", m_start, false);
    c[0].addEventListener("touchend", m_end, false);
    c[0].addEventListener("mousedown", m_start, false);
    c[0].addEventListener("mouseup", m_end, false);

};
function load_image(image_path, region_txt) {
    c = $("#map");
    ctx = c[0].getContext("2d");
    x = 0;
    y = 0;
    bound = c[0].getBoundingClientRect();
    img = new Image();
    img.onload = function () {
        ctx.drawImage(img, 0, 0);
        if (region_txt.length > 4) {
            var reg_arr = region_txt.split(",");
            [x, y, dx, dy] = reg_arr;
            rect = [x, y, dx, dy];
            ctx.strokeRect(...rect);
            $("#server").text("Send: " + rect);
            c2 = $("#map_sel");
            ctx2 = c2[0].getContext("2d");
            ctx2.clearRect(0, 0, ctx2.canvas.width, ctx2.canvas.height);
            ctx2.drawImage(img, rect[0], rect[1], dx, dy, 0, 0, dx, dy);
            ctx2.strokeRect(0, 0, dx, dy);
        }
    };
    img.src = image_path;
};
function get_region() {
    return rect + "";
}