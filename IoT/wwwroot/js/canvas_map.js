let c;
let ctx;
let p1;
let p2;
let bound;
let img;

let rect_set = new Set(); //тут все области цифр

class Point {
    x = 0;
    y = 0;
    constructor(x, y) {
        this.x = x;
        this.y = y;
    }
}
class Rectangle {
    x = 0;
    y = 0;
    dx = 0;
    dy = 0;
    constructor(point1, point2) {
        this.x = Math.min(point1.x, point2.x);
        this.y = Math.min(point1.y, point2.y);
        this.dx = Math.abs(point1.x - point2.x);
        this.dy = Math.abs(point1.y - point2.y);
    }
    pointInRectangle(point) {
        if (point.x >= this.x && point.x <= this.x + dx * 1 && point.y >= this.y && point.y <= this.y + dy * 1) {
            return true;
        }
        return false;
    }
}

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
        p1 = new Point(x, y);
    }
    function m_end(e) {
        bound = c[0].getBoundingClientRect();
        if (e.type == 'touchend') {
            x = (e.changedTouches[0].clientX - bound.left).toFixed(0) * 1;
            y = (e.changedTouches[0].clientY - bound.top).toFixed(0) * 1;
        }
        else {
            x = (e.clientX - bound.left).toFixed(0) * 1;
            y = (e.clientY - bound.top).toFixed(0) * 1;
        }
        p2 = new Point(x, y);
        let rect = new Rectangle(p1, p2);

        if (rect.dx < 8 || rect.dy < 8) { //Не используем, а если внутри сущ прямоугольников - то удалим их
            let rect_set_tmp = new Set();
            for (let value of rect_set) {
                if (!value.pointInRectangle(p1)) { //Оставим только те что не пересеклись с точкой
                    rect_set_tmp.add(value);
                }
            }
            rect_set = rect_set_tmp;
        }
        else {
            rect_set.add(rect);
        }

        ctx.drawImage(img, 0, 0);
        for (let value of rect_set) {
            ctx.strokeRect(value.x, value.y, value.dx, value.dy);
        }


        //$("#server").text("Send: " + rect);
        c2 = $("#map_sel");
        ctx2 = c2[0].getContext("2d");
        ctx2.clearRect(0, 0, ctx2.canvas.width, ctx2.canvas.height);

        let px = 1;
        for (let value of rect_set) {
            ctx2.drawImage(img, value.x, value.y, value.dx, value.dy, px, 1, value.dx, value.dy);
            ctx2.strokeRect(px, 1, value.dx, value.dy);
            px += value.dx + 3;
        }
    }
    c[0].addEventListener("touchstart", m_start, false);
    c[0].addEventListener("touchend", m_end, false);
    c[0].addEventListener("mousedown", m_start, false);
    c[0].addEventListener("mouseup", m_end, false);

};
function load_image(image_path, region_txt) {
    rect_set = new Set();
    c = $("#map");
    ctx = c[0].getContext("2d");
    x = 0;
    y = 0;
    bound = c[0].getBoundingClientRect();
    img = new Image();
    img.onload = function () {
        ctx.drawImage(img, 0, 0);
        if (region_txt.length > 4) {
            let regions = region_txt.split(";");
            for (let region of regions) {
                if (region.length > 4) {
                    [x, y, dx, dy] = region.split(",");
                    let rect = new Rectangle(new Point(x, y), new Point(x * 1 + dx * 1, y * 1 + dy * 1));
                    rect_set.add(rect);
                    ctx.strokeRect(rect.x, rect.y, rect.dx, rect.dy);
                }

            }

            //$("#server").text("Send: " + rect);
            c2 = $("#map_sel");
            ctx2 = c2[0].getContext("2d");
            ctx2.clearRect(0, 0, ctx2.canvas.width, ctx2.canvas.height);

            let px = 1;
            for (let value of rect_set) {
                ctx2.drawImage(img, value.x, value.y, value.dx, value.dy, px, 1, value.dx, value.dy);
                ctx2.strokeRect(px, 1, value.dx, value.dy);
                px += value.dx + 3;
            }
        }
    };
    img.src = image_path;
};
function get_region() {
    let ret_val = "";
    for (let value of rect_set) {
        ret_val += (value.x + "," + value.y + "," + value.dx + "," + value.dy + ";");
    }
    return ret_val;
}