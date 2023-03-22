var map;

var osmLayer = L.tileLayer("https://tile.openstreetmap.org/{z}/{x}/{y}.png", {
  maxZoom: 19,
  attribution: "© OpenStreetMap",
});

var baseMap = {
  OpenStreetMap: osmLayer,
};

var layerControl;

async function createMap(lat, lng, zoomValue) {
  if (map !== undefined) {
    map.remove();
  }
  map = L.map("map", {
    center: [lat, lng],
    zoom: zoomValue,
    layers: [osmLayer],
  });
}

async function addPoint(lat, lng) {
  L.marker([lat, lng]).addTo(map);
}

async function createPath(coordinates, color) {
  var polyline = L.polyline(coordinates, { color: color }).addTo(map);
  L.featureGroup(getArrows(coordinates, 'black', 1, map)).addTo(map);
  map.fitBounds(polyline.getBounds());
}