// ref: https://stackoverflow.com/a/58686215
function getArrows(arrLatlngs, color, arrowCount, animationLength = 8) {

    if (typeof arrLatlngs === undefined || arrLatlngs == null ||
        (!arrLatlngs.length) || arrLatlngs.length < 2)
        return [];

    if (typeof arrowCount === 'undefined' || arrowCount == null)
        arrowCount = 1;

    if (typeof color === 'undefined' || color == null)
        color = '';
    else
        color = 'color:' + color;

    var result = [];
    for (var i = 1; i < arrLatlngs.length - 1; i++) {
        if (i % 20 !== 0) { continue; }
        let animationStyle = "animation:leafletArrowFade 1s " + animationLength + "s forwards;opacity:0;";
        var icon = L.divIcon({ className: 'arrow-icon', bgPos: [5, 5], html: '<div style="' + color + ';' + animationStyle + ';transform: rotate(' + getAngle(arrLatlngs[i - 1], arrLatlngs[i], -1).toString() + 'deg)">▶</div>' });
        for (var c = 1; c <= arrowCount; c++) {
            let p2 = arrLatlngs[i - 1];
            result.push(L.marker(p2, { icon: icon }));
        }
    }
    return result;
}
function getAngle(latLng1, latlng2, coef) {
    var dy = latlng2[0] - latLng1[0];
    var dx = Math.cos(Math.PI / 180 * latLng1[0]) * (latlng2[1] - latLng1[1]);
    var ang = ((Math.atan2(dy, dx) / Math.PI) * 180 * coef);
    return (ang).toFixed(2);
}